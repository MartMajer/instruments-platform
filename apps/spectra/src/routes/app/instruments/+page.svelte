<script lang="ts">
	import { onMount } from 'svelte';
	import { createSetupApi, type InstrumentSummaryResponse } from '$lib/api/setup';
	import { api } from '$lib/core/client';
	import { humanizeToken } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';

	const setup = createSetupApi(api());

	let instruments = $state<InstrumentSummaryResponse[]>([]);
	let loadState = $state<'loading' | 'error' | 'empty' | 'ready'>('loading');

	let importOpen = $state(false);
	let importBusy = $state(false);
	let importError = $state<string | null>(null);
	let form = $state({
		code: '',
		version: '1.0',
		fullName: '',
		domain: 'psychometric',
		provenanceNote: '',
		rightsStatus: 'tenant_attested',
		validityLabel: 'tenant_provided',
		licenseType: 'tenant_provided',
		citationApa: ''
	});

	async function load() {
		try {
			instruments = await setup.listInstruments();
			loadState = instruments.length === 0 ? 'empty' : 'ready';
		} catch {
			loadState = 'error';
		}
	}

	onMount(load);

	async function runImport(event: SubmitEvent) {
		event.preventDefault();
		if (importBusy) return;
		importBusy = true;
		importError = null;

		try {
			await setup.createPrivateInstrumentImport({
				...form,
				code: form.code.trim(),
				fullName: form.fullName.trim(),
				provenanceNote: form.provenanceNote.trim(),
				citationApa: form.citationApa.trim() || null
			});
			importOpen = false;
			form = { ...form, code: '', fullName: '', provenanceNote: '', citationApa: '' };
			await load();
		} catch {
			importError = 'The import was not accepted. Check the fields and try again.';
		} finally {
			importBusy = false;
		}
	}
</script>

<svelte:head><title>Instruments — Spectra</title></svelte:head>

<header class="head">
	<div>
		<p class="eyebrow">Library</p>
		<h1 class="doc-title">Instruments</h1>
		<p class="hint">
			Instruments you provide, with your rights attestation. Nothing here is an official
			platform publication of a named instrument.
		</p>
	</div>
	<button class="btn btn-ink" onclick={() => (importOpen = !importOpen)}>
		{importOpen ? 'Close' : 'Import instrument'}
	</button>
</header>

{#if importOpen}
	<form class="panel import" onsubmit={runImport}>
		<div class="row">
			<div class="field">
				<label class="eyebrow" for="i-code">Code</label>
				<input id="i-code" class="datum" required bind:value={form.code} placeholder="WB-SCALE" />
			</div>
			<div class="field">
				<label class="eyebrow" for="i-version">Version</label>
				<input id="i-version" class="datum" required bind:value={form.version} />
			</div>
			<div class="field grow">
				<label class="eyebrow" for="i-name">Full name</label>
				<input id="i-name" required bind:value={form.fullName} placeholder="Workplace Wellbeing Scale" />
			</div>
		</div>
		<div class="field">
			<label class="eyebrow" for="i-prov">Provenance & rights note</label>
			<textarea
				id="i-prov"
				rows="2"
				required
				bind:value={form.provenanceNote}
				placeholder="Where the instrument comes from and on what basis you may use it."
			></textarea>
		</div>
		<div class="field">
			<label class="eyebrow" for="i-cite">Citation (APA, optional)</label>
			<input id="i-cite" bind:value={form.citationApa} />
		</div>
		{#if importError}<p class="error" role="alert">{importError}</p>{/if}
		<button class="btn btn-stain" type="submit" disabled={importBusy}>
			{importBusy ? 'Importing…' : 'Import as tenant-provided'}
		</button>
	</form>
{/if}

<LoadState
	state={loadState}
	emptyTitle="No instruments yet"
	emptyBody="Import a validated instrument you have the right to use. It becomes available to your studies as tenant-provided content."
>
	<ul class="list">
		{#each instruments as instrument (instrument.id)}
			<li>
				<div class="main">
					<span class="datum code">{instrument.code} v{instrument.version}</span>
					<span class="name doc-title">{instrument.fullName}</span>
				</div>
				<span class="chip">{humanizeToken(instrument.validityLabel)}</span>
				<span class="datum rights">{humanizeToken(instrument.rightsStatus)}</span>
			</li>
		{/each}
	</ul>
</LoadState>

<style>
	.head {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 1.5rem;
		margin-bottom: 2rem;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.375rem;
	}

	.hint {
		margin-top: 0.5rem;
		font-size: 0.875rem;
		color: var(--color-ink-3);
		max-width: 52ch;
	}

	.import {
		padding: 1.5rem;
		margin-bottom: 2rem;
		display: flex;
		flex-direction: column;
		gap: 1rem;
		border-top: 3px solid var(--color-stain);
	}

	.row {
		display: flex;
		gap: 1rem;
		flex-wrap: wrap;
	}

	.field {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
	}

	.field.grow {
		flex: 1;
		min-width: 14rem;
	}

	input,
	textarea {
		font: inherit;
		padding: 0.5625rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	textarea {
		resize: vertical;
	}

	.import .btn {
		align-self: flex-start;
	}

	.error {
		font-size: 0.8125rem;
		color: var(--color-danger);
	}

	.list {
		list-style: none;
	}

	.list li {
		display: flex;
		align-items: baseline;
		gap: 1.25rem;
		padding: 1rem 0;
		border-bottom: 1px solid var(--color-line);
	}

	.main {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
		flex: 1;
		min-width: 0;
	}

	.code {
		font-size: 0.6875rem;
		color: var(--color-stain);
	}

	.name {
		font-size: 1.125rem;
	}

	.rights {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
		white-space: nowrap;
	}

	@media (max-width: 40rem) {
		.list li {
			flex-wrap: wrap;
			gap: 0.5rem;
		}
	}
</style>
