using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FlowWorker.Core.Services;

/// <summary>
/// API 配置服务实现
/// </summary>
public class ApiConfigService : IApiConfigService
{
    private readonly IApiConfigRepository _apiConfigRepository;

    public ApiConfigService(IApiConfigRepository apiConfigRepository)
    {
        _apiConfigRepository = apiConfigRepository;
    }

    public async Task<IReadOnlyList<ApiConfigListItemDto>> GetConfigsAsync()
    {
        var configs = await _apiConfigRepository.GetAllAsync();
        
        return configs.Select(c => new ApiConfigListItemDto
        {
            Id = c.Id,
            Name = c.Name,
            BaseUrl = c.BaseUrl,
            Model = c.Model,
            IsDefault = c.IsDefault,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<ApiConfigDetailDto?> GetConfigDetailAsync(Guid id)
    {
        var config = await _apiConfigRepository.GetByIdAsync(id);
        if (config == null)
        {
            return null;
        }

        return new ApiConfigDetailDto
        {
            Id = config.Id,
            Name = config.Name,
            BaseUrl = config.BaseUrl,
            ApiKey = config.ApiKey,
            Model = config.Model,
            IsDefault = config.IsDefault,
            CreatedAt = config.CreatedAt
        };
    }

    public async Task<ApiConfigDetailDto?> GetDefaultConfigAsync()
    {
        var config = await _apiConfigRepository.GetDefaultConfigAsync();
        if (config == null)
        {
            return null;
        }

        return new ApiConfigDetailDto
        {
            Id = config.Id,
            Name = config.Name,
            BaseUrl = config.BaseUrl,
            ApiKey = config.ApiKey,
            Model = config.Model,
            IsDefault = config.IsDefault,
            CreatedAt = config.CreatedAt
        };
    }

    public async Task<Guid> CreateConfigAsync(CreateApiConfigRequest request)
    {
        var config = new ApiConfig
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            ApiKey = request.ApiKey,
            Model = request.Model,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _apiConfigRepository.AddAsync(config);
        return config.Id;
    }

    public async Task UpdateConfigAsync(Guid id, UpdateApiConfigRequest request)
    {
        var config = await _apiConfigRepository.GetByIdAsync(id);
        if (config == null)
        {
            throw new InvalidOperationException($"配置 {id} 不存在");
        }

        config.Name = request.Name;
        config.BaseUrl = request.BaseUrl;
        config.ApiKey = request.ApiKey;
        config.Model = request.Model;
        config.IsDefault = request.IsDefault;
        config.UpdatedAt = DateTime.UtcNow;

        await _apiConfigRepository.UpdateAsync(config);
    }

    public async Task DeleteConfigAsync(Guid id)
    {
        var config = await _apiConfigRepository.GetByIdAsync(id);
        if (config == null)
        {
            throw new InvalidOperationException($"配置 {id} 不存在");
        }

        await _apiConfigRepository.DeleteAsync(config);
    }

    public async Task SetDefaultConfigAsync(Guid id)
    {
        await _apiConfigRepository.SetDefaultConfigAsync(id);
    }
}