import { get, post, put, del } from './api';
import type { MemberListItemDto, MemberDetailDto, CreateMemberRequest, UpdateMemberRequest } from '$lib/types/member';

/**
 * 成员 API
 */
export const memberApi = {
    /**
     * 获取成员列表
     */
    getMembers: () => get<MemberListItemDto[]>('/members'),

    /**
     * 获取成员详情
     */
    getMember: (id: string) => get<MemberDetailDto>(`/members/${id}`),

    /**
     * 创建成员
     */
    createMember: (data: CreateMemberRequest) => post<string>('/members', data),

    /**
     * 更新成员
     */
    updateMember: (id: string, data: UpdateMemberRequest) => put<void>(`/members/${id}`, data),

    /**
     * 删除成员
     */
    deleteMember: (id: string) => del<void>(`/members/${id}`)
};
