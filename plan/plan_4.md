# 第四步：对话者系统与角色系统

## 目标
实现对话者（Participant）统一抽象和角色（Role）系统，为群聊功能奠定基础。对话者系统统一处理用户和AI，角色系统定义AI的行为模式和能力。

## 具体任务

### 4.1 数据库实体扩展
- [ ] 创建 Participant 实体（对话者表）：
  - Id, Name, Type (User/AI), RoleId, Avatar, Status, ApiConfigId, Model
  - 与 Session 的多对多关系（SessionParticipant关联表）
- [ ] 创建 Role 实体（角色表）：
  - Id, Name, DisplayName, Description, SystemPrompt, AllowedTools, IsBuiltIn
- [ ] 更新 Session 实体：
  - 添加 Type 字段（Single/Group）
  - 添加 CreatedBy 字段
- [ ] 更新 Message 实体：
  - 将 Role 字段改为 ParticipantId（关联对话者）
  - 添加 CallDepth 字段（防循环）
  - 添加 ReplyToMessageId 字段
- [ ] 创建数据库迁移

### 4.2 Repository 层实现
- [ ] IParticipantRepository 接口和实现
- [ ] IRoleRepository 接口和实现
- [ ] 更新 ISessionRepository 支持群聊查询
- [ ] 更新 IMessageRepository 支持按参与者查询

### 4.3 Service 层实现
- [ ] ParticipantService：
  - 创建AI对话者（绑定角色和API配置）
  - 获取对话者列表（区分User/AI类型）
  - 更新对话者信息
  - 删除AI对话者
- [ ] RoleService：
  - 获取内置角色列表（Coder, UI Designer, Architect, Reviewer, General）
  - 创建自定义角色
  - 更新角色配置
  - 删除自定义角色
  - 获取角色的系统提示词模板

### 4.4 API 控制器实现
- [ ] ParticipantsController：
  - GET /api/v1/participants - 获取对话者列表
  - POST /api/v1/participants - 创建AI对话者
  - GET /api/v1/participants/{id} - 获取对话者详情
  - PUT /api/v1/participants/{id} - 更新对话者
  - DELETE /api/v1/participants/{id} - 删除AI对话者
- [ ] RolesController：
  - GET /api/v1/roles - 获取角色列表
  - POST /api/v1/roles - 创建自定义角色
  - GET /api/v1/roles/{id} - 获取角色详情
  - PUT /api/v1/roles/{id} - 更新角色
  - DELETE /api/v1/roles/{id} - 删除自定义角色

### 4.5 前端实现
- [ ] 对话者管理页面（/participants）：
  - 显示所有对话者列表
  - 创建AI对话者（选择角色、API配置）
  - 编辑AI对话者信息
  - 删除AI对话者
- [ ] 角色管理页面（/roles）：
  - 显示所有角色列表（内置+自定义）
  - 创建自定义角色
  - 编辑角色配置（系统提示词、允许的工具）
  - 删除自定义角色
- [ ] 更新类型定义和API服务层

## 预期成果
- 完整的数据库实体支持对话者和角色
- 功能完整的对话者管理API
- 角色系统支持内置和自定义角色
- 前端对话者和角色管理界面
- 为群聊系统奠定基础

## 验收标准
- 能够创建、读取、更新、删除AI对话者
- 能够创建、读取、更新、删除自定义角色
- 内置角色（Coder, UI Designer等）能够正确初始化
- 对话者能够正确关联角色和API配置
- 前端界面能够管理对话者和角色
- 数据库迁移能够正常执行

## 技术要点

### 内置角色定义
```csharp
public static class BuiltInRoles
{
    public const string Coder = "coder";
    public const string UIDesigner = "ui-designer";
    public const string Architect = "architect";
    public const string Reviewer = "reviewer";
    public const string General = "general";
}
```

### 对话者类型枚举
```csharp
public enum ParticipantType
{
    User = 0,    // 人类用户
    AI = 1       // AI助手
}

public enum ParticipantStatus
{
    Offline = 0,
    Online = 1,
    Busy = 2
}
```

### 会话类型枚举
```csharp
public enum SessionType
{
    Single = 0,  // 单聊
    Group = 1    // 群聊
}
```
