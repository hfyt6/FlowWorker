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
        <!-- <h1>API 配置管理</h1> -->
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
        font-size: 1.5rem;
        font-weight: 600;
        color: #f1f5f9;
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

    .config-list {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .config-item {
        padding: 1.5rem;
        background-color: #1e293b;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        transition: all 0.2s;
    }

    .config-item:hover {
        border-color: #60a5fa;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
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
        color: #f1f5f9;
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
        border-top: 1px solid rgba(96, 165, 250, 0.15);
        border-bottom: 1px solid rgba(96, 165, 250, 0.15);
    }

    .detail-item {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.875rem;
        color: #94a3b8;
    }

    .label {
        font-weight: 500;
        color: #cbd5e1;
        min-width: 80px;
    }

    .value {
        flex: 1;
        word-break: break-all;
        font-family: 'Courier New', Courier, monospace;
        color: #e2e8f0;
    }

    .config-actions {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
    }

    .btn-action {
        padding: 0.5rem 1rem;
        background-color: rgba(96, 165, 250, 0.15);
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        font-size: 0.875rem;
        cursor: pointer;
        transition: all 0.2s;
        color: #60a5fa;
    }

    .btn-action:hover {
        background-color: rgba(96, 165, 250, 0.25);
        border-color: #60a5fa;
    }

    .btn-delete {
        padding: 0.5rem 1rem;
        background-color: rgba(239, 68, 68, 0.1);
        border: 1px solid rgba(239, 68, 68, 0.2);
        border-radius: 0.5rem;
        font-size: 0.875rem;
        cursor: pointer;
        transition: all 0.2s;
        color: #f87171;
    }

    .btn-delete:hover:not(:disabled) {
        background-color: rgba(239, 68, 68, 0.2);
        border-color: rgba(239, 68, 68, 0.3);
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
        background-color: rgba(0, 0, 0, 0.7);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        padding: 1rem;
    }

    .modal {
        background-color: #1e293b;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        padding: 2rem;
        width: 100%;
        max-width: 500px;
        box-shadow: 0 10px 25px rgba(0, 0, 0, 0.5);
    }

    .modal h2 {
        font-size: 1.25rem;
        font-weight: 600;
        color: #f1f5f9;
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
        color: #cbd5e1;
    }

    .form-group input {
        width: 100%;
        padding: 0.5rem 0.75rem;
        background-color: #0f172a;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        font-size: 0.875rem;
        outline: none;
        transition: border-color 0.2s;
        color: #f1f5f9;
    }

    .form-group input:focus {
        border-color: #60a5fa;
    }

    .form-group input::placeholder {
        color: #64748b;
    }

    .form-actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        margin-top: 1.5rem;
    }

    .btn-secondary {
        padding: 0.5rem 1rem;
        background-color: rgba(96, 165, 250, 0.15);
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        font-size: 0.875rem;
        cursor: pointer;
        transition: all 0.2s;
        color: #60a5fa;
    }

    .btn-secondary:hover {
        background-color: rgba(96, 165, 250, 0.25);
        border-color: #60a5fa;
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
