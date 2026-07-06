<script lang="ts">
	import { onMount } from 'svelte';
	import '../app.css';

	let { children } = $props();

	// Marks client hydration for anything that must not interact with a
	// server-rendered form before handlers exist (E2E waits on this).
	onMount(() => {
		document.documentElement.dataset.hydrated = 'true';
	});
</script>

<svelte:head>
	<title>ValidatedScale</title>
	<meta
		name="description"
		content="Run validated instruments as rigorous studies — protocol, field, evidence."
	/>
</svelte:head>

{@render children()}
