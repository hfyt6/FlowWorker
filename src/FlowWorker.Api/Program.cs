using FlowWorker.Core.Configuration;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Core.Services;
using FlowWorker.Infrastructure;
using FlowWorker.Infrastructure.OpenAI;
using FlowWorker.Infrastructure.Repositories;
using FlowWorker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog 日志
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/flowworker-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 配置 CORS，允许前端开发服务器访问 API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5121")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 配置 Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=flowworker.db"));

// 配置 HttpClient
builder.Services.AddHttpClient();

// 配置仓储层
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IApiConfigRepository, ApiConfigRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// 配置群聊选项
builder.Services.Configure<GroupChatOptions>(builder.Configuration.GetSection("GroupChat"));
builder.Services.AddScoped<GroupChatOptions>(sp => 
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GroupChatOptions>>().Value);

// 配置服务层
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IApiConfigService, ApiConfigService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IGroupChatService, GroupChatService>();

// 配置数据库初始化服务
builder.Services.AddScoped<DatabaseInitializer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 使用 CORS 中间件
app.UseCors("AllowFrontend");

// 使用静态文件中间件
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

// 映射控制器
app.MapControllers();

// 映射前端路由（处理前端路由的 fallback）
app.MapFallbackToFile("index.html");

// 应用数据库迁移并初始化数据
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
    await initializer.SeedDataAsync();
}

app.Run();
