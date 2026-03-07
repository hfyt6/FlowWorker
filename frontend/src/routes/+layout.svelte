<script lang="ts">
	import Header from './Header.svelte';
	import './layout.css';

	let { children } = $props();
</script>

<div class="app">
	<Header />

	<main>
		{@render children()}
	</main>
</div>

<style>
	:global(html, body) {
		margin: 0;
		padding: 0;
		overflow: hidden;
		height: 100vh;
		width: 100vw;
	}

	.app {
		display: flex;
		flex-direction: column;
		height: 100vh;
		width: 100vw;
		overflow: hidden;
	}

	main {
		flex: 1;
		display: flex;
		flex-direction: column;
		overflow: hidden;
		position: relative;
	}
</style>
