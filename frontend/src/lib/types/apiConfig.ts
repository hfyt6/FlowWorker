/**
 * API 配置列表项
 */
export interface ApiConfigListItemDto {
    id: string;  // Guid 类型序列化为字符串
    name: string;
    baseUrl: string;
    model: string;
    isDefault: boolean;
    createdAt: string;
}

/**
 * API 配置详情
 */
export interface ApiConfigDetailDto {
    id: string;
    name: string;
    baseUrl: string;
    apiKey: string;
    model: string;
    isDefault: boolean;
    createdAt: string;
}

/**
 * 创建 API 配置请求
 */
export interface CreateApiConfigRequest {
    name: string;
    baseUrl: string;
    apiKey: string;
    model: string;
    isDefault: boolean;
}

/**
 * 更新 API 配置请求
 */
export interface UpdateApiConfigRequest {
    name: string;
    baseUrl: string;
    apiKey: string;
    model: string;
    isDefault: boolean;
}