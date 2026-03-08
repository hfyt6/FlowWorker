# 第七步：Coder 角色增强与工具系统

## 目标
实现 Coder 角色的高级功能，包括三层记忆系统、工具调用系统和对话循环，参考 Cline 的功能设计。

## 具体任务

### 7.1 三层记忆系统实现

#### 7.1.1 短期记忆（Context Memory - 滑动窗口）
- [ ] 实现 SlidingWindowManager：
  - Token 预算管理（默认 4000 tokens）
  - 消息重要性评分算法
  - 自动修剪策略（保留系统提示、工具结果）
  - 上下文构建器（ContextBuilder）
- [ ] 实现 Token 计数器：
  - 基于 tiktoken 的 Token 估算
  - 消息 Token 计算
  - 系统提示词 Token 计算
- [ ] 集成到 MessageService：
  - 发送消息前进行上下文修剪
  - 保持关键消息不被裁剪

#### 7.1.2 中期记忆（Task Memory - 任务级）
- [ ] 创建 Task 实体：
  - Id, SessionId, Title, Status, CreatedAt, UpdatedAt
  - 关联的工具调用记录
  - 文件修改历史
- [ ] 实现 TaskStorage 服务：
  - 任务状态持久化到文件系统（.flowworker/memory/tasks/）
  - 任务创建、更新、查询、删除
  - 任务恢复机制
- [ ] 实现 ToolCallRecord：
  - 记录每次工具调用的详细信息
  - 调用参数和返回结果
  - 执行时间和状态
- [ ] 实现 FileModificationTracker：
  - 追踪文件创建、修改、删除
  - 文件变更历史记录
  - 支持撤销操作

#### 7.1.3 长期记忆（Long-term Memory - 跨任务）
- [ ] 实现 CustomInstructionStorage：
  - 自定义指令存储（全局/工作区级别）
  - 指令编辑和管理
  - 指令应用到会话
- [ ] 实现 ModePreferences：
  - 不同模式（Code/Architect/Ask）的偏好设置
  - 模式切换时自动应用
- [ ] 实现全局配置记忆：
  - API 配置默认设置
  - 界面偏好设置
  - 快捷键配置

### 7.2 工具系统实现

#### 7.2.1 工具框架
- [ ] 定义工具接口和基类：
  - ITool 接口（Name, Description, Parameters, Execute）
  - ToolBase 抽象基类
  - ToolResult 结果封装
- [ ] 实现工具注册中心（ToolRegistry）：
  - 工具发现和注册
  - 按角色过滤可用工具
  - 工具元数据管理
- [ ] 实现工具执行引擎：
  - 工具调用解析
  - 参数验证
  - 执行和错误处理
  - 结果格式化

#### 7.2.2 内置工具实现
- [ ] 文件操作工具：
  - read_file - 读取文件内容
  - write_file - 写入文件内容
  - replace_in_file - 局部修改文件
  - list_files - 列出目录文件
  - search_files - 搜索文件内容
- [ ] 代码分析工具：
  - analyze_code - 代码分析
  - find_symbols - 查找符号定义
- [ ] 执行工具：
  - execute_command - 执行 Shell 命令
  - check_syntax - 语法检查
- [ ] 其他工具：
  - ask_followup - 向用户询问
  - attempt_completion - 完成任务

#### 7.2.3 工具调用协议
- [ ] 实现工具定义格式（OpenAI Function Calling 兼容）：
  - 工具名称和描述
  - 参数 Schema 定义
  - 必需参数标记
- [ ] 实现工具调用解析：
  - 从 AI 响应中提取工具调用
  - 参数 JSON 解析
  - 批量工具调用支持
- [ ] 实现工具结果反馈：
  - 工具执行结果格式化
  - 错误信息处理
  - 结果添加到消息历史

### 7.3 对话循环实现（runLoop）

#### 7.3.1 核心对话循环
- [ ] 实现 CoderRoleService：
  - 初始化对话循环
  - 管理消息历史
  - 协调工具调用
