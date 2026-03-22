<script lang="ts">
    import { onMount, type ComponentProps } from 'svelte';
    import type { StreamContentChunk } from '../types/message';
    import { stream } from '../services/api';

    // Props
    export let sessionId: string;
    export let content: string = '';
    export let onStreamComplete: () => void = () => {};
    export let onError: (error: string) => void = () => {};

    // State
    let isStreaming: boolean = false;
    let streamError: string | null = null;

    // 解析 SSE 流
    async function parseStream(reader: ReadableStreamDefaultReader<Uint8Array>) {
        const decoder = new TextDecoder();
        let buffer = '';

        try {
            while (true) {
                const { value, done } = await reader.read();
                
                if (done) {
                    break;
                }

                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop() || '';

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6);
                        
                        if (data === '[DONE]') {
                            isStreaming = false;
                            onStreamComplete();
                            return;
                        }

                        try {
                            const chunk: StreamContentChunk = JSON.parse(data);
                            handleChunk(chunk);
                        } catch (e) {
                            console.error('Failed to parse chunk:', e);
                        }
                    }
                }
            }
        } catch (error) {
            streamError = error instanceof Error ? error.message : 'Unknown error';
            onError(streamError);
        } finally {
            isStreaming = false;
        }
    }

    // 处理流式数据块
    function handleChunk(chunk: StreamContentChunk) {
        switch (chunk.type) {
            case 'content':
                if (chunk.content) {
                    content += chunk.content;
                }
                break;
            case 'error':
                if (chunk.error) {
                    streamError = chunk.error;
                    onError(chunk.error);
                }
                break;
            case 'complete':
                isStreaming = false;
                onStreamComplete();
                break;
            case 'tool_call':
                // TODO: 处理工具调用
                console.log('Tool call:', chunk.content);
                break;
        }
    }

    // 开始流式请求
    async function startStream() {
        if (!sessionId) {
            return;
        }

        isStreaming = true;
        streamError = null;

        try {
            const reader = await stream(`/sessions/${sessionId}/messages/send`, content);
            parseStream(reader);
        } catch (error) {
            streamError = error instanceof Error ? error.message : 'Unknown error';
            onError(streamError);
            isStreaming = false;
        }
    }

    // 组件挂载时开始流式请求
    onMount(() => {
        if (content) {
            startStream();
        }
    });

    // 导出方法供外部调用
    export function stopStream() {
        // TODO: 实现停止流式请求
    }
</script>

<div class="stream-message">
    {#if isStreaming}
        <div class="streaming-indicator">
            <span class="dot"></span>
            <span class="dot"></span>
            <span class="dot"></span>
        </div>
    {/if}

    {#if streamError}
        <div class="error-message">
            <span class="error-icon">⚠️</span>
            {streamError}
        </div>
    {/if}

    <div class="message-content">
        {@html content}
    </div>
</div>

<style>
    .stream-message {
        position: relative;
        padding: 1rem;
        background-color: #f5f5f5;
        border-radius: 0.5rem;
        margin-bottom: 1rem;
    }

    .streaming-indicator {
        display: flex;
        gap: 0.25rem;
        margin-bottom: 0.5rem;
    }

    .dot {
        width: 0.5rem;
        height: 0.5rem;
        background-color: #3b82f6;
        border-radius: 50%;
        animation: bounce 0.6s infinite ease-in-out;
    }

    .dot:nth-child(1) {
        animation-delay: -0.2s;
    }

    .dot:nth-child(2) {
        animation-delay: -0.1s;
    }

    @keyframes bounce {
        0%, 100% {
            transform: translateY(0);
        }
        50% {
            transform: translateY(-0.25rem);
        }
    }

    .error-message {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.75rem;
        background-color: #fee2e2;
        border-radius: 0.5rem;
        color: #991b1b;
    }

    .error-icon {
        font-size: 1.25rem;
    }

    .message-content {
        line-height: 1.6;
        color: #1f2937;
    }

    .message-content :global(p) {
        margin-bottom: 0.5rem;
    }

    .message-content :global(pre) {
        background-color: #1e1e1e;
        color: #d4d4d4;
        padding: 1rem;
        border-radius: 0.5rem;
        overflow-x: auto;
    }

    .message-content :global(code) {
        font-family: 'Courier New', Courier, monospace;
        background-color: #f0f0f0;
        padding: 0.2rem 0.4rem;
        border-radius: 0.25rem;
    }
</style>