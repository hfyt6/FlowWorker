# 第八步：测试、部署与文档

## 目标
完成全面的测试覆盖，实现容器化部署，编写完整的文档，确保项目可维护性和生产就绪。

## 具体任务

### 8.1 测试体系建设

#### 8.1.1 后端单元测试
- [ ] 创建测试项目结构：
  - FlowWorker.Core.Tests（核心业务测试）
  - FlowWorker.Api.Tests（API控制器测试）
  - FlowWorker.Infrastructure.Tests（基础设施测试）
- [ ] 编写 Service 层单元测试：
  - SessionService 测试（CRUD、业务逻辑）
  - MessageService 测试（消息发送、流式响应）
  - MemberService 测试（成员管理）
  - RoleService 测试（角色管理）
  - 目标覆盖率 > 80%
- [ ] 编写 Repository 层单元测试：
  - 使用 InMemory 数据库
  - 测试所有 CRUD 操作
  - 测试复杂查询
- [ ] 编写工具类测试：
  - Token 计数器测试
  - 工具调用解析测试
  - 滑动窗口管理测试

#### 8.1.2 集成测试
- [ ] 编写 API 集成测试：
  - 使用 TestServer
  - 测试所有 API 端点
  - 测试错误处理
- [ ] 编写数据库集成测试：
  - 使用 SQLite 测试数据库
  - 测试迁移
  - 测试事务
- [ ] 编写对话循环集成测试：
  - 测试完整对话流程
  - 测试工具调用链
  - 测试群聊消息路由

#### 8.1.3 前端测试
- [ ] 配置测试环境：
  - Vitest 测试框架
  - Testing Library
  - jsdom 环境
- [ ] 编写组件单元测试：
  - 消息列表组件测试
  - 聊天输入组件测试
  - 会话列表组件测试
- [ ] 编写 Store 测试：
  - sessionStore 测试
  - messageStore 测试
  - apiConfigStore 测试
- [ ] 编写 E2E 测试：
  - 使用 Playwright
  - 关键用户流程测试
  - 跨浏览器测试

### 8.2 Docker 容器化

#### 8.2.1 后端容器化
- [ ] 创建后端 Dockerfile：
  - 多阶段构建（构建/运行）
  - .NET 8.0 运行时
  - 健康检查配置
  - 非 root 用户运行
- [ ] 优化镜像大小：
  - 使用 Alpine 基础镜像
  - 清理构建缓存
  - 多阶段构建优化

#### 8.2.2 前端容器化
- [ ] 创建前端 Dockerfile：
  - Node.js 构建环境
  - Nginx 生产服务器
  - 静态资源服务
  - Gzip 压缩配置
- [ ] 配置 Nginx：
  - 反向代理配置
  - 静态文件缓存
  - SPA 路由支持

#### 8.2.3 Docker Compose 配置
- [ ] 创建 docker-compose.yml：
  - 后端服务配置
  - 前端服务配置
  - 数据库服务（SQLite/PostgreSQL）
  - 数据卷配置
- [ ] 创建 docker-compose.prod.yml：
  - 生产环境优化
  - 环境变量配置
  - 日志配置
- [ ] 创建 docker-compose.override.yml：
  - 开发环境配置
  - 热重载支持
  - 调试配置

### 8.3 CI/CD 配置

#### 8.3.1 GitHub Actions 配置
- [ ] 创建 CI 工作流：
  - 代码提交触发
  - 自动运行测试
  - 代码质量检查
  - 构建验证
- [ ] 创建 CD 工作流：
  - 自动构建镜像
  - 推送到镜像仓库
  - 部署到测试环境
- [ ] 配置代码质量检查：
  - SonarQube 集成
  - Codecov 覆盖率报告
  - Dependabot 依赖更新

#### 8.3.2 发布管理
- [ ] 版本号管理：
  - Semantic Versioning
  - Git 标签管理
  - 版本发布流程
- [ ] 发布说明：
  - 自动生成 Changelog
  - 版本发布说明
  - 迁移指南

### 8.4 生产环境配置

#### 8.4.1 数据库配置
- [ ] PostgreSQL 配置：
  - 生产数据库连接
  - 连接池配置
  - 备份策略
- [ ] 数据迁移：
  - 生产环境迁移
  - 数据备份脚本
  - 数据恢复脚本

#### 8.4.2 安全配置
- [ ] HTTPS 配置：
  - SSL 证书配置
  - HSTS 配置
  - 安全响应头
