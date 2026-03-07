<script lang="ts">
    import { onMount } from 'svelte';
    import { sessions, currentSession, loading, error, fetchSessions, deleteSession, searchSessions, createSession } from '$lib/stores/sessionStore';
    import { goto } from '$app/navigation';
    import { page } from '$app/state';
    import { sessionApi } from '$lib/services/api';
    import { apiConfigs, fetchConfigs } from '$lib/stores/apiConfigStore';
    import SelectApiConfigModal from '$lib/components/SelectApiConfigModal.svelte';
    import type { ApiConfigListItemDto } from '$lib/types/apiConfig';

    // State
    let searchQuery = '';
    let isDeleting = false;
    let deleteId = '';
    let isModalOpen = false;
    let selectedConfig: ApiConfigListItemDto | null = null;

    // Component lifecycle
    onMount(() => {
        fetchSessions();
        fetchConfigs();
    });

    // Handlers
    async function handleCreateSession() {
        // 检查是否有可用的 API 配置
        const configs = $apiConfigs;
        if (configs.length === 0) {
            alert('请先在设置页面配置 API 信息');
            goto('/settings');
            return;
        }
        
        // 如果只有一个配置，直接使用
        if (configs.length === 1) {
            try {
                const id = await createSession({
                    title: 'New Session',
                    apiConfigId: configs[0].id,
                    model: configs[0].model,
                    systemPrompt: 'You are a helpful assistant.',
                    temperature: 0.7,
                    maxTokens: null
                });
                goto(`/sessions/${id}`);
            } catch (err) {
                console.error('Failed to create session:', err);
            }
        } else {
            // 显示选择弹窗
            isModalOpen = true;
        }
    }

    async function handleSelectConfig(config: ApiConfigListItemDto, sessionName: string) {
        selectedConfig = config;
        try {
            const id = await createSession({
                title: sessionName,
                apiConfigId: config.id,
                model: config.model,
                systemPrompt: 'You are a helpful assistant.',
                temperature: 0.7,
                maxTokens: null
            });
            goto(`/sessions/${id}`);
        } catch (err) {
            console.error('Failed to create session:', err);
        }
    }

    function closeModal() {
        isModalOpen = false;
        selectedConfig = null;
    }

    async function handleDeleteSession(id: string, e: MouseEvent) {
        e.stopPropagation();
        isDeleting = true;
        deleteId = id;

        try {
            await deleteSession(id);
            // Refresh session list
            await fetchSessions();
        } catch (err) {
            console.error('Failed to delete session:', err);
        } finally {
            isDeleting = false;
            deleteId = '';
        }
    }

    async function handleSearch(e: Event) {
        const target = e.target as HTMLInputElement;
        searchQuery = target.value;
        await searchSessions(searchQuery);
    }

    async function handleSelectSession(id: string) {
        goto(`/sessions/${id}`);
    }

    // Derived state
    $: sessionList = $sessions;
    $: isLoading = $loading;
    $: errorMessage = $error;
</script>

