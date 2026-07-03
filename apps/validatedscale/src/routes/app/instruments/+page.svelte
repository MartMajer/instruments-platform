<script lang="ts">
	import { onMount } from 'svelte';
	import { createSetupApi, type InstrumentSummaryResponse } from '$lib/api/setup';
	import { api } from '$lib/core/client';
	import { t } from '$lib/core/locale.svelte';
	import { humanizeToken } from '$lib/core/format';
	import { gallery } from '$lib/instruments/gallery';
	import { promptDialog } from '$lib/ui/dialog.svelte';
	import LoadState from '$lib/ui/LoadState.svelte';

	const setup = createSetupApi(api());

	let previewFor = $state<string | null>(null);
	let useBusy = $state<string | null>(null);
	let useStep = $state<string | null>(null);
	let useError = $state<string | null>(null);

	async function useInstrument(code: string) {
		const entry = gallery.find((candidate) => candidate.code === code);
		if (!entry?.autoload || !entry.template || useBusy) return;

		const studyName = await promptDialog({
			title: `${t('Start a study with')} ${entry.code}`,
			body: `${entry.itemCount} items load ready to launch — you add recipients and waves. Name the study the way you would in the paper.`,
			confirmLabel: t('Create study'),
			initialValue: `${entry.code} study`
		});
		if (!studyName) return;

		useBusy = code;
		useError = null;

		try {
			useStep = 'Recording instrument…';
			const instrument = await setup.createPrivateInstrumentImport({
				code: entry.code,
				version: entry.version,
				fullName: entry.name,
				domain: 'psychometric',
				provenanceNote: entry.provenance,
				rightsStatus: 'attested_by_tenant',
				validityLabel: 'tenant_provided',
				licenseType: 'free',
				citationApa: entry.citation
			}).catch(() => null); // already imported earlier is fine

			useStep = 'Creating study…';
			const series = await setup.createCampaignSeries({ name: studyName.trim() });

			useStep = 'Loading questionnaire…';
			const template = await setup.createTemplateVersion({
				templateName: entry.name,
				semver: '1.0.0',
				defaultLocale: 'en',
				instrumentId: instrument?.id ?? null,
				sections: entry.template.sections,
				scales: entry.template.scales,
				questions: entry.template.questions
			});
			if (template.status?.toLowerCase() === 'draft') {
				await setup.publishTemplateVersion(template.templateVersionId);
			}

			useStep = 'Binding scoring…';
			await setup.createScoringRule({
				templateVersionId: template.templateVersionId,
				ruleKey: entry.scoringKey!,
				ruleVersion: '1.0.0',
				schemaVersion: '1.0.0',
				engineMinVersion: '1.0.0',
				document: entry.scoringDocument!,
				produces: entry.scoringProduces!,
				compatibility: '{}'
			});

			useStep = 'Attaching to study…';
			await setup.selectCampaignSeriesSetupTemplate(series.id, {
				templateVersionId: template.templateVersionId
			});

			location.assign(`/app/studies/${series.id}`);
		} catch {
			useError = `Setting up ${entry.code} failed at: ${useStep ?? 'start'}. Try again.`;
			useBusy = null;
			useStep = null;
		}
	}

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

<svelte:window onkeydown={(event) => { if (event.key === 'Escape') previewFor = null; }} />

<svelte:head><title>Instruments — ValidatedScale</title></svelte:head>

<header class="head">
	<div>
		<p class="eyebrow">{t('Library')}</p>
		<h1 class="doc-title">{t('Instruments')}</h1>
		<p class="hint">
			Instruments you provide, with your rights attestation. Nothing here is an official
			platform publication of a named instrument.
		</p>
	</div>
	<button class="btn btn-ink" onclick={() => (importOpen = !importOpen)}>
		{importOpen ? t('Close') : t('Import instrument')}
	</button>
</header>

