/**
 * 成员类型枚举
 */
export enum MemberType {
    User = 0,
    AI = 1
}

/**
 * 成员状态枚举
 */
export enum MemberStatus {
    Offline = 0,
    Online = 1,
    Busy = 2
}

/**
 * 成员列表项
 */
export interface MemberListItemDto {
    id: string;
    name: string;
    type: MemberType;
    avatar: string | null;
    status: MemberStatus;
    createdAt: string;

    // AI类型特有字段
    roleId?: string;
    roleName?: string;
    roleDisplayName?: string;
    apiConfigName?: string;
    model?: string;
    temperature: number;
    maxTokens?: number | null;
}

/**
 * 成员详情
 */
export interface MemberDetailDto {
    id: string;
    name: string;
    type: MemberType;
    avatar: string | null;
    status: MemberStatus;
    createdAt: string;
    updatedAt: string;

    // AI类型特有字段
    roleId?: string;
    role?: RoleDetailDto;
    apiConfigId?: string;
    apiConfigName?: string;
    model?: string;
    temperature: number;
    maxTokens?: number | null;
}

/**
 * 创建AI成员请求
 */
export interface CreateAIMemberRequest {
    name: string;
    avatar?: string;
    roleId: string;
    apiConfigId: string;
    model?: string;
    temperature: number;
    maxTokens?: number;
}

/**
 * 更新成员请求
 */
export interface UpdateMemberRequest {
    name: string;
    avatar?: string;
    status?: MemberStatus;
    roleId?: string;
    apiConfigId?: string;
    model?: string;
    temperature?: number;
    maxTokens?: number;
}

/**
 * 角色详情
 */
export interface RoleDetailDto {
    id: string;
    name: string;
    displayName: string;
    description: string | null;
    systemPrompt: string;
    createdAt: string;
    updatedAt: string;
}