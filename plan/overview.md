# AI 对话工具开发计划

## 项目概述

开发一个基于 OpenAI 接口协议的 AI 对话工具，支持多会话管理和会话记录存储。

---

## 一、需求分析

### 1.1 功能需求

#### 会话管理
- **创建会话**：用户可以创建新的对话会话，设置会话标题、选择 AI 模型、配置系统提示词
- **会话列表**：展示所有会话，支持按时间排序、搜索和筛选
- **会话详情**：查看会话中的完整对话记录
- **会话编辑**：修改会话标题、模型配置、系统提示词等属性
- **会话删除**：删除不需要的会话及其关联的所有消息
- **会话导出**：将会话记录导出为 JSON、Markdown 或 TXT 格式
- **会话导入**：从导出的文件中恢复会话数据

#### 消息管理
- **发送消息**：向 AI 发送用户消息，支持流式和非流式两种模式
- **消息历史**：查看当前会话的完整消息历史
- **消息删除**：删除单条消息
- **重新生成**：请求 AI 重新生成上一条回复
- **停止生成**：在流式响应过程中可以中断生成

#### AI 配置管理
- **多 API 配置**：支持配置多个 OpenAI 兼容的 API 服务（如 OpenAI、Azure OpenAI、本地部署等）
- **模型切换**：在不同会话中使用不同的 AI 模型
- **参数配置**：支持配置 temperature、max_tokens 等生成参数
- **默认配置**：设置默认的 API 配置和模型

#### 用户体验
- **实时响应**：支持 SSE 流式输出，实时显示 AI 回复
- **加载状态**：清晰显示请求处理中的状态
- **错误处理**：友好的错误提示和重试机制
- **响应式布局**：适配桌面和移动端

### 1.2 非功能需求

#### 性能要求
- API 响应时间 < 200ms（不含 AI 生成时间）
- 支持至少 100 个并发会话
- 消息列表加载时间 < 1 秒（1000 条消息以内）

#### 数据安全
- API Key 加密存储
- 敏感数据脱敏处理
- 支持数据备份和恢复

#### 可维护性
- 模块化设计，便于功能扩展
- 完整的日志记录
- 单元测试覆盖率 > 80%

#### 兼容性
- 支持 OpenAI 官方 API
- 支持兼容 OpenAI 协议的第三方服务（如 Azure OpenAI、Ollama、vLLM 等）

---

## 二、技术选型

### 1. 后端技术栈（.NET）

| 技术/框架 | 版本 | 说明 |
|-----------|------|------|
| **.NET** | 8.0+ | 主要开发框架 |
| **ASP.NET Core Web API** | 8.0+ | Web API 框架 |
| **Entity Framework Core** | 8.0+ | ORM 框架 |
| **SQLite / PostgreSQL** | - | 数据库（开发用 SQLite，生产用 PostgreSQL） |
| **Dapper** | - | 轻量级数据访问（可选） |
| **FluentValidation** | - | 数据验证 |
| **AutoMapper** | - | 对象映射 |
| **Serilog** | - | 结构化日志 |
| **xUnit** | - | 单元测试框架 |
| **Moq** | - | 模拟框架 |

### 2. 前端技术栈（Svelte）

| 技术/框架 | 说明 |
|-----------|------|
| **Svelte** | 前端框架 |
| **SvelteKit** | 应用框架（路由、SSR 等） |
| **TypeScript** | 类型安全 |
| **TailwindCSS** | UI 样式 |
| **Svelte Store** | 状态管理 |
| **@tanstack/svelte-query** | 服务端状态管理 |

### 3. 开发工具

| 工具 | 说明 |
|------|------|
| **Visual Studio / Rider** | 后端 IDE |
| **VS Code** | 前端 IDE |
| **Git** | 版本控制 |
| **Docker** | 容器化部署 |
| **GitHub Actions / Azure DevOps** | CI/CD |

---

