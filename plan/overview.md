# AI 对话工具开发计划

## 项目概述

开发一个基于 OpenAI 接口协议的 AI 对话工具，支持多会话管理、群聊系统和角色系统。

---

## 一、需求分析

### 1.1 功能需求

#### 对话者系统（用户与AI统一抽象）

**对话者（Participant）概念**
- **用户对话者**：人类用户，通过界面输入消息
- **AI对话者**：AI助手，根据配置的角色和能力响应消息
- **统一抽象**：所有对话者都实现相同的接口，支持发送/接收消息

**对话者属性**
- `Id`：唯一标识符
- `Name`：显示名称
- `Type`：类型（User/AI）
- `Role`：角色（仅AI对话者，如coder、ui-designer等）
- `Avatar`：头像
- `Status`：在线/离线/忙碌状态

#### 角色系统

**角色定义**
每个AI对话者可以配置一个角色，决定其功能和行为：

| 角色 | 标识 | 功能描述 |
|------|------|----------|
| **Coder** | `coder` | 代码实现、文件操作、工具调用（参考Cline功能） |
| **UI Designer** | `ui-designer` | 界面设计、样式生成、组件建议 |
| **Architect** | `architect` | 架构设计、技术选型、方案规划 |
| **Reviewer** | `reviewer` | 代码审查、质量检查、建议优化 |
| **General** | `general` | 通用问答、信息查询 |

**角色配置**
- 每个角色有独立的系统提示词模板
- 角色决定可用的工具集（Coder可使用文件工具，UI Designer可使用设计工具）
- 角色可配置特定的API模型和参数

**Coder角色详细功能（参考Cline）**
- **短期记忆（Context Memory）**：滑动窗口管理对话历史，基于Token预算自动修剪
- **中期记忆（Task Memory）**：任务状态持久化、工具调用历史记录、文件修改追踪
- **长期记忆（Long-term Memory）**：自定义指令、模式偏好、全局配置记忆
- **工具调用**：支持文件读写、shell命令、代码分析等工具
- **对话循环**：支持工具调用后的递归继续，直到任务完成

#### 群聊系统

**群聊概念**
- 允许多个对话者（用户+多个AI）参与同一会话
- 每个群聊有独立的对话历史和上下文

**防循环调用机制（重要）**
- **调用深度限制**：单次用户消息触发的AI响应链最大深度为5层
- **调用令牌机制**：每个AI响应需要消耗调用令牌，令牌耗尽则停止
- **互斥响应机制**：同一轮次中，一个AI响应不会触发其他AI的自动响应
- **显式@机制**：AI只有在被显式@提及或主动选择参与时才响应
- **超时机制**：单轮对话总耗时超过5分钟自动终止

**群聊消息路由**
```
用户发送消息
    │
    ▼
系统解析消息中的@提及
    │
    ▼
被@的AI对话者响应（消耗1个调用令牌）
    │
    ▼
AI响应中如包含@其他AI，则被@的AI可继续响应（深度+1）
    │
    ▼
达到深度限制或令牌耗尽，本轮结束，等待用户新输入
```

#### 会话管理
- **创建会话**：创建新的对话会话，设置标题、选择AI模型、配置系统提示词
- **群聊会话**：创建群聊，邀请多个AI对话者参与
- **会话列表**：展示所有会话，支持按时间排序、搜索和筛选
- **会话详情**：查看会话中的完整对话记录
- **会话编辑**：修改会话标题、模型配置、系统提示词等属性
- **会话删除**：删除不需要的会话及其关联的所有消息
- **会话导出**：将会话记录导出为JSON、Markdown或TXT格式
- **会话导入**：从导出的文件中恢复会话数据

#### 消息管理
- **发送消息**：向会话发送消息，支持@提及特定AI
- **消息历史**：查看当前会话的完整消息历史
- **消息删除**：删除单条消息
- **重新生成**：请求AI重新生成上一条回复
- **停止生成**：在流式响应过程中可以中断生成
- **Token计数显示**：实时显示消息和会话的Token消耗

#### AI配置管理
- **多API配置**：支持配置多个OpenAI兼容的API服务
- **模型切换**：在不同会话中使用不同的AI模型
- **参数配置**：支持配置temperature、max_tokens等生成参数
- **默认配置**：设置默认的API配置和模型

