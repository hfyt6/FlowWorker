/**
 * API 服务层
 * 封装所有后端 API 调用
 */

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5121/api/v1';

/**
 * API 错误类
 */
export class ApiError extends Error {
    constructor(
        public message: string,
        public status?: number,
        public details?: any
    ) {
        super(message);
        this.name = 'ApiError';
    }
}

/**
 * 基础请求配置
 */
interface RequestOptions extends RequestInit {
    params?: Record<string, string | number | boolean | undefined>;
}

/**
 * 处理响应
 */
async function handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
        let errorDetails;
        try {
            errorDetails = await response.json();
        } catch {
            errorDetails = { message: response.statusText };
        }
        throw new ApiError(
            errorDetails.message || `HTTP error! status: ${response.status}`,
            response.status,
            errorDetails
        );
    }
    // 204 No Content 响应没有响应体，直接返回 undefined
    if (response.status === 204) {
        return undefined as T;
    }
    return response.json();
}

/**
 * GET 请求
 */
export async function get<T>(endpoint: string, options?: RequestOptions): Promise<T> {
    const url = buildUrl(endpoint, options?.params);
    const response = await fetch(url, {
        ...options,
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            ...options?.headers
        }
    });
    return handleResponse<T>(response);
}

/**
 * POST 请求
 */
export async function post<T>(endpoint: string, body?: any, options?: RequestOptions): Promise<T> {
    const url = buildUrl(endpoint, options?.params);
    const response = await fetch(url, {
        ...options,
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...options?.headers
        },
        body: body ? JSON.stringify(body) : undefined
    });
    return handleResponse<T>(response);
}

/**
 * PUT 请求
 */
export async function put<T>(endpoint: string, body?: any, options?: RequestOptions): Promise<T> {
    const url = buildUrl(endpoint, options?.params);
    const response = await fetch(url, {
        ...options,
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            ...options?.headers
        },
        body: body ? JSON.stringify(body) : undefined
    });
    return handleResponse<T>(response);
}

/**
 * DELETE 请求
 */
export async function del<T>(endpoint: string, options?: RequestOptions): Promise<T> {
    const url = buildUrl(endpoint, options?.params);
    const response = await fetch(url, {
        ...options,
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json',
            ...options?.headers
        }
    });
    return handleResponse<T>(response);
}

/**
 * 构建 URL
 */
function buildUrl(endpoint: string, params?: Record<string, string | number | boolean | undefined>): string {
    const url = new URL(`${API_BASE_URL}${endpoint}`, window.location.origin);
    
    if (params) {
        Object.entries(params).forEach(([key, value]) => {
            if (value !== undefined && value !== null) {
                url.searchParams.append(key, String(value));
            }
        });
    }
    
    return url.toString();
}

/**
 * SSE 流式请求
 */
export async function stream(endpoint: string, body?: any, options?: RequestOptions): Promise<ReadableStreamDefaultReader<Uint8Array>> {
    const url = buildUrl(endpoint, options?.params);
    
    const fetchOptions: RequestInit = {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...options?.headers
        },
        body: body ? JSON.stringify(body) : undefined
    };

    const response = await fetch(url, fetchOptions);
    
    if (!response.ok) {
        throw new ApiError(`HTTP error! status: ${response.status}`, response.status);
    }

    const reader = response.body?.getReader();
    if (!reader) {
        throw new ApiError('Failed to get response reader');
    }

    return reader;
}

/**
 * 会话 API
 */
export const sessionApi = {
    /**
     * 获取会话列表
     */
    getSessions: (apiConfigId?: string) => get<SessionListItemDto[]>('/sessions', { params: { apiConfigId } }),
    
    /**
     * 获取会话详情
     */
    getSession: (id: string) => get<SessionDetailDto>(`/sessions/${id}`),
    
    /**
     * 创建新会话
     */
    createSession: (data: CreateSessionRequest) => post<string>('/sessions', data),
    
    /**
     * 更新会话
     */
    updateSession: (id: string, data: UpdateSessionRequest) => put<void>(`/sessions/${id}`, data),
    
    /**
     * 删除会话
     */
    deleteSession: (id: string) => del<void>(`/sessions/${id}`),
    
    /**
     * 搜索会话
     */
    searchSessions: (title: string) => get<SessionListItemDto[]>('/sessions/search', { params: { title } }),

    // ========== 群聊相关 API ==========

    /**
     * 获取群聊会话列表
     */
    getGroupSessions: () => get<SessionListItemDto[]>('/sessions/group'),

    /**
     * 获取群聊会话详情
     */
    getGroupSession: (id: string) => get<GroupSessionDetailDto>(`/sessions/group/${id}`),

    /**
     * 创建群聊会话
     */
    createGroupSession: (data: CreateGroupSessionRequest) => post<string>('/sessions/group', data),

    /**
     * 添加参与者
     */
    addParticipant: (sessionId: string, memberId: string) => post<void>(`/sessions/group/${sessionId}/participants/${memberId}`, undefined),

    /**
     * 移除参与者
     */
    removeParticipant: (sessionId: string, memberId: string) => del<void>(`/sessions/group/${sessionId}/participants/${memberId}`)
};

