<script lang="ts">
    import { marked } from 'marked';
    import { onMount } from 'svelte';

    interface Props {
        content: string;
    }

    let { content }: Props = $props();

    // 配置 marked 选项
    marked.setOptions({
        breaks: true,  // 将换行符转换为 <br>
        gfm: true,     // 启用 GitHub Flavored Markdown
    });

    // 渲染 Markdown 内容，正确处理 thinking 标签
    function renderMarkdown(text: string): string {
        if (!text) return '';
        try {
            // 首先处理被代码块包裹的 thinking 标签
            // 匹配 ```xml <thinking>...</thinking> ``` 或 ``` <thinking>...</thinking> ``` 格式
            const codeBlockThinkingRegex = /```\s*(xml)?\s*<thinking>([\s\S]*?)<\/thinking>\s*```/g;
            text = text.replace(codeBlockThinkingRegex, (match, lang, content) => {
                return `<thinking>${content.trim()}</thinking>`;
            });
            
            // 使用正则表达式匹配 <thinking>...</thinking> 标签，并分割文本
            const thinkingRegex = /<thinking>([\s\S]*?)<\/thinking>/g;
            const parts: Array<{type: 'text' | 'thinking', content: string, id?: string}> = [];
            let lastIndex = 0;
            let thinkingCount = 0;
            let match;
            
            while ((match = thinkingRegex.exec(text)) !== null) {
                // 添加 thinking 标签前的文本
                if (match.index > lastIndex) {
                    parts.push({ type: 'text', content: text.slice(lastIndex, match.index) });
                }
                // 添加 thinking 内容
                const id = `thinking-${thinkingCount++}`;
                parts.push({ type: 'thinking', content: match[1].trim(), id });
                lastIndex = match.index + match[0].length;
            }
            
            // 添加剩余文本
            if (lastIndex < text.length) {
                parts.push({ type: 'text', content: text.slice(lastIndex) });
            }
            
            // 如果没有 thinking 标签，直接渲染 Markdown
            if (parts.length === 0 || !parts.some(p => p.type === 'thinking')) {
                return marked.parse(text) as string;
            }
            
            // 分别渲染各部分，thinking 块默认折叠
            let result = '';
            for (const part of parts) {
                if (part.type === 'text') {
                    result += marked.parse(part.content) as string;
                } else {
                    // 对 thinking 内容也进行 Markdown 渲染
                    const renderedContent = marked.parse(part.content) as string;
                    result += `
                        <div class="thinking-block" data-thinking-id="${part.id}">
                            <div class="thinking-header">
                                <svg class="thinking-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="transform: rotate(-90deg); transition: transform 0.2s;"><path d="m6 9 6 6 6-6"/></svg>
                                <span class="thinking-title">查看思考过程</span>
                            </div>
                            <div class="thinking-content" style="display: none;">
                                <div class="thinking-content-inner">${renderedContent}</div>
                            </div>
                        </div>
                    `;
                }
            }
            
            return result;
        } catch (e) {
            console.error('Markdown parsing error:', e);
            return text;
        }
    }

    // 切换 thinking 块的折叠状态（直接操作 DOM）
    function toggleThinkingBlock(header: HTMLElement) {
        const block = header.closest('.thinking-block');
        if (!block) return;
        
        const content = block.querySelector('.thinking-content') as HTMLElement;
        const icon = header.querySelector('.thinking-icon') as HTMLElement;
        const title = header.querySelector('.thinking-title') as HTMLElement;
        
        if (!content || !icon || !title) return;
        
        const isCollapsed = content.style.display === 'none';
        
        if (isCollapsed) {
            // 展开
            content.style.display = 'block';
            icon.style.transform = 'rotate(0deg)';
            title.textContent = '隐藏思考过程';
        } else {
            // 折叠
            content.style.display = 'none';
            icon.style.transform = 'rotate(-90deg)';
            title.textContent = '查看思考过程';
        }
    }

    // 使用事件委托处理 thinking 块的点击
    onMount(() => {
        function handleClick(e: MouseEvent) {
            const target = e.target as HTMLElement;
            const header = target.closest('.thinking-header');
            if (header) {
                toggleThinkingBlock(header as HTMLElement);
            }
        }
        
        document.addEventListener('click', handleClick);
        
        return () => {
            document.removeEventListener('click', handleClick);
        };
    });
</script>

<div class="markdown-content">
    {@html renderMarkdown(content)}
</div>

