using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;
using FlowWorker.Core.Prompts;

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

        // 初始化默认用户成员
        await SeedDefaultUserMemberAsync();

        // 初始化内置提示词模板
        await SeedBuiltInPromptTemplatesAsync();

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

    private async Task SeedDefaultUserMemberAsync()
    {
        // 检查是否已存在名为"默认用户成员"的用户成员
        if (!await _context.Members.AnyAsync(m => m.Name == "默认用户成员" && m.Type == MemberType.User))
        {
            var defaultUserMember = new Member
            {
                Id = Guid.NewGuid(),
                Name = "默认用户成员",
                Type = MemberType.User,
                Status = MemberStatus.Online,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _context.Members.AddAsync(defaultUserMember);
            await _context.SaveChangesAsync();
        }
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
                Name = "coder",
                DisplayName = "代码工程师",
                Description = "资深全栈开发工程师，专注于代码实现、文件操作和工具调用",
                SystemPrompt = BuiltInPrompts.CoderSystemPrompt,
                AllowedTools = JsonSerializer.Serialize(new List<string> { "read_file", "write_file", "replace_in_file", "search_files", "list_files", "execute_command", "ask_followup_question" }),
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "ui-designer",
                DisplayName = "UI设计师",
                Description = "专业的UI/UX设计师，专注于界面设计、样式开发和用户体验优化",
                SystemPrompt = BuiltInPrompts.UIDesignerSystemPrompt,
                AllowedTools = null,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "architect",
                DisplayName = "架构师",
                Description = "资深软件架构师，专注于系统架构设计、技术选型和方案规划",
                SystemPrompt = BuiltInPrompts.ArchitectSystemPrompt,
                AllowedTools = null,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "reviewer",
                DisplayName = "代码审查专家",
                Description = "资深代码审查专家，专注于代码质量检查、问题识别和优化建议",
                SystemPrompt = BuiltInPrompts.ReviewerSystemPrompt,
                AllowedTools = null,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "general",
                DisplayName = "通用AI助手",
                Description = "通用AI助手，专注于信息查询、问题解答和日常对话",
                SystemPrompt = BuiltInPrompts.GeneralSystemPrompt,
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

    private async Task SeedBuiltInPromptTemplatesAsync()
    {
        var builtInTemplates = BuiltInPrompts.GetAllBuiltInPrompts();

        foreach (var template in builtInTemplates)
        {
            var existingTemplate = await _context.PromptTemplates
                .FirstOrDefaultAsync(t => t.Role == template.Role && 
                                          t.Name == template.Name && 
                                          t.TemplateType == template.TemplateType);

            if (existingTemplate == null)
            {
                // 新增内置模板
                await _context.PromptTemplates.AddAsync(template);
            }
            else if (existingTemplate.IsBuiltIn && existingTemplate.Version < template.Version)
            {
                // 更新内置模板（版本升级）
                existingTemplate.Content = template.Content;
                existingTemplate.Variables = template.Variables;
                existingTemplate.Description = template.Description;
                existingTemplate.Version = template.Version;
                existingTemplate.UpdatedAt = DateTime.UtcNow;
                _context.PromptTemplates.Update(existingTemplate);
            }
        }

        await _context.SaveChangesAsync();
    }
}