## 三、系统架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                        客户端层                              │
│  ┌─────────────────────────────────────────────────────────┐│
│  │              Svelte Web Application                     ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        API 层                                │
│  ┌─────────────────────────────────────────────────────────┐│
│  │           ASP.NET Core Web API                          ││
│  │  - Controllers  - Middleware  - Filters  - Swagger      ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        业务逻辑层                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │SessionService│  │MessageService│  │OpenAIService     │   │
│  └──────────────┘  └──────────────┘  └──────────────────┘   │
│  ┌──────────────┐  ┌──────────────┐                         │
│  │TokenService  │  │ConfigService │                          │
│  └──────────────┘  └──────────────┘                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        数据访问层                            │
│  ┌─────────────────────────────────────────────────────────┐│
│  │         Entity Framework Core + Repositories            ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        数据存储层                            │
│  ┌─────────────┐  ┌─────────────┐                           │
│  │ SQLite/     │  │ File System │                           │
│  │ PostgreSQL  │  │ (Export)    │                           │
│  └─────────────┘  └─────────────┘                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      外部服务层                              │
│  ┌─────────────────────────────────────────────────────────┐│
│  │        OpenAI API / 兼容 OpenAI 协议的 API 服务            ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

---

## 四、数据库设计

### 4.1 数据表结构

#### Sessions 表
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | string (GUID) | 主键 |
| Title | string | 会话标题 |
| ApiConfigId | string (GUID) | 关联的 API 配置 |
| Model | string | 使用的模型 |
| SystemPrompt | string | 系统提示词 |
| Temperature | decimal | 生成温度 |
| MaxTokens | int? | 最大 Token 数 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |
| Metadata | json | 扩展元数据 |

#### Messages 表
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | string (GUID) | 主键 |
| SessionId | string (GUID) | 关联的会话 |
| Role | string | 角色（user/assistant/system） |
| Content | string | 消息内容 |
| Tokens | int? | 消耗的 Token 数 |
| Model | string? | 使用的模型 |
| CreatedAt | datetime | 创建时间 |
| Metadata | json | 扩展元数据 |

#### ApiConfigs 表
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | string (GUID) | 主键 |
| Name | string | 配置名称 |
| BaseUrl | string | API 基础 URL |
| ApiKey | string | API 密钥（加密存储） |
| Model | string | 默认模型 |
| IsDefault | bool | 是否为默认配置 |
| CreatedAt | datetime | 创建时间 |

### 4.2 实体关系

```
Sessions (1) ────── (N) Messages
ApiConfigs (1) ──── (N) Sessions
```

---

## 五、API 接口设计

### 5.1 RESTful API 端点

#### 会话管理
| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/v1/sessions` | 获取会话列表（支持分页、搜索） |
| POST | `/api/v1/sessions` | 创建新会话 |
| GET | `/api/v1/sessions/{id}` | 获取会话详情（含消息列表） |
| PUT | `/api/v1/sessions/{id}` | 更新会话信息 |
| DELETE | `/api/v1/sessions/{id}` | 删除会话 |
| POST | `/api/v1/sessions/{id}/export` | 导出会话 |

#### 消息管理
| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/v1/sessions/{id}/messages` | 获取消息列表 |
| POST | `/api/v1/sessions/{id}/messages` | 发送消息 |
| DELETE | `/api/v1/sessions/{id}/messages/{msgId}` | 删除消息 |
| POST | `/api/v1/sessions/{id}/messages/regenerate` | 重新生成回复 |
| POST | `/api/v1/sessions/{id}/messages/stop` | 停止生成 |

