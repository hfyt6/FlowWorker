import { writable, type Writable } from 'svelte/store';
import type { MessageListItemDto, CreateMessageRequest, SendMessageResponse, StreamContentChunk } from '../types/message';
import { MessageRole } from '../types/message';
import { messageApi } from '../services/api';

// 消息列表存储
export const messages: Writable<MessageListItemDto[]> = writable([]);

// 当前发送的消息内容
export const currentMessage: Writable<string> = writable('');

// 当前流式响应内容
export const streamingContent: Writable<string> = writable('');

// 消息加载状态
export const loading: Writable<boolean> = writable(false);

// 流式响应状态
export const streaming: Writable<boolean> = writable(false);

// 错误信息
export const error: Writable<string | null> = writable(null);

// 当前会话 ID
export const sessionId: Writable<string | null> = writable(null);

// 将后端 PascalCase 数据转换为前端 camelCase 格式
function mapMessageFromBackend(rawMsg: any): MessageListItemDto {
    return {
        id: rawMsg.Id || rawMsg.id,
        role: rawMsg.Role ?? rawMsg.role,
        content: rawMsg.Content || rawMsg.content,
        tokens: rawMsg.Tokens ?? rawMsg.tokens,
        model: rawMsg.Model ?? rawMsg.model,
        createdAt: rawMsg.CreatedAt || rawMsg.createdAt
    };
}

// 获取消息列表
export async function fetchMessages(sessionId: string): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        const data = await messageApi.getMessages(sessionId);
        // 映射后端数据格式到前端格式
        const mappedData = data.map(mapMessageFromBackend);
        messages.set(mappedData);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
        loading.set(false);
    }
}

// 发送消息
export async function sendMessage(sessionId: string, content: string): Promise<SendMessageResponse> {
    loading.set(true);
    error.set(null);
    
    try {
        const response = await messageApi.sendMessage(sessionId, content);
        return response;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 重新生成回复
export async function regenerateResponse(sessionId: string): Promise<SendMessageResponse> {
    loading.set(true);
    error.set(null);
    
    try {
        const response = await messageApi.regenerateResponse(sessionId);
        return response;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 删除消息
export async function deleteMessage(id: string): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        await messageApi.deleteMessage(id);
        // 从列表中移除
        messages.update(currentMessages => currentMessages.filter(m => m.id !== id));
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 流式发送消息
export async function sendMessageStream(
    sessionId: string, 
    content: string,
    onChunk?: (chunk: StreamContentChunk) => void
): Promise<SendMessageResponse> {
    streaming.set(true);
    streamingContent.set('');
    error.set(null);
    
    try {
        const response = await messageApi.sendMessageStream(sessionId, content, (chunk) => {
            if (chunk.type === 'content' && chunk.content) {
                streamingContent.update(current => current + chunk.content);
            }
            onChunk?.(chunk);
        });
        return response;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        streaming.set(false);
        streamingContent.set('');
    }
}

// 流式重新生成回复
export async function regenerateResponseStream(
    sessionId: string,
    onChunk?: (chunk: StreamContentChunk) => void
): Promise<SendMessageResponse> {
    streaming.set(true);
    streamingContent.set('');
    error.set(null);
    
    try {
        const response = await messageApi.regenerateResponseStream(sessionId, (chunk) => {
            if (chunk.type === 'content' && chunk.content) {
                streamingContent.update(current => current + chunk.content);
            }
            onChunk?.(chunk);
        });
        return response;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        streaming.set(false);
        streamingContent.set('');
    }
}
