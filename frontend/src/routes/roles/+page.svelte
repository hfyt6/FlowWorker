<script lang="ts">
	import { onMount } from 'svelte';

	// 类型定义
	interface Role {
		id: string;
		name: string;
		displayName: string;
		description: string;
		isBuiltIn: boolean;
		createdAt: string;
		systemPrompt?: string;
		allowedTools?: string[];
	}

	// 状态
	let roles: Role[] = [];
	let loading = true;
	let error: string | null = null;
	let activeTab: 'all' | 'builtin' | 'custom' = 'all';

	// 模态框状态
	let showCreateModal = false;
	let showEditModal = false;
	let showDeleteModal = false;
	let showDetailModal = false;
	let selectedRole: Role | null = null;

	// 表单数据
	let formData = {
		name: '',
		displayName: '',
		description: '',
		systemPrompt: '',
		allowedTools: ''
	};

	const API_BASE = 'http://localhost:5121/api/v1';

	onMount(async () => {
		await fetchRoles();
		loading = false;
	});

	async function fetchRoles() {
		try {
			const response = await fetch(`${API_BASE}/roles`);
			if (!response.ok) throw new Error('Failed to fetch roles');
			roles = await response.json();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unknown error';
		}
	}

	async function createRole() {
		try {
			const response = await fetch(`${API_BASE}/roles`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					name: formData.name,
					displayName: formData.displayName,
					description: formData.description,
					systemPrompt: formData.systemPrompt,
					allowedTools: formData.allowedTools ? formData.allowedTools.split(',').map(s => s.trim()) : []
				})
			});

			if (!response.ok) {
				const errorData = await response.json();
				throw new Error(errorData.error || 'Failed to create role');
			}

			showCreateModal = false;
			resetForm();
			await fetchRoles();
		} catch (err) {
			alert(err instanceof Error ? err.message : 'Failed to create role');
		}
	}

	async function updateRole() {
		if (!selectedRole) return;

		try {
			const response = await fetch(`${API_BASE}/roles/${selectedRole.id}`, {
				method: 'PUT',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					displayName: formData.displayName,
					description: formData.description,
					systemPrompt: formData.systemPrompt,
					allowedTools: formData.allowedTools ? formData.allowedTools.split(',').map(s => s.trim()) : []
				})
			});

			if (!response.ok) {
				const errorData = await response.json();
				throw new Error(errorData.error || 'Failed to update role');
			}

			showEditModal = false;
			selectedRole = null;
			resetForm();
			await fetchRoles();
		} catch (err) {
			alert(err instanceof Error ? err.message : 'Failed to update role');
		}
	}

	async function deleteRole() {
		if (!selectedRole) return;

		try {
			const response = await fetch(`${API_BASE}/roles/${selectedRole.id}`, {
				method: 'DELETE'
			});

			if (!response.ok) {
				const errorData = await response.json();
				throw new Error(errorData.error || 'Failed to delete role');
			}

			showDeleteModal = false;
			selectedRole = null;
			await fetchRoles();
		} catch (err) {
			alert(err instanceof Error ? err.message : 'Failed to delete role');
		}
	}

	async function initializeBuiltInRoles() {
		try {
			const response = await fetch(`${API_BASE}/roles/initialize`, {
				method: 'POST'
			});

			if (!response.ok) throw new Error('Failed to initialize roles');
			await fetchRoles();
			alert('内置角色初始化成功');
		} catch (err) {
			alert(err instanceof Error ? err.message : 'Failed to initialize roles');
		}
	}

	function openCreateModal() {
		resetForm();
		showCreateModal = true;
	}

	function openEditModal(role: Role) {
		selectedRole = role;
		formData = {
			name: role.name,
			displayName: role.displayName,
			description: role.description,
			systemPrompt: role.systemPrompt || '',
			allowedTools: role.allowedTools?.join(', ') || ''
		};
		showEditModal = true;
	}

	function openDeleteModal(role: Role) {
		selectedRole = role;
		showDeleteModal = true;
	}

	function openDetailModal(role: Role) {
		selectedRole = role;
		showDetailModal = true;
	}

	function resetForm() {
		formData = {
			name: '',
			displayName: '',
			description: '',
			systemPrompt: '',
			allowedTools: ''
		};
	}

	function closeModal() {
		showCreateModal = false;
		showEditModal = false;
		showDeleteModal = false;
		showDetailModal = false;
		selectedRole = null;
		resetForm();
	}

	$: filteredRoles = roles.filter(r => {
		if (activeTab === 'all') return true;
		if (activeTab === 'builtin') return r.isBuiltIn;
		if (activeTab === 'custom') return !r.isBuiltIn;
		return true;
	});

	function formatDate(dateString: string): string {
		return new Date(dateString).toLocaleDateString('zh-CN');
	}
