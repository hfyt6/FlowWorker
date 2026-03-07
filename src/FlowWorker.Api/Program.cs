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

// 配置 Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=flowworker.db"));

// 配置 HttpClient
builder.Services.AddHttpClient();

// 配置仓储层
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IApiConfigRepository, ApiConfigRepository>();

// 配置服务层
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IApiConfigService, ApiConfigService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();

// 配置数据库初始化服务
builder.Services.AddScoped<DatabaseInitializer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 应用数据库迁移并初始化数据
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
    await initializer.SeedDataAsync();
}

app.Run();