- [ ] API 安全配置：
  - API Key 加密存储
  - 请求限流
  - CORS 配置
- [ ] 日志和监控：
  - 结构化日志配置
  - 错误追踪（Sentry）
  - 性能监控

### 8.5 文档编写

#### 8.5.1 API 文档
- [ ] Swagger/OpenAPI 完善：
  - 所有端点文档化
  - 请求/响应示例
  - 错误码说明
  - 认证说明
- [ ] API 使用指南：
  - 快速开始
  - 认证方式
  - 常见用例
  - SDK 示例

#### 8.5.2 开发文档
- [ ] 架构文档：
  - 系统架构图
  - 数据流图
  - 模块说明
- [ ] 开发指南：
  - 开发环境搭建
  - 代码规范
  - 提交规范
  - 分支策略
- [ ] 贡献指南：
  - 如何贡献代码
  - Issue 模板
  - PR 模板

#### 8.5.3 用户文档
- [ ] 快速开始指南：
  - 安装说明
  - 首次配置
  - 创建第一个会话
- [ ] 功能使用说明：
  - 会话管理
  - 群聊功能
  - 角色配置
  - 工具使用
- [ ] 配置管理指南：
  - API 配置
  - 模型参数
  - 自定义指令
- [ ] 故障排除：
  - 常见问题
  - 错误处理
  - 联系支持

### 8.6 项目交付

#### 8.6.1 版本发布
- [ ] 准备发布版本：
  - 版本号更新
  - 发布说明编写
  - 标签创建
- [ ] 发布制品：
  - Docker 镜像
  - 二进制文件
  - 源码包

#### 8.6.2 演示环境
- [ ] 搭建演示环境：
  - 云服务器部署
  - 示例数据准备
  - 演示脚本编写
- [ ] 最终验收测试：
  - 功能验收
  - 性能验收
  - 安全验收

## 预期成果
- 完整的测试覆盖（单元测试 > 80%）
- 容器化部署方案（Docker + Docker Compose）
- 自动化 CI/CD 流水线
- 生产就绪的配置
- 全面的文档（API、开发、用户）
- 成功的项目交付

## 验收标准
- 单元测试覆盖率 > 80%
- 所有集成测试通过
- E2E 测试覆盖关键流程
- 能够通过 docker-compose 一键部署
- API 文档覆盖所有端点
- 用户文档能够帮助新用户快速上手
- CI/CD 流水线能够自动执行测试和部署
- 生产环境配置满足安全和性能要求
- 项目通过最终验收测试

## 技术要点

### 测试项目结构
```
tests/
├── FlowWorker.Core.Tests/
│   ├── Services/
│   │   ├── SessionServiceTests.cs
│   │   ├── MessageServiceTests.cs
│   │   └── MemberServiceTests.cs
│   └── Tools/
│       └── TokenCounterTests.cs
├── FlowWorker.Api.Tests/
│   ├── Controllers/
│   │   ├── SessionsControllerTests.cs
│   │   └── MessagesControllerTests.cs
│   └── Integration/
│       └── ApiIntegrationTests.cs
└── FlowWorker.Frontend.Tests/
    ├── components/
    ├── stores/
    └── e2e/
```

### Docker Compose 示例
```yaml
version: '3.8'
services:
  backend:
    build: ./src/FlowWorker.Api
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./data:/app/data
    
  frontend:
    build: ./frontend
    ports:
      - "3000:80"
    depends_on:
      - backend
```

### CI/CD 流程
```
代码提交 → 触发 CI → 运行测试 → 构建镜像 → 推送仓库 → 部署测试
    ↓
创建 Release → 生成 Changelog → 打标签 → 发布制品
```

## 附录：文档清单

### 必须文档
- [ ] README.md（项目说明）
- [ ] docs/ARCHITECTURE.md（架构文档）
- [ ] docs/API.md（API文档）
- [ ] docs/DEVELOPMENT.md（开发指南）
- [ ] docs/USER_GUIDE.md（用户指南）
- [ ] docs/DEPLOYMENT.md（部署指南）
- [ ] CHANGELOG.md（变更日志）
- [ ] LICENSE（许可证）

### 可选文档
- [ ] docs/CONTRIBUTING.md（贡献指南）
- [ ] docs/SECURITY.md（安全政策）
- [ ] docs/FAQ.md（常见问题）
- [ ] docs/TROUBLESHOOTING.md（故障排除）