#### API 配置
| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/v1/api-configs` | 获取配置列表 |
| POST | `/api/v1/api-configs` | 添加配置 |
| PUT | `/api/v1/api-configs/{id}` | 更新配置 |
| DELETE | `/api/v1/api-configs/{id}` | 删除配置 |
| GET | `/api/v1/api-configs/{id}/models` | 获取可用模型列表 |

---

## 六、项目目录结构

```
TWorker/
├── TWorker.sln
├── README.md
├── .env.example
├── docker-compose.yml
│
├── src/
│   ├── TWorker.Api/                 # API 项目
│   │   ├── TWorker.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Controllers/
│   │   │   ├── v1/
│   │   │   │   ├── SessionsController.cs
│   │   │   │   ├── MessagesController.cs
│   │   │   │   └── ApiConfigsController.cs
│   │   │   └── BaseController.cs
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── Extensions/
│   │
│   ├── TWorker.Core/                # 核心业务层
│   │   ├── TWorker.Core.csproj
│   │   ├── Entities/
│   │   │   ├── Session.cs
│   │   │   ├── Message.cs
│   │   │   └── ApiConfig.cs
│   │   ├── Interfaces/
│   │   │   ├── ISessionService.cs
│   │   │   ├── IMessageService.cs
│   │   │   ├── IOpenAIService.cs
│   │   │   └── IRepository.cs
│   │   ├── Services/
│   │   │   ├── SessionService.cs
│   │   │   ├── MessageService.cs
│   │   │   ├── OpenAIService.cs
│   │   │   └── TokenService.cs
│   │   ├── DTOs/
│   │   │   ├── SessionDtos.cs
│   │   │   ├── MessageDtos.cs
│   │   │   └── ApiConfigDtos.cs
│   │   └── Common/
│   │       ├── Result.cs
│   │       └── Exceptions.cs
│   │
│   ├── TWorker.Infrastructure/     # 基础设施层
│   │   ├── TWorker.Infrastructure.csproj
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   │   ├── Repository.cs
│   │   │   ├── SessionRepository.cs
│   │   │   └── MessageRepository.cs
│   │   ├── OpenAI/
│   │   │   ├── OpenAIClient.cs
│   │   │   └── OpenAIOptions.cs
│   │   └── Security/
│   │       └── EncryptionHelper.cs
│   │
│   └── TWorker.Shared/              # 共享层
│       ├── TWorker.Shared.csproj
│       ├── Constants.cs
│       └── Extensions/
│
├── tests/
│   ├── TWorker.Api.Tests/
│   ├── TWorker.Core.Tests/
│   └── TWorker.Integration.Tests/
│
└── frontend/                        # Svelte 前端
    ├── package.json
    ├── svelte.config.js
    ├── vite.config.js
    ├── src/
    │   ├── app.html
    │   ├── routes/
    │   │   ├── +layout.svelte
    │   │   ├── +page.svelte
    │   │   ├── sessions/
    │   │   │   ├── +page.svelte
    │   │   │   └── [id]/
    │   │   │       └── +page.svelte
    │   │   └── settings/
    │   │       └── +page.svelte
    │   ├── lib/
    │   │   ├── components/
    │   │   ├── stores/
    │   │   ├── services/
    │   │   └── utils/
    │   └── app.css
    └── static/
```

---

## 七、任务编排

### 阶段一：项目初始化（1-2 天）

- [ ] 1.1 创建 .NET 解决方案和项目结构
- [ ] 1.2 配置 Entity Framework Core 和数据库
- [ ] 1.3 设置 Serilog 日志
- [ ] 1.4 配置 Swagger/OpenAPI 文档
- [ ] 1.5 创建基础实体和数据库上下文
- [ ] 1.6 初始化 SvelteKit 前端项目

### 阶段二：后端核心功能开发（3-4 天）

- [ ] 2.1 实现数据实体（Session, Message, ApiConfig）
- [ ] 2.2 实现 Repository 层
- [ ] 2.3 实现 Service 层
  - [ ] SessionService - 会话 CRUD
  - [ ] MessageService - 消息 CRUD
  - [ ] OpenAIService - OpenAI API 调用
- [ ] 2.4 实现 API Controllers
  - [ ] SessionsController
  - [ ] MessagesController
  - [ ] ApiConfigsController
- [ ] 2.5 实现 SSE 流式响应
- [ ] 2.6 实现错误处理和验证

### 阶段三：前端开发（3-4 天）

- [ ] 3.1 搭建 SvelteKit 项目结构
- [ ] 3.2 实现 API 服务层
- [ ] 3.3 实现状态管理（Store）
- [ ] 3.4 实现会话列表页面
- [ ] 3.5 实现会话详情/聊天页面
- [ ] 3.6 实现流式消息显示组件
- [ ] 3.7 实现设置页面（API 配置管理）
- [ ] 3.8 实现响应式布局

### 阶段四：增强功能（2-3 天）

- [ ] 4.1 Token 计数和统计功能
- [ ] 4.2 会话导出/导入功能
- [ ] 4.3 多 API 配置切换
- [ ] 4.4 模型列表自动获取
- [ ] 4.5 前端错误处理和重试

### 阶段五：测试和优化（2 天）

- [ ] 5.1 编写后端单元测试
- [ ] 5.2 编写集成测试
- [ ] 5.3 前端测试
- [ ] 5.4 性能优化
- [ ] 5.5 代码审查和重构

### 阶段六：部署和文档（1-2 天）

- [ ] 6.1 Docker 容器化配置
- [ ] 6.2 编写 API 文档
- [ ] 6.3 编写用户文档
- [ ] 6.4 CI/CD 配置

---

## 八、依赖配置示例

### 后端 packages.config

```xml
<!-- TWorker.Api.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
</ItemGroup>

