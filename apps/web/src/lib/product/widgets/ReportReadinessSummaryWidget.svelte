<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { isReportReadinessSummaryWidgetData } from './report-widget-data';
	import {
		formatProductCopy,
		formatWidgetLabel,
		type ReportWidgetFormatCopy
	} from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(
		isReportReadinessSummaryWidgetData(widget.data) ? widget.data : null
	);
</script>

<ReportWidgetShell {widget} {copy}>
	{#if data}
		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('reportableCampaigns', copy)}</dt>
				<dd class="record-field__value">{data.reportableCampaignCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('submittedResponses', copy)}</dt>
				<dd class="record-field__value">{data.submittedResponseCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('visibleScores', copy)}</dt>
				<dd class="record-field__value">{data.visibleScoreCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{formatWidgetLabel('suppressedScores', copy)}</dt>
				<dd class="record-field__value">{data.suppressedScoreCount}</dd>
			</div>
		</dl>

		{#if data.missingPrerequisites.length > 0}
			<div class="record-list" aria-label={formatWidgetLabel('reportReadinessPrerequisites', copy)}>
				{#each data.missingPrerequisites as prerequisite (prerequisite.code)}
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-row__title">{formatProductCopy(prerequisite.label)}</p>
								<p class="text-sm text-[var(--color-text-muted)]">
									{formatProductCopy(prerequisite.message)}
								</p>
							</div>
							<StatusBadge status="blocked" label={prerequisite.severity} />
						</div>
					</div>
				{/each}
			</div>
		{/if}
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			{formatWidgetLabel('readinessDataUnavailable', copy)}
		</p>
	{/if}
</ReportWidgetShell>
