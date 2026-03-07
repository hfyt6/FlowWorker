<script lang="ts">
import { onMount } from 'svelte';
import { page } from '$app/state';
import { goto } from '$app/navigation';
import { messageApi, apiConfigApi, sessionApi } from '$lib/services/api';
import { messages, loading, error, fetchMessages, sendMessage, currentMessage } from '$lib/stores/messageStore';
import StreamMessage from '$lib/components/StreamMessage.svelte';
import { MessageRole } from '$lib/types/message';
import type { MessageListItemDto } from '$lib/types/message';
import type { ApiConfigListItemDto, SessionDetailDto } from '$lib/types';

// State
let sessionId = '';
let inputMessage = '';
let isSending = false;
let messageListElement: HTMLDivElement;
let isAiResponding = false;
let lastMessageCount = 0;

// API Config State
let apiConfigs: ApiConfigListItemDto[] = [];
let currentSession: SessionDetailDto | null = null;
let selectedConfigId = '';
let isLoadingConfigs = false;
let isUpdatingConfig = false;
let showConfigDropdown = false;
let configDropdownElement: HTMLDivElement;

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

// 加载 API 配置列表
async function loadApiConfigs() {
    isLoadingConfigs = true;
    try {
        apiConfigs = await apiConfigApi.getConfigs();
    } catch (err) {
        console.error('Failed to load API configs:', err);
    } finally {
        isLoadingConfigs = false;
    }
}

// 加载当前会话详情
async function loadSessionDetail(id: string) {
    try {
        currentSession = await sessionApi.getSession(id);
        if (currentSession) {
            selectedConfigId = currentSession.apiConfigId;
        }
    } catch (err) {
        console.error('Failed to load session detail:', err);
    }
}

// 切换 API 配置
async function handleConfigChange(configId: string) {
    if (!currentSession || configId === currentSession.apiConfigId) {
        showConfigDropdown = false;
        return;
    }

    isUpdatingConfig = true;
    try {
        const selectedConfig = apiConfigs.find(c => c.id === configId);
        if (!selectedConfig) return;

        // 更新会话的 API 配置
        await sessionApi.updateSession(sessionId, {
            title: currentSession.title,
            apiConfigId: configId,
            model: selectedConfig.model,
            systemPrompt: currentSession.systemPrompt,
            temperature: currentSession.temperature,
            maxTokens: currentSession.maxTokens
        });

        // 更新本地状态
        currentSession.apiConfigId = configId;
        currentSession.apiConfigName = selectedConfig.name;
        currentSession.model = selectedConfig.model;
        selectedConfigId = configId;

        // 显示成功提示（可选）
        console.log(`已切换到配置: ${selectedConfig.name}`);
    } catch (err) {
        console.error('Failed to update API config:', err);
        error.set(err instanceof Error ? err.message : '切换配置失败');
    } finally {
        isUpdatingConfig = false;
        showConfigDropdown = false;
    }
}

// 点击外部关闭下拉菜单
function handleClickOutside(event: MouseEvent) {
    if (configDropdownElement && !configDropdownElement.contains(event.target as Node)) {
        showConfigDropdown = false;
    }
}

