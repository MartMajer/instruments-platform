<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { isExportArtifactRegistryWidgetData } from './report-widget-data';
	import {
		formatBooleanLabel,
		formatBytes,
		formatCodeLabel,
		formatNullableDate,
		formatProductCopy,
		type ReportWidgetFormatCopy
	} from './report-widget-format';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget, copy }: { widget: ReportWidget; copy?: ReportWidgetFormatCopy } = $props();

	const data = $derived(isExportArtifactRegistryWidgetData(widget.data) ? widget.data : null);
</script>

<ReportWidgetShell {widget} {copy}>
	{#if data}
		<dl class="record-grid">
			<div class="metric-card">
				<dt class="metric-card__label">Export files</dt>
				<dd class="metric-card__value">{data.exportArtifactCount}</dd>
			</div>
			<div class="metric-card">
				<dt class="metric-card__label">Listed</dt>
				<dd class="metric-card__value">{data.artifacts.length}</dd>
			</div>
		</dl>

		{#if data.artifacts.length > 0}
			<div class="record-list" aria-label="Export files">
				{#each data.artifacts as artifact (artifact.id)}
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-row__title">{artifact.fileName}</p>
								<p class="text-sm text-[var(--color-text-muted)]">
									{artifact.targetLabel} / {formatCodeLabel(artifact.format, copy)}
								</p>
							</div>
							<StatusBadge status="neutral" label={formatCodeLabel(artifact.status, copy)} />
						</div>

						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">Rows</dt>
								<dd class="record-field__value">{artifact.rowCount}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Size</dt>
								<dd class="record-field__value">{formatBytes(artifact.byteSize)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Created</dt>
								<dd class="record-field__value">{formatNullableDate(artifact.createdAt, copy)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Completed</dt>
								<dd class="record-field__value">{formatNullableDate(artifact.completedAt, copy)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Download</dt>
								<dd class="record-field__value">{formatBooleanLabel(artifact.canDownload, copy)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Failure reason</dt>
								<dd class="record-field__value">
									{formatCodeLabel(artifact.failureReasonCode, copy)}
								</dd>
							</div>
						</dl>
					</div>
				{/each}
			</div>
		{:else}
			<p class="text-sm text-[var(--color-text-muted)]">No export files recorded.</p>
		{/if}

		{#if widget.actions.length > 0}
			<div class="record-list" aria-label="Export actions">
				{#each widget.actions as action (action.id)}
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-row__title">{formatProductCopy(action.label)}</p>
								<p class="text-sm text-[var(--color-text-muted)]">
									{action.enabled ? 'Ready to run' : 'Unavailable'}
								</p>
							</div>
							<StatusBadge
								status={action.enabled ? 'ready' : 'blocked'}
								label={action.enabled ? 'Enabled' : 'Disabled'}
							/>
						</div>
						{#if !action.enabled && action.disabledReason}
							<p class="text-sm text-[var(--color-text-muted)]">
								{formatProductCopy(action.disabledReason)}
							</p>
						{/if}
					</div>
				{/each}
			</div>
		{/if}
	{:else}
		<p class="text-sm text-[var(--color-text-muted)]">
			Export file data is unavailable.
		</p>
	{/if}
</ReportWidgetShell>