<!-- TWorker.Core.csproj -->
<ItemGroup>
  <PackageReference Include="AutoMapper" Version="12.0.0" />
  <PackageReference Include="FluentValidation" Version="11.9.0" />
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
</ItemGroup>

<!-- TWorker.Infrastructure.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
  <PackageReference Include="Dapper" Version="2.1.0" />
</ItemGroup>
```

### 前端 package.json

```json
{
  "name": "tworker-frontend",
  "version": "0.1.0",
  "scripts": {
    "dev": "vite dev",
    "build": "vite build",
    "preview": "vite preview"
  },
  "devDependencies": {
    "@sveltejs/adapter-auto": "^3.0.0",
    "@sveltejs/kit": "^2.0.0",
    "@sveltejs/vite-plugin-svelte": "^3.0.0",
    "svelte": "^4.2.0",
    "typescript": "^5.3.0",
    "vite": "^5.0.0"
  },
  "dependencies": {
    "@tanstack/svelte-query": "^5.0.0",
    "tailwindcss": "^3.4.0"
  }
}
```

---

## 九、Docker 部署

### docker-compose.yml

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: src/TWorker.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/tworker.db
      - OpenAI__BaseUrl=https://api.openai.com/v1
      - OpenAI__ApiKey=${OPENAI_API_KEY}
    volumes:
      - ./data:/app/data

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    depends_on:
      - api

volumes:
  data:
```

---

## 十、扩展功能建议

1. **多模型支持** - 支持在不同会话中使用不同的 AI 模型
2. **对话模板** - 预设不同场景的系统提示词模板
3. **会话搜索** - 全文搜索会话内容
4. **会话标签** - 为会话添加标签进行分类管理
5. **API 使用统计** - Token 使用量和费用统计图表
6. **批量操作** - 批量删除、导出会话
7. **快捷键支持** - 常用操作的键盘快捷键
8. **主题切换** - 支持亮色/暗色主题
9. **离线支持** - PWA 支持，离线查看历史会话
10. **插件系统** - 支持自定义插件扩展功能

---

## 十一、总结

本计划设计了一个完整的基于 OpenAI 接口协议的 AI 对话工具，采用以下技术栈：

- **后端**：.NET 8.0 + ASP.NET Core Web API + Entity Framework Core
- **前端**：Svelte + SvelteKit + TypeScript + TailwindCSS
- **数据库**：SQLite（开发）/ PostgreSQL（生产）

主要特点：

- **清晰的分层架构** - API 层、业务逻辑层、数据访问层分离
- **流式响应支持** - SSE 实时显示 AI 回复
- **多会话管理** - 完整的会话 CRUD 操作
- **多 API 配置** - 支持配置多个 AI 服务提供商
- **数据持久化** - 完整的会话记录存储和导出功能
- **容器化部署** - Docker Compose 一键部署