<script lang="ts">
    import { onMount } from 'svelte';
    import { sessions, currentSession, loading, error, fetchSessions, deleteSession, searchSessions, createSession } from '$lib/stores/sessionStore';
    import { goto } from '$app/navigation';
    import { page } from '$app/state';
    import { sessionApi } from '$lib/services/api';
    import { apiConfigs, fetchConfigs } from '$lib/stores/apiConfigStore';
    import { members, fetchMembers } from '$lib/stores/memberStore';
    import SelectSessionTypeModal from '$lib/components/SelectSessionTypeModal.svelte';
    import CreateSingleSessionModal from '$lib/components/CreateSingleSessionModal.svelte';
    import CreateGroupSessionModal from '$lib/components/CreateGroupSessionModal.svelte';
    import type { ApiConfigListItemDto } from '$lib/types/apiConfig';
    import type { MemberListItemDto } from '$lib/types/member';

    // State
    let searchQuery = '';
    let isDeleting = false;
    let deleteId = '';
    
    // 弹窗状态
    let isSessionTypeModalOpen = false;
    let isSingleSessionModalOpen = false;
    let isGroupSessionModalOpen = false;

    // Component lifecycle
    onMount(() => {
        fetchSessions();
        fetchConfigs();
        fetchMembers();
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
        
        // 显示选择会话类型弹窗
        isSessionTypeModalOpen = true;
    }

    function handleSessionTypeSelect(event: CustomEvent<{ type: string }>) {
        const { type } = event.detail;
        isSessionTypeModalOpen = false;
        
        if (type === 'single') {
            isSingleSessionModalOpen = true;
        } else if (type === 'group') {
            isGroupSessionModalOpen = true;
        }
    }

    function closeSessionTypeModal() {
        isSessionTypeModalOpen = false;
    }

    function closeSingleSessionModal() {
        isSingleSessionModalOpen = false;
    }

    function closeGroupSessionModal() {
        isGroupSessionModalOpen = false;
    }

    function handleSessionCreated(event: CustomEvent<{ sessionId: string }>) {
        const { sessionId } = event.detail;
        goto(`/sessions/${sessionId}`);
    }

    function handleGroupSessionCreated() {
        // 群聊创建成功后刷新列表
        fetchSessions();
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

    <!-- 选择会话类型弹窗 -->
    <SelectSessionTypeModal 
        isOpen={isSessionTypeModalOpen} 
        on:close={closeSessionTypeModal}
        on:select={handleSessionTypeSelect}
    />

    <!-- 创建单聊弹窗 -->
    <CreateSingleSessionModal
        isOpen={isSingleSessionModalOpen}
        members={$members}
        apiConfigs={$apiConfigs}
        on:close={closeSingleSessionModal}
        on:created={handleSessionCreated}
    />

    <!-- 创建群聊弹窗 -->
    <CreateGroupSessionModal
        isOpen={isGroupSessionModalOpen}
        members={$members}
        apiConfigs={$apiConfigs}
        on:close={closeGroupSessionModal}
        on:created={handleGroupSessionCreated}
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