<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';

	// 类型定义
	interface Member {
		id: string;
		name: string;
		type: 'User' | 'AI' | 0 | 1;
		avatar?: string;
		status: 'Active' | 'Inactive';
		createdAt: string;
		roleId?: string;
		roleName?: string;
		roleDisplayName?: string;
		apiConfigName?: string;
		model?: string;
	}

	// 辅助函数：判断成员是否为AI类型
	function isAIMember(member: Member): boolean {
		return member.type === 'AI' || member.type === 1;
	}

	// 辅助函数：判断成员是否为用户类型
	function isUserMember(member: Member): boolean {
		return member.type === 'User' || member.type === 0;
	}

	interface Role {
		id: string;
		name: string;
		displayName: string;
		description: string;
		isBuiltIn: boolean;
	}

	interface ApiConfig {
		id: string;
		name: string;
		provider: string;
		model: string;
	}

	// 状态
	let members: Member[] = [];
	let roles: Role[] = [];
	let apiConfigs: ApiConfig[] = [];
	let loading = true;
	let error: string | null = null;
	let activeTab: 'all' | 'ai' | 'user' = 'all';

	// 模态框状态
	let showCreateModal = false;
	let showEditModal = false;
	let showDeleteModal = false;
	let selectedMember: Member | null = null;

	// 表单数据
	let formData = {
		name: '',
		avatar: '',
		type: 'AI' as 'User' | 'AI',
		roleId: '',
		apiConfigId: '',
		model: '',
		temperature: 0.7
	};

	const API_BASE = 'http://localhost:5121/api/v1';

	onMount(async () => {
		await Promise.all([
			fetchMembers(),
			fetchRoles(),
			fetchApiConfigs()
		]);
		loading = false;
	});

	async function fetchMembers() {
		try {
			const response = await fetch(`${API_BASE}/members`);
			if (!response.ok) throw new Error('Failed to fetch members');
			members = await response.json();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Unknown error';
		}
	}

	async function fetchRoles() {
		try {
			const response = await fetch(`${API_BASE}/roles`);
			if (!response.ok) throw new Error('Failed to fetch roles');
			roles = await response.json();
		} catch (err) {
			console.error('Failed to fetch roles:', err);
		}
	}

	async function fetchApiConfigs() {
		try {
			const response = await fetch(`${API_BASE}/api-configs`);
			if (!response.ok) throw new Error('Failed to fetch API configs');
			apiConfigs = await response.json();
			console.log('API configs loaded:', apiConfigs);
		} catch (err) {
			console.error('Failed to fetch API configs:', err);
		}
	}

	async function createMember() {
		try {
			const url = formData.type === 'AI' 
				? `${API_BASE}/members/ai`
				: `${API_BASE}/members/user`;
			
			const body = formData.type === 'AI'
				? {
					name: formData.name,
					avatar: formData.avatar || undefined,
					roleId: formData.roleId,
					apiConfigId: formData.apiConfigId,
					model: formData.model || undefined,
					temperature: formData.temperature
				}
				: {
					name: formData.name,
					avatar: formData.avatar || undefined
				};

			const response = await fetch(url, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify(body)
			});

			if (!response.ok) {
				const errorData = await response.json();
				throw new Error(errorData.error || 'Failed to create member');
			}

			showCreateModal = false;
			resetForm();
			await fetchMembers();
		} catch (err) {
			alert(err instanceof Error ? err.message : 'Failed to create member');
		}
	}

	async function updateMember() {
		if (!selectedMember) return;

		try {
			const response = await fetch(`${API_BASE}/members/${selectedMember.id}`, {
				method: 'PUT',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					name: formData.name,
					avatar: formData.avatar || undefined,
					status: selectedMember.status,
					roleId: formData.roleId || undefined,
					apiConfigId: formData.apiConfigId || undefined,
					model: formData.model || undefined,
					temperature: formData.temperature
				})
			});

			if (!response.ok) {
				const errorData = await response.json();
				throw new Error(errorData.error || 'Failed to update member');
			}

			showEditModal = false;
			selectedMember = null;
			resetForm();
			await fetchMembers();
		} catch (err) {
			alert(err instanceof Error ? err.message : 'Failed to update member');
		}
	}

	async function deleteMember() {
		if (!selectedMember) return;

		try {
			const response = await fetch(`${API_BASE}/members/${selectedMember.id}`, {
				method: 'DELETE'
			});

			if (!response.ok) {
				const errorData = await response.json();
				throw new Error(errorData.error || 'Failed to delete member');
			}

			showDeleteModal = false;
			selectedMember = null;
			await fetchMembers();
		} catch (err) {
			alert(err instanceof Error ? err.message : 'Failed to delete member');
		}
	}

	function openCreateModal(type: 'User' | 'AI') {
		formData.type = type;
		showCreateModal = true;
	}

	function openEditModal(member: Member) {
		selectedMember = member;
		formData = {
			name: member.name,
			avatar: member.avatar || '',
			type: isAIMember(member) ? 'AI' : 'User',
			roleId: member.roleId || '',
			apiConfigId: '',
			model: member.model || '',
			temperature: (member as any).temperature ?? 0.7
		};
		showEditModal = true;
	}

	function openDeleteModal(member: Member) {
		selectedMember = member;
		showDeleteModal = true;
	}

	function resetForm() {
		formData = {
			name: '',
			avatar: '',
			type: 'AI',
			roleId: '',
			apiConfigId: '',
			model: '',
			temperature: 0.7
		};
	}

	function closeModal() {
		showCreateModal = false;
		showEditModal = false;
		showDeleteModal = false;
		selectedMember = null;
		resetForm();
	}

	// 计算各类型成员数量
	$: aiMemberCount = members.filter(isAIMember).length;
	$: userMemberCount = members.filter(isUserMember).length;

	$: filteredMembers = members.filter(m => {
		if (activeTab === 'all') return true;
		if (activeTab === 'ai') return isAIMember(m);
		if (activeTab === 'user') return isUserMember(m);
		return true;
	});

	function formatDate(dateString: string): string {
		return new Date(dateString).toLocaleDateString('zh-CN');
	}

	function getTypeIcon(type: string): string {
		return type === 'AI' ? '🤖' : '👤';
	}

	function getStatusColor(status: string): string {
		return status === 'Active' ? 'var(--color-success)' : 'var(--color-text-muted)';
	}

	// 处理API配置变更，自动填充默认模型
	function handleApiConfigChange(event: Event) {
		const select = event.target as HTMLSelectElement;
		const selectedId = select.value;
		if (selectedId) {
			const selectedConfig = apiConfigs.find(c => c.id === selectedId);
			if (selectedConfig && !formData.model) {
				formData.model = selectedConfig.model;
			}
		}
	}
