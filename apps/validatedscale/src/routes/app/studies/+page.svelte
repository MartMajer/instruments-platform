<script lang="ts">
	import { onMount } from 'svelte';
	import {
		createProductApi,
		type CampaignSeriesListItemResponse
	} from '$lib/api/product';
	import { api } from '$lib/core/client';
	import { t } from '$lib/core/locale.svelte';
	import { formatCount, formatDate, humanizeToken } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';

	const product = createProductApi(api());

	let items = $state<CampaignSeriesListItemResponse[]>([]);
	let loadState = $state<'loading' | 'error' | 'empty' | 'ready'>('loading');
	let search = $state('');
	let sampleBusy = $state(false);

	async function load() {
		try {
			const response = await product.listCampaignSeries();
			items = response.items;
			loadState = items.length === 0 ? 'empty' : 'ready';
		} catch {
			loadState = 'error';
		}
	}

	async function addExamples() {
		if (sampleBusy) return;
		sampleBusy = true;
		try {
			await product.ensureSampleStudies();
			await load();
		} finally {
			sampleBusy = false;
		}
	}

	type Bucket = {
		key: string;
		title: string;
		note: string;
		items: CampaignSeriesListItemResponse[];
	};

	const filtered = $derived(
		items.filter((s) => s.name.toLowerCase().includes(search.trim().toLowerCase()))
	);

	const buckets = $derived.by<Bucket[]>(() => {
		const inField = filtered.filter((s) => !s.archived && s.liveCampaignCount > 0);
		const preparing = filtered.filter(
			(s) => !s.archived && s.liveCampaignCount === 0 && s.submittedResponseCount === 0
		);
		const collected = filtered.filter(
			(s) => !s.archived && s.liveCampaignCount === 0 && s.submittedResponseCount > 0
		);
		const archived = filtered.filter((s) => s.archived);

		return [
			{ key: 'field', title: t('In the field'), note: t('collecting now'), items: inField },
			{ key: 'prep', title: t('In preparation'), note: t('not yet launched'), items: preparing },
			{ key: 'done', title: t('Collected'), note: t('waves closed, evidence available'), items: collected },
			{ key: 'archived', title: t('Archived'), note: '', items: archived }
		].filter((bucket) => bucket.items.length > 0);
	});

	onMount(load);

	function stateChip(study: CampaignSeriesListItemResponse): { cls: string; label: string } {
		if (study.archived) return { cls: 'chip', label: t('Archived') };
		if (study.liveCampaignCount > 0) return { cls: 'chip chip-live', label: t('In field') };
		if (study.submittedResponseCount > 0) return { cls: 'chip', label: t('Collected') };
		return { cls: 'chip chip-stain', label: humanizeToken(study.readinessStatus) };
	}
</script>

<svelte:head><title>Studies — ValidatedScale</title></svelte:head>

<header class="head">
	<div>
		<p class="eyebrow">{t('Portfolio')}</p>
		<h1 class="doc-title">{t('Studies')}</h1>
	</div>
	<div class="tools">
		<input
			type="search"
			placeholder={t('Find a study')}
			aria-label={t('Find a study')}
			bind:value={search}
		/>
		<button class="btn btn-ghost" disabled={sampleBusy} onclick={addExamples}>
			{sampleBusy ? t('Adding…') : t('Add example studies')}
		</button>
		<a class="btn btn-ink" href="/app/studies/new">{t('New study')}</a>
	</div>
</header>

<LoadState
	state={loadState}
	emptyTitle="No studies yet"
	emptyBody="A study pairs one instrument with a cohort and one or more waves. Create your first one, or add three example studies with prefilled waves and responses to explore the product."
>
	{#each buckets as bucket (bucket.key)}
		<section class="bucket">
			<h2 class="eyebrow">
				{bucket.title}
				{#if bucket.note}<span class="note">— {bucket.note}</span>{/if}
			</h2>
			<ul>
				{#each bucket.items as study (study.id)}
					{@const chip = stateChip(study)}
					<li>
						<div class="row-main">
							<a class="name doc-title" href={`/app/studies/${study.id}`}>{study.name}</a>
							<span class="datum meta">
								{formatCount(study.campaignCount)}
								{study.campaignCount === 1 ? t('wave') : t('waves')}
								· {formatCount(study.submittedResponseCount)} {t('responses')}
								· {t('updated')} {formatDate(study.updatedAt)}
							</span>
						</div>
						<span class={chip.cls}>{chip.label}</span>
						{#if study.liveCampaignCount > 0}
							<a class="go" href={`/app/studies/${study.id}/field`}>{t('Field')} →</a>
						{:else if study.submittedResponseCount > 0}
							<a class="go" href={`/app/studies/${study.id}/evidence`}>{t('Evidence')} →</a>
						{:else}
							<a class="go" href={`/app/studies/${study.id}`}>{t('Protocol')} →</a>
						{/if}
					</li>
				{/each}
			</ul>
		</section>
	{/each}
</LoadState>

<style>
	.head {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		gap: 1.5rem;
		margin-bottom: 2rem;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.375rem;
	}

	.tools {
		display: flex;
		gap: 0.625rem;
	}

	.tools input {
		font: inherit;
		font-size: 0.875rem;
		padding: 0.5rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
		width: 15rem;
	}

	.bucket {
		margin-bottom: 2.5rem;
	}

	.note {
		text-transform: none;
		letter-spacing: 0;
		font-weight: 480;
		color: var(--color-ink-3);
	}

	.bucket ul {
		list-style: none;
		margin-top: 0.5rem;
	}

	.bucket li {
		display: flex;
		align-items: center;
		gap: 1.25rem;
		padding: 1rem 0;
		border-bottom: 1px solid var(--color-line);
	}

	.row-main {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
		min-width: 0;
		flex: 1;
	}

	.name {
		font-size: 1.1875rem;
		color: var(--color-ink);
		text-decoration: none;
		width: fit-content;
	}

	.name:hover {
		color: var(--color-stain);
	}

	.meta {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.go {
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-stain);
		text-decoration: none;
		white-space: nowrap;
	}

	@media (max-width: 44rem) {
		.head {
			flex-direction: column;
			align-items: stretch;
		}

		.tools input {
			flex: 1;
			width: auto;
		}

		.bucket li {
			flex-wrap: wrap;
			gap: 0.625rem;
		}
	}
</style>
