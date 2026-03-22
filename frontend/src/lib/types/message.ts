import { MessageRole } from './session';

export { MessageRole };

/**
 * 消息列表项
 */
export interface MessageListItemDto {
    id: string;
    role: MessageRole;
    content: string;
    tokens: number | null;
    model: string | null;
    createdAt: string;
}

/**
 * 创建消息请求
 */
export interface CreateMessageRequest {
    role: MessageRole;
    content: string;
    tokens: number | null;
    model: string | null;
}

/**
 * 发送消息响应
 */
export interface SendMessageResponse {
    content: string;
    model: string;
    tokens: number | null;
    isComplete: boolean;
}

/**
 * 流式响应内容块
 */
export interface StreamContentChunk {
    type: 'content' | 'tool_call' | 'complete' | 'error';
    content?: string;
    error?: string;
}