#### 用户体验
- **实时响应**：支持SSE流式输出，实时显示AI回复
- **加载状态**：清晰显示请求处理中的状态
- **错误处理**：友好的错误提示和重试机制
- **响应式布局**：适配桌面和移动端

### 1.2 非功能需求

#### 性能要求
- API响应时间 < 200ms（不含AI生成时间）
- 支持至少100个并发会话
- 消息列表加载时间 < 1秒（1000条消息以内）

#### 数据安全
- API Key加密存储
- 敏感数据脱敏处理
- 支持数据备份和恢复

#### 可维护性
- 模块化设计，便于功能扩展
- 完整的日志记录
- 单元测试覆盖率 > 80%

#### 兼容性
- 支持OpenAI官方API
- 支持兼容OpenAI协议的第三方服务

---

## 二、技术选型

### 1. 后端技术栈（.NET）

| 技术/框架 | 版本 | 说明 |
|-----------|------|------|
| **.NET** | 8.0+ | 主要开发框架 |
| **ASP.NET Core Web API** | 8.0+ | Web API框架 |
| **Entity Framework Core** | 8.0+ | ORM框架 |
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
| **TailwindCSS** | UI样式 |
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
│  │SessionService│  │MessageService│  │ParticipantService│   │
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
│  │   - 确定目标AI对话者       │                                     │
│  │   - 检查调用令牌余额        │                                     │
│  └─────────────────────────┘                                     │
│              │                                                   │
│              ▼                                                   │
│  为每个被@的AI创建响应任务（并行或顺序）                          │
│              │                                                   │
│              ▼                                                   │
│  ┌─────────────────────────┐                                     │
│  │   调用深度检查（当前: 0）  │ ← 最大深度: 5                      │
│  │   调用令牌检查（剩余: N）  │ ← 每轮初始: 3                      │
│  └─────────────────────────┘                                     │
│              │                                                   │
│              ▼                                                   │
│  AI生成响应（流式）                                               │
│              │                                                   │
│              ▼                                                   │
│  响应中是否包含@其他AI？                                          │
│      ├─ 是 → 检查深度和令牌 → 继续路由                            │
│      └─ 否 → 本轮结束                                             │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  终止条件：                                               │     │
│  │  - 无更多@提及                                           │     │
│  │  - 调用深度达到5                                         │     │
│  │  - 调用令牌耗尽                                          │     │
│  │  - 用户主动停止                                          │     │
│  │  - 单轮超时（5分钟）                                      │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.3 Coder角色的对话循环（参考Cline）

```
┌─────────────────────────────────────────────────────────────────┐
│                   Coder角色对话循环 (runLoop)                    │
├─────────────────────────────────────────────────────────────────┤
│  1. 将用户输入添加到消息历史                                      │
│              │                                                   │
│              ▼                                                   │
│  2. 构建API请求消息（系统提示 + 历史 + 工具定义）                  │
│              │                                                   │
│              ▼                                                   │
│  3. 调用LLM Provider获取响应（流式）                              │
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
│  6. 检查是否需要继续（有工具调用则递归，深度+1）                     │
│              │                                                   │
│              ▼                                                   │
│  7. 循环终止（无工具调用/达到最大迭代/用户取消/深度限制）            │
└─────────────────────────────────────────────────────────────────┘
```

### 3.4 Coder角色的记忆系统架构（参考Cline）

```
┌─────────────────────────────────────────────────────────────┐
│                    Layer 1: Context Memory                   │
│                    (短期记忆 / 滑动窗口)                      │
│  - 当前会话的对话历史                                         │
│  - 自动修剪以保持Token预算                                   │
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
│  - API配置和默认设置                                         │
│  - 存储在全局存储或项目级存储中                                │
└─────────────────────────────────────────────────────────────┘
```

---

## 四、数据库设计

### 4.1 核心数据表结构

#### Participants 表（对话者表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| Name | string | 显示名称 |
| Type | enum | 类型（User=0, AI=1） |
| RoleId | GUID? | 关联的角色ID（仅AI类型） |
| Avatar | string? | 头像URL |
| Status | enum | 状态（Offline=0, Online=1, Busy=2） |
| ApiConfigId | GUID? | 关联的API配置（仅AI类型） |
| Model | string? | 使用的模型（仅AI类型） |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

#### Roles 表（角色表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| Name | string | 角色名称（coder/ui-designer/architect等） |
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
| Type | enum | 类型（Single=单聊, Group=群聊） |
| CreatedBy | GUID | 创建者（User Participant ID） |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |
| Metadata | json | 扩展元数据 |

