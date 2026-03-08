# AI 对话工具开发计划

## 项目概述

开发一个基于 OpenAI 接口协议的 AI 对话工具，支持多会话管理、群聊系统和角色系统。

---

## 一、需求分析

### 1.1 功能需求

#### 成员系统（用户与 AI 统一抽象）

**成员（Member）概念**
- **用户成员**：人类用户，通过界面输入消息
- **AI 成员**：AI 助手，根据配置的角色和能力响应消息
- **统一抽象**：所有成员都实现相同的接口，支持发送/接收消息

**成员属性**
- `Id`：唯一标识符
- `Name`：显示名称
- `Type`：类型（User/AI）
- `Role`：角色（仅 AI 成员，如 coder、ui-designer 等）
- `Avatar`：头像
- `Status`：在线/离线/忙碌状态

#### 角色系统

**角色定义**
每个 AI 成员可以配置一个角色，决定其功能和行为：

| 角色 | 标识 | 功能描述 |
|------|------|----------|
| **Coder** | `coder` | 代码实现、文件操作、工具调用（参考 Cline 功能） |
| **UI Designer** | `ui-designer` | 界面设计、样式生成、组件建议 |
| **Architect** | `architect` | 架构设计、技术选型、方案规划 |
| **Reviewer** | `reviewer` | 代码审查、质量检查、建议优化 |
| **General** | `general` | 通用问答、信息查询 |

**角色配置**
- 每个角色有独立的系统提示词模板
- 角色决定可用的工具集（Coder 可使用文件工具，UI Designer 可使用设计工具）
- 角色可配置特定的 API 模型和参数

**Coder 角色详细功能（参考 Cline）**
- **短期记忆（Context Memory）**：滑动窗口管理对话历史，基于 Token 预算自动修剪
- **中期记忆（Task Memory）**：任务状态持久化、工具调用历史记录、文件修改追踪
- **长期记忆（Long-term Memory）**：自定义指令、模式偏好、全局配置记忆
- **工具调用**：支持文件读写、shell 命令、代码分析等工具
- **对话循环**：支持工具调用后的递归继续，直到任务完成

#### 群聊系统

**群聊概念**
- 允许多个成员（用户 + 多个 AI）参与同一会话
- 每个群聊有独立的对话历史和上下文

**防循环调用机制（重要）**
- **调用深度限制**：单次用户消息触发的 AI 响应链最大深度为 5 层
- **调用令牌机制**：每个 AI 响应需要消耗调用令牌，令牌耗尽则停止
- **互斥响应机制**：同一轮次中，一个 AI 响应不会触发其他 AI 的自动响应
- **显式@机制**：AI 只有在被显式@提及或主动选择参与时才响应
- **超时机制**：单轮对话总耗时超过 5 分钟自动终止

**群聊消息路由**
```
用户发送消息
    │
    ▼
系统解析消息中的@提及
    │
    ▼
被@的 AI 成员响应（消耗 1 个调用令牌）
    │
    ▼
AI 响应中如包含@其他 AI，则被@的 AI 可继续响应（深度 +1）
    │
    ▼
达到深度限制或令牌耗尽，本轮结束，等待用户新输入
```

#### 会话管理
- **创建会话**：创建新的对话会话，设置标题、选择 AI 模型、配置系统提示词
- **群聊会话**：创建群聊，邀请多个 AI 成员参与
- **会话列表**：展示所有会话，支持按时间排序、搜索和筛选
- **会话详情**：查看会话中的完整对话记录
- **会话编辑**：修改会话标题、模型配置、系统提示词等属性
- **会话删除**：删除不需要的会话及其关联的所有消息
- **会话导出**：将会话记录导出为 JSON、Markdown 或 TXT 格式
- **会话导入**：从导出的文件中恢复会话数据

#### 消息管理
- **发送消息**：向会话发送消息，支持@提及特定 AI
- **消息历史**：查看当前会话的完整消息历史
- **消息删除**：删除单条消息
- **重新生成**：请求 AI 重新生成上一条回复
- **停止生成**：在流式响应过程中可以中断生成
- **Token 计数显示**：实时显示消息和会话的 Token 消耗

#### AI 配置管理
- **多 API 配置**：支持配置多个 OpenAI 兼容的 API 服务
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
- 支持兼容 OpenAI 协议的第三方服务

---

## 二、技术选型

