# 第六步：提示词工程实现

## 目标
基于 docs/prompt_tech.md 和 docs/prompt_cline_tech.md 的技术文档，为每个角色设计并实现专门的提示词工程方案，包括提示词模板管理系统和高级提示词技术。

## 具体任务

### 6.1 提示词模板管理系统

#### 6.1.1 提示词存储结构
- [ ] 创建提示词模板目录结构：
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
- [ ] 实现 PromptTemplate 实体：
  - Id, Name, Role, Content, Variables, IsBuiltIn
- [ ] 实现提示词模板 Repository：
  - IPromptTemplateRepository 接口
  - PromptTemplateRepository 实现
  - 支持模板的 CRUD 操作

#### 6.1.2 提示词模板服务
- [ ] 实现 PromptTemplateService：
  - 获取角色对应的系统提示词
  - 模板变量替换功能
  - 支持自定义指令注入
- [ ] 实现模板变量解析器：
  - `{customInstructions}` - 用户自定义指令
  - `{workspace}` - 工作区路径
  - `{mode}` - 当前模式
  - `{history}` - 对话历史摘要
- [ ] 实现模板缓存机制：
  - 内存缓存常用模板
  - 支持模板热重载

#### 6.1.3 提示词模板 API
- [ ] 实现 PromptsController：
  - GET /api/v1/prompts - 获取提示词模板列表
  - GET /api/v1/prompts/{role} - 获取角色提示词
  - PUT /api/v1/prompts/{id} - 更新自定义模板
  - POST /api/v1/prompts/{role}/reset - 重置为内置模板
- [ ] 实现模板预览 API：
  - POST /api/v1/prompts/preview - 预览渲染后的提示词

### 6.2 Coder 角色提示词实现（参考 Cline）

#### 6.2.1 系统提示词架构
- [ ] 实现 Coder 角色系统提示词：
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

#### 6.2.2 高级提示词技术集成
- [ ] 实现思维链（Chain of Thought）：
  - 使用 `<thinking></thinking>` 标签进行推理
  - 在复杂任务中引导模型展示推理过程
- [ ] 实现任务进度追踪：
  - 使用 `<task_progress>` 标签跟踪任务状态
  - 支持 Markdown checklist 格式
- [ ] 实现迭代式执行：
  - 逐步验证，确保准确性
  - 支持工具调用后的递归继续
- [ ] 实现上下文管理：
  - 滑动窗口管理对话历史
  - 基于 Token 预算自动修剪

#### 6.2.3 Coder 角色配置
- [ ] 实现 CoderRoleOptions：
  - 最大迭代次数配置
  - Token 预算设置
  - 工具白名单/黑名单
  - 自动执行设置
- [ ] 实现模式切换：
  - Code 模式（专注代码实现）
  - Architect 模式（专注架构设计）
  - Ask 模式（专注问答）

### 6.3 UI Designer 角色提示词实现

#### 6.3.1 系统提示词
- [ ] 实现 UI Designer 角色系统提示词：
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

#### 6.3.2 UI Designer 特定功能
- [ ] 实现设计建议生成
- [ ] 实现色彩搭配推荐
- [ ] 实现组件库引用

### 6.4 Architect 角色提示词实现

#### 6.4.1 系统提示词
- [ ] 实现 Architect 角色系统提示词：
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

#### 6.4.2 Architect 特定功能
- [ ] 实现架构模式库
- [ ] 实现技术选型评估框架
- [ ] 实现风险评估矩阵

### 6.5 Reviewer 角色提示词实现

#### 6.5.1 系统提示词
- [ ] 实现 Reviewer 角色系统提示词：
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

#### 6.5.2 Reviewer 特定功能
- [ ] 实现代码质量评分
- [ ] 实现问题分类系统
- [ ] 实现最佳实践检查清单

### 6.6 General 角色提示词实现

#### 6.6.1 系统提示词
- [ ] 实现 General 角色系统提示词：
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

### 6.7 多 Agent 协作提示词策略

#### 6.7.1 角色分配提示词
- [ ] 实现团队协作配置：
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

#### 6.7.2 对话协调提示词
- [ ] 实现讨论流程管理：
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

### 6.8 高级提示词技术实现

#### 6.8.1 Zero-shot Prompting
- [ ] 实现直接任务执行

#### 6.8.2 Few-shot Prompting
- [ ] 实现示例学习
- [ ] 创建示例库

#### 6.8.3 Chain of Thought (CoT)
- [ ] 实现推理过程引导
- [ ] 实现"逐步思考"模式

#### 6.8.4 Tree of Thoughts (ToT)
- [ ] 实现多路径探索
- [ ] 实现角度分析模式

#### 6.8.5 ReAct (Reasoning + Acting)
- [ ] 实现推理与行动结合
- [ ] 实现工具调用链

#### 6.8.6 Self-Reflection
- [ ] 实现自我审查机制
- [ ] 实现错误检测和修正

### 6.9 前端提示词管理界面

#### 6.9.1 提示词模板管理
- [ ] 实现提示词模板列表页面
- [ ] 实现提示词编辑器
- [ ] 实现模板预览功能
- [ ] 实现变量高亮显示

#### 6.9.2 角色提示词配置
- [ ] 实现角色提示词配置页面
- [ ] 实现自定义指令编辑
- [ ] 实现模式切换配置

## 预期成果
- 完整的提示词模板管理系统
- 五个角色的系统提示词实现
- Coder 角色的高级提示词技术（参考 Cline）
- 多 Agent 协作提示词策略
- 高级提示词技术实现
- 前端提示词管理界面

## 验收标准
- 能够正确加载和渲染角色提示词
- 模板变量能够正确替换
- Coder 角色支持思维链和任务进度追踪
- 多 Agent 协作提示词能够正确引导对话
- 高级提示词技术能够正常工作
- 前端能够编辑和预览提示词模板

## 技术要点

### 提示词模板数据结构
```csharp
public class PromptTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }  // coder, ui-designer, etc.
    public string Content { get; set; }
    public List<string> Variables { get; set; }
    public bool IsBuiltIn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 模板变量替换示例
```csharp
public string RenderTemplate(string template, Dictionary<string, string> variables)
{
    var result = template;
    foreach (var kvp in variables)
    {
        result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
    }
    return result;
}
```

### Coder 角色提示词关键点
- 角色定义清晰明确
- 工具使用规范详细
- 工作流程结构化
- 支持高级提示词技术（CoT, task_progress）