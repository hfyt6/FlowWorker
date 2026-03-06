# 第二步：后端核心功能开发

## 目标
实现后端核心数据模型、服务层和 API 控制器，支持基本的会话和消息管理功能。

## 具体任务
- [ ] 2.1 实现数据实体：
  - Session 实体（会话信息）
  - Message 实体（消息内容）
  - ApiConfig 实体（API 配置）
- [ ] 2.2 实现 Repository 层：
  - 基础 Repository 接口和实现
  - SessionRepository（会话数据访问）
  - MessageRepository（消息数据访问）
  - ApiConfigRepository（API 配置数据访问）
- [ ] 2.3 实现 Service 层：
  - SessionService - 会话 CRUD 操作
  - MessageService - 消息 CRUD 操作
  - OpenAIService - OpenAI API 调用封装
- [ ] 2.4 实现 API Controllers：
  - SessionsController（会话管理 API）
  - MessagesController（消息管理 API）
  - ApiConfigsController（API 配置管理 API）
- [ ] 2.5 实现基础的 SSE 流式响应支持
- [ ] 2.6 实现错误处理和数据验证机制

## 预期成果
- 完整的数据模型定义
- 功能完整的 Repository 层
- 业务逻辑清晰的 Service 层
- RESTful API 接口实现
- 基础的流式响应能力
- 完善的错误处理机制

## 验收标准
- 所有实体能够正确映射到数据库表
- Repository 层能够完成所有 CRUD 操作
- Service 层能够正确处理业务逻辑
- API 控制器能够正常响应 HTTP 请求
- 能够通过 API 创建、读取、更新、删除会话和消息
- 错误情况能够返回适当的错误响应