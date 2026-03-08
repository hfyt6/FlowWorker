# 第四步：成员系统与角色系统

## 目标
实现成员（Member）统一抽象和角色（Role）系统，为群聊功能奠定基础。成员系统统一处理用户和AI，角色系统定义AI的行为模式和能力。

## 具体任务

### 4.1 数据库实体扩展
- [ ] 创建 Member 实体（成员表）：
  - Id, Name, Type (User/AI), RoleId, Avatar, Status, ApiConfigId, Model
  - 与 Session 的多对多关系（SessionMember关联表）
- [ ] 创建 Role 实体（角色表）：
  - Id, Name, DisplayName, Description, SystemPrompt, AllowedTools, IsBuiltIn
- [ ] 更新 Session 实体：
  - 添加 Type 字段（Single/Group）
  - 添加 CreatedBy 字段
- [ ] 更新 Message 实体：
  - 将 Role 字段改为 MemberId（关联成员）
  - 添加 CallDepth 字段（防循环）
  - 添加 ReplyToMessageId 字段
- [ ] 创建数据库迁移

### 4.2 Repository 层实现
- [ ] IMemberRepository 接口和实现
- [ ] IRoleRepository 接口和实现
- [ ] 更新 ISessionRepository 支持群聊查询
- [ ] 更新 IMessageRepository 支持按参与者查询

### 4.3 Service 层实现
- [ ] MemberService：
  - 创建AI成员（绑定角色和API配置）
  - 获取成员列表（区分User/AI类型）
  - 更新成员信息
  - 删除AI成员
- [ ] RoleService：
  - 获取内置角色列表（Coder, UI Designer, Architect, Reviewer, General）
  - 创建自定义角色
  - 更新角色配置
  - 删除自定义角色
  - 获取角色的系统提示词模板

### 4.4 API 控制器实现
- [ ] MembersController：
  - GET /api/v1/members - 获取成员列表
  - POST /api/v1/members - 创建AI成员
  - GET /api/v1/members/{id} - 获取成员详情
  - PUT /api/v1/members/{id} - 更新成员
  - DELETE /api/v1/members/{id} - 删除AI成员
- [ ] RolesController：
  - GET /api/v1/roles - 获取角色列表
  - POST /api/v1/roles - 创建自定义角色
  - GET /api/v1/roles/{id} - 获取角色详情
  - PUT /api/v1/roles/{id} - 更新角色
  - DELETE /api/v1/roles/{id} - 删除自定义角色

### 4.5 前端实现
- [ ] 成员管理页面（/members）：
  - 显示所有成员列表
  - 创建AI成员（选择角色、API配置）
  - 编辑AI成员信息
  - 删除AI成员
- [ ] 角色管理页面（/roles）：
  - 显示所有角色列表（内置+自定义）
  - 创建自定义角色
  - 编辑角色配置（系统提示词、允许的工具）
  - 删除自定义角色
- [ ] 更新类型定义和API服务层

## 预期成果
- 完整的数据库实体支持成员和角色
- 功能完整的成员管理API
- 角色系统支持内置和自定义角色
- 前端成员和角色管理界面
- 为群聊系统奠定基础

## 验收标准
- 能够创建、读取、更新、删除AI成员
- 能够创建、读取、更新、删除自定义角色
- 内置角色（Coder, UI Designer等）能够正确初始化
- 成员能够正确关联角色和API配置
- 前端界面能够管理成员和角色
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

### 成员类型枚举
```csharp
public enum MemberType
{
    User = 0,    // 人类用户
    AI = 1       // AI助手
}

public enum MemberStatus
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