<div class="sessions-page">
    <div class="header">
        <h1>会话列表</h1>
        <button class="btn-primary" on:click={handleCreateSession}>
            <span class="icon">+</span>
            新建会话
        </button>
    </div>

    {#if errorMessage}
        <div class="error-banner">
            {errorMessage}
        </div>
    {/if}

    <div class="search-bar">
        <input
            type="text"
            placeholder="搜索会话..."
            value={searchQuery}
            on:input={handleSearch}
            class="search-input"
        />
        <button class="btn-icon" on:click={() => searchSessions(searchQuery)}>
            <span class="icon">🔍</span>
        </button>
    </div>

    {#if isLoading}
        <div class="loading-spinner">
            <div class="spinner"></div>
            <p>加载中...</p>
        </div>
    {:else if sessionList.length === 0}
        <div class="empty-state">
            <div class="empty-icon">💬</div>
            <p>暂无会话</p>
            <button class="btn-link" on:click={handleCreateSession}>
                创建第一个会话
            </button>
        </div>
    {:else}
        <div class="session-list">
            {#each sessionList as session (session.id)}
                <div 
                    class="session-item"
                    on:click={() => handleSelectSession(session.id)}
                >
                    <div class="session-info">
                        <h3 class="session-title">{session.title}</h3>
                        <div class="session-meta">
                            <span class="model">{session.model}</span>
                            <span class="divider">•</span>
                            <span class="messages">{session.messageCount} 条消息</span>
                            <span class="divider">•</span>
                            <span class="time">{new Date(session.updatedAt).toLocaleString()}</span>
                        </div>
                    </div>
                    <button 
                        class="btn-delete"
                        on:click={(e) => handleDeleteSession(session.id, e)}
                        disabled={isDeleting}
                        title="删除会话"
                    >
                        {isDeleting && deleteId === session.id ? '...' : '🗑️'}
                    </button>
                </div>
            {/each}
        </div>
    {/if}

    <SelectApiConfigModal 
        isOpen={isModalOpen} 
        onClose={closeModal} 
        onSelect={handleSelectConfig} 
    />
</div>

<style>
    .sessions-page {
        max-width: 800px;
        margin: 0 auto;
        padding: 1rem;
        background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
        min-height: 100vh;
    }

    .header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1.5rem;
    }

    .header h1 {
        display: none;
    }

    .btn-primary {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 1rem;
        background-color: #3b82f6;
        color: white;
        border: none;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        font-weight: 500;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    .btn-primary:hover {
        background-color: #2563eb;
    }

    .btn-primary .icon {
        font-size: 1.25rem;
    }

    .error-banner {
        padding: 0.75rem;
        background-color: rgba(239, 68, 68, 0.1);
        border: 1px solid rgba(239, 68, 68, 0.2);
        border-radius: 0.5rem;
        color: #f87171;
        margin-bottom: 1rem;
    }

    .search-bar {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 1rem;
    }

    .search-input {
        flex: 1;
        padding: 0.5rem 1rem;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        font-size: 0.875rem;
        outline: none;
        transition: border-color 0.2s;
        background-color: #1e293b;
        color: #f1f5f9;
    }

    .search-input:focus {
        border-color: #60a5fa;
    }

    .search-input::placeholder {
        color: #64748b;
    }

    .btn-icon {
        padding: 0.5rem;
        background-color: #1e293b;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        cursor: pointer;
        transition: all 0.2s;
        color: #94a3b8;
    }

    .btn-icon:hover {
        background-color: rgba(96, 165, 250, 0.15);
        color: #60a5fa;
    }

    .loading-spinner {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 3rem;
        color: #94a3b8;
    }

    .spinner {
        width: 2rem;
        height: 2rem;
        border: 3px solid rgba(96, 165, 250, 0.2);
        border-top-color: #60a5fa;
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
        color: #94a3b8;
    }

    .empty-icon {
        font-size: 4rem;
        margin-bottom: 1rem;
        opacity: 0.5;
    }

    .empty-state p {
        font-size: 1rem;
        margin-bottom: 1rem;
    }

    .btn-link {
        color: #60a5fa;
        text-decoration: none;
        font-weight: 500;
        cursor: pointer;
    }

    .btn-link:hover {
        text-decoration: underline;
    }

    .session-list {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
    }

    .session-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem;
        background-color: #1e293b;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        cursor: pointer;
        transition: all 0.2s;
    }

    .session-item:hover {
        border-color: #60a5fa;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
    }

    .session-info {
        flex: 1;
        min-width: 0;
    }

    .session-title {
        font-size: 1rem;
        font-weight: 600;
        color: #f1f5f9;
        margin: 0 0 0.5rem 0;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .session-meta {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.75rem;
        color: #94a3b8;
        flex-wrap: wrap;
    }

    .model {
        color: #60a5fa;
    }

    .divider {
        color: #64748b;
    }

    .time {
        color: #64748b;
    }

    .btn-delete {
        padding: 0.5rem;
        background-color: transparent;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        cursor: pointer;
        transition: all 0.2s;
        color: #94a3b8;
    }

    .btn-delete:hover {
        background-color: rgba(239, 68, 68, 0.15);
        border-color: rgba(239, 68, 68, 0.3);
        color: #f87171;
    }

    .btn-delete:disabled {
        cursor: not-allowed;
        opacity: 0.5;
    }

    @media (max-width: 640px) {
        .sessions-page {
            padding: 0.5rem;
        }

        .header {
            flex-direction: column;
            gap: 1rem;
            align-items: flex-start;
        }

        .search-bar {
            flex-direction: column;
        }

        .session-item {
            flex-direction: column;
            align-items: flex-start;
            gap: 0.75rem;
        }

        .btn-delete {
            position: absolute;
            top: 0.75rem;
            right: 0.75rem;
        }
    }
</style>