{#if importOpen}
	<form class="panel import" onsubmit={runImport}>
		<div class="row">
			<div class="field">
				<label class="eyebrow" for="i-code">{t('Code')}</label>
				<input id="i-code" class="datum" required bind:value={form.code} placeholder="WB-SCALE" />
			</div>
			<div class="field">
				<label class="eyebrow" for="i-version">{t('Version')}</label>
				<input id="i-version" class="datum" required bind:value={form.version} />
			</div>
			<div class="field grow">
				<label class="eyebrow" for="i-name">{t('Full name')}</label>
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
			<label class="eyebrow" for="i-cite">{t('Citation (APA, optional)')}</label>
			<input id="i-cite" bind:value={form.citationApa} />
		</div>
		{#if importError}<p class="error" role="alert">{importError}</p>{/if}
		<button class="btn btn-stain" type="submit" disabled={importBusy}>
			{importBusy ? t('Importing…') : t('Import as tenant-provided')}
		</button>
	</form>
{/if}

<section class="gallery">
	<h2 class="eyebrow">{t('Instrument gallery — press Use, then add recipients and waves')}</h2>
	{#if useError}<p class="use-error" role="alert">{useError}</p>{/if}
	<div class="gallery-grid">
		{#each gallery as entry (entry.code)}
			<article class="panel g-card" class:muted={!entry.autoload}>
				<div class="g-head">
					<span class="datum g-code">{entry.code}</span>
					<span
						class="chip"
						class:chip-live={entry.licenseBadge === 'public-domain'}
						class:chip-stain={entry.licenseBadge === 'free'}
					>
						{entry.licenseBadge === 'public-domain'
							? t('Public domain')
							: entry.licenseBadge === 'free'
								? t('Free standard')
								: t('Rights required')}
					</span>
				</div>
				<h3 class="doc-title g-name">{t(entry.name)}</h3>
				<p class="g-measures">{t(entry.measures)}</p>
				<p class="datum g-meta">
					{entry.itemCount} {t('items')} · ~{entry.minutes} min · {t(entry.audience)}
				</p>
				<p class="g-license">{t(entry.license)}</p>
				{#if entry.autoload}
					<div class="g-actions">
						<button class="btn btn-stain g-use" disabled={useBusy !== null} onclick={() => useInstrument(entry.code)}>
							{useBusy === entry.code ? (useStep ?? 'Setting up…') : `${t('Use')} ${entry.code}`}
						</button>
						<button class="g-preview-btn" onclick={() => (previewFor = entry.code)}>
							{t('View all')} {entry.itemCount} {t('items')}
						</button>
					</div>
				{:else}
					<p class="g-how">{t(entry.howToGet ?? '')}</p>
				{/if}
			</article>
		{/each}
	</div>
</section>

{#if previewFor}
	{@const entry = gallery.find((candidate) => candidate.code === previewFor)}
	{#if entry?.template}
		<div class="scrim" role="presentation" onclick={() => (previewFor = null)}>
			<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
			<div
				class="items-modal panel"
				role="dialog"
				aria-modal="true"
				tabindex="-1"
				aria-label={`${entry.code} items`}
				onclick={(event) => event.stopPropagation()}
				onkeydown={(event) => { if (event.key === 'Escape') previewFor = null; }}
			>
				<div class="im-head">
					<div>
						<span class="datum g-code">{entry.code}</span>
						<h2 class="doc-title im-title">{entry.name}</h2>
					</div>
					<button class="im-close" aria-label="Close" onclick={() => (previewFor = null)}>×</button>
				</div>
				<ol class="g-items">
					{#each entry.template.questions as question (question.code)}
						<li>
							{question.textDefault}
							{#if question.scaleCode}
								{@const scale = entry.template.scales.find((s) => s.code === question.scaleCode)}
								{#if scale}
									<span class="datum g-scale">{scale.minValue}–{scale.maxValue}</span>
								{/if}
							{:else}
								<span class="datum g-scale">yes / no</span>
							{/if}
						</li>
					{/each}
				</ol>
				<p class="g-cite datum">{entry.citation}</p>
				<div class="im-actions">
					<button class="btn btn-stain" disabled={useBusy !== null} onclick={() => { previewFor = null; useInstrument(entry.code); }}>
						{t('Use')} {entry.code}
					</button>
				</div>
			</div>
		</div>
	{/if}
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
				<span class="chip">{t(humanizeToken(instrument.validityLabel))}</span>
				<span class="datum rights">{t(humanizeToken(instrument.rightsStatus))}</span>
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

	.gallery {
		margin-bottom: 2.5rem;
	}

	.use-error {
		margin-top: 0.5rem;
		font-size: 0.8125rem;
		color: var(--color-danger);
	}

	.gallery-grid {
		margin-top: 1rem;
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(19rem, 1fr));
		gap: 1rem;
	}

	.g-card {
		padding: 1.125rem 1.25rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.g-card.muted {
		background: var(--color-sunk);
	}

	.g-head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 0.5rem;
	}

	.g-code {
		font-size: 0.75rem;
		color: var(--color-stain);
	}

	.g-name {
		font-size: 1.0625rem;
		line-height: 1.3;
	}

	.g-measures {
		font-size: 0.8125rem;
		line-height: 1.55;
		color: var(--color-ink-2);
		flex: 1;
	}

	.g-meta {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.g-license {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.g-actions {
		display: flex;
		align-items: center;
		gap: 0.875rem;
		margin-top: 0.25rem;
	}

	.g-use {
		flex-shrink: 0;
	}

	.g-preview-btn {
		background: none;
		border: none;
		font: inherit;
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-stain);
		cursor: pointer;
		padding: 0;
	}

	.scrim {
		position: fixed;
		inset: 0;
		background: rgb(21 28 37 / 0.45);
		display: flex;
		align-items: flex-start;
		justify-content: center;
		padding: 10vh 1rem 2rem;
		z-index: 100;
	}

	.items-modal {
		width: min(36rem, 100%);
		max-height: 78vh;
		overflow-y: auto;
		padding: 1.75rem;
		border-top: 3px solid var(--color-stain);
	}

	.im-head {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 1rem;
	}

	.im-title {
		font-size: 1.25rem;
		margin-top: 0.25rem;
	}

	.im-close {
		background: none;
		border: none;
		font-size: 1.5rem;
		line-height: 1;
		color: var(--color-ink-3);
		cursor: pointer;
	}

	.im-close:hover {
		color: var(--color-ink);
	}

	.im-actions {
		margin-top: 1.25rem;
	}

	.g-items {
		margin: 1rem 0 0 1.125rem;
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
		font-size: 0.8125rem;
		line-height: 1.5;
		color: var(--color-ink-2);
	}

	.g-scale {
		margin-left: 0.375rem;
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.g-cite {
		margin-top: 0.625rem;
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.g-how {
		font-size: 0.75rem;
		color: var(--color-ink-2);
		border-top: 1px dashed var(--color-line-2);
		padding-top: 0.5rem;
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
