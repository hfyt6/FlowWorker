# FlowWorker 后端 API

FlowWorker 是一个基于 .NET 9.0 开发的 AI 对话管理后端服务，支持与 OpenAI 兼容的 API 进行集成，提供会话管理、消息管理、API 配置等功能。

## 功能特性

- **会话管理**：创建、查询、更新、删除对话会话，支持按标题搜索
- **消息管理**：管理会话中的消息，支持发送消息到 AI、重新生成回复
- **API 配置**：管理多个 OpenAI API 配置，支持设置默认配置
- **流式响应**：支持流式接收 AI 回复，实现实时对话体验
- **数据持久化**：使用 SQLite 数据库存储所有数据
- **日志记录**：基于 Serilog 的日志系统，支持按天滚动记录

## 技术栈

- **框架**：.NET 9.0 (ASP.NET Core)
- **数据库**：SQLite (Entity Framework Core 8.0)
- **日志**：Serilog
- **API 文档**：OpenAPI (Swagger)

## 项目结构

```
src/
├── FlowWorker.Api/          # API 项目（入口）
│   ├── Controllers/         # API 控制器
│   │   └── v1/             # v1 版本控制器
│   │       ├── ApiConfigsController.cs    # API 配置管理
│   │       ├── MessagesController.cs      # 消息管理
│   │       └── SessionsController.cs      # 会话管理
│   ├── Program.cs           # 程序入口
│   └── FlowWorker.Api.csproj
├── FlowWorker.Core/         # 核心业务逻辑层
│   ├── DTOs/                # 数据传输对象
│   ├── Interfaces/          # 接口定义
│   ├── Services/            # 服务实现
│   └── FlowWorker.Core.csproj
├── FlowWorker.Infrastructure/  # 基础设施层
│   ├── OpenAI/              # OpenAI 集成
│   ├── Repositories/        # 数据仓储实现
│   ├── Services/            # 数据库服务
│   ├── AppDbContext.cs      # 数据库上下文
│   └── FlowWorker.Infrastructure.csproj
├── FlowWorker.Shared/       # 共享代码
│   ├── DTOs/                # 共享 DTO
│   ├── Entities/            # 实体定义
│   └── FlowWorker.Shared.csproj
└── README.md                # 本文档
```

## API 端点

### 会话管理 (`/api/v1/sessions`)

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/v1/sessions` | 获取会话列表（可选 `apiConfigId` 参数） |
| GET | `/api/v1/sessions/{id}` | 获取会话详情 |
| POST | `/api/v1/sessions` | 创建新会话 |
| PUT | `/api/v1/sessions/{id}` | 更新会话 |
| DELETE | `/api/v1/sessions/{id}` | 删除会话 |
| GET | `/api/v1/sessions/search?title={title}` | 搜索会话 |

### 消息管理 (`/api/v1/sessions/{sessionId}/messages`)

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/v1/sessions/{sessionId}/messages` | 获取消息列表 |
| GET | `/api/v1/sessions/{sessionId}/messages/last?count={n}` | 获取最后 N 条消息 |
| POST | `/api/v1/sessions/{sessionId}/messages` | 创建消息 |
| POST | `/api/v1/sessions/{sessionId}/messages/send` | 发送消息到 AI |
| POST | `/api/v1/sessions/{sessionId}/messages/regenerate` | 重新生成回复 |
| DELETE | `/api/v1/sessions/{sessionId}/messages/{id}` | 删除消息 |

### API 配置管理 (`/api/v1/api-configs`)

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/v1/api-configs` | 获取配置列表 |
| GET | `/api/v1/api-configs/{id}` | 获取配置详情 |
| GET | `/api/v1/api-configs/default` | 获取默认配置 |
| POST | `/api/v1/api-configs` | 创建配置 |
| PUT | `/api/v1/api-configs/{id}` | 更新配置 |
| DELETE | `/api/v1/api-configs/{id}` | 删除配置 |
| POST | `/api/v1/api-configs/{id}/set-default` | 设置默认配置 |

## 启动项目

### 前置要求

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### 运行步骤

1. **导航到项目目录**
   ```bash
   cd src/FlowWorker.Api
   ```

2. **运行项目**
   ```bash
   dotnet run
   ```

3. **访问 API 文档**
   - 开发环境访问：`https://localhost:7001/swagger`（具体端口以控制台输出为准）

