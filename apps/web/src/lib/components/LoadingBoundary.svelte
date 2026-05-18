<script lang="ts">
	import type { Snippet } from 'svelte';
	import { LoaderCircle } from 'lucide-svelte';

	let {
		loading,
		label = 'Loading',
		children
	}: {
		loading: boolean;
		label?: string;
		children: Snippet;
	} = $props();
</script>

{#if loading}
	<div class="loading-boundary" role="status" aria-live="polite">
		<LoaderCircle size={18} aria-hidden="true" class="animate-spin" />
		<span>{label}</span>
	</div>
{:else}
	{@render children()}
{/if}
