using Microsoft.EntityFrameworkCore;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Infrastructure.Repositories;

/// <summary>
/// 提示词模板仓库实现
/// </summary>
public class PromptTemplateRepository : IPromptTemplateRepository
{
    private readonly AppDbContext _context;

    public PromptTemplateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PromptTemplate>> GetAllAsync()
    {
        return await _context.Set<PromptTemplate>()
            .OrderBy(t => t.Role)
            .ThenBy(t => t.TemplateType)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PromptTemplate>> GetByRoleAsync(string role)
    {
        return await _context.Set<PromptTemplate>()
            .Where(t => t.Role == role)
            .OrderBy(t => t.TemplateType)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PromptTemplate>> GetByRoleAndTypeAsync(string role, string templateType)
    {
        return await _context.Set<PromptTemplate>()
            .Where(t => t.Role == role && t.TemplateType == templateType)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<PromptTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.Set<PromptTemplate>().FindAsync(id);
    }

    public async Task<PromptTemplate?> GetByRoleAndNameAsync(string role, string name)
    {
        return await _context.Set<PromptTemplate>()
            .FirstOrDefaultAsync(t => t.Role == role && t.Name == name);
    }

    public async Task<PromptTemplate?> GetSystemTemplateByRoleAsync(string role)
    {
        return await _context.Set<PromptTemplate>()
            .FirstOrDefaultAsync(t => t.Role == role && t.TemplateType == "system" && t.Name == "system");
    }

    public async Task AddAsync(PromptTemplate template)
    {
        await _context.Set<PromptTemplate>().AddAsync(template);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PromptTemplate template)
    {
        _context.Set<PromptTemplate>().Update(template);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(PromptTemplate template)
    {
        _context.Set<PromptTemplate>().Remove(template);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Func<PromptTemplate, bool> predicate)
    {
        return await Task.FromResult(_context.Set<PromptTemplate>().Any(predicate));
    }

    public async Task<IReadOnlyList<PromptTemplate>> GetBuiltInTemplatesAsync()
    {
        return await _context.Set<PromptTemplate>()
            .Where(t => t.IsBuiltIn)
            .OrderBy(t => t.Role)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PromptTemplate>> GetCustomTemplatesAsync()
    {
        return await _context.Set<PromptTemplate>()
            .Where(t => !t.IsBuiltIn)
            .OrderBy(t => t.Role)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }
}
