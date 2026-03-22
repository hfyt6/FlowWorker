export * from './session';
export * from './message';
export * from './apiConfig';
export * from './member';

// 群聊相关类型
export interface GroupMessageDto {
    id: string;
    sessionId: string;
    senderId: string;
    senderName: string;
    senderType: string;
    content: string;
    tokens: number | null;
    model: string | null;
    createdAt: string;
}

export interface SendGroupMessageRequest {
    content: string;
    senderMemberId: string;
}
