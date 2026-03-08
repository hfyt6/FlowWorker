using System.Text.Json;
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
        // 初始化内置角色
        await SeedBuiltInRolesAsync();

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

    private async Task SeedBuiltInRolesAsync()
    {
        var builtInRoles = new List<Role>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "assistant",
                DisplayName = "通用助手",
                Description = "通用的AI助手，可以回答各种问题",
                SystemPrompt = "你是一个有帮助的AI助手。请用简洁、准确的方式回答用户的问题。",
                AllowedTools = null,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "code_assistant",
                DisplayName = "代码助手",
                Description = "专注于编程和代码相关的AI助手",
                SystemPrompt = "你是一个专业的编程助手。你可以帮助用户编写、调试和优化代码。请提供清晰、可运行的代码示例，并解释关键概念。",
                AllowedTools = null,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "creative_writer",
                DisplayName = "创意写手",
                Description = "擅长创意写作和内容创作的AI助手",
                SystemPrompt = "你是一个有创意的写作助手。你可以帮助用户进行创意写作、故事创作、文案撰写等工作。请用生动、富有想象力的语言回应。",
                AllowedTools = null,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "analyst",
                DisplayName = "数据分析师",
                Description = "擅长数据分析和逻辑推理的AI助手",
                SystemPrompt = "你是一个数据分析专家。你可以帮助用户分析数据、识别模式、做出逻辑推理。请用结构化的方式呈现分析结果。",
                AllowedTools = null,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "teacher",
                DisplayName = "教育导师",
                Description = "擅长教育和知识传授的AI助手",
                SystemPrompt = "你是一个耐心的教育导师。你可以帮助用户学习新知识、理解复杂概念。请用循序渐进的方式解释，并提供例子帮助理解。",
                AllowedTools = null,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var role in builtInRoles)
        {
            if (!await _context.Roles.AnyAsync(r => r.Name == role.Name))
            {
                await _context.Roles.AddAsync(role);
            }
        }

        await _context.SaveChangesAsync();
    }
}
