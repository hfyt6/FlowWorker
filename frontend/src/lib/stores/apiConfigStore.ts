import { writable, type Writable } from 'svelte/store';
import type { ApiConfigListItemDto, ApiConfigDetailDto, CreateApiConfigRequest, UpdateApiConfigRequest } from '../types/apiConfig';
import { apiConfigApi } from '../services/api';

// API 配置列表存储
export const apiConfigs: Writable<ApiConfigListItemDto[]> = writable([]);

// 当前选中的配置
export const currentConfig: Writable<ApiConfigDetailDto | null> = writable(null);

// API 配置加载状态
export const loading: Writable<boolean> = writable(false);

// 错误信息
export const error: Writable<string | null> = writable(null);

// 将后端 PascalCase 数据转换为前端 camelCase 格式
function mapApiConfigFromBackend(rawConfig: any): ApiConfigListItemDto {
    return {
        id: rawConfig.Id || rawConfig.id,
        name: rawConfig.Name || rawConfig.name,
        baseUrl: rawConfig.BaseUrl || rawConfig.baseUrl,
        model: rawConfig.Model || rawConfig.model,
        isDefault: rawConfig.IsDefault ?? rawConfig.isDefault,
        createdAt: rawConfig.CreatedAt || rawConfig.createdAt
    };
}

// 将后端 PascalCase 数据转换为前端 camelCase 格式
function mapApiConfigDetailFromBackend(rawConfig: any): ApiConfigDetailDto {
    return {
        id: rawConfig.Id || rawConfig.id,
        name: rawConfig.Name || rawConfig.name,
        baseUrl: rawConfig.BaseUrl || rawConfig.baseUrl,
        apiKey: rawConfig.ApiKey || rawConfig.apiKey,
        model: rawConfig.Model || rawConfig.model,
        isDefault: rawConfig.IsDefault ?? rawConfig.isDefault,
        createdAt: rawConfig.CreatedAt || rawConfig.createdAt
    };
}

// 获取配置列表
export async function fetchConfigs(): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        const data = await apiConfigApi.getConfigs();
        // 映射后端数据格式到前端格式
        const mappedData = data.map(mapApiConfigFromBackend);
        apiConfigs.set(mappedData);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
        loading.set(false);
    }
}

// 获取配置详情
export async function fetchConfigDetail(id: string): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        const data = await apiConfigApi.getConfig(id);
        // 映射后端数据格式到前端格式
        const mappedData = mapApiConfigDetailFromBackend(data);
        currentConfig.set(mappedData);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
        loading.set(false);
    }
}

// 获取默认配置
export async function fetchDefaultConfig(): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        const data = await apiConfigApi.getDefaultConfig();
        // 映射后端数据格式到前端格式
        const mappedData = mapApiConfigDetailFromBackend(data);
        currentConfig.set(mappedData);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
    } finally {
        loading.set(false);
    }
}

// 创建配置
export async function createConfig(request: CreateApiConfigRequest): Promise<string> {
    loading.set(true);
    error.set(null);
    
    try {
        const id = await apiConfigApi.createConfig(request);
        return id;
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 更新配置
export async function updateConfig(id: string, request: UpdateApiConfigRequest): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        await apiConfigApi.updateConfig(id, request);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 删除配置
export async function deleteConfig(id: string): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        await apiConfigApi.deleteConfig(id);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}

// 设置默认配置
export async function setDefaultConfig(id: string): Promise<void> {
    loading.set(true);
    error.set(null);
    
    try {
        await apiConfigApi.setDefaultConfig(id);
    } catch (err) {
        error.set(err instanceof Error ? err.message : 'Unknown error');
        throw err;
    } finally {
        loading.set(false);
    }
}