### 1. 后端技术栈（.NET）

| 技术/框架 | 版本 | 说明 |
|-----------|------|------|
| **.NET** | 8.0+ | 主要开发框架 |
| **ASP.NET Core Web API** | 8.0+ | Web API 框架 |
| **Entity Framework Core** | 8.0+ | ORM 框架 |
| **SQLite / PostgreSQL** | - | 数据库 |
| **FluentValidation** | - | 数据验证 |
| **AutoMapper** | - | 对象映射 |
| **Serilog** | - | 结构化日志 |

### 2. 前端技术栈（Svelte）

| 技术/框架 | 说明 |
|-----------|------|
| **Svelte** | 前端框架 |
| **SvelteKit** | 应用框架 |
| **TypeScript** | 类型安全 |
| **TailwindCSS** | UI 样式 |
| **Svelte Store** | 状态管理 |

---

## 三、系统架构设计

### 3.1 整体架构

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
│  │SessionService│  │MessageService│  │MemberService│   │
│  └──────────────┘  └──────────────┘  └──────────────────┘   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │RoleService   │  │GroupChatSvc  │  │OpenAIService     │   │
│  └──────────────┘  └──────────────┘  └──────────────────┘   │
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
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ SQLite/     │  │ File System │  │ Memory Storage      │  │
│  │ PostgreSQL  │  │ (Export)    │  │ (Task/Custom Instr) │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 群聊消息处理流程

