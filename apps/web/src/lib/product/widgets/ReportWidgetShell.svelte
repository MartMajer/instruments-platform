<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { ReportWidget } from '$lib/api/product';
	import StatusBadge from '$lib/components/StatusBadge.svelte';

	let { widget, children }: { widget: ReportWidget; children?: Snippet } = $props();
</script>

<article
	class="record-row report-widget-card"
	data-widget-size={widget.size}
	aria-label={widget.title}
>
	<div class="record-row__header">
		<div>
			<p class="product-kicker">Report widget</p>
			<h3 class="record-row__title">{widget.title}</h3>
		</div>
		<StatusBadge status={widget.state} />
	</div>

	{#if widget.message}
		<p class="text-sm text-[var(--color-text-muted)]">{widget.message}</p>
	{/if}

	{@render children?.()}
</article>