/**
 * 消息 API
 */
export const messageApi = {
    /**
     * 获取消息列表
     */
    getMessages: (sessionId: string) => get<MessageListItemDto[]>(`/sessions/${sessionId}/messages`),
    
    /**
     * 获取最后 N 条消息
     */
    getLastMessages: (sessionId: string, count: number = 10) => get<MessageListItemDto[]>(`/sessions/${sessionId}/messages/last`, { params: { count } }),
    
    /**
     * 创建消息
     */
    createMessage: (sessionId: string, data: CreateMessageRequest) => post<string>(`/sessions/${sessionId}/messages`, data),
    
    /**
     * 删除消息
     */
    deleteMessage: (id: string) => del<void>(`/messages/${id}`),
    
    /**
     * 发送消息到 AI
     */
    sendMessage: (sessionId: string, content: string) => post<SendMessageResponse>(`/sessions/${sessionId}/messages/send`, { content }),
    
    /**
     * 重新生成回复
     */
    regenerateResponse: (sessionId: string) => post<SendMessageResponse>(`/sessions/${sessionId}/messages/regenerate`, undefined),
    
    /**
     * 流式发送消息到 AI
     */
    sendMessageStream: (sessionId: string, content: string, onChunk: (chunk: StreamContentChunk) => void) => 
        streamMessage(`/sessions/${sessionId}/messages/send-stream`, { content }, onChunk),
    
    /**
     * 流式重新生成回复
     */
    regenerateResponseStream: (sessionId: string, onChunk: (chunk: StreamContentChunk) => void) => 
        streamMessage(`/sessions/${sessionId}/messages/regenerate-stream`, undefined, onChunk),

    // ========== 群聊消息相关 API ==========

    /**
     * 发送群聊消息
     */
    sendGroupMessage: (sessionId: string, data: SendGroupMessageRequest) => 
        post<GroupMessageDto[]>(`/sessions/${sessionId}/messages/group`, data),

    /**
     * 流式发送群聊消息
     */
    sendGroupMessageStream: (sessionId: string, data: SendGroupMessageRequest, onChunk: (chunk: GroupStreamContentChunk) => void) => 
        streamGroupMessage(`/sessions/${sessionId}/messages/send-group-stream`, data, onChunk)
};

/**
 * 群聊流式消息处理
 */
async function streamGroupMessage(
    endpoint: string, 
    body: any, 
    onChunk: (chunk: GroupStreamContentChunk) => void
): Promise<void> {
    const url = buildUrl(endpoint, undefined);
    console.log('[API Group Stream] 开始流式请求，URL:', url);
    console.log('[API Group Stream] 请求体:', body);
    
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        },
        body: body ? JSON.stringify(body) : undefined
    });
    
    console.log('[API Group Stream] 响应状态:', response.status);
    
    if (!response.ok) {
        throw new ApiError(`HTTP error! status: ${response.status}`, response.status);
    }
    
    const reader = response.body?.getReader();
    if (!reader) {
        throw new ApiError('Failed to get response reader');
    }
    
    const decoder = new TextDecoder();
    let buffer = '';
    let chunkCount = 0;
    
    try {
        while (true) {
            const { done, value } = await reader.read();
            
            if (done) {
                console.log('[API Group Stream] 读取完成');
                break;
            }
            
            const decoded = decoder.decode(value, { stream: true });
            console.log('[API Group Stream] 收到原始数据:', decoded);
            buffer += decoded;
            
            // 处理 NDJSON 格式（每行一个 JSON 对象）
            const lines = buffer.split('\n');
            buffer = lines.pop() || ''; // 保留不完整的行
            
            for (const line of lines) {
                const trimmedLine = line.trim();
                if (!trimmedLine) continue;
                
                console.log('[API Group Stream] 处理行:', trimmedLine);
                
                try {
                    const chunk: GroupStreamContentChunk = JSON.parse(trimmedLine);
                    chunkCount++;
                    console.log(`[API Group Stream] 解析成功 chunk #${chunkCount}:`, chunk);
                    onChunk(chunk);
                    
                    if (chunk.type === 'group_complete') {
                        console.log('[API Group Stream] 收到 group_complete，结束流式处理');
                        return;
                    }
                } catch (e) {
                    console.warn('[API Group Stream] 解析 chunk 失败:', e);
                    console.warn('[API Group Stream] 失败的行:', trimmedLine);
                }
            }
        }
        
        // 处理剩余缓冲区
        if (buffer.trim()) {
            console.log('[API Group Stream] 处理剩余缓冲区:', buffer.trim());
            try {
                const chunk: GroupStreamContentChunk = JSON.parse(buffer.trim());
                chunkCount++;
                console.log(`[API Group Stream] 解析最终 chunk #${chunkCount}:`, chunk);
                onChunk(chunk);
            } catch (e) {
                console.warn('[API Group Stream] 解析最终 chunk 失败:', e);
            }
        }
        
        console.log(`[API Group Stream] 流式处理结束，共处理 ${chunkCount} 个 chunks`);
    } finally {
        reader.releaseLock();
    }
}

