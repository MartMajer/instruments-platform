<script lang="ts">
	import { page } from '$app/state';
	import type { CampaignSeriesReportsWidgetManifestResponse } from '$lib/api/product';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { formatWidgetLabel } from './report-widget-format';
	import { getReportWidgetComponent } from './report-widget-registry';

	let {
		manifest,
		warning,
		embedded = false,
		includeKinds = null,
		hideWhenEmpty = false,
		showChrome = true
	}: {
		manifest: CampaignSeriesReportsWidgetManifestResponse | null;
		warning?: string | null;
		embedded?: boolean;
		includeKinds?: string[] | null;
		hideWhenEmpty?: boolean;
		showChrome?: boolean;
	} = $props();

	const appLocale = $derived(appLocaleFromPageData(page.data));
	const copy = $derived(routePageCopy(appLocale).selectedStudy.reportWidgets);
	const filteredWidgets = $derived(
		manifest?.widgets.filter((widget) => !includeKinds || includeKinds.includes(widget.kind)) ?? []
	);
	const filteredManifest = $derived(
		manifest
			? {
					...manifest,
					widgets: filteredWidgets
				}
			: null
	);
	const shouldRender = $derived(
		!hideWhenEmpty || Boolean(warning) || !manifest || filteredWidgets.length > 0
	);
</script>

{#if shouldRender && !showChrome}
	{@render WidgetBody()}
{:else if shouldRender && embedded}
	<div
		class="score-result-panel report-proof-panel"
		data-priority="trust"
		role="group"
		aria-label={formatWidgetLabel('resultsPreviewWidgets', copy)}
	>
		<div class="score-result-panel__header">
			<div>
				<p class="product-kicker">{formatWidgetLabel('resultsPreview', copy)}</p>
				<h4 class="record-row__title">{formatWidgetLabel('currentResultSummary', copy)}</h4>
			</div>
			{#if manifest}
				<span class="status-badge" data-status="ready">
					{formatWidgetLabel('previewReady', copy)}
				</span>
			{/if}
		</div>
		{@render WidgetBody()}
	</div>
{:else if shouldRender}
	<section
		class="product-panel"
		data-priority="trust"
		aria-label={formatWidgetLabel('resultsSummary', copy)}
	>
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">{formatWidgetLabel('resultsPreview', copy)}</p>
				<h3 class="product-title">{formatWidgetLabel('resultsSummary', copy)}</h3>
			</div>
			{#if manifest}
				<span class="status-badge" data-status="ready">
					{formatWidgetLabel('ready', copy)}
				</span>
			{/if}
		</div>
		{@render WidgetBody()}
	</section>
{/if}

{#snippet WidgetBody()}
	{#if warning}
		<p class="error-line">{warning}</p>
	{/if}

	{#if filteredManifest}
		<div class="report-widget-grid">
			{#each filteredManifest.widgets as widget (widget.id)}
				{@const WidgetComponent = getReportWidgetComponent(widget.kind)}
				<WidgetComponent {widget} {copy} />
			{/each}
		</div>
	{:else}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">
				{formatWidgetLabel('resultsPreviewUnavailable', copy)}
			</strong>
			<span>
				{warning
					? formatWidgetLabel('resultsPreviewUnavailableExport', copy)
					: formatWidgetLabel('resultsPreviewLoading', copy)}
			</span>
		</p>
	{/if}
{/snippet}
