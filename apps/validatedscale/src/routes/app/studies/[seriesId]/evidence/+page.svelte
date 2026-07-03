<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import {
		createProductApi,
		type CampaignSeriesReportsWorkspaceResponse,
		type ResultsDashboardBarResponse
	} from '$lib/api/product';
	import { createSetupApi } from '$lib/api/setup';
	import { api } from '$lib/core/client';
	import { formatCount, formatDateTime, humanizeToken } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';

	const product = createProductApi(api());
	const setup = createSetupApi(api());
	const seriesId = $derived(page.params.seriesId!);

	let workspace = $state<CampaignSeriesReportsWorkspaceResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'ready'>('loading');
	let exportBusy = $state<string | null>(null);
	let exportNote = $state<string | null>(null);
	let hover = $state<{ bar: ResultsDashboardBarResponse; x: number; y: number } | null>(null);

	const analytics = $derived(workspace?.resultsAnalytics ?? null);
	const dashboard = $derived(workspace?.resultsDashboard ?? null);

	const visibleOutputs = $derived(analytics?.scoreOutputs ?? []);

	const bars = $derived(dashboard?.outputBars ?? []);
	const barMax = $derived(
		Math.max(1, ...bars.map((bar) => (bar.value == null ? 0 : bar.value)))
	);

	/** wave matrix grouped by dimension for the trajectory table */
	const waveMatrix = $derived.by(() => {
		const rows = analytics?.waveRows ?? [];
		const byDimension = new Map<string, typeof rows>();
		for (const row of rows) {
			const bucket = byDimension.get(row.dimensionCode) ?? [];
			bucket.push(row);
			byDimension.set(row.dimensionCode, bucket);
		}
		return [...byDimension.entries()];
	});

	async function load() {
		try {
			workspace = await product.getCampaignSeriesReportsWorkspace(seriesId);
			loadState = 'ready';
		} catch {
			loadState = 'error';
		}
	}

	onMount(load);

	async function runExport(kind: 'responses' | 'matrix' | 'pdf') {
		if (exportBusy) return;
		exportBusy = kind;
		exportNote = null;

		try {
			if (kind === 'responses') await setup.createCampaignSeriesResponseExport(seriesId);
			else if (kind === 'matrix') await setup.createCampaignSeriesResultsMatrixExport(seriesId);
			else await setup.createCampaignSeriesReportPdfArtifact(seriesId);
			exportNote = 'Export queued. It appears below when ready.';
			await load();
		} catch {
			exportNote = 'The export could not be queued. Try again.';
		} finally {
			exportBusy = null;
		}
	}

	async function download(artifactId: string) {
		try {
			const signed = await setup.getExportArtifactSignedDownloadUrl(artifactId);
			location.assign(signed.url);
		} catch {
			exportNote = 'Download link could not be created. Try again.';
		}
	}

	function fmt(value: number | null | undefined, digits = 2): string {
		return value == null ? '—' : value.toFixed(digits);
	}

	function delta(value: number | null): string {
		if (value == null) return '';
		const sign = value > 0 ? '+' : '';
		return `${sign}${value.toFixed(2)}`;
	}

	/** SVG polyline points for one dimension's mean across waves (small multiple). */
	function trendPoints(rows: { mean: number | null; disclosure: string }[]): string {
		const visible = rows.map((row) =>
			row.disclosure.toLowerCase() === 'visible' && row.mean != null ? row.mean : null
		);
		const values = visible.filter((value): value is number => value !== null);
		if (values.length < 2) return '';
		const min = Math.min(...values);
		const max = Math.max(...values);
		const span = max - min || 1;
		const width = 96;
		const height = 26;
		const step = width / (visible.length - 1);
		return visible
			.map((value, index) =>
				value === null
					? null
					: `${(index * step).toFixed(1)},${(height - 4 - ((value - min) / span) * (height - 8) + 2).toFixed(1)}`
			)
			.filter((point): point is string => point !== null)
			.join(' ');
	}
</script>

<svelte:head><title>Evidence — ValidatedScale</title></svelte:head>