</script>

<svelte:head>
	<title>角色管理 - FlowWorker</title>
</svelte:head>

<div class="container">
	<div class="header">
		<h1>角色管理</h1>
		<div class="actions">
			<button class="btn btn-secondary" on:click={initializeBuiltInRoles}>
				🔄 初始化内置角色
			</button>
			<button class="btn btn-primary" on:click={openCreateModal}>
				<span>+</span> 创建自定义角色
			</button>
		</div>
	</div>

	<div class="tabs">
		<button 
			class="tab" 
			class:active={activeTab === 'all'}
			on:click={() => activeTab = 'all'}
		>
			全部 ({roles.length})
		</button>
		<button 
			class="tab" 
			class:active={activeTab === 'builtin'}
			on:click={() => activeTab = 'builtin'}
		>
			内置 ({roles.filter(r => r.isBuiltIn).length})
		</button>
		<button 
			class="tab" 
			class:active={activeTab === 'custom'}
			on:click={() => activeTab = 'custom'}
		>
			自定义 ({roles.filter(r => !r.isBuiltIn).length})
		</button>
	</div>

	{#if loading}
		<div class="loading">加载中...</div>
	{:else if error}
		<div class="error">{error}</div>
	{:else if filteredRoles.length === 0}
		<div class="empty">
			<p>暂无角色</p>
			{#if activeTab === 'custom'}
				<button class="btn btn-primary" on:click={openCreateModal}>
					创建第一个自定义角色
				</button>
			{:else}
				<button class="btn btn-secondary" on:click={initializeBuiltInRoles}>
					初始化内置角色
				</button>
			{/if}
		</div>
	{:else}
		<div class="roles-grid">
			{#each filteredRoles as role}
				<div class="role-card" class:builtin={role.isBuiltIn}>
					<div class="card-header">
						<div class="role-icon">
							{role.isBuiltIn ? '🔒' : '⚙️'}
						</div>
						<div class="info">
							<h3>{role.displayName}</h3>
							<span class="name">{role.name}</span>
						</div>
						<span class="badge" class:builtin={role.isBuiltIn}>
							{role.isBuiltIn ? '内置' : '自定义'}
						</span>
					</div>
					
					<p class="description">{role.description}</p>
					
					<div class="card-footer">
						<span class="date">创建于 {formatDate(role.createdAt)}</span>
					<div class="actions">
						<button class="btn-icon" on:click={() => openDetailModal(role)} title="查看详情">
							👁️
						</button>
						<button class="btn-icon" on:click={() => openEditModal(role)} title="编辑">
							✏️
						</button>
						{#if !role.isBuiltIn}
							<button class="btn-icon" on:click={() => openDeleteModal(role)} title="删除">
								🗑️
							</button>
						{/if}
					</div>
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>

<!-- 创建模态框 -->
{#if showCreateModal}
	<div class="modal-overlay" on:click={closeModal}>
		<div class="modal" on:click|stopPropagation>
			<div class="modal-header">
				<h2>创建自定义角色</h2>
				<button class="btn-close" on:click={closeModal}>×</button>
			</div>
			<div class="modal-body">
				<div class="form-group">
					<label>角色标识 *</label>
					<input type="text" bind:value={formData.name} placeholder="例如: my_assistant" />
					<span class="hint">用于系统识别的唯一标识，创建后不可修改</span>
				</div>
				
				<div class="form-group">
					<label>显示名称 *</label>
					<input type="text" bind:value={formData.displayName} placeholder="例如: 我的助手" />
				</div>
				
				<div class="form-group">
					<label>描述</label>
					<input type="text" bind:value={formData.description} placeholder="简要描述角色的用途" />
				</div>
				
				<div class="form-group">
					<label>系统提示词 (System Prompt) *</label>
					<textarea bind:value={formData.systemPrompt} rows="6" placeholder="定义AI角色的行为和性格..."></textarea>
					<span class="hint">这是定义角色行为的核心提示词</span>
				</div>
				
				<div class="form-group">
					<label>允许的工具 (可选)</label>
					<input type="text" bind:value={formData.allowedTools} placeholder="tool1, tool2, tool3" />
					<span class="hint">用逗号分隔的工具名称列表</span>
				</div>
			</div>
			<div class="modal-footer">
				<button class="btn btn-secondary" on:click={closeModal}>取消</button>
				<button 
					class="btn btn-primary" 
					on:click={createRole}
					disabled={!formData.name || !formData.displayName || !formData.systemPrompt}
				>
					创建
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- 编辑模态框 -->
{#if showEditModal && selectedRole}
	<div class="modal-overlay" on:click={closeModal}>
		<div class="modal" on:click|stopPropagation>
			<div class="modal-header">
				<h2>编辑角色</h2>
				<button class="btn-close" on:click={closeModal}>×</button>
			</div>
			<div class="modal-body">
				<div class="form-group">
					<label>角色标识</label>
					<input type="text" value={formData.name} disabled />
					<span class="hint">角色标识不可修改</span>
				</div>
				
				<div class="form-group">
					<label>显示名称 *</label>
					<input type="text" bind:value={formData.displayName} placeholder="例如: 我的助手" />
				</div>
				
				<div class="form-group">
					<label>描述</label>
					<input type="text" bind:value={formData.description} placeholder="简要描述角色的用途" />
				</div>
				
				<div class="form-group">
					<label>系统提示词 (System Prompt) *</label>
					<textarea bind:value={formData.systemPrompt} rows="6" placeholder="定义AI角色的行为和性格..."></textarea>
				</div>
				
				<div class="form-group">
					<label>允许的工具 (可选)</label>
					<input type="text" bind:value={formData.allowedTools} placeholder="tool1, tool2, tool3" />
					<span class="hint">用逗号分隔的工具名称列表</span>
				</div>
			</div>
			<div class="modal-footer">
				<button class="btn btn-secondary" on:click={closeModal}>取消</button>
				<button 
					class="btn btn-primary" 
					on:click={updateRole}
					disabled={!formData.displayName || !formData.systemPrompt}
				>
					保存
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- 删除确认模态框 -->
{#if showDeleteModal && selectedRole}
	<div class="modal-overlay" on:click={closeModal}>
		<div class="modal modal-small" on:click|stopPropagation>
			<div class="modal-header">
				<h2>确认删除</h2>
				<button class="btn-close" on:click={closeModal}>×</button>
			</div>
			<div class="modal-body">
				<p>确定要删除角色 "{selectedRole.displayName}" 吗？此操作不可撤销。</p>
				<p class="warning">注意：如果该角色正在被对话者使用，将无法删除。</p>
			</div>
			<div class="modal-footer">
				<button class="btn btn-secondary" on:click={closeModal}>取消</button>
				<button class="btn btn-danger" on:click={deleteRole}>删除</button>
			</div>
		</div>
	</div>
{/if}

<!-- 详情模态框 -->
{#if showDetailModal && selectedRole}
	<div class="modal-overlay" on:click={closeModal}>
		<div class="modal" on:click|stopPropagation>
			<div class="modal-header">
				<h2>{selectedRole.displayName}</h2>
				<button class="btn-close" on:click={closeModal}>×</button>
			</div>
			<div class="modal-body">
				<div class="detail-section">
					<label>角色标识</label>
					<code>{selectedRole.name}</code>
				</div>
				
				<div class="detail-section">
					<label>描述</label>
					<p>{selectedRole.description || '无描述'}</p>
				</div>
				
				<div class="detail-section">
					<label>类型</label>
					<span class="badge" class:builtin={selectedRole.isBuiltIn}>
						{selectedRole.isBuiltIn ? '内置角色' : '自定义角色'}
					</span>
				</div>
				
				<div class="detail-section">
					<label>系统提示词</label>
					<div class="prompt-box">
						<pre>{selectedRole.systemPrompt || '无系统提示词'}</pre>
					</div>
				</div>
				
				{#if selectedRole.allowedTools && selectedRole.allowedTools.length > 0}
					<div class="detail-section">
						<label>允许的工具</label>
						<div class="tools-list">
							{#each selectedRole.allowedTools as tool}
								<span class="tool-tag">{tool}</span>
							{/each}
						</div>
					</div>
				{/if}
				
				<div class="detail-section">
					<label>创建时间</label>
					<span>{formatDate(selectedRole.createdAt)}</span>
				</div>
			</div>
			<div class="modal-footer">
				<button class="btn btn-secondary" on:click={closeModal}>关闭</button>
			</div>
		</div>
	</div>
{/if}

<style>
	.container {
		max-width: 1200px;
		margin: 0 auto;
		padding: 2rem;
	}

	.header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 2rem;
	}

	.header h1 {
		margin: 0;
		font-size: 1.75rem;
		font-weight: 600;
	}

	.actions {
		display: flex;
		gap: 0.75rem;
	}

	.tabs {
		display: flex;
		gap: 0.5rem;
		margin-bottom: 1.5rem;
		border-bottom: 1px solid rgba(96, 165, 250, 0.2);
		padding-bottom: 0.5rem;
	}

	.tab {
		padding: 0.5rem 1rem;
		border: none;
		background: transparent;
		color: var(--color-text-muted);
		cursor: pointer;
		border-radius: 0.375rem;
		transition: all 0.2s;
	}

	.tab:hover {
		background: rgba(96, 165, 250, 0.1);
		color: var(--color-text);
	}

	.tab.active {
		background: rgba(96, 165, 250, 0.2);
		color: var(--color-theme-1);
	}

	.loading, .error, .empty {
		text-align: center;
		padding: 3rem;
		color: var(--color-text-muted);
	}

	.error {
		color: var(--color-error);
	}

	.empty p {
		margin-bottom: 1rem;
	}

	.roles-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
		gap: 1rem;
	}

	.role-card {
		background: rgba(30, 41, 59, 0.6);
		border: 1px solid rgba(96, 165, 250, 0.15);
		border-radius: 0.75rem;
		padding: 1.25rem;
		transition: all 0.2s;
	}

	.role-card:hover {
		border-color: rgba(96, 165, 250, 0.3);
		transform: translateY(-2px);
	}

	.role-card.builtin {
		border-left: 3px solid var(--color-theme-1);
	}

	.card-header {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		margin-bottom: 0.75rem;
	}

	.role-icon {
		width: 2.5rem;
		height: 2.5rem;
		border-radius: 50%;
		background: rgba(96, 165, 250, 0.2);
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 1rem;
	}

	.info {
		flex: 1;
	}

	.info h3 {
		margin: 0 0 0.125rem 0;
		font-size: 1rem;
		font-weight: 600;
	}

	.info .name {
		font-size: 0.75rem;
		color: var(--color-text-muted);
		font-family: monospace;
	}

	.badge {
		font-size: 0.75rem;
		padding: 0.25rem 0.5rem;
		border-radius: 1rem;
		background: rgba(139, 92, 246, 0.2);
		color: #a78bfa;
	}

	.badge.builtin {
		background: rgba(96, 165, 250, 0.2);
		color: var(--color-theme-1);
	}

	.description {
		margin: 0 0 1rem 0;
		font-size: 0.875rem;
		color: var(--color-text-muted);
		line-height: 1.5;
		display: -webkit-box;
		-webkit-line-clamp: 2;
		-webkit-box-orient: vertical;
		overflow: hidden;
	}

	.card-footer {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding-top: 0.75rem;
		border-top: 1px solid rgba(96, 165, 250, 0.1);
	}

	.date {
		font-size: 0.75rem;
		color: var(--color-text-muted);
	}

	.btn {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.625rem 1rem;
		border: none;
		border-radius: 0.5rem;
		font-size: 0.875rem;
		font-weight: 500;
		cursor: pointer;
		transition: all 0.2s;
	}

	.btn:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.btn-primary {
		background: var(--color-theme-1);
		color: white;
	}

	.btn-primary:hover:not(:disabled) {
		background: #3b82f6;
	}

	.btn-secondary {
		background: rgba(96, 165, 250, 0.15);
		color: var(--color-text);
	}

	.btn-secondary:hover:not(:disabled) {
		background: rgba(96, 165, 250, 0.25);
	}

	.btn-danger {
		background: rgba(239, 68, 68, 0.2);
		color: #ef4444;
	}

	.btn-danger:hover:not(:disabled) {
		background: rgba(239, 68, 68, 0.3);
	}

	.btn-icon {
		width: 2rem;
		height: 2rem;
		border: none;
		background: transparent;
		border-radius: 0.375rem;
		cursor: pointer;
		transition: all 0.2s;
	}

	.btn-icon:hover {
		background: rgba(96, 165, 250, 0.15);
	}

	/* Modal Styles */
	.modal-overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.6);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 100;
		padding: 1rem;
	}

	.modal {
		background: var(--color-bg-1);
		border: 1px solid rgba(96, 165, 250, 0.2);
		border-radius: 0.75rem;
		width: 100%;
		max-width: 560px;
		max-height: 90vh;
		overflow: hidden;
		display: flex;
		flex-direction: column;
	}

	.modal-small {
		max-width: 400px;
	}

	.modal-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 1.25rem;
		border-bottom: 1px solid rgba(96, 165, 250, 0.15);
	}

	.modal-header h2 {
		margin: 0;
		font-size: 1.25rem;
	}

	.btn-close {
		width: 2rem;
		height: 2rem;
		border: none;
		background: transparent;
		color: var(--color-text-muted);
		font-size: 1.5rem;
		cursor: pointer;
		border-radius: 0.375rem;
		transition: all 0.2s;
	}

	.btn-close:hover {
		background: rgba(96, 165, 250, 0.15);
		color: var(--color-text);
	}

	.modal-body {
		padding: 1.25rem;
		overflow-y: auto;
		flex: 1;
	}

	.modal-footer {
		display: flex;
		justify-content: flex-end;
		gap: 0.75rem;
		padding: 1.25rem;
		border-top: 1px solid rgba(96, 165, 250, 0.15);
	}

	.form-group {
		margin-bottom: 1rem;
	}

	.form-group:last-child {
		margin-bottom: 0;
	}

	.form-group label {
		display: block;
		margin-bottom: 0.375rem;
		font-size: 0.875rem;
		font-weight: 500;
		color: var(--color-text);
	}

	.form-group input,
	.form-group select,
	.form-group textarea {
		width: 100%;
		padding: 0.625rem 0.875rem;
		border: 1px solid rgba(96, 165, 250, 0.2);
		border-radius: 0.5rem;
		background: rgba(15, 23, 42, 0.5);
		color: var(--color-text);
		font-size: 0.875rem;
		transition: all 0.2s;
		font-family: inherit;
	}

	.form-group input:focus,
	.form-group select:focus,
	.form-group textarea:focus {
		outline: none;
		border-color: var(--color-theme-1);
	}

	.form-group input::placeholder,
	.form-group textarea::placeholder {
		color: var(--color-text-muted);
	}

	.form-group input:disabled {
		background: rgba(15, 23, 42, 0.3);
		color: var(--color-text-muted);
	}

	.form-group .hint {
		display: block;
		margin-top: 0.25rem;
		font-size: 0.75rem;
		color: var(--color-text-muted);
	}

	.detail-section {
		margin-bottom: 1.25rem;
	}

	.detail-section:last-child {
		margin-bottom: 0;
	}

	.detail-section label {
		display: block;
		margin-bottom: 0.5rem;
		font-size: 0.75rem;
		font-weight: 500;
		color: var(--color-text-muted);
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.detail-section code {
		display: inline-block;
		padding: 0.25rem 0.5rem;
		background: rgba(15, 23, 42, 0.6);
		border-radius: 0.25rem;
		font-family: monospace;
		font-size: 0.875rem;
	}

	.detail-section p {
		margin: 0;
		color: var(--color-text);
	}

	.prompt-box {
		background: rgba(15, 23, 42, 0.6);
		border: 1px solid rgba(96, 165, 250, 0.15);
		border-radius: 0.5rem;
		padding: 1rem;
	}

	.prompt-box pre {
		margin: 0;
		white-space: pre-wrap;
		word-wrap: break-word;
		font-size: 0.875rem;
		line-height: 1.6;
		color: var(--color-text);
	}

	.tools-list {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
	}

	.tool-tag {
		padding: 0.25rem 0.625rem;
		background: rgba(96, 165, 250, 0.15);
		border-radius: 1rem;
		font-size: 0.75rem;
		color: var(--color-text);
	}

	.warning {
		color: #fbbf24;
		font-size: 0.875rem;
		margin-top: 0.5rem;
	}

	@media (max-width: 640px) {
		.container {
			padding: 1rem;
		}

		.header {
			flex-direction: column;
			gap: 1rem;
			align-items: stretch;
		}

		.actions {
			flex-direction: column;
		}

		.roles-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
