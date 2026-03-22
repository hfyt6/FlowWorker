/**
 * 会话类型
 */
export enum SessionType {
    Single = 0,
    Group = 1
}

/**
 * 会话类型名称映射
 */
export const SessionTypeNames: Record<SessionType, string> = {
    [SessionType.Single]: '单聊',
    [SessionType.Group]: '群聊'
};

/**
 * 会话列表项
 */
export interface SessionListItemDto {
    id: string;
    title: string;
    type: SessionType;
    model: string;
    createdAt: string;
    updatedAt: string;
    messageCount: number;
    memberCount?: number;
}

/**
 * 会话详情
 */
export interface SessionDetailDto {
    id: string;
    title: string;
    type: SessionType;
    apiConfigId: string;
    apiConfigName: string;
    model: string;
    systemPrompt: string;
    temperature: number;
    maxTokens: number | null;
    createdAt: string;
    updatedAt: string;
    messages: Message[];
    members?: SessionMemberDto[];  // 后端返回的是 members，不是 participants
}

/**
 * 创建会话请求（单聊）
 */
export interface CreateSessionRequest {
    title: string;
    memberId: string;
    workingDirectory?: string;
}

/**
 * 更新会话请求
 */
export interface UpdateSessionRequest {
    title: string;
    apiConfigId: string;
    model: string;
    systemPrompt: string;
    temperature: number;
    maxTokens: number | null;
}

/**
 * 消息角色类型
 */
export enum MessageRole {
    System = 'system',
    User = 'user',
    Assistant = 'assistant',
    Tool = 'tool'
}

/**
 * 消息实体
 */
export interface Message {
    id: string;
    sessionId: string;
    role: MessageRole;
    content: string;
    tokens: number | null;
    model: string | null;
    createdAt: string;
    metadata: string | null;
}

// ========== 群聊相关类型 ==========

/**
 * 群聊会话详情
 */
export interface GroupSessionDetailDto {
    id: string;
    title: string;
    apiConfigId: string;
    apiConfigName: string;
    model: string;
    systemPrompt: string;
    temperature: number;
    maxTokens: number | null;
    createdAt: string;
    updatedAt: string;
    participants: ParticipantDto[];
    messages: GroupMessageDto[];
}

/**
 * 参与者信息
 */
export interface ParticipantDto {
    memberId: string;
    memberName: string;
    memberType: number | string;  // 后端返回数字：0 = User, 1 = AI
    roleName: string;
    order: number;
    isActive: boolean;
}

/**
 * 群聊消息
 */
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

/**
 * 创建群聊会话请求
 */
export interface CreateGroupSessionRequest {
    title: string;
    createdBy: string;
    aiMemberIds: string[];
    systemPrompt?: string;
    workingDirectory?: string;
}

/**
 * 发送群聊消息请求
 */
export interface SendGroupMessageRequest {
    content: string;
    senderMemberId: string;
}

/**
 * 发送群聊消息结果
 */
export interface SendGroupMessageResult {
    messageId: string;
    responses: GroupMessageResponse[];
}

/**
 * 群聊消息响应
 */
export interface GroupMessageResponse {
    memberId: string;
    memberName: string;
    content: string;
    tokens: number | null;
}

/**
 * 群聊状态
 */
export interface GroupChatStateDto {
    isProcessing: boolean;
    currentMemberId: string | null;
    currentMemberName: string | null;
    processedCount: number;
    totalCount: number;
}

/**
 * 会话成员信息
 */
export interface SessionMemberDto {
    id: string;
    name: string;
    type: number | string;  // 后端返回数字：0 = User, 1 = AI
    avatar: string | null;
    status: string;
    roleName: string | null;
    roleDisplayName: string | null;
    joinedAt: string;
    isActive: boolean;
}