```
┌─────────────────────────────────────────────────────────────────┐
│                      群聊消息处理流程                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  用户发送消息（可能包含@AI1 @AI2）                                │
│              │                                                   │
│              ▼                                                   │
│  ┌─────────────────────────┐                                     │
│  │   MessageRouterService  │                                     │
│  │   - 解析@提及             │                                     │
│  │   - 确定目标 AI 成员       │                                     │
│  │   - 检查调用令牌余额        │                                     │
│  └─────────────────────────┘                                     │
│              │                                                   │
│              ▼                                                   │
│  为每个被@的 AI 创建响应任务（并行或顺序）                          │
│              │                                                   │
│              ▼                                                   │
│  ┌─────────────────────────┐                                     │
│  │   调用深度检查（当前：0）  │ ← 最大深度：5                      │
│  │   调用令牌检查（剩余：N）  │ ← 每轮初始：3                      │
│  └─────────────────────────┘                                     │
│              │                                                   │
│              ▼                                                   │
│  AI 生成响应（流式）                                               │
│              │                                                   │
│              ▼                                                   │
│  响应中是否包含@其他 AI？                                          │
│      ├─ 是 → 检查深度和令牌 → 继续路由                            │
│      └─ 否 → 本轮结束                                             │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  终止条件：                                               │     │
│  │  - 无更多@提及                                           │     │
│  │  - 调用深度达到 5                                         │     │
│  │  - 调用令牌耗尽                                          │     │
│  │  - 用户主动停止                                          │     │
│  │  - 单轮超时（5 分钟）                                      │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.3 Coder 角色的对话循环（参考 Cline）

```
┌─────────────────────────────────────────────────────────────────┐
│                   Coder 角色对话循环 (runLoop)                    │
├─────────────────────────────────────────────────────────────────┤
│  1. 将用户输入添加到消息历史                                      │
│              │                                                   │
│              ▼                                                   │
│  2. 构建 API 请求消息（系统提示 + 历史 + 工具定义）                  │
│              │                                                   │
│              ▼                                                   │
│  3. 调用 LLM Provider 获取响应（流式）                              │
│              │                                                   │
│              ▼                                                   │
│  4. 处理流式响应                                                  │
│     ┌─────────────────────┐                                     │
│     │ chunk.type = content │ → 显示部分消息                       │
│     │ chunk.type = tool_call│ → 执行工具调用                      │
│     └─────────────────────┘                                     │
│              │                                                   │
│              ▼                                                   │
│  5. 将工具调用结果添加到历史                                      │
│              │                                                   │
│              ▼                                                   │
│  6. 检查是否需要继续（有工具调用则递归，深度 +1）                     │
│              │                                                   │
│              ▼                                                   │
│  7. 循环终止（无工具调用/达到最大迭代/用户取消/深度限制）            │
└─────────────────────────────────────────────────────────────────┘
```

### 3.4 Coder 角色的记忆系统架构（参考 Cline）

```
┌─────────────────────────────────────────────────────────────┐
│                    Layer 1: Context Memory                   │
│                    (短期记忆 / 滑动窗口)                      │
│  - 当前会话的对话历史                                         │
│  - 自动修剪以保持 Token 预算                                   │
│  - 使用滑动窗口算法管理                                        │
│  - 内存存储，会话结束释放                                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Layer 2: Task Memory                      │
│                    (中期记忆 / 任务级)                        │
│  - 当前任务的元数据和状态                                      │
│  - 已执行的工具调用历史                                        │
│  - 文件修改记录                                                │
│  - 存储在 .flowworker/memory/tasks/<task_id>.json             │
│  - 支持跨会话恢复                                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Layer 3: Long-term Memory                 │
│                    (长期记忆 / 跨任务)                        │
│  - 自定义指令（Custom Instructions）                          │
│  - 模式偏好（Mode Preferences）                               │
│  - API 配置和默认设置                                         │
│  - 存储在全局存储或项目级存储中                                │
└─────────────────────────────────────────────────────────────┘
```

---

## 四、数据库设计

### 4.1 核心数据表结构

#### Members 表（成员表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| Name | string | 显示名称 |
| Type | enum | 类型（User=0, AI=1） |
| RoleId | GUID? | 关联的角色 ID（仅 AI 类型） |
| Avatar | string? | 头像 URL |
| Status | enum | 状态（Offline=0, Online=1, Busy=2） |
| ApiConfigId | GUID? | 关联的 API 配置（仅 AI 类型） |
| Model | string? | 使用的模型（仅 AI 类型） |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

#### Roles 表（角色表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| Name | string | 角色名称（coder/ui-designer/architect 等） |
| DisplayName | string | 显示名称 |
| Description | string | 角色描述 |
| SystemPrompt | string | 系统提示词模板 |
| AllowedTools | json | 允许使用的工具列表 |
| IsBuiltIn | bool | 是否为内置角色 |
| CreatedAt | datetime | 创建时间 |

#### Sessions 表（会话表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| Title | string | 会话标题 |
| Type | enum | 类型（Single=单聊，Group=群聊） |
| CreatedBy | GUID | 创建者（User Member ID） |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |
| Metadata | json | 扩展元数据 |

#### SessionMembers 表（会话参与者关联表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| SessionId | GUID | 会话 ID |
| MemberId | GUID | 成员 ID |
| JoinedAt | datetime | 加入时间 |
| IsActive | bool | 是否活跃 |

#### Messages 表（消息表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| SessionId | GUID | 关联的会话 ID |
| MemberId | GUID | 发送者 ID |
| Content | string | 消息内容 |
| Tokens | int? | 消耗的 Token 数 |
| Model | string? | 使用的模型 |
| ReplyToMessageId | GUID? | 回复的消息 ID |
| CallDepth | int | 调用深度（防循环） |
| CreatedAt | datetime | 创建时间 |
| Metadata | json | 扩展元数据 |

#### ApiConfigs 表（API 配置表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| Name | string | 配置名称 |
| BaseUrl | string | API 基础 URL |
| ApiKey | string | API 密钥（加密存储） |
| Model | string | 默认模型 |
| IsDefault | bool | 是否为默认配置 |
| CreatedAt | datetime | 创建时间 |

### 4.2 实体关系

```
Members (1) ────── (N) SessionMembers (N) ────── (1) Sessions
      │                                                        │
      │ (AI 类型)                                               │
      ▼                                                        ▼
Roles (1)                                              Messages (N)
      │
      └────── (N) ApiConfigs

Sessions (1) ────── (N) Messages
Members (1) ────── (N) Messages
```

---

## 五、API 接口设计

### 5.1 RESTful API 端点

#### 成员管理
| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/v1/members` | 获取成员列表 |
| POST | `/api/v1/members` | 创建 AI 成员 |
| GET | `/api/v1/members/{id}` | 获取成员详情 |
| PUT | `/api/v1/members/{id}` | 更新成员信息 |
| DELETE | `/api/v1/members/{id}` | 删除 AI 成员 |

