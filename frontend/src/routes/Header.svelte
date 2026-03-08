<script lang="ts">
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import github from '$lib/images/github.svg';
</script>

<header>
	<div class="logo">
		<a href={resolve('/')} class="logo-link">
			<svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
				<path d="M12 2L2 7l10 5 10-5-10-5z"/>
				<path d="M2 17l10 5 10-5"/>
				<path d="M2 12l10 5 10-5"/>
			</svg>
			<span class="logo-text">FlowWorker</span>
		</a>
	</div>

	<nav>
		<ul>
			<li aria-current={page.url.pathname === '/' ? 'page' : undefined}>
				<a href={resolve('/')}>首页</a>
			</li>
			<li aria-current={page.url.pathname.startsWith('/sessions') ? 'page' : undefined}>
				<a href={resolve('/sessions')}>会话</a>
			</li>
			<li aria-current={page.url.pathname.startsWith('/members') ? 'page' : undefined}>
				<a href={resolve('/members')}>成员</a>
			</li>
			<li aria-current={page.url.pathname.startsWith('/roles') ? 'page' : undefined}>
				<a href={resolve('/roles')}>角色</a>
			</li>
			<li aria-current={page.url.pathname.startsWith('/settings') ? 'page' : undefined}>
				<a href={resolve('/settings')}>设置</a>
			</li>
		</ul>
	</nav>

	<div class="corner">
		<a href="https://github.com/sveltejs/kit" class="github-link">
			<img src={github} alt="GitHub" />
		</a>
	</div>
</header>

<style>
	header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 0.75rem 1.5rem;
		background: rgba(30, 41, 59, 0.8);
		backdrop-filter: blur(12px);
		border-bottom: 1px solid rgba(96, 165, 250, 0.1);
	}

	.logo {
		display: flex;
		align-items: center;
	}

	.logo-link {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		color: var(--color-text);
		text-decoration: none;
		transition: all 0.2s ease;
	}

	.logo-link:hover {
		color: var(--color-theme-1);
	}

	.logo-link svg {
		color: var(--color-theme-1);
	}

	.logo-text {
		font-size: 1.1rem;
		font-weight: 700;
		letter-spacing: -0.02em;
	}

	.corner {
		width: 2.5em;
		height: 2.5em;
	}

	.corner a {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 100%;
	}

	.github-link {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 2.5em;
		height: 2.5em;
		border-radius: 50%;
		background: rgba(96, 165, 250, 0.1);
		transition: all 0.2s ease;
	}

	.github-link:hover {
		background: rgba(96, 165, 250, 0.2);
		transform: translateY(-1px);
	}

	.corner img {
		width: 1.5em;
		height: 1.5em;
		object-fit: contain;
		filter: invert(1);
		opacity: 0.8;
	}

	nav {
		display: flex;
		justify-content: center;
	}

	nav ul {
		display: flex;
		gap: 0.5rem;
		padding: 0.25rem;
		margin: 0;
		list-style: none;
		background: rgba(15, 23, 42, 0.6);
		border-radius: 2rem;
		border: 1px solid rgba(96, 165, 250, 0.15);
	}

	nav li {
		position: relative;
	}

	nav li[aria-current='page'] a {
		background: rgba(96, 165, 250, 0.2);
		color: var(--color-theme-1);
	}

	nav a {
		display: flex;
		align-items: center;
		padding: 0.5rem 1.25rem;
		color: var(--color-text-muted);
		font-weight: 500;
		font-size: 0.875rem;
		text-decoration: none;
		border-radius: 1.5rem;
		transition: all 0.2s ease;
	}

	nav a:hover {
		color: var(--color-text);
		background: rgba(96, 165, 250, 0.1);
	}

	@media (max-width: 640px) {
		header {
			padding: 0.75rem 1rem;
		}

		.logo-text {
			display: none;
		}

		nav a {
			padding: 0.5rem 1rem;
			font-size: 0.8rem;
		}
	}
</style>
