# FlowWorker 前端项目

## 项目结构

```
frontend/
├── src/
│   ├── lib/
│   │   ├── components/      # 可复用组件
│   │   │   └── StreamMessage.svelte
│   │   ├── stores/          # 状态管理
│   │   │   ├── sessionStore.ts
│   │   │   ├── messageStore.ts
│   │   │   └── apiConfigStore.ts
│   │   ├── services/        # API 服务层
│   │   │   └── api.ts
│   │   └── types/           # TypeScript 类型定义
│   │       ├── index.ts
│   │       ├── session.ts
│   │       ├── message.ts
│   │       └── apiConfig.ts
│   ├── routes/              # 页面路由
│   │   ├── +page.svelte     # 首页
│   │   ├── sessions/+page.svelte       # 会话列表
│   │   ├── sessions/[id]/+page.svelte  # 会话详情
│   │   └── settings/+page.svelte       # 设置页面
│   ├── app.d.ts
│   └── app.html
├── static/
├── package.json
├── svelte.config.js
├── tsconfig.json
└── vite.config.ts
```

## 功能特性

- **会话管理**: 创建、删除、搜索会话
- **消息聊天**: 发送消息、流式响应、重新生成
- **API 配置**: 管理多个 API 配置，设置默认配置
- **响应式设计**: 支持桌面和移动设备

## 运行项目

### 前置要求

- Node.js 18+ 
- npm 或 pnpm

### 安装依赖

```bash
npm install
# 或
pnpm install
```

### 配置环境变量

在项目根目录创建 `.env` 文件：

```env
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

### 开发模式

```bash
npm run dev
# 或
pnpm dev
```

### 构建生产版本

```bash
npm run build
# 或
pnpm build
```

## 类型定义

### 会话相关

- `SessionListItemDto` - 会话列表项
- `SessionDetailDto` - 会话详情
- `CreateSessionRequest` - 创建会话请求
- `UpdateSessionRequest` - 更新会话请求

### 消息相关

- `MessageListItemDto` - 消息列表项
- `CreateMessageRequest` - 创建消息请求
- `SendMessageResponse` - 发送消息响应
- `StreamContentChunk` - 流式响应内容块

### API 配置相关

- `ApiConfigListItemDto` - API 配置列表项
- `ApiConfigDetailDto` - API 配置详情
- `CreateApiConfigRequest` - 创建配置请求
- `UpdateApiConfigRequest` - 更新配置请求

## 后端 API

后端 API 地址: `http://localhost:5000/api/v1`

### 会话 API

- `GET /sessions` - 获取会话列表
- `GET /sessions/{id}` - 获取会话详情
- `POST /sessions` - 创建新会话
- `PUT /sessions/{id}` - 更新会话
- `DELETE /sessions/{id}` - 删除会话
- `GET /sessions/search?title={title}` - 搜索会话

### 消息 API

- `GET /sessions/{sessionId}/messages` - 获取消息列表
- `POST /sessions/{sessionId}/messages` - 创建消息
- `POST /sessions/{sessionId}/messages/send` - 发送消息到 AI
- `POST /sessions/{sessionId}/messages/regenerate` - 重新生成回复
- `DELETE /messages/{id}` - 删除消息

### API 配置 API

- `GET /api-configs` - 获取配置列表
- `GET /api-configs/{id}` - 获取配置详情
- `GET /api-configs/default` - 获取默认配置
- `POST /api-configs` - 创建配置
- `PUT /api-configs/{id}` - 更新配置
- `DELETE /api-configs/{id}` - 删除配置
- `POST /api-configs/{id}/set-default` - 设置默认配置