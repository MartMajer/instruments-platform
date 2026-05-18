<script lang="ts">
	import { page } from '$app/state';
	import AppShell from '$lib/components/AppShell.svelte';
	import favicon from '$lib/assets/favicon.svg';
	import './app.css';

	let { children } = $props();
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
	<link rel="preconnect" href="https://fonts.googleapis.com" />
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="anonymous" />
	<link
		href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;600&family=Source+Serif+4:opsz,wght@8..60,300;8..60,400;8..60,500;8..60,600&display=swap"
		rel="stylesheet"
	/>
</svelte:head>

{#if page.url.pathname.startsWith('/r/')}
	{@render children()}
{:else}
	<AppShell>
		{@render children()}
	</AppShell>
{/if}
