<script lang="ts">
	import { page } from '$app/state';
	import AppShell from '$lib/components/AppShell.svelte';
	import favicon from '$lib/assets/favicon.svg';
	import './app.css';

	let { children } = $props();
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>

{#if page.url.pathname.startsWith('/r/')}
	{@render children()}
{:else}
	<AppShell>
		{@render children()}
	</AppShell>
{/if}
