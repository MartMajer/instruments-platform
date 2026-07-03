<script lang="ts">
	import { onMount } from 'svelte';
	import {
		createProductApi,
		type ExportArtifactLibraryResponse
	} from '$lib/api/product';
	import { createSetupApi } from '$lib/api/setup';
	import { api } from '$lib/core/client';
	import { formatCount, formatDateTime, humanizeToken } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';

	const product = createProductApi(api());
	const setup = createSetupApi(api());

	let library = $state<ExportArtifactLibraryResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'empty' | 'ready'>('loading');
	let note = $state<string | null>(null);

	onMount(async () => {
		try {
			library = await product.listExportArtifacts();
			loadState = library.artifacts.length === 0 ? 'empty' : 'ready';
		} catch {
			loadState = 'error';
		}
	});

	async function download(artifactId: string) {
		try {
			const signed = await setup.getExportArtifactSignedDownloadUrl(artifactId);
			location.assign(signed.url);
		} catch {
			note = 'Download link could not be created. Try again.';
		}
	}

	function formatBytes(size: number): string {
		if (size < 1024) return `${size} B`;
		if (size < 1024 * 1024) return `${(size / 1024).toFixed(0)} KB`;
		return `${(size / (1024 * 1024)).toFixed(1)} MB`;
	}
</script>

<svelte:head><title>Exports — Spectra</title></svelte:head>

<header class="head">
	<p class="eyebrow">Artifact library</p>
	<h1 class="doc-title">Exports</h1>
	{#if library}
		<p class="datum meta">
			{formatCount(library.summary.totalCount)} artifacts ·
			{formatCount(library.summary.downloadableCount)} downloadable ·
			{formatCount(library.summary.pendingCount)} pending ·
			{formatCount(library.summary.failedCount)} failed
		</p>
	{/if}
</header>

{#if note}<p class="note" role="status">{note}</p>{/if}

<LoadState
	state={loadState}
	emptyTitle="No exports yet"
	emptyBody="Exports are queued from a study's Evidence page — responses CSV with codebook, results matrix, or report PDF."
>
	{#if library}
		<div class="table-wrap">
			<table>
				<thead>
					<tr>
						<th>File</th>
						<th>Study</th>
						<th>Type</th>
						<th>Status</th>
						<th class="num">Rows</th>
						<th class="num">Size</th>
						<th>Created</th>
						<th></th>
					</tr>
				</thead>
				<tbody>
					{#each library.artifacts as artifact (artifact.id)}
						<tr>
							<td class="file">{artifact.fileName}</td>
							<td>{artifact.targetLabel}</td>
							<td>{humanizeToken(artifact.artifactType)}</td>
							<td>
								<span
									class="chip"
									class:chip-live={artifact.status.toLowerCase() === 'completed'}
									class:chip-danger={artifact.status.toLowerCase() === 'failed'}
								>
									{humanizeToken(artifact.status)}
								</span>
							</td>
							<td class="num datum">{formatCount(artifact.rowCount)}</td>
							<td class="num datum">{formatBytes(artifact.byteSize)}</td>
							<td class="datum when">{formatDateTime(artifact.createdAt)}</td>
							<td class="act">
								{#if artifact.canDownload}
									<button class="dl" onclick={() => download(artifact.id)}>Download</button>
								{/if}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</LoadState>

<style>
	.head {
		margin-bottom: 2rem;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.375rem;
	}

	.meta {
		margin-top: 0.5rem;
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.note {
		margin-bottom: 1rem;
		font-size: 0.875rem;
		color: var(--color-ink-2);
	}

	.table-wrap {
		overflow-x: auto;
	}

	table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.875rem;
	}

	th {
		text-align: left;
		font-size: 0.6875rem;
		font-weight: 600;
		letter-spacing: 0.1em;
		text-transform: uppercase;
		color: var(--color-ink-3);
		padding: 0.5rem 1rem 0.5rem 0;
		border-bottom: 1px solid var(--color-line-2);
		white-space: nowrap;
	}

	td {
		padding: 0.625rem 1rem 0.625rem 0;
		border-bottom: 1px solid var(--color-line);
		vertical-align: baseline;
	}

	.file {
		font-family: var(--font-mono);
		font-size: 0.8125rem;
		max-width: 20rem;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.num {
		text-align: right;
	}

	.when {
		font-size: 0.75rem;
		color: var(--color-ink-3);
		white-space: nowrap;
	}

	.act {
		text-align: right;
	}

	.dl {
		background: none;
		border: none;
		font: inherit;
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-stain);
		cursor: pointer;
	}
</style>
