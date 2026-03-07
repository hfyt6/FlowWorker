<script lang="ts">
import { onMount } from 'svelte';
import { page } from '$app/state';
import { goto } from '$app/navigation';
import { messageApi } from '$lib/services/api';
import { messages, loading, error, fetchMessages, sendMessage, currentMessage } from '$lib/stores/messageStore';
import StreamMessage from '$lib/components/StreamMessage.svelte';
import type { MessageRole } from '$lib/types/message';

// State
let sessionId = '';
let inputMessage = '';
let isSending = false;

// Component lifecycle
onMount(() => {
    const id = page.params.id;
    if (id) {
        sessionId = id;
        fetchMessages(id);
    }
});

// Handlers
async function handleSendMessage() {
    if (!inputMessage.trim() || isSending) {
        return;
    }

    isSending = true;
    
    try {
        const response = await messageApi.sendMessage(sessionId, inputMessage);
        
        // 添加用户消息
        messages.update(currentMessages => [
            ...currentMessages,
            {
                id: 'user-' + Date.now(),
                role: MessageRole.User,
                content: inputMessage,
                tokens: null,
                model: null,
                createdAt: new Date().toISOString()
            }
        ]);

        // 添加 AI 回复
        messages.update(currentMessages => [
            ...currentMessages,
            {
                id: 'assistant-' + Date.now(),
                role: MessageRole.Assistant,
                content: response.content,
                tokens: response.tokens,
                model: response.model,
                createdAt: new Date().toISOString()
            }
        ]);

        inputMessage = '';
    } catch (err) {
        console.error('Failed to send message:', err);
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
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
    $: sessionMessages = $messages;
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

<div class="chat-page">
    <div class="chat-container">
        <div class="chat-header">
            <h1>会话详情</h1>
            <div class="header-actions">
                <button class="btn-icon" on:click={handleRegenerate} title="重新生成">
                    <span class="icon">🔄</span>
                </button>
                <button class="btn-icon" on:click={() => goto('/sessions')} title="返回列表">
                    <span class="icon">⬅️</span>
                </button>
            </div>
        </div>

        {#if errorMessage}
            <div class="error-banner">
                {errorMessage}
            </div>
        {/if}

        <div class="message-list">
            {#if isLoading && sessionMessages.length === 0}
                <div class="loading-spinner">
                    <div class="spinner"></div>
                    <p>加载中...</p>
                </div>
            {:else if sessionMessages.length === 0}
                <div class="empty-state">
                    <div class="empty-icon">👋</div>
                    <p>开始新的对话吧！</p>
                </div>
            {:else}
                {#each sessionMessages as message (message.id)}
                    <div class="message-item {message.role}">
                        <div class="message-role">
                            {message.role === 'user' ? '👤' : message.role === 'assistant' ? '🤖' : '⚙️'}
                            <span class="role-name">{message.role}</span>
                        </div>
                        <div class="message-content">
                            <pre><code>{message.content}</code></pre>
                        </div>
                        <div class="message-meta">
                            <span class="time">{new Date(message.createdAt).toLocaleString()}</span>
                            {#if message.tokens}
                                <span class="tokens">{message.tokens} tokens</span>
                            {/if}
                            {#if message.model}
                                <span class="model">{message.model}</span>
                            {/if}
                            <button 
                                class="btn-delete-small"
                                on:click={() => handleDeleteMessage(message.id)}
                                title="删除消息"
                            >
                                🗑️
                            </button>
                        </div>
                    </div>
                {/each}
            {/if}
        </div>

        <div class="input-area">
            <textarea
                bind:value={inputMessage}
                on:keydown={handleKeyDown}
                placeholder="输入消息..."
                disabled={isSending}
                class="input-message"
            />
            <div class="input-actions">
                <button 
                    class="btn-primary" 
                    on:click={handleSendMessage}
                    disabled={isInputEmpty || isSending}
                >
                    {isSending ? '发送中...' : '发送'}
                </button>
            </div>
        </div>
    </div>
</div>

<style>
    .chat-page {
        height: 100vh;
        display: flex;
        flex-direction: column;
    }

    .chat-container {
        flex: 1;
        display: flex;
        flex-direction: column;
        max-width: 1000px;
        margin: 0 auto;
        width: 100%;
        padding: 1rem;
        box-sizing: border-box;
    }

    .chat-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1rem;
        padding-bottom: 1rem;
        border-bottom: 1px solid #e5e7eb;
    }

    .chat-header h1 {
        font-size: 1.25rem;
        font-weight: 600;
        color: #1f2937;
    }

    .header-actions {
        display: flex;
        gap: 0.5rem;
    }

    .btn-icon {
        padding: 0.5rem;
        background-color: #f3f4f6;
        border: none;
        border-radius: 0.5rem;
        cursor: pointer;
        transition: background-color 0.2s;
        color: #6b7280;
    }

    .btn-icon:hover {
        background-color: #e5e7eb;
    }

    .error-banner {
        padding: 0.75rem;
        background-color: #fee2e2;
        border-radius: 0.5rem;
        color: #991b1b;
        margin-bottom: 1rem;
    }

    .message-list {
        flex: 1;
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        gap: 1rem;
        padding-bottom: 1rem;
    }

    .loading-spinner {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 3rem;
        color: #6b7280;
    }

    .spinner {
        width: 2rem;
        height: 2rem;
        border: 3px solid #f3f4f6;
        border-top-color: #3b82f6;
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin-bottom: 1rem;
    }

    @keyframes spin {
        to {
            transform: rotate(360deg);
        }
    }

    .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 3rem;
        color: #6b7280;
    }

    .empty-icon {
        font-size: 4rem;
        margin-bottom: 1rem;
        opacity: 0.5;
    }

    .message-item {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        padding: 1rem;
        background-color: white;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
    }

    .message-item.user {
        background-color: #f0f9ff;
        border-color: #bae6fd;
    }

    .message-item.assistant {
        background-color: #f9fafb;
        border-color: #e5e7eb;
    }

    .message-item.system {
        background-color: #fef2f2;
        border-color: #fca5a5;
    }

    .message-role {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.75rem;
        color: #6b7280;
        font-weight: 500;
    }

    .role-name {
        text-transform: capitalize;
    }

    .message-content {
        line-height: 1.6;
        color: #1f2937;
    }

    .message-content pre {
        background-color: #1e1e1e;
        color: #d4d4d4;
        padding: 1rem;
        border-radius: 0.5rem;
        overflow-x: auto;
        margin: 0;
    }

    .message-content code {
        font-family: 'Courier New', Courier, monospace;
        background-color: #f0f0f0;
        padding: 0.2rem 0.4rem;
        border-radius: 0.25rem;
    }

    .message-meta {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        font-size: 0.75rem;
        color: #6b7280;
        flex-wrap: wrap;
    }

    .time {
        color: #9ca3af;
    }

    .tokens {
        color: #3b82f6;
    }

    .model {
        color: #10b981;
    }

    .btn-delete-small {
        padding: 0.25rem 0.5rem;
        background-color: transparent;
        border: 1px solid #e5e7eb;
        border-radius: 0.25rem;
        cursor: pointer;
        transition: all 0.2s;
        color: #6b7280;
        font-size: 0.875rem;
    }

    .btn-delete-small:hover {
        background-color: #fee2e2;
        border-color: #fca5a5;
        color: #991b1b;
    }

    .input-area {
        margin-top: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
    }

    .input-message {
        width: 100%;
        min-height: 100px;
        padding: 0.75rem;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        resize: vertical;
        outline: none;
        transition: border-color 0.2s;
        font-family: inherit;
    }

    .input-message:focus {
        border-color: #3b82f6;
    }

    .input-message:disabled {
        background-color: #f3f4f6;
        cursor: not-allowed;
    }

    .input-actions {
        display: flex;
        justify-content: flex-end;
    }

    .btn-primary {
        padding: 0.5rem 1.5rem;
        background-color: #3b82f6;
        color: white;
        border: none;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        font-weight: 500;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    .btn-primary:hover:not(:disabled) {
        background-color: #2563eb;
    }

    .btn-primary:disabled {
        background-color: #93c5fd;
        cursor: not-allowed;
    }

    @media (max-width: 640px) {
        .chat-container {
            padding: 0.5rem;
        }

        .chat-header {
            flex-direction: column;
            gap: 0.75rem;
            align-items: flex-start;
        }

        .header-actions {
            width: 100%;
            justify-content: space-between;
        }

        .message-item {
            padding: 0.75rem;
        }

        .message-meta {
            flex-direction: column;
            align-items: flex-start;
            gap: 0.25rem;
        }
    }
</style>