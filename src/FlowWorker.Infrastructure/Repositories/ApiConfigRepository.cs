using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowWorker.Infrastructure.Repositories;

/// <summary>
/// API 配置仓储实现
/// </summary>
public class ApiConfigRepository : IApiConfigRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<ApiConfig> _dbSet;

    public ApiConfigRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<ApiConfig>();
    }

    public async Task<ApiConfig?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IReadOnlyList<ApiConfig>> GetAllAsync()
    {
        return await _dbSet
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ApiConfig>> GetByConditionAsync(Expression<Func<ApiConfig, bool>> predicate)
    {
        return await _dbSet
            .Where(predicate)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ApiConfig?> GetFirstAsync(Expression<Func<ApiConfig, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task AddAsync(ApiConfig entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<ApiConfig> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ApiConfig entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ApiConfig entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<ApiConfig> entities)
    {
        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<ApiConfig, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<ApiConfig, bool>>? predicate = null)
    {
        return predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);
    }

    public async Task<ApiConfig?> GetDefaultConfigAsync()
    {
        return await _dbSet
            .Where(c => c.IsDefault)
            .FirstOrDefaultAsync();
    }

    public async Task SetDefaultConfigAsync(Guid configId)
    {
        // 取消所有配置的默认状态
        var allConfigs = await _dbSet.ToListAsync();
        foreach (var config in allConfigs)
        {
            config.IsDefault = false;
            _dbSet.Update(config);
        }
        
        // 设置指定配置为默认
        var configToSet = await _dbSet.FindAsync(configId);
        if (configToSet != null)
        {
            configToSet.IsDefault = true;
            _dbSet.Update(configToSet);
            await _context.SaveChangesAsync();
        }
    }
}