### 开发环境

默认使用 SQLite 数据库，数据库文件 `flowworker.db` 会自动在项目目录下创建。

### 数据库初始化

**首次启动时会自动执行数据库迁移和初始化**，无需手动操作。项目在启动时会自动：
1. 检查并应用所有挂起的数据库迁移
2. 初始化种子数据

如果需要手动执行数据库迁移，可以使用以下命令：
```bash
# 导航到项目目录
cd src/FlowWorker.Api

# 添加新的迁移
dotnet ef migrations add AddInitialSchema -c AppDbContext -p ../FlowWorker.Infrastructure

# 更新数据库到最新版本
dotnet ef database update -c AppDbContext -p ../FlowWorker.Infrastructure
```

## 配置说明

### 数据库配置

默认使用 SQLite 数据库，连接字符串在 `Program.cs` 中配置：
```csharp
options.UseSqlite("Data Source=flowworker.db")
```

如需修改数据库位置，可修改此连接字符串。

### 日志配置

日志文件默认保存在 `logs/flowworker-.log`，按天滚动，保留最近 30 天的日志。

## 使用示例

### 创建会话

```bash
curl -X POST https://localhost:7001/api/v1/sessions \
  -H "Content-Type: application/json" \
  -d '{
    "title": "我的会话",
    "apiConfigId": "your-api-config-id",
    "model": "gpt-3.5-turbo",
    "systemPrompt": "你是一个 helpful assistant",
    "temperature": 0.7,
    "maxTokens": 2000
  }'
```

### 发送消息

```bash
curl -X POST https://localhost:7001/api/v1/sessions/{sessionId}/messages/send \
  -H "Content-Type: application/json" \
  -d "你好，请介绍一下你自己"
```

### 创建 API 配置

```bash
curl -X POST https://localhost:7001/api/v1/api-configs \
  -H "Content-Type: application/json" \
  -d '{
    "name": "OpenAI 配置",
    "baseUrl": "https://api.openai.com/v1",
    "apiKey": "sk-xxxx",
    "model": "gpt-3.5-turbo",
    "isDefault": true
  }'
```

## 数据模型

### Session（会话）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 主键 |
| Title | string | 会话标题 |
| ApiConfigId | Guid | API 配置 ID |
| Model | string | 使用的模型 |
| SystemPrompt | string | 系统提示词 |
| Temperature | decimal | 生成温度 |
| MaxTokens | int? | 最大 Token 数 |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |

### Message（消息）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 主键 |
| SessionId | Guid | 会话 ID |
| Role | enum | 角色（User/Assistant/System/Tool） |
| Content | string | 消息内容 |
| Tokens | int? | 消耗的 Token 数 |
| Model | string? | 使用的模型 |
| CreatedAt | DateTime | 创建时间 |

### ApiConfig（API 配置）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 主键 |
| Name | string | 配置名称 |
| BaseUrl | string | API 基础 URL |
| ApiKey | string | API 密钥（加密存储） |
| Model | string | 默认模型 |
| IsDefault | bool | 是否为默认配置 |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |

## 扩展开发

### 添加新的 API 端点

1. 在 `FlowWorker.Core.Interfaces` 中定义服务接口
2. 在 `FlowWorker.Core.Services` 中实现服务逻辑
3. 在 `FlowWorker.Api.Controllers.v1` 中添加控制器
4. 在 `Program.cs` 中注册服务

### 数据库迁移

```bash
# 添加迁移
dotnet ef migrations add MigrationName -c AppDbContext -p ../FlowWorker.Infrastructure -s ../FlowWorker.Api

# 更新数据库
dotnet ef database update -c AppDbContext -p ../FlowWorker.Infrastructure -s ../FlowWorker.Api
```

## 许可证

MIT License