#### SessionParticipants 表（会话参与者关联表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| SessionId | GUID | 会话ID |
| ParticipantId | GUID | 对话者ID |
| JoinedAt | datetime | 加入时间 |
| IsActive | bool | 是否活跃 |

#### Messages 表（消息表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| SessionId | GUID | 关联的会话ID |
| ParticipantId | GUID | 发送者ID |
| Content | string | 消息内容 |
| Tokens | int? | 消耗的Token数 |
| Model | string? | 使用的模型 |
| ReplyToMessageId | GUID? | 回复的消息ID |
| CallDepth | int | 调用深度（防循环） |
| CreatedAt | datetime | 创建时间 |
| Metadata | json | 扩展元数据 |

#### ApiConfigs 表（API配置表）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | GUID | 主键 |
| Name | string | 配置名称 |
| BaseUrl | string | API基础URL |
| ApiKey | string | API密钥（加密存储） |
| Model | string | 默认模型 |
| IsDefault | bool | 是否为默认配置 |
| CreatedAt | datetime | 创建时间 |

### 4.2 实体关系

```
Participants (1) ────── (N) SessionParticipants (N) ────── (1) Sessions
      │                                                        │
      │ (AI类型)                                               │
      ▼                                                        ▼
Roles (1)                                              Messages (N)
      │
      └────── (N) ApiConfigs

Sessions (1) ────── (N) Messages
Participants (1) ────── (N) Messages
```

---

## 五、API接口设计

### 5.1 RESTful API端点

#### 对话者管理
| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/v1/participants` | 获取对话者列表 |
| POST | `/api/v1/participants` | 创建AI对话者 |
| GET | `/api/v1/participants/{id}` | 获取对话者详情 |
| PUT | `/api/v1/participants/{id}` | 更新对话者信息 |
| DELETE | `/api/v1/participants/{id}` | 删除AI对话者 |

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
| POST | `/api/v1/sessions/{id}/participants` | 添加参与者 |
| DELETE | `/api/v1/sessions/{id}/participants/{pid}` | 移除参与者 |
| POST | `/api/v1/sessions/{id}/export` | 导出会话 |

#### 消息管理
| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/v1/sessions/{id}/messages` | 获取消息列表 |
| POST | `/api/v1/sessions/{id}/messages` | 发送消息 |
| DELETE | `/api/v1/sessions/{id}/messages/{msgId}` | 删除消息 |
| POST | `/api/v1/sessions/{id}/messages/regenerate` | 重新生成回复 |
| POST | `/api/v1/sessions/{id}/messages/stop` | 停止生成 |

#### API配置
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
│   ├── FlowWorker.Api/                 # API项目
│   │   ├── FlowWorker.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Controllers/
│   │   │   ├── v1/
│   │   │   │   ├── ParticipantsController.cs
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
│   │   │   ├── Participant.cs         # 对话者实体
│   │   │   ├── Role.cs                # 角色实体
│   │   │   ├── Session.cs             # 会话实体
│   │   │   ├── SessionParticipant.cs  # 会话参与者关联
│   │   │   ├── Message.cs             # 消息实体
│   │   │   └── ApiConfig.cs           # API配置实体
│   │   ├── Interfaces/
│   │   │   ├── IParticipantService.cs
│   │   │   ├── IRoleService.cs
│   │   │   ├── ISessionService.cs
│   │   │   ├── IMessageService.cs
│   │   │   ├── IOpenAIService.cs
│   │   │   └── IRepository.cs
│   │   ├── Services/
│   │   │   ├── ParticipantService.cs
│   │   │   ├── RoleService.cs
│   │   │   ├── SessionService.cs
│   │   │   ├── MessageService.cs
│   │   │   ├── GroupChatService.cs    # 群聊服务（防循环）
│   │   │   └── OpenAIService.cs
│   │   ├── Roles/                     # 角色实现
│   │   │   ├── CoderRoleService.cs    # Coder角色（含Cline功能）
│   │   │   ├── UIDesignerRoleService.cs
│   │   │   ├── ArchitectRoleService.cs
│   │   │   └── GeneralRoleService.cs
│   │   ├── DTOs/
│   │   │   ├── ParticipantDtos.cs
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
│   │   │   ├── ParticipantRepository.cs
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
│       │   ├── ParticipantType.cs
│       │   ├── ParticipantStatus.cs
│       │   ├── SessionType.cs
│       │   └── MessageRole.cs
│       └── Extensions/
│
├── .flowworker/                        # 数据存储目录
│   └── memory/
│       ├── tasks/                      # Coder角色任务记忆
│       └── instructions/               # 自定义指令
│
├── tests/
│   ├── FlowWorker.Api.Tests/
│   ├── FlowWorker.Core.Tests/
│   └── FlowWorker.Integration.Tests/
│
└── frontend/                           # Svelte前端
    ├── package.json
    ├── svelte.config.js
    ├── vite.config.js
    ├── src/
    │   ├── app.html
    │   ├── routes/
    │   │   ├── +layout.svelte
    │   │   ├── +page.svelte
    │   │   ├── participants/
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
    │   │   │   │   └── ParticipantList.svelte
    │   │   │   ├── Participants/
    │   │   │   │   └── ParticipantCard.svelte
    │   │   │   └── Roles/
    │   │   │       └── RoleForm.svelte
    │   │   ├── stores/
    │   │   │   ├── participantStore.ts
    │   │   │   ├── roleStore.ts
    │   │   │   ├── sessionStore.ts
    │   │   │   └── messageStore.ts
    │   │   └── services/
    │   │       └── api.ts
    │   └── app.css
    └── static/
