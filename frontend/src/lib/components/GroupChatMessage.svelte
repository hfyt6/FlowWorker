<script lang="ts">
    import type { GroupMessageDto } from '$lib/types';

    export let message: GroupMessageDto;

    // Determine message type based on sender
    $: isUser = message.senderType === 'User';
    $: isSystem = message.senderType === 'System';
    $: isAI = !isUser && !isSystem;

    // Format timestamp
    $: formattedTime = new Date(message.createdAt).toLocaleTimeString('zh-CN', {
        hour: '2-digit',
        minute: '2-digit'
    });

    // Get avatar color based on sender name
    function getAvatarColor(name: string): string {
        const colors = [
            '#3b82f6', '#22d3ee', '#a78bfa', '#f472b6', '#fbbf24',
            '#34d399', '#f87171', '#60a5fa', '#c084fc', '#fb923c'
        ];
        let hash = 0;
        for (let i = 0; i < name.length; i++) {
            hash = name.charCodeAt(i) + ((hash << 5) - hash);
        }
        return colors[Math.abs(hash) % colors.length];
    }

    $: avatarColor = getAvatarColor(message.senderName);
    $: avatarInitial = message.senderName.charAt(0).toUpperCase();
</script>

<div class="message" class:user={isUser} class:system={isSystem} class:ai={isAI}>
    {#if isSystem}
        <div class="system-message">
            {message.content}
        </div>
    {:else}
        <div class="message-avatar" style="background-color: {avatarColor}">
            {avatarInitial}
        </div>
        <div class="message-content">
            <div class="message-header">
                <span class="sender-name">{message.senderName}</span>
                <span class="message-time">{formattedTime}</span>
                {#if message.model}
                    <span class="model-info">{message.model}</span>
                {/if}
            </div>
            <div class="message-body">
                {message.content}
            </div>
            {#if message.tokens}
                <div class="message-footer">
                    <span class="tokens">{message.tokens} tokens</span>
                </div>
            {/if}
        </div>
    {/if}
</div>

<style>
    .message {
        display: flex;
        gap: 0.75rem;
        padding: 0.75rem;
        border-radius: 0.75rem;
        transition: background-color 0.2s;
    }

    .message:hover {
        background-color: rgba(96, 165, 250, 0.05);
    }

    .message.user {
        flex-direction: row-reverse;
    }

    .message.user .message-content {
        align-items: flex-end;
    }

    .message.user .message-header {
        flex-direction: row-reverse;
    }

    .message.user .message-body {
        background-color: #3b82f6;
        color: white;
        border-radius: 0.75rem 0.75rem 0.25rem 0.75rem;
    }

    .message.ai .message-body {
        background-color: #1e293b;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.75rem 0.75rem 0.75rem 0.25rem;
    }

    .message-avatar {
        width: 2.5rem;
        height: 2.5rem;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 600;
        font-size: 1rem;
        color: white;
        flex-shrink: 0;
    }

    .message-content {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        max-width: 70%;
    }

    .message-header {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.75rem;
    }

    .sender-name {
        font-weight: 600;
        color: #f1f5f9;
    }

    .message-time {
        color: #64748b;
    }

    .model-info {
        color: #60a5fa;
        background-color: rgba(96, 165, 250, 0.1);
        padding: 0.125rem 0.375rem;
        border-radius: 0.25rem;
        font-size: 0.625rem;
    }

    .message-body {
        padding: 0.75rem 1rem;
        color: #f1f5f9;
        font-size: 0.875rem;
        line-height: 1.5;
        white-space: pre-wrap;
        word-break: break-word;
    }

    .message-footer {
        font-size: 0.625rem;
        color: #64748b;
    }

    .system-message {
        width: 100%;
        text-align: center;
        padding: 0.5rem;
        color: #94a3b8;
        font-size: 0.875rem;
        font-style: italic;
    }

    @media (max-width: 640px) {
        .message-content {
            max-width: 85%;
        }

        .message-avatar {
            width: 2rem;
            height: 2rem;
            font-size: 0.875rem;
        }
    }
</style>
