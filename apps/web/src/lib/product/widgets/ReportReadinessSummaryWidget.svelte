<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { isReportReadinessSummaryWidgetData } from './report-widget-data';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget }: { widget: ReportWidget } = $props();

	const data = $derived(
		isReportReadinessSummaryWidgetData(widget.data) ? widget.data : null
	);
</script>

<ReportWidgetShell {widget}>
	{#if data}
		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">Reportable campaigns</dt>
				<dd class="record-field__value">{data.reportableCampaignCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Submitted responses</dt>
				<dd class="record-field__value">{data.submittedResponseCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Visible scores</dt>
				<dd class="record-field__value">{data.visibleScoreCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Suppressed scores</dt>
				<dd class="record-field__value">{data.suppressedScoreCount}</dd>
			</div>
		</dl>

		{#if data.missingPrerequisites.length > 0}
			<div class="record-list" aria-label="Report readiness prerequisites">
				{#each data.missingPrerequisites as prerequisite (prerequisite.code)}
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-row__title">{prerequisite.label}</p>
								<p class="text-sm text-[var(--color-text-muted)]">{prerequisite.message}</p>
							</div>
							<StatusBadge status="blocked" label={prerequisite.severity} />
						</div>
					</div>
				{/each}
			</div>
		{/if}
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">Readiness data is unavailable.</p>
	{/if}
</ReportWidgetShell>
