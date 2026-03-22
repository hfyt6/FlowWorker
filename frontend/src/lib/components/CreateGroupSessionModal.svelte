<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import { sessionApi } from '$lib/services/api';
    import type { MemberListItemDto } from '$lib/types/member';
    import { MemberType } from '$lib/types/member';
    import type { ApiConfigListItemDto } from '$lib/types/apiConfig';

    export let isOpen = false;
    export let members: MemberListItemDto[] = [];
    export let apiConfigs: ApiConfigListItemDto[] = [];

    const dispatch = createEventDispatcher();

    // Form state
    let title = '';
    let systemPrompt = '';
    let workingDirectory = '';
    let selectedAiMemberIds: string[] = [];
    let selectedUserMemberId: string | null = null;
    let isSubmitting = false;
    let formError: string | null = null;

    // Reset form when modal opens
    $: if (isOpen) {
        resetForm();
    }

    function resetForm() {
        title = '';
        systemPrompt = '';
        workingDirectory = '';
        selectedAiMemberIds = [];
        selectedUserMemberId = null;
        formError = null;
    }

    function handleClose() {
        dispatch('close');
    }

    function toggleAiMember(memberId: string) {
        const index = selectedAiMemberIds.indexOf(memberId);
        if (index === -1) {
            selectedAiMemberIds = [...selectedAiMemberIds, memberId];
        } else {
            selectedAiMemberIds = selectedAiMemberIds.filter(id => id !== memberId);
        }
    }

    function selectUserMember(memberId: string) {
        selectedUserMemberId = memberId;
    }

    async function handleSubmit() {
        if (!title.trim()) {
            formError = '请输入会话标题';
            return;
        }
        if (selectedAiMemberIds.length === 0) {
            formError = '请至少选择一个AI成员';
            return;
        }

        isSubmitting = true;
        formError = null;

        try {
            // 创建群聊会话 - 使用默认用户或 Guid.Empty（系统创建）
            const createdBy = selectedUserMemberId || '00000000-0000-0000-0000-000000000000';
            
            const request: import('$lib/types').CreateGroupSessionRequest = {
                title: title.trim(),
                createdBy: createdBy,
                aiMemberIds: selectedAiMemberIds,
                systemPrompt: systemPrompt.trim() || undefined
            };
            // 如果填写了工作目录，则添加到请求中
            if (workingDirectory.trim()) {
                request.workingDirectory = workingDirectory.trim();
            }
            
            const sessionId = await sessionApi.createGroupSession(request);
            
            dispatch('created', { sessionId });
            handleClose();
        } catch (err) {
            formError = err instanceof Error ? err.message : '创建群聊失败';
        } finally {
            isSubmitting = false;
        }
    }

    // Filter members by type
    $: aiMembers = members.filter(m => m.type === MemberType.AI);
    $: userMembers = members.filter(m => m.type === MemberType.User);
</script>

