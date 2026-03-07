<script lang="ts">
    import { onMount } from 'svelte';
    import { apiConfigs, currentConfig, loading, error, fetchConfigs, deleteConfig, setDefaultConfig, createConfig, updateConfig } from '$lib/stores/apiConfigStore';
    import { goto } from '$app/navigation';
    import type { ApiConfigDetailDto, ApiConfigListItemDto } from '$lib/types/apiConfig';

    // State
    let isEditing = false;
    let editingConfig: ApiConfigDetailDto | null = null;
    let isDeleting = false;
    let deleteId = '';

    // Component lifecycle
    onMount(() => {
        fetchConfigs();
    });

    // Handlers
    function handleAddConfig() {
        editingConfig = {
            id: '',
            name: '',
            baseUrl: '',
            apiKey: '',
            model: 'gpt-3.5-turbo',
            isDefault: false,
            createdAt: new Date().toISOString()
        };
        isEditing = true;
    }

    function handleEditConfig(config: ApiConfigListItemDto) {
        editingConfig = {
            id: config.id,
            name: config.name,
            baseUrl: config.baseUrl,
            apiKey: '',
            model: config.model,
            isDefault: config.isDefault,
            createdAt: config.createdAt
        };
        isEditing = true;
    }

    function handleCancelEdit() {
        editingConfig = null;
        isEditing = false;
    }

    async function handleSaveConfig() {
        if (!editingConfig) return;

        try {
            if (editingConfig.id) {
                await updateConfig(editingConfig.id, editingConfig);
            } else {
                await createConfig(editingConfig);
            }
            await fetchConfigs();
            editingConfig = null;
            isEditing = false;
        } catch (err) {
            console.error('Failed to save config:', err);
        }
    }

    async function handleDeleteConfig(id: string, e: MouseEvent) {
        e.stopPropagation();
        isDeleting = true;
        deleteId = id;

        try {
            await deleteConfig(id);
            await fetchConfigs();
        } catch (err) {
            console.error('Failed to delete config:', err);
        } finally {
            isDeleting = false;
            deleteId = '';
        }
    }

    async function handleSetDefaultConfig(id: string) {
        try {
            await setDefaultConfig(id);
            await fetchConfigs();
        } catch (err) {
            console.error('Failed to set default config:', err);
        }
    }

    // Derived state
    $: configList = $apiConfigs;
    $: isLoading = $loading;
    $: errorMessage = $error;
</script>

