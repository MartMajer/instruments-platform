<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { ReportWidget } from '$lib/api/product';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import {
		formatProductCopy,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';

	let {
		widget,
		copy,
		children
	}: { widget: ReportWidget; copy?: ReportWidgetFormatCopy; children?: Snippet } = $props();

	const displayTitle = $derived(
		formatProductCopy(
			widget.kind === 'export-artifact-registry/v1'
				? formatWidgetLabel('exportFiles', copy)
				: widget.title
		)
	);
	const displayMessage = $derived(formatProductCopy(widget.message));
	const kicker = $derived(formatWidgetLabel('resultsSummary', copy));
</script>

<article
	class="record-row report-widget-card"
	data-widget-size={widget.size}
	aria-label={displayTitle}
>
	<div class="record-row__header">
		<div>
			<p class="product-kicker">{kicker}</p>
			<h3 class="record-row__title">{displayTitle}</h3>
		</div>
		<StatusBadge status={widget.state} />
	</div>

	{#if displayMessage}
		<p class="text-sm text-[var(--color-text-muted)]">{displayMessage}</p>
	{/if}

	{@render children?.()}
</article>
