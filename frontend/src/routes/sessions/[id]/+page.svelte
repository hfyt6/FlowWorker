<script lang="ts">
import { onMount } from 'svelte';
import { page } from '$app/state';
import { goto } from '$app/navigation';
import { messageApi } from '$lib/services/api';
import { messages, loading, error, fetchMessages, sendMessage, currentMessage } from '$lib/stores/messageStore';
import StreamMessage from '$lib/components/StreamMessage.svelte';
import { MessageRole } from '$lib/types/message';
import type { MessageListItemDto } from '$lib/types/message';

// State
let sessionId = '';
let inputMessage = '';
let isSending = false;
let messageListElement: HTMLDivElement;
let isAiResponding = false;
let lastMessageCount = 0;

// 将后端返回的 role（可能是数字或字符串）转换为字符串
function normalizeRole(role: MessageRole | number | string): MessageRole {
    if (typeof role === 'string') {
        return role as MessageRole;
    }
    // 如果是数字，转换为对应的字符串
    const roleMap: Record<number, MessageRole> = {
        0: MessageRole.System,
        1: MessageRole.User,
        2: MessageRole.Assistant,
        3: MessageRole.Tool
    };
    return roleMap[role as number] || MessageRole.User;
}

// 处理消息数据，确保 role 是字符串
function processMessages(msgs: MessageListItemDto[]): MessageListItemDto[] {
    return msgs.map(msg => ({
        ...msg,
        role: normalizeRole(msg.role)
    }));
}

// Component lifecycle
onMount(() => {
    const id = page.params.id;
    if (id) {
        sessionId = id;
        fetchMessages(id);
    }
});

// Auto-scroll to bottom only when AI is responding
$: if (messageListElement && $messages) {
    const currentMessageCount = $messages.length;
    // 只在消息数量增加且是AI回复时自动滚动
    if (currentMessageCount > lastMessageCount && isAiResponding) {
        setTimeout(() => {
            messageListElement.scrollTop = messageListElement.scrollHeight;
        }, 0);
    }
    lastMessageCount = currentMessageCount;
}

// Handlers
async function handleSendMessage() {
    if (!inputMessage.trim() || isSending) {
        return;
    }

    isSending = true;

    // 先保存用户输入并清空输入框
    const userContent = inputMessage.trim();
    inputMessage = '';

    // 立即添加用户消息到列表（乐观更新）
    const userMessageId = 'user-' + Date.now();
    messages.update(currentMessages => [
        ...currentMessages,
        {
            id: userMessageId,
            role: MessageRole.User,
            content: userContent,
            tokens: null,
            model: null,
            createdAt: new Date().toISOString()
        }
    ]);

    // 标记AI正在回复，此时应该自动滚动
    isAiResponding = true;

    try {
        const response = await messageApi.sendMessage(sessionId, userContent);

        // 添加 AI 回复（处理后端 PascalCase 响应）
        messages.update(currentMessages => [
            ...currentMessages,
            {
                id: 'assistant-' + Date.now(),
                role: MessageRole.Assistant,
                content: response.content || (response as any).Content,
                tokens: response.tokens ?? (response as any).Tokens,
                model: response.model || (response as any).Model,
                createdAt: new Date().toISOString()
            }
        ]);
    } catch (err) {
        console.error('Failed to send message:', err);
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
        // AI回复完成，重置标记
        isAiResponding = false;
        isSending = false;
    }
}

async function handleRegenerate() {
    // TODO: 实现重新生成功能
    console.log('Regenerate');
}

async function handleDeleteMessage(id: string) {
    // TODO: 实现删除消息功能
    console.log('Delete message:', id);
}

// Derived state
$: sessionMessages = processMessages($messages);
$: isLoading = $loading;
$: errorMessage = $error;
$: isInputEmpty = !inputMessage.trim();

// Event handlers
function handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        handleSendMessage();
    }
}
</script>