// Component lifecycle
onMount(() => {
    const id = page.params.id;
    if (id) {
        sessionId = id;
        fetchMessages(id);
        loadApiConfigs();
        loadSessionDetail(id);
    }

    // 添加点击外部监听
    document.addEventListener('click', handleClickOutside);

    return () => {
        document.removeEventListener('click', handleClickOutside);
    };
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
    // Shift + Enter 会默认换行，不需要额外处理
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
            <!-- API 配置切换器 -->
            <div class="config-selector" bind:this={configDropdownElement}>
                <button
                    class="btn-config"
                    on:click|stopPropagation={() => showConfigDropdown = !showConfigDropdown}
                    disabled={isLoadingConfigs || isUpdatingConfig}
                    title="切换 API 配置"
                >
                    {#if isUpdatingConfig}
                        <div class="config-spinner"></div>
                    {:else}
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2v4"/><path d="m16.2 7.8 2.9-2.9"/><path d="M18 12h4"/><path d="m16.2 16.2 2.9 2.9"/><path d="M12 18v4"/><path d="m4.9 19.1 2.9-2.9"/><path d="M2 12h4"/><path d="m4.9 4.9 2.9 2.9"/></svg>
                    {/if}
                    <span class="config-name">
                        {currentSession?.apiConfigName || '选择配置'}
                    </span>
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class:rotate={showConfigDropdown}><path d="m6 9 6 6 6-6"/></svg>
                </button>

                {#if showConfigDropdown}
                    <div class="config-dropdown">
                        <div class="dropdown-header">
                            <span>选择 API 配置</span>
                            {#if isLoadingConfigs}
                                <div class="dropdown-spinner"></div>
                            {/if}
                        </div>
                        {#if apiConfigs.length === 0 && !isLoadingConfigs}
                            <div class="dropdown-empty">暂无配置</div>
                        {:else}
                            <div class="config-list">
                                {#each apiConfigs as config (config.id)}
                                    <button
                                        class="config-item"
                                        class:active={config.id === selectedConfigId}
                                        on:click|stopPropagation={() => handleConfigChange(config.id)}
                                    >
                                        <div class="config-item-info">
                                            <span class="config-item-name">{config.name}</span>
                                            <span class="config-item-model">{config.model}</span>
                                        </div>
                                        {#if config.isDefault}
                                            <span class="config-default-badge">默认</span>
                                        {/if}
                                        {#if config.id === selectedConfigId}
                                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="check-icon"><path d="M20 6 9 17l-5-5"/></svg>
                                        {/if}
                                    </button>
                                {/each}
                            </div>
                        {/if}
                        <div class="dropdown-footer">
                            <a href="/settings" on:click|preventDefault={() => goto('/settings')}>
                                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/></svg>
                                管理配置
                            </a>
                        </div>
                    </div>
                {/if}
            </div>

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
            ></textarea>
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
    .chat-app {
        display: flex;
        flex-direction: column;
        height: 100%;
        background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
        overflow: hidden;
    }

    .chat-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem 1.5rem;
        background: rgba(30, 41, 59, 0.8);
        backdrop-filter: blur(10px);
        border-bottom: 1px solid rgba(96, 165, 250, 0.1);
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
        flex-shrink: 0;
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
        color: #60a5fa;
        border-radius: 50%;
        cursor: pointer;
        transition: all 0.2s;
    }

    .btn-back:hover {
        background: rgba(96, 165, 250, 0.15);
    }

    .header-info h1 {
        margin: 0;
        font-size: 1.1rem;
        font-weight: 600;
        color: #f1f5f9;
    }

    .header-subtitle {
        font-size: 0.75rem;
        color: #94a3b8;
    }

    .header-actions {
        display: flex;
        gap: 0.5rem;
        align-items: center;
    }

    /* API 配置选择器样式 */
    .config-selector {
        position: relative;
    }

    .btn-config {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 0.75rem;
        border: 1px solid rgba(96, 165, 250, 0.2);
        background: rgba(15, 23, 42, 0.6);
        color: #f1f5f9;
        border-radius: 0.75rem;
        cursor: pointer;
        transition: all 0.2s;
        font-size: 0.875rem;
        font-weight: 500;
        white-space: nowrap;
    }

    .btn-config:hover:not(:disabled) {
        border-color: #60a5fa;
        background: rgba(96, 165, 250, 0.1);
    }

    .btn-config:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    .btn-config svg {
        color: #94a3b8;
        flex-shrink: 0;
    }

    .btn-config svg.rotate {
        transform: rotate(180deg);
    }

    .config-name {
        max-width: 120px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .config-spinner {
        width: 14px;
        height: 14px;
        border: 2px solid rgba(96, 165, 250, 0.3);
        border-top-color: #60a5fa;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }

    .config-dropdown {
        position: absolute;
        top: calc(100% + 0.5rem);
        right: 0;
        min-width: 280px;
        max-width: 320px;
        background: rgba(30, 41, 59, 0.95);
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.75rem;
        box-shadow: 0 10px 40px rgba(0, 0, 0, 0.4);
        z-index: 100;
        overflow: hidden;
        backdrop-filter: blur(10px);
    }

    .dropdown-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 0.75rem 1rem;
        background: rgba(15, 23, 42, 0.8);
        border-bottom: 1px solid rgba(96, 165, 250, 0.1);
        font-size: 0.75rem;
        font-weight: 600;
        color: #94a3b8;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }

    .dropdown-spinner {
        width: 12px;
        height: 12px;
        border: 2px solid rgba(96, 165, 250, 0.3);
        border-top-color: #60a5fa;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }

    .dropdown-empty {
        padding: 1.5rem;
        text-align: center;
        color: #64748b;
        font-size: 0.875rem;
    }

    .config-list {
        max-height: 240px;
        overflow-y: auto;
    }

    .config-item {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        width: 100%;
        padding: 0.75rem 1rem;
        border: none;
        background: transparent;
        cursor: pointer;
        transition: all 0.15s;
        text-align: left;
    }

    .config-item:hover {
        background: rgba(96, 165, 250, 0.1);
    }

    .config-item.active {
        background: rgba(96, 165, 250, 0.15);
    }

    .config-item-info {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 0.125rem;
        min-width: 0;
    }

    .config-item-name {
        font-size: 0.875rem;
        font-weight: 500;
        color: #f1f5f9;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .config-item-model {
        font-size: 0.75rem;
        color: #94a3b8;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .config-default-badge {
        font-size: 0.625rem;
        font-weight: 600;
        color: #60a5fa;
        background: rgba(96, 165, 250, 0.15);
        padding: 0.125rem 0.375rem;
        border-radius: 0.25rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }

    .check-icon {
        color: #60a5fa;
        flex-shrink: 0;
    }

    .dropdown-footer {
        padding: 0.5rem;
        border-top: 1px solid rgba(96, 165, 250, 0.1);
        background: rgba(15, 23, 42, 0.6);
    }

    .dropdown-footer a {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        padding: 0.5rem;
        color: #94a3b8;
        font-size: 0.75rem;
        font-weight: 500;
        text-decoration: none;
        border-radius: 0.5rem;
        transition: all 0.15s;
    }

    .dropdown-footer a:hover {
        background: rgba(96, 165, 250, 0.15);
        color: #60a5fa;
    }

    .btn-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 40px;
        height: 40px;
        border: none;
        background: transparent;
        color: #94a3b8;
        border-radius: 50%;
        cursor: pointer;
        transition: all 0.2s;
    }

    .btn-icon:hover {
        background: rgba(96, 165, 250, 0.15);
        color: #60a5fa;
    }

    .error-banner {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin: 1rem 1.5rem 0;
        padding: 0.75rem 1rem;
        background: rgba(239, 68, 68, 0.1);
        border: 1px solid rgba(239, 68, 68, 0.2);
        border-radius: 0.75rem;
        color: #f87171;
        font-size: 0.875rem;
        flex-shrink: 0;
    }

    .message-list {
        flex: 1;
        overflow-y: auto;
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
        min-height: 0;
    }

    .loading-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100%;
        color: #94a3b8;
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
        color: #94a3b8;
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
        color: #f1f5f9;
        margin: 0 0 0.5rem;
    }

    .empty-desc {
        font-size: 0.875rem;
        color: #94a3b8;
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
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
    }

    .message-bubble.user .message-bubble-content {
        background: linear-gradient(135deg, #3b82f6, #2563eb);
        color: white;
        border-bottom-right-radius: 0.25rem;
    }

    .message-bubble.assistant .message-bubble-content {
        background: rgba(30, 41, 59, 0.8);
        color: #f1f5f9;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-bottom-left-radius: 0.25rem;
    }

    .message-bubble.system .message-bubble-content {
        background: rgba(245, 158, 11, 0.15);
        color: #fbbf24;
        border: 1px solid rgba(245, 158, 11, 0.3);
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
        background: rgba(0, 0, 0, 0.2);
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
        color: #64748b;
    }

    .tokens {
        color: #60a5fa;
        background: rgba(96, 165, 250, 0.15);
        padding: 0.125rem 0.5rem;
        border-radius: 1rem;
    }

    .model {
        color: #22d3ee;
        background: rgba(34, 211, 238, 0.15);
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
        color: #475569;
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
        background: rgba(239, 68, 68, 0.15);
        color: #f87171;
    }

    .input-area {
        padding: 1rem 1.5rem 1.5rem;
        background: rgba(30, 41, 59, 0.8);
        backdrop-filter: blur(10px);
        border-top: 1px solid rgba(96, 165, 250, 0.1);
        flex-shrink: 0;
    }

    .input-container {
        display: flex;
        gap: 0.75rem;
        align-items: flex-end;
        background: rgba(15, 23, 42, 0.6);
        border: 2px solid rgba(96, 165, 250, 0.2);
        border-radius: 1.5rem;
        padding: 0.5rem 0.5rem 0.5rem 1.25rem;
        transition: all 0.2s;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
    }

    .input-container:focus-within {
        border-color: #60a5fa;
        box-shadow: 0 4px 20px rgba(96, 165, 250, 0.2);
    }

    .input-message {
        flex: 1;
        border: none;
        background: transparent;
        font-size: 0.9375rem;
        line-height: 1.5;
        resize: none;
        outline: none;
        color: #f1f5f9;
        max-height: 120px;
        min-height: 24px;
        padding: 0.5rem 0;
    }

    .input-message::placeholder {
        color: #64748b;
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
        background: #475569;
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
        color: #64748b;
        margin: 0.75rem 0 0;
    }

    @media (max-width: 640px) {
        .chat-header {
            padding: 0.75rem 1rem;
        }

        .header-info h1 {
            font-size: 1rem;
        }

        .config-name {
            max-width: 80px;
        }

        .config-dropdown {
            min-width: 260px;
            right: -1rem;
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
