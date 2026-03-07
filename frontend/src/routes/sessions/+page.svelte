<script lang="ts">
    import { onMount } from 'svelte';
    import { sessions, currentSession, loading, error, fetchSessions, deleteSession, searchSessions } from '$lib/stores/sessionStore';
    import { goto } from '$app/navigation';
    import { page } from '$app/state';
    import { sessionApi } from '$lib/services/api';

    // State
    let searchQuery = '';
    let isDeleting = false;
    let deleteId = '';

    // Component lifecycle
    onMount(() => {
        fetchSessions();
    });

    // Handlers
    async function handleCreateSession() {
        try {
            const id = await sessionApi.createSession({
                title: 'New Session',
                apiConfigId: '',
                model: 'gpt-3.5-turbo',
                systemPrompt: 'You are a helpful assistant.',
                temperature: 0.7,
                maxTokens: null
            });
            goto(`/sessions/${id}`);
        } catch (err) {
            console.error('Failed to create session:', err);
        }
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
</div>

<style>
    .sessions-page {
        max-width: 800px;
        margin: 0 auto;
        padding: 1rem;
    }

    .header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1.5rem;
    }

    .header h1 {
        font-size: 1.5rem;
        font-weight: 600;
        color: #1f2937;
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
        background-color: #fee2e2;
        border-radius: 0.5rem;
        color: #991b1b;
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
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        outline: none;
        transition: border-color 0.2s;
    }

    .search-input:focus {
        border-color: #3b82f6;
    }

    .btn-icon {
        padding: 0.5rem;
        background-color: #f3f4f6;
        border: none;
        border-radius: 0.5rem;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    .btn-icon:hover {
        background-color: #e5e7eb;
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

    .empty-state p {
        font-size: 1rem;
        margin-bottom: 1rem;
    }

    .btn-link {
        color: #3b82f6;
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
        background-color: white;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        cursor: pointer;
        transition: all 0.2s;
    }

    .session-item:hover {
        border-color: #3b82f6;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .session-info {
        flex: 1;
        min-width: 0;
    }

    .session-title {
        font-size: 1rem;
        font-weight: 600;
        color: #1f2937;
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
        color: #6b7280;
        flex-wrap: wrap;
    }

    .model {
        color: #3b82f6;
    }

    .divider {
        color: #d1d5db;
    }

    .time {
        color: #9ca3af;
    }

    .btn-delete {
        padding: 0.5rem;
        background-color: transparent;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        cursor: pointer;
        transition: all 0.2s;
        color: #6b7280;
    }

    .btn-delete:hover {
        background-color: #fee2e2;
        border-color: #fca5a5;
        color: #991b1b;
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