<LoadState state={loadState}>
	{#if workspace}
		<header class="head">
			<p class="eyebrow">
				<a href="/app/studies">Studies</a> /
				<a href={`/app/studies/${seriesId}`}>{workspace.series.name}</a>
			</p>
			<h1 class="doc-title">Evidence</h1>
			<p class="datum provenance">
				{formatCount(workspace.summary.submittedResponseCount)} responses ·
				{formatCount(workspace.summary.visibleScoreCount)} scores visible ·
				{formatCount(workspace.summary.suppressedScoreCount)} suppressed
				{#if dashboard}· reporting threshold k = {dashboard.disclosureKMin}{/if}
			</p>

			<nav class="phases" aria-label="Study phases">
				<a class="phase" href={`/app/studies/${seriesId}`}>Protocol</a>
				<a class="phase" href={`/app/studies/${seriesId}/field`}>Field</a>
				<span class="phase current">Evidence</span>
			</nav>
		</header>

		{#if visibleOutputs.length === 0 && bars.length === 0}
			<div class="panel nothing">
				<span class="eyebrow">No reportable scores yet</span>
				<p>
					Evidence appears when a wave has submitted, scored responses above the reporting
					threshold. Until then, nothing is shown — by design.
				</p>
			</div>
		{:else}
			<div class="grid">
				<div class="main">
					{#if bars.length > 0}
						<section class="findings-chart">
							<h2 class="eyebrow">Score profile {#if dashboard?.selectedCampaignName}— {dashboard.selectedCampaignName}{/if}</h2>
							<div class="chart" role="img" aria-label="Mean score by dimension">
								{#each bars as bar (bar.id)}
									<div
										class="bar-row"
										role="presentation"
										onmouseenter={(event) => (hover = { bar, x: event.clientX, y: event.clientY })}
										onmousemove={(event) => (hover = { bar, x: event.clientX, y: event.clientY })}
										onmouseleave={() => (hover = null)}
									>
										<span class="bar-label">{bar.label}</span>
										<div class="bar-track">
											{#if bar.disclosure.toLowerCase() === 'visible' && bar.value != null}
												<div class="bar-fill" style={`width: ${(bar.value / barMax) * 100}%`}></div>
												<span class="datum bar-value">{fmt(bar.value)}</span>
											{:else}
												<div class="bar-suppressed"></div>
												<span class="datum bar-value suppressed-note">
													suppressed{bar.suppressionReason ? ` — ${humanizeToken(bar.suppressionReason)}` : ''}
												</span>
											{/if}
										</div>
									</div>
								{/each}
							</div>
						</section>
					{/if}

					{#if visibleOutputs.length > 0}
						<section class="findings">
							<h2 class="eyebrow">Findings</h2>
							<div class="table-wrap">
								<table>
									<thead>
										<tr>
											<th>Dimension</th>
											<th class="num">n</th>
											<th class="num">Mean</th>
											<th class="num">Median</th>
											<th class="num">SD</th>
											<th class="num">Range</th>
										</tr>
									</thead>
									<tbody>
										{#each visibleOutputs as row (row.dimensionCode)}
											<tr class:suppressed={row.disclosure.toLowerCase() !== 'visible'}>
												<td>{row.dimensionCode}</td>
												{#if row.disclosure.toLowerCase() === 'visible'}
													<td class="num datum">{formatCount(row.scoreCount)}</td>
													<td class="num datum">{fmt(row.mean)}</td>
													<td class="num datum">{fmt(row.median)}</td>
													<td class="num datum">{fmt(row.standardDeviation)}</td>
													<td class="num datum">{fmt(row.min, 1)}–{fmt(row.max, 1)}</td>
												{:else}
													<td colspan="5" class="suppress-cell">
														Suppressed — {humanizeToken(row.suppressionReason) || 'below reporting threshold'}
													</td>
												{/if}
											</tr>
										{/each}
									</tbody>
								</table>
							</div>
						</section>
					{/if}

					{#if waveMatrix.length > 0}
						<section class="trajectory">
							<h2 class="eyebrow">Across waves</h2>
							<div class="table-wrap">
								<table>
									<thead>
										<tr>
											<th>Dimension</th>
											<th class="trend-h">Trend</th>
											{#each waveMatrix[0][1] as wave (wave.campaignId)}
												<th class="num">{wave.campaignName}</th>
											{/each}
										</tr>
									</thead>
									<tbody>
										{#each waveMatrix as [dimension, rows] (dimension)}
											<tr>
												<td>{dimension}</td>
												<td class="trend-cell">
													{#if trendPoints(rows)}
														<svg width="96" height="26" role="img" aria-label={`${dimension} mean across waves`}>
															<polyline
																points={trendPoints(rows)}
																fill="none"
																stroke="var(--color-chart-violet)"
																stroke-width="2"
																stroke-linecap="round"
																stroke-linejoin="round"
															/>
														</svg>
													{/if}
												</td>
												{#each rows as wave (wave.campaignId)}
													<td class="num datum">
														{#if wave.disclosure.toLowerCase() === 'visible'}
															{fmt(wave.mean)}
															{#if wave.deltaFromPreviousMean != null}
																<span
																	class="delta"
																	class:up={wave.deltaFromPreviousMean > 0}
																	class:down={wave.deltaFromPreviousMean < 0}
																>
																	{delta(wave.deltaFromPreviousMean)}
																</span>
															{/if}
														{:else}
															<span class="suppress-cell">suppr.</span>
														{/if}
													</td>
												{/each}
											</tr>
										{/each}
									</tbody>
								</table>
							</div>
						</section>
					{/if}

					{#if (analytics?.insights ?? []).length > 0}
						<section class="notes">
							<h2 class="eyebrow">Notes</h2>
							<ul>
								{#each analytics?.insights ?? [] as note (note.title)}
									<li>
										<strong>{note.title}</strong>
										<p>{note.detail}</p>
									</li>
								{/each}
							</ul>
						</section>
					{/if}
				</div>

				<aside class="side">
					<div class="panel exports">
						<h2 class="eyebrow">Exports</h2>
						<div class="export-actions">
							<button class="btn btn-ghost" disabled={exportBusy !== null} onclick={() => runExport('responses')}>
								{exportBusy === 'responses' ? 'Queueing…' : 'Responses CSV + codebook'}
							</button>
							<button class="btn btn-ghost" disabled={exportBusy !== null} onclick={() => runExport('matrix')}>
								{exportBusy === 'matrix' ? 'Queueing…' : 'Results matrix CSV'}
							</button>
							<button class="btn btn-ghost" disabled={exportBusy !== null} onclick={() => runExport('pdf')}>
								{exportBusy === 'pdf' ? 'Queueing…' : 'Report PDF'}
							</button>
						</div>
						{#if exportNote}<p class="export-note" role="status">{exportNote}</p>{/if}

						<ul class="artifacts">
							{#each workspace.exportArtifacts.slice(0, 8) as artifact (artifact.id)}
								<li>
									<div class="artifact-main">
										<span class="artifact-name">{artifact.fileName}</span>
										<span class="datum artifact-meta">
											{humanizeToken(artifact.status)} · {formatDateTime(artifact.createdAt)}
										</span>
									</div>
									{#if artifact.canDownload}
										<button class="dl" onclick={() => download(artifact.id)}>Download</button>
									{/if}
								</li>
							{:else}
								<li class="artifact-none">No exports yet.</li>
							{/each}
						</ul>
					</div>
				</aside>
			</div>
		{/if}

		{#if hover}
			<div class="tooltip datum" style={`left: ${hover.x + 12}px; top: ${hover.y + 12}px`} role="status">
				<strong>{hover.bar.label}</strong>
				{#if hover.bar.value != null}
					mean {fmt(hover.bar.value)}{#if hover.bar.count != null}&nbsp;· n = {hover.bar.count}{/if}
				{:else}
					suppressed
				{/if}
			</div>
		{/if}
	{/if}
</LoadState>

<style>
	.head {
		margin-bottom: 2rem;
	}

	.head .eyebrow a {
		color: inherit;
		text-decoration: none;
	}

	.head .eyebrow a:hover {
		color: var(--color-stain);
	}

	.head h1 {
		font-size: 2.25rem;
		margin-top: 0.5rem;
	}

	.provenance {
		margin-top: 0.5rem;
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.phases {
		display: flex;
		gap: 0.25rem;
		margin-top: 1.5rem;
		border-bottom: 1px solid var(--color-line);
	}

	.phase {
		padding: 0.5rem 0.875rem;
		font-size: 0.875rem;
		font-weight: 540;
		color: var(--color-ink-2);
		text-decoration: none;
		border-bottom: 2px solid transparent;
		margin-bottom: -1px;
	}

	.phase.current {
		color: var(--color-stain);
		border-bottom-color: var(--color-stain);
	}

	.nothing {
		padding: 2.5rem;
		display: flex;
		flex-direction: column;
		gap: 0.625rem;
	}

	.nothing p {
		font-size: 0.9375rem;
		color: var(--color-ink-2);
		max-width: 52ch;
	}

	.grid {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 19rem;
		gap: 3rem;
	}

	section {
		margin-bottom: 2.75rem;
	}

	/* score profile bars */
	.chart {
		margin-top: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.625rem;
	}

	.bar-row {
		display: grid;
		grid-template-columns: 11rem minmax(0, 1fr);
		align-items: center;
		gap: 1rem;
	}

	.bar-label {
		font-size: 0.875rem;
		color: var(--color-ink-2);
		text-align: right;
	}

	.bar-track {
		position: relative;
		height: 18px;
		display: flex;
		align-items: center;
	}

	.bar-fill {
		height: 12px;
		background: var(--color-chart-violet);
		border-radius: 2px 4px 4px 2px;
		min-width: 2px;
	}

	.bar-suppressed {
		height: 12px;
		width: 4.5rem;
		border: 1px dashed var(--color-line-2);
		border-radius: 2px;
		background: repeating-linear-gradient(
			45deg,
			transparent 0 3px,
			var(--color-line) 3px 4px
		);
	}

	.bar-value {
		margin-left: 0.625rem;
		font-size: 0.75rem;
		color: var(--color-ink-2);
		white-space: nowrap;
	}

	.suppressed-note {
		color: var(--color-ink-3);
	}

	/* tables */
	.table-wrap {
		margin-top: 0.75rem;
		overflow-x: auto;
	}

	table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.875rem;
	}

	th {
		text-align: left;
		font-family: var(--font-ui);
		font-size: 0.6875rem;
		font-weight: 600;
		letter-spacing: 0.1em;
		text-transform: uppercase;
		color: var(--color-ink-3);
		padding: 0.5rem 0.75rem 0.5rem 0;
		border-bottom: 1px solid var(--color-line-2);
	}

	td {
		padding: 0.5625rem 0.75rem 0.5625rem 0;
		border-bottom: 1px solid var(--color-line);
		vertical-align: baseline;
	}

	.num {
		text-align: right;
	}

	tr.suppressed td {
		color: var(--color-ink-3);
	}

	.suppress-cell {
		color: var(--color-ink-3);
		font-style: italic;
		font-size: 0.8125rem;
	}

	.trend-h {
		width: 7rem;
	}

	.trend-cell svg {
		display: block;
	}

	.delta {
		display: inline-block;
		margin-left: 0.375rem;
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.delta.up {
		color: var(--color-series-1);
	}

	.delta.down {
		color: var(--color-series-2);
	}

	.notes ul {
		list-style: none;
		margin-top: 0.75rem;
		display: flex;
		flex-direction: column;
		gap: 1rem;
	}

	.notes strong {
		font-weight: 580;
	}

	.notes p {
		margin-top: 0.25rem;
		font-size: 0.875rem;
		color: var(--color-ink-2);
		max-width: 56ch;
	}

	/* exports */
	.exports {
		padding: 1.25rem;
		border-top: 3px solid var(--color-stain);
		position: sticky;
		top: 1.5rem;
	}

	.export-actions {
		margin-top: 0.75rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.export-actions .btn {
		justify-content: center;
	}

	.export-note {
		margin-top: 0.625rem;
		font-size: 0.8125rem;
		color: var(--color-ink-2);
	}

	.artifacts {
		list-style: none;
		margin-top: 1.25rem;
		display: flex;
		flex-direction: column;
	}

	.artifacts li {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 0.75rem;
		padding: 0.625rem 0;
		border-bottom: 1px dashed var(--color-line);
	}

	.artifacts li:last-child {
		border-bottom: none;
	}

	.artifact-main {
		display: flex;
		flex-direction: column;
		gap: 0.125rem;
		min-width: 0;
	}

	.artifact-name {
		font-size: 0.8125rem;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.artifact-meta {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.artifact-none {
		font-size: 0.875rem;
		color: var(--color-ink-3);
	}

	.dl {
		background: none;
		border: none;
		font: inherit;
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-stain);
		cursor: pointer;
		white-space: nowrap;
	}

	.tooltip {
		position: fixed;
		z-index: 40;
		background: var(--color-ink);
		color: #fff;
		font-size: 0.75rem;
		padding: 0.5rem 0.625rem;
		border-radius: var(--radius-instrument);
		pointer-events: none;
		display: flex;
		flex-direction: column;
		gap: 0.125rem;
	}

	@media (max-width: 58rem) {
		.grid {
			grid-template-columns: 1fr;
		}

		.exports {
			position: static;
		}

		.bar-row {
			grid-template-columns: 7.5rem minmax(0, 1fr);
		}
	}
</style>
