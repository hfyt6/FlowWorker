<script lang="ts">
    import { apiConfigs, fetchConfigs, loading as configLoading, error as configError } from '$lib/stores/apiConfigStore';
    import type { ApiConfigListItemDto } from '$lib/types/apiConfig';

    // Props
    export let isOpen = false;
    export let onClose: () => void;
    export let onSelect: (config: ApiConfigListItemDto, sessionName: string) => void;

    // State
    let sessionName = '';

    // Derived state
    $: configs = $apiConfigs;
    $: isLoading = $configLoading;
    $: errorMessage = $configError;

    // Watch for isOpen changes to fetch configs when modal opens
    $: if (isOpen) {
        fetchConfigs();
        sessionName = '';
    }

    function handleSelect(config: ApiConfigListItemDto) {
        const name = sessionName.trim() || 'New Session';
        onSelect(config, name);
        onClose();
    }
</script>

{#if isOpen}
    <div class="modal-overlay" on:click={onClose}>
        <div class="modal-content" on:click|stopPropagation>
            <div class="modal-header">
                <h2>选择 API 配置</h2>
                <button class="btn-close" on:click={onClose}>×</button>
            </div>

            <div class="session-name-input">
                <label for="session-name">会话名称</label>
                <input
                    type="text"
                    id="session-name"
                    placeholder="输入会话名称（可选）"
                    bind:value={sessionName}
                />
            </div>

            {#if errorMessage}
                <div class="error-banner">
                    {errorMessage}
                </div>
            {/if}

            {#if isLoading}
                <div class="loading-spinner">
                    <div class="spinner"></div>
                    <p>加载中...</p>
                </div>
            {:else if configs.length === 0}
                <div class="empty-state">
                    <div class="empty-icon">⚙️</div>
                    <p>暂无可用的 API 配置</p>
                    <button class="btn-link" on:click={onClose}>
                        关闭
                    </button>
                </div>
            {:else}
                <div class="config-list">
                    {#each configs as config (config.id)}
                        <div 
                            class="config-item {config.isDefault ? 'default' : ''}"
                            on:click={() => handleSelect(config)}
                        >
                            <div class="config-info">
                                <h3 class="config-name">
                                    {config.name}
                                    {#if config.isDefault}
                                        <span class="default-badge">默认</span>
                                    {/if}
                                </h3>
                                <p class="config-model">{config.model}</p>
                                <p class="config-url">{config.baseUrl}</p>
                            </div>
                            <div class="config-action">
                                <span class="select-icon">➤</span>
                            </div>
                        </div>
                    {/each}
                </div>
            {/if}
        </div>
    </div>
{/if}

<style>
    .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-color: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        animation: fadeIn 0.2s;
    }

    .modal-content {
        background-color: white;
        border-radius: 0.75rem;
        width: 90%;
        max-width: 600px;
        max-height: 80vh;
        display: flex;
        flex-direction: column;
        box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
        animation: slideUp 0.2s;
    }

    .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1.25rem 1.5rem;
        border-bottom: 1px solid #e5e7eb;
    }

    .modal-header h2 {
        font-size: 1.25rem;
        font-weight: 600;
        color: #1f2937;
        margin: 0;
    }

    .session-name-input {
        padding: 1rem 1.5rem;
        border-bottom: 1px solid #e5e7eb;
    }

    .session-name-input label {
        display: block;
        font-size: 0.875rem;
        font-weight: 500;
        color: #374151;
        margin-bottom: 0.5rem;
    }

    .session-name-input input {
        width: 100%;
        padding: 0.625rem 0.875rem;
        border: 1px solid #d1d5db;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        outline: none;
        transition: border-color 0.2s, box-shadow 0.2s;
        box-sizing: border-box;
    }

    .session-name-input input:focus {
        border-color: #3b82f6;
        box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .session-name-input input::placeholder {
        color: #9ca3af;
    }

    .btn-close {
        background: none;
        border: none;
        font-size: 1.5rem;
        cursor: pointer;
        color: #6b7280;
        line-height: 1;
        padding: 0.25rem;
        width: 2rem;
        height: 2rem;
        border-radius: 0.375rem;
        transition: all 0.2s;
    }

    .btn-close:hover {
        background-color: #f3f4f6;
        color: #1f2937;
    }

    .error-banner {
        padding: 0.75rem 1.5rem;
        background-color: #fee2e2;
        border-radius: 0 0 0.75rem 0.75rem;
        color: #991b1b;
        font-size: 0.875rem;
    }

    .loading-spinner {
        padding: 3rem 1.5rem;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        color: #6b7280;
    }

    .spinner {
        width: 2.5rem;
        height: 2.5rem;
        border: 4px solid #f3f4f6;
        border-top-color: #3b82f6;
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin-bottom: 1rem;
    }

    .empty-state {
        padding: 3rem 1.5rem;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        color: #6b7280;
    }

    .empty-icon {
        font-size: 4rem;
        margin-bottom: 1rem;
        opacity: 0.5;
    }

    .empty-state p {
        font-size: 1rem;
        margin-bottom: 1.5rem;
        color: #6b7280;
    }

    .btn-link {
        color: #3b82f6;
        text-decoration: none;
        font-weight: 500;
        cursor: pointer;
        background: none;
        border: none;
        padding: 0.5rem 1rem;
        border-radius: 0.375rem;
        transition: all 0.2s;
    }

    .btn-link:hover {
        background-color: #eff6ff;
        text-decoration: underline;
    }

    .config-list {
        flex: 1;
        overflow-y: auto;
        padding: 0.5rem;
    }

    .config-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem;
        margin: 0.25rem 0;
        background-color: white;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        cursor: pointer;
        transition: all 0.2s;
    }

    .config-item:hover {
        border-color: #3b82f6;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        background-color: #f9fafb;
    }

    .config-item.default {
        border-color: #3b82f6;
        background-color: #eff6ff;
    }

    .config-info {
        flex: 1;
        min-width: 0;
    }

    .config-name {
        font-size: 1rem;
        font-weight: 600;
        color: #1f2937;
        margin: 0 0 0.25rem 0;
        display: flex;
        align-items: center;
        gap: 0.5rem;
    }

    .default-badge {
        background-color: #3b82f6;
        color: white;
        font-size: 0.75rem;
        padding: 0.125rem 0.5rem;
        border-radius: 9999px;
        font-weight: 500;
    }

    .config-model {
        font-size: 0.875rem;
        color: #6b7280;
        margin: 0 0 0.25rem 0;
        font-family: monospace;
    }

    .config-url {
        font-size: 0.75rem;
        color: #9ca3af;
        margin: 0;
        word-break: break-all;
    }

    .config-action {
        padding-left: 1rem;
    }

    .select-icon {
        font-size: 1.5rem;
        color: #3b82f6;
    }

    @keyframes fadeIn {
        from {
            opacity: 0;
        }
        to {
            opacity: 1;
        }
    }

    @keyframes slideUp {
        from {
            transform: translateY(20px);
            opacity: 0;
        }
        to {
            transform: translateY(0);
            opacity: 1;
        }
    }

    @keyframes spin {
        to {
            transform: rotate(360deg);
        }
    }

    @media (max-width: 640px) {
        .modal-content {
            width: 95%;
            max-height: 90vh;
        }

        .config-item {
            flex-direction: column;
            align-items: flex-start;
        }

        .config-action {
            padding-left: 0;
            padding-top: 0.75rem;
            width: 100%;
            display: flex;
            justify-content: flex-end;
        }
    }
</style>