<style>
    /* Thinking 折叠块样式 */
    .markdown-content :global(.thinking-block) {
        margin: 0.75rem 0;
        border: 1px solid rgba(96, 165, 250, 0.2);
        border-radius: 0.5rem;
        background: rgba(15, 23, 42, 0.5);
        overflow: hidden;
    }

    .markdown-content :global(.thinking-header) {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.625rem 0.875rem;
        background: rgba(96, 165, 250, 0.1);
        cursor: pointer;
        transition: background 0.2s;
        user-select: none;
    }

    .markdown-content :global(.thinking-header:hover) {
        background: rgba(96, 165, 250, 0.15);
    }

    .markdown-content :global(.thinking-icon) {
        color: #60a5fa;
        transition: transform 0.2s;
    }

    .markdown-content :global(.thinking-title) {
        font-size: 0.8125rem;
        font-weight: 500;
        color: #94a3b8;
    }

    .markdown-content :global(.thinking-content) {
        border-top: 1px solid rgba(96, 165, 250, 0.1);
    }

    .markdown-content :global(.thinking-content-inner) {
        padding: 0.875rem 1rem;
        background: rgba(0, 0, 0, 0.2);
        font-size: 0.875rem;
        line-height: 1.6;
        color: #cbd5e1;
    }

    .markdown-content :global(h1) {
        font-size: 1.5rem;
        font-weight: 600;
        margin: 1rem 0 0.75rem;
        color: #f1f5f9;
    }

    .markdown-content :global(h2) {
        font-size: 1.25rem;
        font-weight: 600;
        margin: 1rem 0 0.75rem;
        color: #f1f5f9;
    }

    .markdown-content :global(h3) {
        font-size: 1.125rem;
        font-weight: 600;
        margin: 0.875rem 0 0.625rem;
        color: #f1f5f9;
    }

    .markdown-content :global(h4),
    .markdown-content :global(h5),
    .markdown-content :global(h6) {
        font-size: 1rem;
        font-weight: 600;
        margin: 0.75rem 0 0.5rem;
        color: #f1f5f9;
    }

    .markdown-content :global(p) {
        margin: 0.5rem 0;
        line-height: 1.6;
    }

    .markdown-content :global(ul),
    .markdown-content :global(ol) {
        margin: 0.5rem 0;
        padding-left: 1.5rem;
    }

    .markdown-content :global(li) {
        margin: 0.25rem 0;
    }

    .markdown-content :global(code) {
        font-family: 'SF Mono', Monaco, 'Cascadia Code', 'Roboto Mono', monospace;
        background: rgba(0, 0, 0, 0.3);
        padding: 0.125rem 0.375rem;
        border-radius: 0.25rem;
        font-size: 0.875em;
        color: #e2e8f0;
    }

    .markdown-content :global(pre) {
        background: rgba(0, 0, 0, 0.4);
        border-radius: 0.5rem;
        padding: 1rem;
        margin: 0.75rem 0;
        overflow-x: auto;
    }

    .markdown-content :global(pre code) {
        background: transparent;
        padding: 0;
        border-radius: 0;
        font-size: 0.875rem;
        line-height: 1.5;
    }

    .markdown-content :global(blockquote) {
        border-left: 3px solid #60a5fa;
        margin: 0.75rem 0;
        padding: 0.5rem 1rem;
        background: rgba(96, 165, 250, 0.1);
        border-radius: 0 0.25rem 0.25rem 0;
    }

    .markdown-content :global(blockquote p) {
        margin: 0;
        color: #cbd5e1;
    }

    .markdown-content :global(a) {
        color: #60a5fa;
        text-decoration: none;
    }

    .markdown-content :global(a:hover) {
        text-decoration: underline;
    }

    .markdown-content :global(table) {
        width: 100%;
        border-collapse: collapse;
        margin: 0.75rem 0;
    }

    .markdown-content :global(th),
    .markdown-content :global(td) {
        border: 1px solid rgba(96, 165, 250, 0.2);
        padding: 0.5rem 0.75rem;
        text-align: left;
    }

    .markdown-content :global(th) {
        background: rgba(96, 165, 250, 0.15);
        font-weight: 600;
        color: #f1f5f9;
    }

    .markdown-content :global(tr:nth-child(even)) {
        background: rgba(255, 255, 255, 0.03);
    }

    .markdown-content :global(hr) {
        border: none;
        border-top: 1px solid rgba(96, 165, 250, 0.2);
        margin: 1rem 0;
    }

    .markdown-content :global(strong) {
        font-weight: 600;
        color: #f1f5f9;
    }

    .markdown-content :global(em) {
        font-style: italic;
    }

    .markdown-content :global(del) {
        text-decoration: line-through;
        opacity: 0.7;
    }

    /* 代码块语法高亮颜色 */
    .markdown-content :global(.hljs-keyword) {
        color: #c678dd;
    }

    .markdown-content :global(.hljs-selector-tag) {
        color: #c678dd;
    }

    .markdown-content :global(.hljs-literal) {
        color: #c678dd;
    }

    .markdown-content :global(.hljs-section) {
        color: #c678dd;
    }

    .markdown-content :global(.hljs-link) {
        color: #c678dd;
    }

    .markdown-content :global(.hljs-function .hljs-keyword) {
        color: #c678dd;
    }

    .markdown-content :global(.hljs-string) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-title) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-name) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-type) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-attribute) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-symbol) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-bullet) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-addition) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-variable) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-template-tag) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-template-variable) {
        color: #98c379;
    }

    .markdown-content :global(.hljs-comment) {
        color: #5c6370;
        font-style: italic;
    }

    .markdown-content :global(.hljs-quote) {
        color: #5c6370;
        font-style: italic;
    }

    .markdown-content :global(.hljs-deletion) {
        color: #5c6370;
        font-style: italic;
    }

    .markdown-content :global(.hljs-meta) {
        color: #5c6370;
        font-style: italic;
    }

    .markdown-content :global(.hljs-number) {
        color: #d19a66;
    }

    .markdown-content :global(.hljs-regexp) {
        color: #d19a66;
    }

    .markdown-content :global(.hljs-built_in) {
        color: #d19a66;
    }

    .markdown-content :global(.hljs-builtin-name) {
        color: #d19a66;
    }

    .markdown-content :global(.hljs-params) {
        color: #e06c75;
    }

    .markdown-content :global(.hljs-class .hljs-title) {
        color: #61afef;
    }

    .markdown-content :global(.hljs-function .hljs-title) {
        color: #61afef;
    }
</style>