<div class="chat-app">
    <div class="chat-header">
        <div class="header-left">
            <button class="btn-back" on:click={() => goto('/sessions')} title="返回列表">
                <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m15 18-6-6 6-6"/></svg>
            </button>
            <div class="header-info">
                <h1>会话详情</h1>
                <span class="header-subtitle">{sessionMessages.length} 条消息</span>
            </div>
        </div>
        <div class="header-actions">
            <button class="btn-icon" on:click={handleRegenerate} title="重新生成">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16"/><path d="M8 16H3v5"/></svg>
            </button>
        </div>
    </div>

    {#if errorMessage}
        <div class="error-banner">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" x2="12" y1="8" y2="12"/><line x1="12" x2="12.01" y1="16" y2="16"/></svg>
            {errorMessage}
        </div>
    {/if}

    <div class="message-list" bind:this={messageListElement}>
        {#if isLoading && sessionMessages.length === 0}
            <div class="loading-container">
                <div class="loading-spinner">
                    <div class="spinner"></div>
                </div>
                <p>加载中...</p>
            </div>
        {:else if sessionMessages.length === 0}
            <div class="empty-state">
                <div class="empty-icon">
                    <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
                </div>
                <p class="empty-title">开始新的对话</p>
                <p class="empty-desc">输入消息开始与 AI 助手交流</p>
            </div>
        {:else}
            {#each sessionMessages as message (message.id)}
                <div class="message-bubble {message.role}">
                    <div class="message-avatar">
                        {#if message.role === 'user'}
                            <div class="avatar user-avatar">
                                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
                            </div>
                        {:else if message.role === 'assistant'}
                            <div class="avatar ai-avatar">
                                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 8V4H8"/><rect width="16" height="12" x="4" y="8" rx="2"/><path d="M2 14h2"/><path d="M20 14h2"/><path d="M15 13v2"/><path d="M9 13v2"/></svg>
                            </div>
                        {:else}
                            <div class="avatar system-avatar">⚙️</div>
                        {/if}
                    </div>
                    <div class="message-content-wrapper">
                        <div class="message-bubble-content">
                            <pre><code>{message.content}</code></pre>
                        </div>
                        <div class="message-meta">
                            <span class="time">{new Date(message.createdAt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>
                            {#if message.tokens}
                                <span class="tokens">{message.tokens} tokens</span>
                            {/if}
                            {#if message.model}
                                <span class="model">{message.model}</span>
                            {/if}
                        </div>
                    </div>
                    <button
                        class="btn-delete"
                        on:click={() => handleDeleteMessage(message.id)}
                        title="删除消息"
                    >
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/></svg>
                    </button>
                </div>
            {/each}
        {/if}
    </div>

    <div class="input-area">
        <div class="input-container">
            <textarea
                bind:value={inputMessage}
                on:keydown={handleKeyDown}
                placeholder="输入消息..."
                disabled={isSending}
                class="input-message"
                rows="1"
            />
            <button
                class="btn-send"
                on:click={handleSendMessage}
                disabled={isInputEmpty || isSending}
            >
                {#if isSending}
                    <div class="send-spinner"></div>
                {:else}
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m22 2-7 20-4-9-9-4Z"/><path d="M22 2 11 13"/></svg>
                {/if}
            </button>
        </div>
        <p class="input-hint">按 Enter 发送，Shift + Enter 换行</p>
    </div>
</div>

<style>
    :global(body) {
        margin: 0;
        padding: 0;
    }

    .chat-app {
        display: flex;
        flex-direction: column;
        height: 100vh;
        background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
    }

    .chat-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem 1.5rem;
        background: rgba(255, 255, 255, 0.9);
        backdrop-filter: blur(10px);
        border-bottom: 1px solid rgba(59, 130, 246, 0.1);
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    }

    .header-left {
        display: flex;
        align-items: center;
        gap: 1rem;
    }

    .btn-back {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        border: none;
        background: transparent;
        color: #3b82f6;
        border-radius: 50%;
        cursor: pointer;
        transition: all 0.2s;
    }

    .btn-back:hover {
        background: rgba(59, 130, 246, 0.1);
    }

    .header-info h1 {
        margin: 0;
        font-size: 1.1rem;
        font-weight: 600;
        color: #1e293b;
    }

    .header-subtitle {
        font-size: 0.75rem;
        color: #64748b;
    }

    .header-actions {
        display: flex;
        gap: 0.5rem;
    }

    .btn-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        border: none;
        background: transparent;
        color: #64748b;
        border-radius: 50%;
        cursor: pointer;
        transition: all 0.2s;
    }

    .btn-icon:hover {
        background: rgba(59, 130, 246, 0.1);
        color: #3b82f6;
    }

    .error-banner {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin: 1rem 1.5rem 0;
        padding: 0.75rem 1rem;
        background: #fef2f2;
        border: 1px solid #fecaca;
        border-radius: 0.75rem;
        color: #dc2626;
        font-size: 0.875rem;
    }

    .message-list {
        flex: 1;
        overflow-y: auto;
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .loading-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100%;
        color: #64748b;
    }

    .loading-spinner {
        width: 48px;
        height: 48px;
        border-radius: 50%;
        background: linear-gradient(135deg, #3b82f6, #06b6d4);
        display: flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 1rem;
        animation: pulse 2s infinite;
    }

    .spinner {
        width: 24px;
        height: 24px;
        border: 3px solid rgba(255, 255, 255, 0.3);
        border-top-color: white;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }

    @keyframes spin {
        to { transform: rotate(360deg); }
    }

    @keyframes pulse {
        0%, 100% { transform: scale(1); opacity: 1; }
        50% { transform: scale(1.05); opacity: 0.8; }
    }

    .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100%;
        color: #64748b;
        text-align: center;
    }

    .empty-icon {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        background: linear-gradient(135deg, #3b82f6, #06b6d4);
        display: flex;
        align-items: center;
        justify-content: center;
        color: white;
        margin-bottom: 1.5rem;
        box-shadow: 0 10px 25px rgba(59, 130, 246, 0.3);
    }

    .empty-title {
        font-size: 1.25rem;
        font-weight: 600;
        color: #1e293b;
        margin: 0 0 0.5rem;
    }

    .empty-desc {
        font-size: 0.875rem;
        color: #64748b;
        margin: 0;
    }

    .message-bubble {
        display: flex;
        gap: 0.75rem;
        max-width: 85%;
        animation: fadeIn 0.3s ease-out;
    }

    @keyframes fadeIn {
        from { opacity: 0; transform: translateY(10px); }
        to { opacity: 1; transform: translateY(0); }
    }

    .message-bubble.user {
        align-self: flex-end;
        flex-direction: row-reverse;
    }

    .message-bubble.assistant {
        align-self: flex-start;
    }

    .message-avatar {
        flex-shrink: 0;
    }

    .avatar {
        width: 36px;
        height: 36px;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .user-avatar {
        background: linear-gradient(135deg, #3b82f6, #2563eb);
        color: white;
    }

    .ai-avatar {
        background: linear-gradient(135deg, #06b6d4, #0891b2);
        color: white;
    }

    .system-avatar {
        background: #f59e0b;
    }

    .message-content-wrapper {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        max-width: calc(100% - 48px);
    }

    .message-bubble-content {
        padding: 0.875rem 1.125rem;
        border-radius: 1.125rem;
        word-wrap: break-word;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
    }

    .message-bubble.user .message-bubble-content {
        background: linear-gradient(135deg, #3b82f6, #2563eb);
        color: white;
        border-bottom-right-radius: 0.25rem;
    }

    .message-bubble.assistant .message-bubble-content {
        background: white;
        color: #1e293b;
        border: 1px solid rgba(59, 130, 246, 0.15);
        border-bottom-left-radius: 0.25rem;
    }

    .message-bubble.system .message-bubble-content {
        background: #fef3c7;
        color: #92400e;
        border: 1px solid #fcd34d;
    }

    .message-bubble-content pre {
        margin: 0;
        white-space: pre-wrap;
        word-wrap: break-word;
        font-family: inherit;
        font-size: 0.9375rem;
        line-height: 1.6;
        background: transparent;
        padding: 0;
    }

    .message-bubble-content code {
        font-family: 'SF Mono', Monaco, 'Cascadia Code', monospace;
        background: rgba(0, 0, 0, 0.1);
        padding: 0.125rem 0.375rem;
        border-radius: 0.25rem;
        font-size: 0.875em;
    }

    .message-bubble.user .message-bubble-content code {
        background: rgba(255, 255, 255, 0.2);
    }

    .message-meta {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.75rem;
        padding: 0 0.5rem;
    }

    .message-bubble.user .message-meta {
        justify-content: flex-end;
    }

    .time {
        color: #94a3b8;
    }

    .tokens {
        color: #3b82f6;
        background: rgba(59, 130, 246, 0.1);
        padding: 0.125rem 0.5rem;
        border-radius: 1rem;
    }

    .model {
        color: #06b6d4;
        background: rgba(6, 182, 212, 0.1);
        padding: 0.125rem 0.5rem;
        border-radius: 1rem;
    }

    .btn-delete {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 28px;
        height: 28px;
        border: none;
        background: transparent;
        color: #cbd5e1;
        border-radius: 50%;
        cursor: pointer;
        opacity: 0;
        transition: all 0.2s;
        flex-shrink: 0;
        align-self: center;
    }

    .message-bubble:hover .btn-delete {
        opacity: 1;
    }

    .btn-delete:hover {
        background: #fee2e2;
        color: #ef4444;
    }

    .input-area {
        padding: 1rem 1.5rem 1.5rem;
        background: rgba(255, 255, 255, 0.9);
        backdrop-filter: blur(10px);
        border-top: 1px solid rgba(59, 130, 246, 0.1);
    }

    .input-container {
        display: flex;
        gap: 0.75rem;
        align-items: flex-end;
        background: white;
        border: 2px solid rgba(59, 130, 246, 0.15);
        border-radius: 1.5rem;
        padding: 0.5rem 0.5rem 0.5rem 1.25rem;
        transition: all 0.2s;
        box-shadow: 0 4px 12px rgba(59, 130, 246, 0.08);
    }

    .input-container:focus-within {
        border-color: #3b82f6;
        box-shadow: 0 4px 20px rgba(59, 130, 246, 0.15);
    }

    .input-message {
        flex: 1;
        border: none;
        background: transparent;
        font-size: 0.9375rem;
        line-height: 1.5;
        resize: none;
        outline: none;
        color: #1e293b;
        max-height: 120px;
        min-height: 24px;
        padding: 0.5rem 0;
    }

    .input-message::placeholder {
        color: #94a3b8;
    }

    .input-message:disabled {
        opacity: 0.6;
    }

    .btn-send {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        border: none;
        background: linear-gradient(135deg, #3b82f6, #2563eb);
        color: white;
        border-radius: 50%;
        cursor: pointer;
        transition: all 0.2s;
        flex-shrink: 0;
        box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
    }

    .btn-send:hover:not(:disabled) {
        transform: scale(1.05);
        box-shadow: 0 6px 16px rgba(59, 130, 246, 0.4);
    }

    .btn-send:disabled {
        background: #cbd5e1;
        box-shadow: none;
        cursor: not-allowed;
    }

    .send-spinner {
        width: 18px;
        height: 18px;
        border: 2px solid rgba(255, 255, 255, 0.3);
        border-top-color: white;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }

    .input-hint {
        text-align: center;
        font-size: 0.75rem;
        color: #94a3b8;
        margin: 0.75rem 0 0;
    }

    @media (max-width: 640px) {
        .chat-header {
            padding: 0.75rem 1rem;
        }

        .message-list {
            padding: 1rem;
        }

        .message-bubble {
            max-width: 92%;
        }

        .input-area {
            padding: 0.75rem 1rem 1rem;
        }

        .btn-delete {
            opacity: 1;
        }
    }
</style>
