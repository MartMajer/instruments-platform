<script lang="ts">
	import type { Snippet } from 'svelte';

	let {
		label = 'Actions',
		children
	}: {
		label?: string;
		children: Snippet;
	} = $props();
</script>

<section class="action-toolbar" aria-label={label}>
	{@render children()}
</section>