```

---

## 七、任务编排

### 阶段一：基础架构（1-2天）

- [ ] 1.1 更新数据库实体（Participant, Role, SessionParticipant）
- [ ] 1.2 更新数据库上下文和迁移
- [ ] 1.3 实现对话者管理API
- [ ] 1.4 实现角色管理API

### 阶段二：群聊系统（2-3天）

- [ ] 2.1 实现群聊会话管理
- [ ] 2.2 实现消息路由服务
- [ ] 2.3 实现防循环调用机制
- [ ] 2.4 实现@提及解析
- [ ] 2.5 更新消息服务支持群聊

### 阶段三：角色系统（2-3天）

- [ ] 3.1 实现角色基类和接口
- [ ] 3.2 实现Coder角色（含Cline功能）
- [ ] 3.3 实现其他内置角色（UI Designer, Architect等）
- [ ] 3.4 实现角色特定的系统提示词
- [ ] 3.5 实现角色工具权限控制

### 阶段四：Coder角色增强（3-4天）

- [ ] 4.1 实现滑动窗口记忆管理
- [ ] 4.2 实现任务记忆持久化
- [ ] 4.3 实现工具调用系统
- [ ] 4.4 实现对话循环（runLoop）
- [ ] 4.5 实现文件操作工具

### 阶段五：前端适配（2-3天）

- [ ] 5.1 实现对话者管理页面
- [ ] 5.2 实现角色管理页面
- [ ] 5.3 更新会话页面支持群聊
- [ ] 5.4 实现@提及功能
- [ ] 5.5 实现参与者列表显示

### 阶段六：测试和优化（2天）

- [ ] 6.1 编写单元测试
- [ ] 6.2 测试群聊防循环机制
- [ ] 6.3 性能优化
- [ ] 6.4 代码审查和重构

---

## 八、扩展功能建议

1. **语音输入** - 支持语音消息输入
2. **文件共享** - 在群聊中共享文件
3. **AI间协作** - 支持AI之间的任务委托
4. **会话模板** - 预设不同场景的群聊模板
5. **权限系统** - 细粒度的参与者权限控制
6. **消息反应** - 对消息添加表情反应
7. **会话归档** - 自动归档旧会话
8. **使用统计** - Token使用量和费用统计

---

## 九、总结

本计划设计了一个支持群聊和角色系统的AI对话工具，主要特点：

- **对话者统一抽象**：用户和AI都是对话者，统一处理
- **群聊系统**：支持多AI参与讨论，内置防循环调用机制
- **角色系统**：不同AI可配置不同角色（Coder, UI Designer等）
- **Coder角色**：集成Cline的三层记忆系统和工具调用能力
- **流式响应**：SSE实时显示AI回复
- **数据持久化**：完整的会话记录存储

技术栈：
- **后端**：.NET 8.0 + ASP.NET Core Web API + Entity Framework Core
- **前端**：Svelte + SvelteKit + TypeScript + TailwindCSS
- **数据库**：SQLite（开发）/ PostgreSQL（生产）