/**
 * 群聊流式响应内容块
 */
export interface GroupStreamContentChunk {
    type: 'content' | 'member_start' | 'member_complete' | 'user_message' | 'error' | 'group_complete';
    content?: string;
    messageId?: string;
    memberId?: string;
    memberName?: string;
    model?: string;
    finishReason?: string;
    error?: string;
}

/**
 * 流式消息处理
 */
async function streamMessage(
    endpoint: string, 
    body: any, 
    onChunk: (chunk: StreamContentChunk) => void
): Promise<SendMessageResponse> {
    const url = buildUrl(endpoint, undefined);
    
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        },
        body: body ? JSON.stringify(body) : undefined
    });
    
    if (!response.ok) {
        throw new ApiError(`HTTP error! status: ${response.status}`, response.status);
    }
    
    const reader = response.body?.getReader();
    if (!reader) {
        throw new ApiError('Failed to get response reader');
    }
    
    const decoder = new TextDecoder();
    let buffer = '';
    let fullContent = '';
    
    try {
        while (true) {
            const { done, value } = await reader.read();
            
            if (done) {
                break;
            }
            
            const decoded = decoder.decode(value, { stream: true });
            buffer += decoded;
            
            // 处理 NDJSON 格式（每行一个 JSON 对象）
            const lines = buffer.split('\n');
            buffer = lines.pop() || ''; // 保留不完整的行
            
            for (const line of lines) {
                const trimmedLine = line.trim();
                if (!trimmedLine) continue;
                
                try {
                    const chunk: StreamContentChunk = JSON.parse(trimmedLine);
                    
                    if (chunk.type === 'content' && chunk.content) {
                        fullContent += chunk.content;
                    }
                    
                    onChunk(chunk);
                    
                    if (chunk.type === 'complete') {
                        return {
                            content: chunk.content || fullContent,
                            model: '',
                            tokens: null,
                            isComplete: true
                        };
                    }
                } catch (e) {
                    console.warn('[API Stream] 解析 chunk 失败:', e);
                }
            }
        }
        
        // 处理剩余缓冲区
        if (buffer.trim()) {
            try {
                const chunk: StreamContentChunk = JSON.parse(buffer.trim());
                if (chunk.type === 'complete') {
                    return {
                        content: chunk.content || fullContent,
                        model: '',
                        tokens: null,
                        isComplete: true
                    };
                }
            } catch (e) {
                console.warn('[API Stream] 解析最终 chunk 失败:', e);
            }
        }
    } finally {
        reader.releaseLock();
    }
    
    return {
        content: fullContent,
        model: '',
        tokens: null,
        isComplete: true
    };
}

/**
 * API 配置 API
 */
export const apiConfigApi = {
    /**
     * 获取配置列表
     */
    getConfigs: () => get<ApiConfigListItemDto[]>('/api-configs'),
    
    /**
     * 获取配置详情
     */
    getConfig: (id: string) => get<ApiConfigDetailDto>(`/api-configs/${id}`),
    
    /**
     * 获取默认配置
     */
    getDefaultConfig: () => get<ApiConfigDetailDto>('/api-configs/default'),
    
    /**
     * 创建配置
     */
    createConfig: (data: CreateApiConfigRequest) => post<string>('/api-configs', data),
    
    /**
     * 更新配置
     */
    updateConfig: (id: string, data: UpdateApiConfigRequest) => put<void>(`/api-configs/${id}`, data),
    
    /**
     * 删除配置
     */
    deleteConfig: (id: string) => del<void>(`/api-configs/${id}`),
    
    /**
     * 设置默认配置
     */
    setDefaultConfig: (id: string) => post<void>(`/api-configs/${id}/set-default`, undefined)
};

// 类型导入（用于类型检查）
import type {
    SessionListItemDto,
    SessionDetailDto,
    CreateSessionRequest,
    UpdateSessionRequest,
    MessageListItemDto,
    CreateMessageRequest,
    SendMessageResponse,
    StreamContentChunk,
    ApiConfigListItemDto,
    ApiConfigDetailDto,
    CreateApiConfigRequest,
    UpdateApiConfigRequest,
    GroupSessionDetailDto,
    CreateGroupSessionRequest,
    GroupMessageDto,
    SendGroupMessageRequest
} from '../types';
