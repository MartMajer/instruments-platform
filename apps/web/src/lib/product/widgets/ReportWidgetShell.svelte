<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { ReportWidget } from '$lib/api/product';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { formatProductCopy } from './report-widget-format';

	let { widget, children }: { widget: ReportWidget; children?: Snippet } = $props();

	const displayTitle = $derived(
		formatProductCopy(widget.kind === 'export-artifact-registry/v1' ? 'Export files' : widget.title)
	);
	const displayMessage = $derived(formatProductCopy(widget.message));
</script>

<article
	class="record-row report-widget-card"
	data-widget-size={widget.size}
	aria-label={displayTitle}
>
	<div class="record-row__header">
		<div>
			<p class="product-kicker">Result summary</p>
			<h3 class="record-row__title">{displayTitle}</h3>
		</div>
		<StatusBadge status={widget.state} />
	</div>

	{#if displayMessage}
		<p class="text-sm text-[var(--color-text-muted)]">{displayMessage}</p>
	{/if}

	{@render children?.()}
</article>