#### 角色管理
| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/v1/roles` | 获取角色列表 |
| POST | `/api/v1/roles` | 创建自定义角色 |
| GET | `/api/v1/roles/{id}` | 获取角色详情 |
| PUT | `/api/v1/roles/{id}` | 更新角色 |
| DELETE | `/api/v1/roles/{id}` | 删除自定义角色 |

#### 会话管理
| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/v1/sessions` | 获取会话列表 |
| POST | `/api/v1/sessions` | 创建会话 |
| GET | `/api/v1/sessions/{id}` | 获取会话详情 |
| PUT | `/api/v1/sessions/{id}` | 更新会话信息 |
| DELETE | `/api/v1/sessions/{id}` | 删除会话 |
| POST | `/api/v1/sessions/{id}/members` | 添加参与者 |
| DELETE | `/api/v1/sessions/{id}/members/{pid}` | 移除参与者 |
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

---

## 六、项目目录结构

```
FlowWorker/
├── FlowWorker.sln
├── README.md
├── .env.example
├── docker-compose.yml
│
├── src/
│   ├── FlowWorker.Api/                 # API 项目
│   │   ├── FlowWorker.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Controllers/
│   │   │   ├── v1/
│   │   │   │   ├── MembersController.cs
│   │   │   │   ├── RolesController.cs
│   │   │   │   ├── SessionsController.cs
│   │   │   │   ├── MessagesController.cs
│   │   │   │   └── ApiConfigsController.cs
│   │   │   └── BaseController.cs
│   │   ├── Middleware/
│   │   └── Extensions/
│   │
│   ├── FlowWorker.Core/                # 核心业务层
│   │   ├── FlowWorker.Core.csproj
│   │   ├── Entities/
│   │   │   ├── Member.cs         # 成员实体
│   │   │   ├── Role.cs                # 角色实体
│   │   │   ├── Session.cs             # 会话实体
│   │   │   ├── SessionMember.cs  # 会话参与者关联
│   │   │   ├── Message.cs             # 消息实体
│   │   │   └── ApiConfig.cs           # API 配置实体
│   │   ├── Interfaces/
│   │   │   ├── IMemberService.cs
│   │   │   ├── IRoleService.cs
│   │   │   ├── ISessionService.cs
│   │   │   ├── IMessageService.cs
│   │   │   ├── IOpenAIService.cs
│   │   │   └── IRepository.cs
│   │   ├── Services/
│   │   │   ├── MemberService.cs
│   │   │   ├── RoleService.cs
│   │   │   ├── SessionService.cs
│   │   │   ├── MessageService.cs
│   │   │   ├── GroupChatService.cs    # 群聊服务（防循环）
│   │   │   └── OpenAIService.cs
│   │   ├── Roles/                     # 角色实现
│   │   │   ├── CoderRoleService.cs    # Coder 角色（含 Cline 功能）
│   │   │   ├── UIDesignerRoleService.cs
│   │   │   ├── ArchitectRoleService.cs
│   │   │   └── GeneralRoleService.cs
│   │   ├── DTOs/
│   │   │   ├── MemberDtos.cs
│   │   │   ├── RoleDtos.cs
│   │   │   ├── SessionDtos.cs
│   │   │   ├── MessageDtos.cs
│   │   │   └── ApiConfigDtos.cs
│   │   └── Common/
│   │       ├── Result.cs
│   │       └── Exceptions.cs
│   │
│   ├── FlowWorker.Infrastructure/     # 基础设施层
│   │   ├── FlowWorker.Infrastructure.csproj
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   │   ├── Repository.cs
│   │   │   ├── MemberRepository.cs
│   │   │   ├── RoleRepository.cs
│   │   │   ├── SessionRepository.cs
│   │   │   └── MessageRepository.cs
│   │   ├── OpenAI/
│   │   │   ├── OpenAIClient.cs
│   │   │   └── OpenAIOptions.cs
│   │   └── Security/
│   │       └── EncryptionHelper.cs
│   │
│   └── FlowWorker.Shared/              # 共享层
│       ├── FlowWorker.Shared.csproj
│       ├── Constants.cs
│       ├── Enums/
│       │   ├── MemberType.cs
│       │   ├── MemberStatus.cs
│       │   ├── SessionType.cs
│       │   └── MessageRole.cs
│       └── Extensions/
│
├── .flowworker/                        # 数据存储目录
│   └── memory/
│       ├── tasks/                      # Coder 角色任务记忆
│       └── instructions/               # 自定义指令
│
├── tests/
│   ├── FlowWorker.Api.Tests/
│   ├── FlowWorker.Core.Tests/
│   └── FlowWorker.Integration.Tests/
│
└── frontend/                           # Svelte 前端
    ├── package.json
    ├── svelte.config.js
    ├── vite.config.js
    ├── src/
    │   ├── app.html
    │   ├── routes/
    │   │   ├── +layout.svelte
    │   │   ├── +page.svelte
    │   │   ├── members/
    │   │   │   └── +page.svelte
    │   │   ├── roles/
    │   │   │   └── +page.svelte
    │   │   ├── sessions/
    │   │   │   ├── +page.svelte
    │   │   │   └── [id]/
    │   │   │       └── +page.svelte
    │   │   └── settings/
    │   │       └── +page.svelte
    │   ├── lib/
    │   │   ├── components/
    │   │   │   ├── Chat/
    │   │   │   │   ├── MessageList.svelte
    │   │   │   │   ├── ChatInput.svelte
    │   │   │   │   └── MemberList.svelte
    │   │   │   ├── Members/
    │   │   │   │   └── MemberCard.svelte
    │   │   │   └── Roles/
    │   │   │       └── RoleForm.svelte
    │   │   ├── stores/
    │   │   │   ├── memberStore.ts
    │   │   │   ├── roleStore.ts
    │   │   │   ├── sessionStore.ts
    │   │   │   └── messageStore.ts
    │   │   └── services/
    │   │       └── api.ts
    │   └── app.css
    └── static/
```

