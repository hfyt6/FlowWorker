<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import { addParticipant } from '$lib/stores/groupSessionStore';
    import type { MemberListItemDto } from '$lib/types/member';

    export let isOpen = false;
    export let sessionId: string = '';
    export let availableMembers: MemberListItemDto[] = [];

    const dispatch = createEventDispatcher();

    let selectedMemberId = '';
    let isSubmitting = false;
    let formError: string | null = null;

    function handleClose() {
        dispatch('close');
        resetForm();
    }

    function resetForm() {
        selectedMemberId = '';
        formError = null;
        isSubmitting = false;
    }

    async function handleSubmit() {
        if (!selectedMemberId) {
            formError = '请选择一个成员';
            return;
        }

        isSubmitting = true;
        formError = null;

        try {
            await addParticipant(sessionId, selectedMemberId);
            dispatch('added');
            handleClose();
        } catch (err) {
            formError = err instanceof Error ? err.message : '添加参与者失败';
        } finally {
            isSubmitting = false;
        }
    }
</script>

{#if isOpen}
    <div class="modal-overlay" on:click={handleClose}>
        <div class="modal-content" on:click|stopPropagation>
            <div class="modal-header">
                <h2>添加参与者</h2>
                <button class="btn-close" on:click={handleClose}>×</button>
            </div>

            <form on:submit|preventDefault={handleSubmit}>
                {#if formError}
                    <div class="error-message">
                        {formError}
                    </div>
                {/if}

                <div class="form-group">
                    <label for="member">选择成员 *</label>
                    {#if availableMembers.length === 0}
                        <p class="no-members">没有可用的成员可以添加</p>
                    {:else}
                        <select id="member" bind:value={selectedMemberId} disabled={isSubmitting}>
                            <option value="">选择成员</option>
                            {#each availableMembers as member}
                                <option value={member.id}>
                                    {member.name} - {member.roleName}
                                </option>
                            {/each}
                        </select>
                    {/if}
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn-secondary" on:click={handleClose} disabled={isSubmitting}>
                        取消
                    </button>
                    <button 
                        type="submit" 
                        class="btn-primary" 
                        disabled={isSubmitting || availableMembers.length === 0}
                    >
                        {isSubmitting ? '添加中...' : '添加'}
                    </button>
                </div>
            </form>
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
        max-width: 400px;
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

    form {
        padding: 1.5rem;
    }

    .error-message {
        padding: 0.75rem;
        background-color: rgba(239, 68, 68, 0.1);
        border: 1px solid rgba(239, 68, 68, 0.2);
        border-radius: 0.5rem;
        color: #f87171;
        margin-bottom: 1rem;
    }

    .form-group {
        margin-bottom: 1.5rem;
    }

    label {
        display: block;
        font-size: 0.875rem;
        font-weight: 500;
        color: #94a3b8;
        margin-bottom: 0.5rem;
    }

    select {
        width: 100%;
        padding: 0.5rem 0.75rem;
        background-color: #0f172a;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        color: #f1f5f9;
        font-size: 0.875rem;
        transition: border-color 0.2s;
    }

    select:focus {
        outline: none;
        border-color: #60a5fa;
    }

    select:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    .no-members {
        color: #94a3b8;
        font-size: 0.875rem;
        padding: 1rem;
        background-color: #0f172a;
        border-radius: 0.5rem;
        text-align: center;
    }

    .modal-footer {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        padding-top: 1rem;
        border-top: 1px solid rgba(96, 165, 250, 0.2);
    }

    .btn-secondary,
    .btn-primary {
        padding: 0.5rem 1rem;
        border-radius: 0.5rem;
        font-size: 0.875rem;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s;
    }

    .btn-secondary {
        background-color: transparent;
        border: 1px solid rgba(96, 165, 250, 0.2);
        color: #94a3b8;
    }

    .btn-secondary:hover:not(:disabled) {
        background-color: rgba(96, 165, 250, 0.1);
        color: #f1f5f9;
    }

    .btn-primary {
        background-color: #3b82f6;
        border: none;
        color: white;
    }

    .btn-primary:hover:not(:disabled) {
        background-color: #2563eb;
    }

    .btn-secondary:disabled,
    .btn-primary:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }
</style>
