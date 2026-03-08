using Microsoft.EntityFrameworkCore;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using System.Linq.Expressions;

namespace FlowWorker.Infrastructure.Repositories;

/// <summary>
/// 角色仓储实现
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _context;

    public RoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }

    public async Task<IReadOnlyList<Role>> GetByConditionAsync(Expression<Func<Role, bool>> predicate)
    {
        return await _context.Roles.Where(predicate).ToListAsync();
    }

    public async Task<Role?> GetFirstAsync(Expression<Func<Role, bool>> predicate)
    {
        return await _context.Roles.FirstOrDefaultAsync(predicate);
    }

    public async Task AddAsync(Role entity)
    {
        await _context.Roles.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Role> entities)
    {
        await _context.Roles.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Role entity)
    {
        _context.Roles.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Role entity)
    {
        _context.Roles.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<Role> entities)
    {
        _context.Roles.RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<Role, bool>> predicate)
    {
        return await _context.Roles.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<Role, bool>>? predicate = null)
    {
        return predicate == null
            ? await _context.Roles.CountAsync()
            : await _context.Roles.CountAsync(predicate);
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
    }

    public async Task<IReadOnlyList<Role>> GetBuiltInRolesAsync()
    {
        return await _context.Roles
            .Where(r => r.IsBuiltIn)
            .OrderBy(r => r.DisplayName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Role>> GetCustomRolesAsync()
    {
        return await _context.Roles
            .Where(r => !r.IsBuiltIn)
            .OrderBy(r => r.DisplayName)
            .ToListAsync();
    }

    public async Task<bool> IsRoleInUseAsync(Guid roleId)
    {
        return await _context.Members.AnyAsync(p => p.RoleId == roleId);
    }
}