---

## 七、提示词工程计划

### 7.1 提示词工程概述

基于 docs/prompt_tech.md 和 docs/prompt_cline_tech.md 的技术文档，为每个角色设计专门的提示词工程方案。

### 7.2 提示词设计原则（CLEAR 原则）

所有角色的提示词设计遵循以下原则：

1. **Concise（简洁）**：去除冗余，直击要点
2. **Logical（逻辑）**：结构清晰，层次分明
3. **Explicit（明确）**：指令具体，无歧义
4. **Adaptive（适应）**：根据场景调整风格
5. **Reflective（反思）**：包含验证和修正机制

### 7.3 角色提示词工程

#### 7.3.1 Coder 角色提示词（参考 Cline）

**系统提示词架构**：

```markdown
# 角色定义
你是一位资深全栈开发工程师，名为 Coder，专注于代码实现、文件操作和工具调用。
你具备多种编程语言、框架、设计模式和最佳实践的深厚知识。

# 核心能力
- 代码生成和修改
- 文件读写操作
- Shell 命令执行
- 代码分析和审查
- 问题诊断和修复

# 行为准则
1. 始终保持专业和精确的工作态度
2. 优先理解用户需求的完整上下文
3. 执行操作前确认理解正确
4. 提供清晰的代码注释和说明
5. 遵循项目现有的代码规范和结构

# 工具使用规范
你拥有以下工具：
- read_file: 读取文件内容
- write_file: 创建/覆盖文件
- replace_in_file: 局部修改文件
- search_files: 搜索文件内容
- list_files: 列出目录文件
- execute_command: 执行 Shell 命令
- ask_followup_question: 向用户询问

# 工具调用格式
使用 XML 格式调用工具：
<tool_name>
<parameter_name>value</parameter_name>
</tool_name>

# 工作流程
1. 分析任务需求
2. 制定执行计划
3. 逐步执行（使用工具）
4. 验证结果
5. 完成任务或请求反馈

# 输出格式
- 使用 Markdown 格式化代码和说明
- 代码块指定语言类型
- 重要信息使用强调标记
```

**Coder 角色高级提示词技术**：

1. **思维链（Chain of Thought）**：使用 `<thinking></thinking>` 标签进行推理
2. **任务进度追踪**：使用 `<task_progress>` 标签跟踪任务状态
3. **迭代式执行**：逐步验证，确保准确性
4. **上下文管理**：滑动窗口管理对话历史

#### 7.3.2 UI Designer 角色提示词

```markdown
# 角色定义
你是一位专业的 UI/UX 设计师，名为 UI Designer，专注于界面设计、样式开发和用户体验优化。

# 核心能力
- 界面布局和组件设计
- CSS/TailwindCSS 样式开发
- 响应式设计
- 色彩搭配和视觉层次
- 用户体验优化建议

# 行为准则
1. 以用户为中心进行设计
2. 遵循现代设计趋势和规范
3. 考虑可访问性和包容性
4. 提供设计理由和参考

# 输出格式
- 使用 Markdown 展示设计说明
- 提供代码示例和预览效果
- 附上设计参考和最佳实践
```