- [ ] 实现对话循环逻辑：
  ```
  1. 接收用户输入
  2. 构建 API 请求（系统提示 + 历史 + 工具定义）
  3. 调用 LLM 获取响应（流式）
  4. 处理响应内容
     - 普通内容 → 显示给用户
     - 工具调用 → 执行工具
  5. 如有工具调用，将结果加入历史
  6. 递归调用（深度 +1，检查最大迭代）
  7. 无工具调用时结束循环
  ```
- [ ] 实现循环控制：
  - 最大迭代次数限制（默认 10 次）
  - 用户取消支持
  - 错误阈值控制
  - 超时处理

#### 7.3.2 流式响应处理增强
- [ ] 实现流解析器（StreamParser）：
  - 解析 SSE 流中的内容块
  - 识别工具调用块
  - 增量内容更新
- [ ] 实现内容收集器：
  - 收集流式内容片段
  - 组装完整响应
  - 提取工具调用信息
- [ ] 更新前端流式显示：
  - 实时显示思考过程
  - 显示工具调用状态
  - 显示执行结果

### 7.4 Coder 角色配置

#### 7.4.1 系统提示词模板
- [ ] 实现 Coder 角色系统提示词：
  - 角色定义和能力说明
  - 工具使用指南
  - 代码规范要求
  - 响应格式要求
- [ ] 支持提示词模板变量：
  - {customInstructions} - 自定义指令
  - {mode} - 当前模式
  - {workspace} - 工作区路径

#### 7.4.2 角色特定配置
- [ ] 实现 CoderRoleOptions：
  - 最大迭代次数
  - Token 预算
  - 工具白名单/黑名单
  - 自动执行设置
- [ ] 实现模式切换：
  - Code 模式（专注代码实现）
  - Architect 模式（专注架构设计）
  - Ask 模式（专注问答）

### 7.5 前端适配

#### 7.5.1 任务记忆界面
- [ ] 实现任务列表面板：
  - 显示当前会话的任务
  - 任务状态指示
  - 任务详情查看
- [ ] 实现工具调用历史：
  - 显示工具调用记录
  - 查看调用参数和结果
  - 支持重新执行

#### 7.5.2 工具执行界面
- [ ] 实现工具调用显示：
  - 显示正在执行的工具
  - 显示工具执行进度
  - 显示工具执行结果
- [ ] 实现文件变更显示：
  - 显示修改的文件列表
  - 文件变更对比视图
  - 撤销文件修改

#### 7.5.3 自定义指令编辑器
- [ ] 实现指令编辑界面：
  - 全局指令编辑
  - 工作区指令编辑
  - 实时预览效果

## 预期成果
- 完整的三层记忆系统架构
- 功能完善的工具系统
- 支持工具调用的对话循环
- Coder 角色的高级功能实现
- 前端适配工具执行和记忆管理

## 验收标准
- 滑动窗口能够根据 Token 预算自动修剪历史
- 任务状态能够正确持久化和恢复
- 工具能够被正确注册、调用和执行
- 对话循环能够正确处理工具调用和递归
- 流式响应能够实时显示工具执行状态
- 文件修改能够被正确追踪和记录
- 自定义指令能够按作用域正确应用
- 最大迭代次数能够正确限制循环深度

## 技术要点

### 工具定义示例
```json
{
  "name": "read_file",
  "description": "读取指定文件的内容",
  "parameters": {
    "type": "object",
    "properties": {
      "path": {
        "type": "string",
        "description": "文件路径"
      }
    },
    "required": ["path"]
  }
}
```

### 对话循环状态机
```
[Idle] → 用户输入 → [Processing]
[Processing] → 流式响应 → [Streaming]
[Streaming] → 工具调用 → [ToolExecuting]
[ToolExecuting] → 执行完成 → [Processing]
[Streaming] → 无工具调用 → [Complete]
[Processing] → 达到最大迭代 → [Complete]
[任何状态] → 用户取消 → [Cancelled]
```

### 记忆系统存储结构
```
.flowworker/
├── memory/
│   ├── tasks/
│   │   ├── {task_id}.json
│   │   └── ...
│   ├── instructions/
│   │   ├── global.json
│   │   └── {workspace_id}.json
│   └── preferences/
│       └── mode_preferences.json