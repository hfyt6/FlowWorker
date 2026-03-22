import { writable, type Writable } from 'svelte/store';
import type { SessionListItemDto, SessionDetailDto, CreateSessionRequest, UpdateSessionRequest } from '../types/session';
import { sessionApi } from '../services/api';

// 会话列表存储
export const sessions: Writable<SessionListItemDto[]> = writable([]);

// 当前选中的会话
export const currentSession: Writable<SessionDetailDto | null> = writable(null);

// 会话加载状态
export const loading: Writable<boolean> = writable(false);

// 错误信息
export const error: Writable<string | null> = writable(null);

// 搜索关键词
export const searchQuery: Writable<string> = writable('');

// 将后端 PascalCase 数据转换为前端 camelCase 格式
function mapSessionFromBackend(rawSession: any): SessionListItemDto {
    return {
        id: rawSession.Id || rawSession.id,
        title: rawSession.Title || rawSession.title,
        model: rawSession.Model || rawSession.model,
        createdAt: rawSession.CreatedAt || rawSession.createdAt,
        updatedAt: rawSession.UpdatedAt || rawSession.updatedAt,
        messageCount: rawSession.MessageCount ?? rawSession.messageCount
    };
}

// 将后端 PascalCase 数据转换为前端 camelCase 格式
function mapSessionDetailFromBackend(rawSession: any): SessionDetailDto {
    return {
        id: rawSession.Id || rawSession.id,
        title: rawSession.Title || rawSession.title,
        apiConfigId: rawSession.ApiConfigId || rawSession.apiConfigId,
        apiConfigName: rawSession.ApiConfigName || rawSession.apiConfigName,
        model: rawSession.Model || rawSession.model,
        systemPrompt: rawSession.SystemPrompt || rawSession.systemPrompt,
        temperature: rawSession.Temperature ?? rawSession.temperature,
        maxTokens: rawSession.MaxTokens ?? rawSession.maxTokens,
        createdAt: rawSession.CreatedAt || rawSession.createdAt,
        updatedAt: rawSession.UpdatedAt || rawSession.updatedAt,
        messages: rawSession.Messages || rawSession.messages || []
    };
}

// 创建新会话
export async function createSession(request: CreateSessionRequest): Promise<string> {
    loading.set(true);
    error.set(null);
    
    try {
        const id = await sessionApi.createSession(request);
        return id;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 更新会话
export async function updateSession(id: string, request: UpdateSessionRequest): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        await sessionApi.updateSession(id, request);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 删除会话
export async function deleteSession(id: string): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        await sessionApi.deleteSession(id);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 获取会话列表
export async function fetchSessions(apiConfigId?: string): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        const data = await sessionApi.getSessions(apiConfigId);
        // 映射后端数据格式到前端格式
        const mappedData = data.map(mapSessionFromBackend);
        sessions.set(mappedData);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
        loading.set(false);
    }
}

// 获取会话详情
export async function fetchSessionDetail(id: string): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        const data = await sessionApi.getSession(id);
        // 映射后端数据格式到前端格式
        const mappedData = mapSessionDetailFromBackend(data);
        currentSession.set(mappedData);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
        loading.set(false);
    }
}

// 搜索会话
export async function searchSessions(title: string): Promise<void> {
    loading.set(true);
    error.set(null);
    searchQuery.set(title);
    
    try {
        const data = await sessionApi.searchSessions(title);
        // 映射后端数据格式到前端格式
        const mappedData = data.map(mapSessionFromBackend);
        sessions.set(mappedData);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
        loading.set(false);
    }
}