#### 7.3.3 Architect 角色提示词

```markdown
# 角色定义
你是一位资深软件架构师，名为 Architect，专注于系统架构设计、技术选型和方案规划。

# 核心能力
- 系统架构设计
- 技术选型和评估
- 架构模式分析
- 可扩展性和可维护性规划
- 风险识别和缓解策略

# 行为准则
1. 从全局视角分析问题
2. 权衡各种方案的利弊
3. 考虑长期维护和演进
4. 提供清晰的架构图和说明

# 输出格式
- 使用图表和结构化描述
- 提供多方案对比分析
- 给出明确的推荐方案
```

#### 7.3.4 Reviewer 角色提示词

```markdown
# 角色定义
你是一位资深代码审查专家，名为 Reviewer，专注于代码质量检查、问题识别和优化建议。

# 核心能力
- 代码问题识别
- 最佳实践检查
- 性能瓶颈分析
- 安全漏洞检测
- 重构建议

# 行为准则
1. 保持建设性和友善的态度
2. 优先指出严重问题
3. 提供具体的改进建议
4. 肯定代码的优点

# 输出格式
使用以下 Markdown 结构：
## 🐛 发现的问题
## 💡 优化建议
## ✅ 优点
```

#### 7.3.5 General 角色提示词

```markdown
# 角色定义
你是一位通用 AI 助手，名为 General，专注于信息查询、问题解答和日常对话。

# 核心能力
- 通用知识问答
- 信息查询和总结
- 问题分析和解答
- 创意写作和内容生成

# 行为准则
1. 提供准确和有用的信息
2. 保持友善和专业的语气
3. 承认知识边界，不编造信息
4. 必要时建议咨询专业人士

# 输出格式
- 结构化回答，层次清晰
- 重要信息突出显示
- 必要时提供参考资料
```

### 7.4 多 Agent 协作提示词策略

#### 7.4.1 角色分配提示词

```markdown
# 团队协作配置

## Agent 1：项目经理
职责：任务分解、进度跟踪、资源协调
沟通风格：结构化、目标导向

## Agent 2：技术专家（Coder）
职责：技术方案设计、代码审查、问题解决
沟通风格：精确、技术性强

## Agent 3：用户体验设计师
职责：界面设计、用户流程、可用性评估
沟通风格：同理心、视觉化

# 协作规则
1. 每个 Agent 从自己的专业角度分析问题
2. 定期同步信息和进展
3. 出现分歧时通过讨论达成共识
```

#### 7.4.2 对话协调提示词

```markdown
# 讨论主题：[主题]

# 讨论流程
1. 项目经理开场，明确讨论目标
2. 各 Agent 依次发表观点
3. 自由讨论，交换意见
4. 项目经理总结，形成结论

# 发言格式
[角色名称]: [观点内容]

# 开始讨论
```

### 7.5 高级提示词技术

#### 7.5.1 Zero-shot Prompting
直接让模型执行任务，不提供示例。

#### 7.5.2 Few-shot Prompting
提供少量示例帮助模型理解任务模式。

#### 7.5.3 Chain of Thought (CoT)
引导模型展示推理过程。

#### 7.5.4 Tree of Thoughts (ToT)
探索多个推理路径。

#### 7.5.5 ReAct (Reasoning + Acting)
结合推理与行动，用于工具调用场景。

#### 7.5.6 Self-Reflection
让模型自我审查和改进。

### 7.6 提示词模板管理

#### 7.6.1 模板存储结构
```
prompts/
├── system/
│   ├── coder.md
│   ├── ui-designer.md
│   ├── architect.md
│   ├── reviewer.md
│   └── general.md
├── templates/
│   ├── code-review.md
│   ├── task-analysis.md
│   └── design-feedback.md
└── examples/
    ├── few-shot-examples.json
    └── conversation-patterns.json
```

#### 7.6.2 模板变量支持
- `{customInstructions}` - 用户自定义指令
- `{workspace}` - 工作区路径
- `{mode}` - 当前模式
- `{history}` - 对话历史摘要

---

## 八、任务编排

### 阶段一：基础架构（1-2 天）

