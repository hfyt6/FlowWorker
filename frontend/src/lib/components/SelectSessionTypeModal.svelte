<script lang="ts">
    import { createEventDispatcher } from 'svelte';

    export let isOpen = false;

    const dispatch = createEventDispatcher();

    function handleClose() {
        dispatch('close');
    }

    function selectSingleChat() {
        dispatch('select', { type: 'single' });
        handleClose();
    }

    function selectGroupChat() {
        dispatch('select', { type: 'group' });
        handleClose();
    }
</script>

{#if isOpen}
    <div class="modal-overlay" on:click={handleClose}>
        <div class="modal-content" on:click|stopPropagation>
            <div class="modal-header">
                <h2>选择会话类型</h2>
                <button class="btn-close" on:click={handleClose}>×</button>
            </div>

            <div class="modal-body">
                <p class="description">请选择要创建的会话类型</p>
                
                <div class="options">
                    <button class="option-card" on:click={selectSingleChat}>
                        <div class="option-icon">👤</div>
                        <div class="option-title">单聊</div>
                        <div class="option-desc">与单个AI助手进行一对一对话</div>
                    </button>

                    <button class="option-card" on:click={selectGroupChat}>
                        <div class="option-icon">👥</div>
                        <div class="option-title">群聊</div>
                        <div class="option-desc">与多个AI助手进行群聊对话</div>
                    </button>
                </div>
            </div>

            <div class="modal-footer">
                <button type="button" class="btn-secondary" on:click={handleClose}>
                    取消
                </button>
            </div>
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
        background-color: rgba(0, 0, 0, 0.7);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        padding: 1rem;
    }

    .modal-content {
        background-color: #1e293b;
        border-radius: 0.75rem;
        width: 100%;
        max-width: 480px;
        border: 1px solid rgba(96, 165, 250, 0.2);
    }

    .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem 1.5rem;
        border-bottom: 1px solid rgba(96, 165, 250, 0.2);
    }

    .modal-header h2 {
        margin: 0;
        font-size: 1.25rem;
        color: #f1f5f9;
    }

    .btn-close {
        background: none;
        border: none;
        color: #94a3b8;
        font-size: 1.5rem;
        cursor: pointer;
        padding: 0;
        width: 2rem;
        height: 2rem;
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: 0.375rem;
        transition: all 0.2s;
    }

    .btn-close:hover {
        background-color: rgba(96, 165, 250, 0.1);
        color: #f1f5f9;
    }

    .modal-body {
        padding: 1.5rem;
    }

    .description {
        color: #94a3b8;
        text-align: center;
        margin-bottom: 1.5rem;
        font-size: 0.875rem;
    }

    .options {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
    }

    .option-card {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 1.5rem;
        background-color: #0f172a;
        border: 2px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.75rem;
        cursor: pointer;
        transition: all 0.2s;
        text-align: center;
    }

    .option-card:hover {
        border-color: #60a5fa;
        background-color: rgba(96, 165, 250, 0.1);
        transform: translateY(-2px);
    }

    .option-icon {
        font-size: 2.5rem;
        margin-bottom: 0.75rem;
    }

    .option-title {
        font-size: 1rem;
        font-weight: 600;
        color: #f1f5f9;
        margin-bottom: 0.5rem;
    }

    .option-desc {
        font-size: 0.75rem;
        color: #94a3b8;
        line-height: 1.4;
    }

    .modal-footer {
        display: flex;
        justify-content: center;
        padding: 1rem 1.5rem;
        border-top: 1px solid rgba(96, 165, 250, 0.2);
    }

    .btn-secondary {
        padding: 0.5rem 1.5rem;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s;
        background-color: transparent;
        border: 1px solid rgba(96, 165, 250, 0.2);
        color: #94a3b8;
    }

    .btn-secondary:hover {
        background-color: rgba(96, 165, 250, 0.1);
        color: #f1f5f9;
    }

    @media (max-width: 480px) {
        .options {
            grid-template-columns: 1fr;
        }
    }
</style>