{#if isOpen}
    <div class="modal-overlay" on:click={handleClose}>
        <div class="modal-content" on:click|stopPropagation>
            <div class="modal-header">
                <h2>新建群聊</h2>
                <button class="btn-close" on:click={handleClose}>×</button>
            </div>

            <form on:submit|preventDefault={handleSubmit}>
                {#if formError}
                    <div class="error-message">
                        {formError}
                    </div>
                {/if}

                <div class="form-group">
                    <label for="title">会话标题 *</label>
                    <input
                        type="text"
                        id="title"
                        bind:value={title}
                        placeholder="输入会话标题"
                        disabled={isSubmitting}
                    />
                </div>

                <div class="form-group">
                    <label for="systemPrompt">系统提示词（可选）</label>
                    <textarea
                        id="systemPrompt"
                        bind:value={systemPrompt}
                        placeholder="输入系统提示词（可选，默认使用群聊助手提示词）"
                        rows="3"
                        disabled={isSubmitting}
                    />
                </div>

                <div class="form-group">
                    <label for="workingDirectory">工作目录（可选）</label>
                    <input
                        type="text"
                        id="workingDirectory"
                        bind:value={workingDirectory}
                        placeholder="输入工作目录路径，用于限定AI工具的文件操作范围"
                        disabled={isSubmitting}
                    />
                    <span class="field-hint">留空则使用系统临时目录</span>
                </div>

                {#if userMembers.length > 0}
                    <div class="form-group">
                        <label>选择用户成员（发起者）</label>
                        <div class="members-list">
                            {#each userMembers as member}
                                <label class="member-radio" class:selected={selectedUserMemberId === member.id}>
                                    <input
                                        type="radio"
                                        name="userMember"
                                        value={member.id}
                                        checked={selectedUserMemberId === member.id}
                                        on:change={() => selectUserMember(member.id)}
                                        disabled={isSubmitting}
                                    />
                                    <span class="member-info">
                                        <span class="member-name">{member.name}</span>
                                        <span class="member-type">用户</span>
                                    </span>
                                </label>
                            {/each}
                        </div>
                    </div>
                {/if}

                <div class="form-group">
                    <label>
                        选择AI成员 * (已选择 {selectedAiMemberIds.length} 个)
                    </label>
                    
                    {#if aiMembers.length > 0}
                        <div class="members-list">
                            {#each aiMembers as member}
                                <label class="member-checkbox" class:selected={selectedAiMemberIds.includes(member.id)}>
                                    <input
                                        type="checkbox"
                                        checked={selectedAiMemberIds.includes(member.id)}
                                        on:change={() => toggleAiMember(member.id)}
                                        disabled={isSubmitting}
                                    />
                                    <span class="member-info">
                                        <span class="member-name">{member.name}</span>
                                        <span class="member-meta">
                                            {member.roleName || '无角色'} | {member.model || '无模型'}
                                        </span>
                                    </span>
                                </label>
                            {/each}
                        </div>
                    {:else}
                        <p class="no-members">没有可用的AI成员，请先创建AI成员</p>
                    {/if}
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn-secondary" on:click={handleClose} disabled={isSubmitting}>
                        取消
                    </button>
                    <button type="submit" class="btn-primary" disabled={isSubmitting}>
                        {isSubmitting ? '创建中...' : '创建群聊'}
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
        max-width: 500px;
        max-height: 90vh;
        overflow-y: auto;
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
        margin-bottom: 1rem;
    }

    label {
        display: block;
        font-size: 0.875rem;
        font-weight: 500;
        color: #94a3b8;
        margin-bottom: 0.5rem;
    }

    input[type="text"],
    textarea {
        width: 100%;
        padding: 0.5rem 0.75rem;
        background-color: #0f172a;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        color: #f1f5f9;
        font-size: 0.875rem;
        transition: border-color 0.2s;
    }

    input:focus,
    textarea:focus {
        outline: none;
        border-color: #60a5fa;
    }

    input:disabled,
    textarea:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    textarea {
        resize: vertical;
        min-height: 80px;
    }

    .members-list {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        max-height: 200px;
        overflow-y: auto;
        padding: 0.5rem;
        background-color: #0f172a;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
    }

    .member-checkbox,
    .member-radio {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.75rem;
        border-radius: 0.375rem;
        cursor: pointer;
        transition: background-color 0.2s;
        border: 2px solid transparent;
    }

    .member-checkbox:hover,
    .member-radio:hover {
        background-color: rgba(96, 165, 250, 0.1);
    }

    .member-checkbox.selected,
    .member-radio.selected {
        background-color: rgba(96, 165, 250, 0.2);
        border-color: #60a5fa;
    }

    .member-checkbox input,
    .member-radio input {
        cursor: pointer;
        width: 1.25rem;
        height: 1.25rem;
        accent-color: #60a5fa;
    }

    .member-info {
        display: flex;
        flex-direction: column;
        gap: 0.125rem;
        flex: 1;
    }

    .member-name {
        font-size: 0.875rem;
        color: #f1f5f9;
        font-weight: 500;
    }

    .member-meta,
    .member-type {
        font-size: 0.75rem;
        color: #64748b;
    }

    .no-members {
        color: #94a3b8;
        font-size: 0.875rem;
        text-align: center;
        padding: 1rem;
    }

    .field-hint {
        display: block;
        font-size: 0.75rem;
        color: #64748b;
        margin-top: 0.25rem;
    }

    .modal-footer {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        margin-top: 1.5rem;
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