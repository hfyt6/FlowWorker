using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowWorker.Infrastructure.Repositories;

/// <summary>
/// 消息仓储实现
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Message> _dbSet;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Message>();
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IReadOnlyList<Message>> GetAllAsync()
    {
        return await _dbSet
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Message>> GetByConditionAsync(Expression<Func<Message, bool>> predicate)
    {
        return await _dbSet
            .Where(predicate)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<Message?> GetFirstAsync(Expression<Func<Message, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task AddAsync(Message entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Message> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Message entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Message entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<Message> entities)
    {
        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<Message, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<Message, bool>>? predicate = null)
    {
        return predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);
    }

    public async Task<IReadOnlyList<Message>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _dbSet
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Message>> GetLastMessagesAsync(Guid sessionId, int count)
    {
        return await _dbSet
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteBySessionIdAsync(Guid sessionId)
    {
        var messages = await _dbSet
            .Where(m => m.SessionId == sessionId)
            .ToListAsync();
        
        _dbSet.RemoveRange(messages);
        await _context.SaveChangesAsync();
    }
}