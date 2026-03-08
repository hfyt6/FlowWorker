using Microsoft.EntityFrameworkCore;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;
using System.Linq.Expressions;

namespace FlowWorker.Infrastructure.Repositories;

/// <summary>
/// 成员仓储实现
/// </summary>
public class MemberRepository : IMemberRepository
{
    private readonly AppDbContext _context;

    public MemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Member?> GetByIdAsync(Guid id)
    {
        return await _context.Members
            .Include(p => p.Role)
            .Include(p => p.ApiConfig)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Member>> GetAllAsync()
    {
        return await _context.Members
            .Include(p => p.Role)
            .Include(p => p.ApiConfig)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Member>> GetByConditionAsync(Expression<Func<Member, bool>> predicate)
    {
        return await _context.Members
            .Include(p => p.Role)
            .Include(p => p.ApiConfig)
            .Where(predicate)
            .ToListAsync();
    }

    public async Task<Member?> GetFirstAsync(Expression<Func<Member, bool>> predicate)
    {
        return await _context.Members
            .Include(p => p.Role)
            .Include(p => p.ApiConfig)
            .FirstOrDefaultAsync(predicate);
    }

    public async Task AddAsync(Member entity)
    {
        await _context.Members.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Member> entities)
    {
        await _context.Members.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Member entity)
    {
        _context.Members.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Member entity)
    {
        _context.Members.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<Member> entities)
    {
        _context.Members.RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<Member, bool>> predicate)
    {
        return await _context.Members.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<Member, bool>>? predicate = null)
    {
        return predicate == null
            ? await _context.Members.CountAsync()
            : await _context.Members.CountAsync(predicate);
    }

    public async Task<IReadOnlyList<Member>> GetByTypeAsync(MemberType type)
    {
        return await _context.Members
            .Include(p => p.Role)
            .Include(p => p.ApiConfig)
            .Where(p => p.Type == type)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Member>> GetByRoleIdAsync(Guid roleId)
    {
        return await _context.Members
            .Include(p => p.Role)
            .Include(p => p.ApiConfig)
            .Where(p => p.RoleId == roleId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Member>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _context.SessionMembers
            .Where(sp => sp.SessionId == sessionId && sp.IsActive)
            .Include(sp => sp.Member)
            .ThenInclude(p => p.Role)
            .Include(sp => sp.Member)
            .ThenInclude(p => p.ApiConfig)
            .Select(sp => sp.Member)
            .Where(p => p != null)
            .Cast<Member>()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Member>> GetAIMembersWithRoleAsync()
    {
        return await _context.Members
            .Include(p => p.Role)
            .Include(p => p.ApiConfig)
            .Where(p => p.Type == MemberType.AI)
            .ToListAsync();
    }
}
