using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowWorker.Infrastructure.Repositories;

/// <summary>
/// 会话仓储实现
/// </summary>
public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Session> _dbSet;

    public SessionRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Session>();
    }

    public async Task<Session?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(s => s.ApiConfig)
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IReadOnlyList<Session>> GetAllAsync()
    {
        return await _dbSet
            .Include(s => s.ApiConfig)
            .Include(s => s.Messages)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Session>> GetByConditionAsync(Expression<Func<Session, bool>> predicate)
    {
        return await _dbSet
            .Include(s => s.ApiConfig)
            .Include(s => s.Messages)
            .Where(predicate)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Session?> GetFirstAsync(Expression<Func<Session, bool>> predicate)
    {
        return await _dbSet
            .Include(s => s.ApiConfig)
            .FirstOrDefaultAsync(predicate);
    }

    public async Task AddAsync(Session entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Session> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Session entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Session entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<Session> entities)
    {
        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<Session, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<Session, bool>>? predicate = null)
    {
        return predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);
    }

    public async Task<IReadOnlyList<Session>> GetByApiConfigIdAsync(Guid apiConfigId)
    {
        return await _dbSet
            .Include(s => s.ApiConfig)
            .Include(s => s.Messages)
            .Where(s => s.ApiConfigId == apiConfigId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Session>> GetLatestSessionsAsync(int count)
    {
        return await _dbSet
            .Include(s => s.ApiConfig)
            .Include(s => s.Messages)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Session>> SearchAsync(string title)
    {
        return await _dbSet
            .Include(s => s.ApiConfig)
            .Include(s => s.Messages)
            .Where(s => s.Title.Contains(title))
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }
}