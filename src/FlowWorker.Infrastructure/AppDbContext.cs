using Microsoft.EntityFrameworkCore;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Entity1> Entity1s { get; set; } = default!;
    public DbSet<Session> Sessions { get; set; } = default!;
    public DbSet<Message> Messages { get; set; } = default!;
    public DbSet<ApiConfig> ApiConfigs { get; set; } = default!;
    public DbSet<Member> Members { get; set; } = default!;
    public DbSet<Role> Roles { get; set; } = default!;
    public DbSet<SessionMember> SessionMembers { get; set; } = default!;
    public DbSet<PromptTemplate> PromptTemplates { get; set; } = default!;
    public DbSet<RolePromptConfig> RolePromptConfigs { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置 Entity1
        modelBuilder.Entity<Entity1>(entity =>
        {
            entity.ToTable("Entity1");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
        });

        // 配置 Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SystemPrompt).IsRequired();
            entity.Property(e => e.AllowedTools).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // 配置 Member
        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Members");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.Temperature).HasDefaultValue(0.7m);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Members)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.ApiConfig)
                .WithMany()
                .HasForeignKey(e => e.ApiConfigId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置 Session
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SystemPrompt).HasMaxLength(1000);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.ApiConfig)
                .WithMany(c => c.Sessions)
                .HasForeignKey(e => e.ApiConfigId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 SessionMember
        modelBuilder.Entity<SessionMember>(entity =>
        {
            entity.ToTable("SessionMembers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Session)
                .WithMany(s => s.SessionMembers)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Member)
                .WithMany(p => p.SessionMembers)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.SessionId, e.MemberId }).IsUnique();
        });

        // 配置 Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Member)
                .WithMany(p => p.Messages)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置 ApiConfig
        modelBuilder.Entity<ApiConfig>(entity =>
        {
            entity.ToTable("ApiConfigs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BaseUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ApiKey).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
        });

        // 配置 PromptTemplate
        modelBuilder.Entity<PromptTemplate>(entity =>
        {
            entity.ToTable("PromptTemplates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TemplateType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Variables).HasColumnType("json");
            entity.Property(e => e.IsBuiltIn).HasDefaultValue(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasIndex(e => new { e.Role, e.Name, e.TemplateType }).IsUnique();
        });

        // 配置 RolePromptConfig
        modelBuilder.Entity<RolePromptConfig>(entity =>
        {
            entity.ToTable("RolePromptConfigs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RoleId).IsRequired();
            entity.Property(e => e.CurrentMode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomInstructions).HasMaxLength(2000);
            entity.Property(e => e.MaxIterations).HasDefaultValue(10);
            entity.Property(e => e.TokenBudget).HasDefaultValue(4000);
            entity.Property(e => e.ToolWhitelist).HasColumnType("json");
            entity.Property(e => e.ToolBlacklist).HasColumnType("json");
            entity.Property(e => e.EnableChainOfThought).HasDefaultValue(true);
            entity.Property(e => e.EnableTaskProgress).HasDefaultValue(true);
            entity.Property(e => e.EnableSelfReflection).HasDefaultValue(false);
            entity.Property(e => e.AdvancedTechniques).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");

            entity.HasIndex(e => e.RoleId).IsUnique();
        });
    }
}
