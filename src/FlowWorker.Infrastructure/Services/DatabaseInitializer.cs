using Microsoft.EntityFrameworkCore;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Infrastructure.Services;

public class DatabaseInitializer
{
    private readonly AppDbContext _context;

    public DatabaseInitializer(AppDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        // 检查并应用挂起的迁移
        var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await _context.Database.MigrateAsync();
        }
    }

    public async Task SeedDataAsync()
    {
        // 种子数据逻辑
        if (await _context.Entity1s.AnyAsync())
        {
            return;
        }

        var entities = new List<Entity1>
        {
            new Entity1 { Name = "Sample Entity 1", Description = "This is a sample entity" },
            new Entity1 { Name = "Sample Entity 2", Description = "This is another sample entity" }
        };

        await _context.Entity1s.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }
}