<div class="settings-page">
    <div class="header">
        <h1>API 配置管理</h1>
        <button class="btn-primary" on:click={handleAddConfig}>
            <span class="icon">+</span>
            添加配置
        </button>
    </div>

    {#if errorMessage}
        <div class="error-banner">
            {errorMessage}
        </div>
    {/if}

    {#if isLoading && configList.length === 0}
        <div class="loading-spinner">
            <div class="spinner"></div>
            <p>加载中...</p>
        </div>
    {:else if configList.length === 0}
        <div class="empty-state">
            <div class="empty-icon">⚙️</div>
            <p>暂无 API 配置</p>
            <button class="btn-link" on:click={handleAddConfig}>
                添加第一个配置
            </button>
        </div>
    {:else}
        <div class="config-list">
            {#each configList as config (config.id)}
                <div class="config-item">
                    <div class="config-info">
                        <div class="config-header">
                            <h3 class="config-name">{config.name}</h3>
                            {#if config.isDefault}
                                <span class="default-badge">默认</span>
                            {/if}
                        </div>
                        <div class="config-details">
                            <div class="detail-item">
                                <span class="label">Base URL:</span>
                                <span class="value">{config.baseUrl}</span>
                            </div>
                            <div class="detail-item">
                                <span class="label">Model:</span>
                                <span class="value">{config.model}</span>
                            </div>
                            <div class="detail-item">
                                <span class="label">API Key:</span>
                                <span class="value">••••••••••••</span>
                            </div>
                        </div>
                        <div class="config-actions">
                            <button class="btn-action" on:click={() => handleEditConfig(config)}>
                                编辑
                            </button>
                            {#if !config.isDefault}
                                <button class="btn-action" on:click={() => handleSetDefaultConfig(config.id)}>
                                    设为默认
                                </button>
                            {/if}
                            <button 
                                class="btn-delete"
                                on:click={(e) => handleDeleteConfig(config.id, e)}
                                disabled={isDeleting}
                            >
                                {isDeleting && deleteId === config.id ? '...' : '删除'}
                            </button>
                        </div>
                    </div>
                </div>
            {/each}
        </div>
    {/if}

    {#if isEditing && editingConfig}
        <div class="modal-overlay" on:click={handleCancelEdit}>
            <div class="modal" on:click|stopPropagation>
                <h2>{editingConfig.id ? '编辑配置' : '添加配置'}</h2>
                
                <div class="form-group">
                    <label for="name">配置名称</label>
                    <input
                        id="name"
                        bind:value={editingConfig.name}
                        placeholder="例如: OpenAI"
                        required
                    />
                </div>

                <div class="form-group">
                    <label for="baseUrl">Base URL</label>
                    <input
                        id="baseUrl"
                        bind:value={editingConfig.baseUrl}
                        placeholder="例如: https://api.openai.com/v1"
                        required
                    />
                </div>

                <div class="form-group">
                    <label for="apiKey">API Key</label>
                    <input
                        id="apiKey"
                        bind:value={editingConfig.apiKey}
                        type="password"
                        placeholder="输入 API Key"
                        required
                    />
                </div>

                <div class="form-group">
                    <label for="model">默认模型</label>
                    <input
                        id="model"
                        bind:value={editingConfig.model}
                        placeholder="例如: gpt-3.5-turbo"
                        required
                    />
                </div>

                <div class="form-actions">
                    <button class="btn-secondary" on:click={handleCancelEdit}>
                        取消
                    </button>
                    <button class="btn-primary" on:click={handleSaveConfig}>
                        保存
                    </button>
                </div>
            </div>
        </div>
    {/if}
</div>

<style>
    .settings-page {
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

    .config-list {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .config-item {
        padding: 1.5rem;
        background-color: white;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
    }

    .config-info {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .config-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .config-name {
        font-size: 1.125rem;
        font-weight: 600;
        color: #1f2937;
        margin: 0;
    }

    .default-badge {
        display: inline-block;
        padding: 0.25rem 0.75rem;
        background-color: #10b981;
        color: white;
        border-radius: 1rem;
        font-size: 0.75rem;
        font-weight: 500;
    }

    .config-details {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        padding: 1rem 0;
        border-top: 1px solid #f3f4f6;
        border-bottom: 1px solid #f3f4f6;
    }

    .detail-item {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.875rem;
        color: #6b7280;
    }

    .label {
        font-weight: 500;
        color: #374151;
        min-width: 80px;
    }

    .value {
        flex: 1;
        word-break: break-all;
        font-family: 'Courier New', Courier, monospace;
    }

    .config-actions {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
    }

    .btn-action {
        padding: 0.5rem 1rem;
        background-color: #f3f4f6;
        border: none;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        cursor: pointer;
        transition: background-color 0.2s;
        color: #374151;
    }

    .btn-action:hover {
        background-color: #e5e7eb;
    }

    .btn-delete {
        padding: 0.5rem 1rem;
        background-color: #fee2e2;
        border: none;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        cursor: pointer;
        transition: background-color 0.2s;
        color: #991b1b;
    }

    .btn-delete:hover:not(:disabled) {
        background-color: #fecaca;
    }

    .btn-delete:disabled {
        cursor: not-allowed;
        opacity: 0.5;
    }

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
        padding: 1rem;
    }

    .modal {
        background-color: white;
        border-radius: 0.5rem;
        padding: 2rem;
        width: 100%;
        max-width: 500px;
        box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
    }

    .modal h2 {
        font-size: 1.25rem;
        font-weight: 600;
        color: #1f2937;
        margin: 0 0 1.5rem 0;
    }

    .form-group {
        margin-bottom: 1rem;
    }

    .form-group label {
        display: block;
        margin-bottom: 0.5rem;
        font-size: 0.875rem;
        font-weight: 500;
        color: #374151;
    }

    .form-group input {
        width: 100%;
        padding: 0.5rem 0.75rem;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        outline: none;
        transition: border-color 0.2s;
    }

    .form-group input:focus {
        border-color: #3b82f6;
    }

    .form-actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        margin-top: 1.5rem;
    }

    .btn-secondary {
        padding: 0.5rem 1rem;
        background-color: #f3f4f6;
        border: none;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        cursor: pointer;
        transition: background-color 0.2s;
        color: #374151;
    }

    .btn-secondary:hover {
        background-color: #e5e7eb;
    }

    @media (max-width: 640px) {
        .settings-page {
            padding: 0.5rem;
        }

        .header {
            flex-direction: column;
            gap: 1rem;
            align-items: flex-start;
        }

        .config-item {
            padding: 1rem;
        }

        .config-details {
            flex-direction: column;
        }

        .detail-item {
            flex-direction: column;
            align-items: flex-start;
        }

        .label {
            margin-bottom: 0.25rem;
        }
    }
</style>