</script>

<svelte:head>
	<title>成员管理 - FlowWorker</title>
</svelte:head>

<div class="container">
	<div class="header">
		<h1>成员管理</h1>
		<div class="actions">
			<button class="btn btn-primary" on:click={() => openCreateModal('AI')}>
				<span>+</span> 创建AI虚拟成员
			</button>
			<button class="btn btn-secondary" on:click={() => openCreateModal('User')}>
				<span>+</span> 创建用户
			</button>
		</div>
	</div>

	<div class="tabs">
		<button 
			class="tab" 
			class:active={activeTab === 'all'}
			on:click={() => activeTab = 'all'}
		>
			全部 ({members.length})
		</button>
		<button 
			class="tab" 
			class:active={activeTab === 'ai'}
			on:click={() => activeTab = 'ai'}
		>
			AI虚拟成员 ({aiMemberCount})
		</button>
		<button 
			class="tab" 
			class:active={activeTab === 'user'}
			on:click={() => activeTab = 'user'}
		>
			用户 ({userMemberCount})
		</button>
	</div>

	{#if loading}
		<div class="loading">加载中...</div>
	{:else if error}
		<div class="error">{error}</div>
	{:else if filteredMembers.length === 0}
		<div class="empty">
			<p>暂无成员</p>
			<button class="btn btn-primary" on:click={() => openCreateModal('AI')}>
				创建第一个成员
			</button>
		</div>
	{:else}
		<div class="members-grid">
			{#each filteredMembers as member}
				<div class="member-card">
					<div class="card-header">
						<div class="avatar">
							{member.avatar || getTypeIcon(isAIMember(member) ? 'AI' : 'User')}
						</div>
						<div class="info">
							<h3>{member.name}</h3>
							<span class="type-badge" class:ai={isAIMember(member)}>
								{isAIMember(member) ? 'AI虚拟成员' : '用户'}
							</span>
						</div>
						<div class="status" style="color: {getStatusColor(member.status)}">
							{member.status === 'Active' ? '●' : '○'}
						</div>
					</div>
					
					{#if isAIMember(member)}
						<div class="card-details">
							{#if member.roleDisplayName}
								<div class="detail">
									<span class="label">角色:</span>
									<span class="value">{member.roleDisplayName}</span>
								</div>
							{/if}
							{#if member.apiConfigName}
								<div class="detail">
									<span class="label">API:</span>
									<span class="value">{member.apiConfigName}</span>
								</div>
							{/if}
							{#if member.model}
								<div class="detail">
									<span class="label">模型:</span>
									<span class="value">{member.model}</span>
								</div>
							{/if}
						</div>
					{/if}
					
					<div class="card-footer">
						<span class="date">创建于 {formatDate(member.createdAt)}</span>
						<div class="actions">
							<button class="btn-icon" on:click={() => openEditModal(member)} title="编辑">
								✏️
							</button>
							<button class="btn-icon" on:click={() => openDeleteModal(member)} title="删除">
								🗑️
							</button>
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
				<h2>创建{formData.type === 'AI' ? 'AI虚拟成员' : '用户'}</h2>
				<button class="btn-close" on:click={closeModal}>×</button>
			</div>
			<div class="modal-body">
				<div class="form-group">
					<label>名称 *</label>
					<input type="text" bind:value={formData.name} placeholder="输入成员名称" />
				</div>
				
				<div class="form-group">
					<label>头像 (可选)</label>
					<input type="text" bind:value={formData.avatar} placeholder="输入emoji或URL" />
				</div>
				
				{#if formData.type === 'AI'}
					<div class="form-group">
						<label>角色 *</label>
						<select bind:value={formData.roleId}>
							<option value="">选择角色</option>
							{#each roles as role}
								<option value={role.id}>{role.displayName}</option>
							{/each}
						</select>
					</div>
					
					<div class="form-group">
						<label>API配置 *</label>
						<select bind:value={formData.apiConfigId} on:change={handleApiConfigChange}>
							<option value="">选择API配置</option>
							{#each apiConfigs as config}
								<option value={config.id}>{config.name} ({config.model})</option>
							{/each}
						</select>
					</div>
					
					<div class="form-group">
						<label>模型 (可选，留空使用API配置默认模型)</label>
						<input type="text" bind:value={formData.model} placeholder="例如: gpt-4" />
					</div>
					
					<div class="form-group">
						<label>温度 (Temperature): {formData.temperature.toFixed(2)}</label>
						<input 
							type="range" 
							bind:value={formData.temperature} 
							min="0" 
							max="2" 
							step="0.1" 
							class="temperature-slider"
						/>
						<div class="temperature-hint">
							<span>精确 (0)</span>
							<span>创造性 (2)</span>
						</div>
					</div>
				{/if}
			</div>
			<div class="modal-footer">
				<button class="btn btn-secondary" on:click={closeModal}>取消</button>
				<button 
					class="btn btn-primary" 
					on:click={createMember}
					disabled={!formData.name || (formData.type === 'AI' && (!formData.roleId || !formData.apiConfigId))}
				>
					创建
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- 编辑模态框 -->
{#if showEditModal && selectedMember}
	<div class="modal-overlay" on:click={closeModal}>
		<div class="modal" on:click|stopPropagation>
			<div class="modal-header">
				<h2>编辑成员</h2>
				<button class="btn-close" on:click={closeModal}>×</button>
			</div>
			<div class="modal-body">
				<div class="form-group">
					<label>名称 *</label>
					<input type="text" bind:value={formData.name} placeholder="输入成员名称" />
				</div>
				
				<div class="form-group">
					<label>头像 (可选)</label>
					<input type="text" bind:value={formData.avatar} placeholder="输入emoji或URL" />
				</div>
				
				{#if isAIMember(selectedMember)}
					<div class="form-group">
						<label>角色</label>
						<select bind:value={formData.roleId}>
							<option value="">选择角色</option>
							{#each roles as role}
								<option value={role.id}>{role.displayName}</option>
							{/each}
						</select>
					</div>
					
					<div class="form-group">
						<label>模型 (可选)</label>
						<input type="text" bind:value={formData.model} placeholder="例如: gpt-4" />
					</div>
					
					<div class="form-group">
						<label>温度 (Temperature): {formData.temperature.toFixed(2)}</label>
						<input 
							type="range" 
							bind:value={formData.temperature} 
							min="0" 
							max="2" 
							step="0.1" 
							class="temperature-slider"
						/>
						<div class="temperature-hint">
							<span>精确 (0)</span>
							<span>创造性 (2)</span>
						</div>
					</div>
				{/if}
			</div>
			<div class="modal-footer">
				<button class="btn btn-secondary" on:click={closeModal}>取消</button>
				<button 
					class="btn btn-primary" 
					on:click={updateMember}
					disabled={!formData.name}
				>
					保存
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- 删除确认模态框 -->
{#if showDeleteModal && selectedMember}
	<div class="modal-overlay" on:click={closeModal}>
		<div class="modal modal-small" on:click|stopPropagation>
			<div class="modal-header">
				<h2>确认删除</h2>
				<button class="btn-close" on:click={closeModal}>×</button>
			</div>
			<div class="modal-body">
				<p>确定要删除成员 "{selectedMember.name}" 吗？此操作不可撤销。</p>
			</div>
			<div class="modal-footer">
				<button class="btn btn-secondary" on:click={closeModal}>取消</button>
				<button class="btn btn-danger" on:click={deleteMember}>删除</button>
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

	.members-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
		gap: 1rem;
	}

	.member-card {
		background: rgba(30, 41, 59, 0.6);
		border: 1px solid rgba(96, 165, 250, 0.15);
		border-radius: 0.75rem;
		padding: 1.25rem;
		transition: all 0.2s;
	}

	.member-card:hover {
		border-color: rgba(96, 165, 250, 0.3);
		transform: translateY(-2px);
	}

	.card-header {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		margin-bottom: 1rem;
	}

	.avatar {
		width: 3rem;
		height: 3rem;
		border-radius: 50%;
		background: rgba(96, 165, 250, 0.2);
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 1.25rem;
	}

	.info {
		flex: 1;
	}

	.info h3 {
		margin: 0 0 0.25rem 0;
		font-size: 1rem;
		font-weight: 600;
	}

	.type-badge {
		font-size: 0.75rem;
		padding: 0.125rem 0.5rem;
		border-radius: 1rem;
		background: rgba(96, 165, 250, 0.15);
		color: var(--color-text-muted);
	}

	.type-badge.ai {
		background: rgba(139, 92, 246, 0.2);
		color: #a78bfa;
	}

	.status {
		font-size: 0.75rem;
	}

	.card-details {
		margin-bottom: 1rem;
		padding: 0.75rem;
		background: rgba(15, 23, 42, 0.4);
		border-radius: 0.5rem;
	}

	.detail {
		display: flex;
		gap: 0.5rem;
		margin-bottom: 0.375rem;
		font-size: 0.875rem;
	}

	.detail:last-child {
		margin-bottom: 0;
	}

	.detail .label {
		color: var(--color-text-muted);
		min-width: 3rem;
	}

	.detail .value {
		color: var(--color-text);
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
		max-width: 480px;
		max-height: 90vh;
		overflow: hidden;
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
	}

	.form-group input:focus,
	.form-group select:focus,
	.form-group textarea:focus {
		outline: none;
		border-color: var(--color-theme-1);
	}

	.form-group input::placeholder {
		color: var(--color-text-muted);
	}

	.temperature-slider {
		width: 100%;
		height: 6px;
		border-radius: 3px;
		background: rgba(96, 165, 250, 0.2);
		outline: none;
		-webkit-appearance: none;
		appearance: none;
	}

	.temperature-slider::-webkit-slider-thumb {
		-webkit-appearance: none;
		appearance: none;
		width: 18px;
		height: 18px;
		border-radius: 50%;
		background: var(--color-theme-1);
		cursor: pointer;
		transition: all 0.2s;
	}

	.temperature-slider::-webkit-slider-thumb:hover {
		transform: scale(1.1);
	}

	.temperature-slider::-moz-range-thumb {
		width: 18px;
		height: 18px;
		border-radius: 50%;
		background: var(--color-theme-1);
		cursor: pointer;
		border: none;
	}

	.temperature-hint {
		display: flex;
		justify-content: space-between;
		font-size: 0.75rem;
		color: var(--color-text-muted);
		margin-top: 0.25rem;
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

		.members-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