- [x] 1.1 更新数据库实体（Member, Role, SessionMember）
- [x] 1.2 更新数据库上下文和迁移
- [x] 1.3 实现成员管理 API
- [x] 1.4 实现角色管理 API

### 阶段二：群聊系统（2-3 天）

- [x] 2.1 实现群聊会话管理
- [x] 2.2 实现消息路由服务
- [x] 2.3 实现防循环调用机制
- [x] 2.4 实现@提及解析
- [x] 2.5 更新消息服务支持群聊

### 阶段三：角色系统（2-3 天）

- [x] 3.1 实现角色基类和接口
- [x] 3.2 实现 Coder 角色（含 Cline 功能）
- [x] 3.3 实现其他内置角色（UI Designer, Architect 等）
- [x] 3.4 实现角色特定的系统提示词
- [x] 3.5 实现角色工具权限控制

### 阶段四：Coder 角色增强（3-4 天）

- [x] 4.1 实现滑动窗口记忆管理
- [x] 4.2 实现任务记忆持久化
- [x] 4.3 实现工具调用系统
- [x] 4.4 实现对话循环（runLoop）
- [x] 4.5 实现文件操作工具

### 阶段五：前端适配（2-3 天）

- [x] 5.1 实现成员管理页面
- [x] 5.2 实现角色管理页面
- [x] 5.3 更新会话页面支持群聊
- [x] 5.4 实现@提及功能
- [x] 5.5 实现参与者列表显示

### 阶段六：提示词工程实现（3-4 天）

- [ ] 6.1 实现提示词模板管理系统
- [ ] 6.2 实现 Coder 角色提示词（参考 Cline）
- [ ] 6.3 实现 UI Designer 角色提示词
- [ ] 6.4 实现 Architect 角色提示词
- [ ] 6.5 实现 Reviewer 角色提示词
- [ ] 6.6 实现 General 角色提示词
- [ ] 6.7 实现多 Agent 协作提示词策略
- [ ] 6.8 实现高级提示词技术（CoT, Few-shot 等）

### 阶段七：Coder 角色增强与工具系统（3-4 天）

- [ ] 7.1 实现三层记忆系统（短期/中期/长期）
- [ ] 7.2 实现工具系统框架
- [ ] 7.3 实现内置工具（文件操作、代码分析、执行工具）
- [ ] 7.4 实现对话循环（runLoop）
- [ ] 7.5 实现前端工具执行界面

### 阶段八：增强功能与优化（2-3 天）

- [ ] 8.1 实现 Token 计数和统计功能
- [ ] 8.2 实现会话导出/导入功能
- [ ] 8.3 实现多 API 配置和模型管理增强
- [ ] 8.4 实现错误处理和重试机制
- [ ] 8.5 实现性能优化

### 阶段九：测试、部署与文档（2-3 天）

- [ ] 9.1 编写单元测试和集成测试
- [ ] 9.2 实现 Docker 容器化部署
- [ ] 9.3 配置 CI/CD 流水线
- [ ] 9.4 编写完整的文档（API、开发、用户、提示词工程）

---

## 九、扩展功能建议

1. **语音输入** - 支持语音消息输入
2. **文件共享** - 在群聊中共享文件
3. **AI 间协作** - 支持 AI 之间的任务委托
4. **会话模板** - 预设不同场景的群聊模板
5. **权限系统** - 细粒度的参与者权限控制
6. **消息反应** - 对消息添加表情反应
7. **会话归档** - 自动归档旧会话
8. **使用统计** - Token 使用量和费用统计

---

## 十、总结

本计划设计了一个支持群聊和角色系统的 AI 对话工具，主要特点：

- **成员统一抽象**：用户和 AI 都是成员，统一处理
- **群聊系统**：支持多 AI 参与讨论，内置防循环调用机制
- **角色系统**：不同 AI 可配置不同角色（Coder, UI Designer 等）
- **Coder 角色**：集成 Cline 的三层记忆系统和工具调用能力
- **提示词工程**：为每个角色设计专门的提示词，基于 CLEAR 原则和高级提示词技术
- **流式响应**：SSE 实时显示 AI 回复
- **数据持久化**：完整的会话记录存储

技术栈：
- **后端**：.NET 8.0 + ASP.NET Core Web API + Entity Framework Core
- **前端**：Svelte + SvelteKit + TypeScript + TailwindCSS
- **数据库**：SQLite（开发）/ PostgreSQL（生产）