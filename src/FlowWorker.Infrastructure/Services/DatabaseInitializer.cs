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
        // 确保数据库存在（如果不存在则创建）
        // EnsureCreated 会创建数据库和所有表，但不会使用迁移
        // 它会根据当前的模型创建数据库结构
        await _context.Database.EnsureCreatedAsync();

        // 检查并应用挂起的迁移
        // EnsureCreated 创建的数据库可能没有 __EFMigrationsHistory 表
        // 所以我们需要先检查是否有挂起的迁移
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