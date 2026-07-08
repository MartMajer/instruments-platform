<script lang="ts">
	import { onMount } from 'svelte';
	import { createProductApi, type WorkspaceOverviewResponse } from '$lib/api/product';
	import { api } from '$lib/core/client';
	import { localeState, t } from '$lib/core/locale.svelte';
	import { commandCopy } from '$lib/core/backend-copy';
	import { formatCount, formatDateTime, humanizeToken } from '$lib/core/format';
	import { mapPlatformRoute } from '$lib/core/routes';
	import LoadState from '$lib/ui/LoadState.svelte';

	const product = createProductApi(api());

	let overview = $state<WorkspaceOverviewResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'ready'>('loading');
	let sampleBusy = $state(false);

	async function addExamples() {
		if (sampleBusy) return;
		sampleBusy = true;
		try {
			await product.ensureSampleStudies(localeState.current === 'hr' ? 'hr-HR' : 'en');
			overview = await product.getWorkspaceOverview();
		} finally {
			sampleBusy = false;
		}
	}

	const today = $derived(
		new Intl.DateTimeFormat(localeState.current === 'hr' ? 'hr-HR' : 'en-GB', {
			weekday: 'long',
			day: 'numeric',
			month: 'long'
		}).format(new Date())
	);

	const attention = $derived(
		(overview?.commandCenter.items ?? []).slice().sort((a, b) => a.priority - b.priority)
	);

	/** hr number agreement: 1 val je · 2–4 vala su · 5+ valova je. */
	function waveHeadline(count: number): string {
		if (localeState.current !== 'hr') {
			return count === 1 ? 'wave is in the field.' : 'waves are in the field.';
		}
		const mod10 = count % 10;
		const mod100 = count % 100;
		if (mod10 === 1 && mod100 !== 11) return 'krug prikuplja odgovore.';
		if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 'kruga prikupljaju odgovore.';
		return 'krugova prikuplja odgovore.';
	}

	const liveStudies = $derived(
		(overview?.studyCollections.ownStudies ?? []).filter((s) => s.liveCampaignCount > 0)
	);

	onMount(async () => {
		try {
			overview = await product.getWorkspaceOverview();
			loadState = 'ready';
		} catch {
			loadState = 'error';
		}
	});
</script>

<svelte:head><title>Today — ValidatedScale</title></svelte:head>

<header class="head">
	<p class="eyebrow">{today}</p>
	<h1 class="doc-title">
		{#if loadState === 'ready' && overview}
			{#if overview.totals.liveCampaignCount > 0}
				{formatCount(overview.totals.liveCampaignCount)}
				{waveHeadline(overview.totals.liveCampaignCount)}
			{:else if overview.totals.campaignSeriesCount > 0}
				{t('Nothing is collecting today.')}
			{:else}
				{t('Your workspace is ready.')}
			{/if}
		{:else}
			{t('Today')}
		{/if}
	</h1>
</header>

<LoadState state={loadState}>
	{#if overview}
		<div class="grid">
			<section class="main">
				{#if overview.totals.campaignSeriesCount === 0}
					<div class="first-run panel">
						<h2 class="eyebrow">{t('Start here')}</h2>
						<p>
							{t('Register your first study, or add three example studies with waves and responses to see the product working.')}
						</p>
						<div class="first-run-actions">
							<a class="btn btn-ink" href="/app/studies/new">{t('Register a study')}</a>
							<button class="btn btn-ghost" disabled={sampleBusy} onclick={addExamples}>
								{sampleBusy ? t('Adding…') : t('Add example studies')}
							</button>
						</div>
					</div>
				{/if}

				<h2 class="eyebrow">{t('Needs attention')}</h2>
				{#if attention.length === 0}
					<p class="quiet">{t('Nothing needs your attention. The field runs itself today.')}</p>
				{:else}
					<ol class="attention">
						{#each attention as item (item.id)}
							{@const copy = commandCopy(item)}
							<li>
								<div>
									<strong>{copy.title}</strong>
									<p>{copy.description}</p>
								</div>
								<a class="btn btn-ghost" href={mapPlatformRoute(item.route)}>{copy.action}</a>
							</li>
						{/each}
					</ol>
				{/if}

				{#if liveStudies.length > 0}
					<h2 class="eyebrow live-h">{t('In the field now')}</h2>
					<ul class="live">
						{#each liveStudies as study (study.id)}
							<li>
								<span class="pip" aria-hidden="true"></span>
								<a class="live-name doc-title" href={`/app/studies/${study.id}`}>{study.name}</a>
								<span class="datum meta">
									{formatCount(study.submittedResponseCount)} {t('responses')} · {t('last')}
									{formatDateTime(study.latestSubmissionAt)}
								</span>
								<a class="field-link" href={`/app/studies/${study.id}/field`}>{t('Field')} →</a>
							</li>
						{/each}
					</ul>
				{/if}
			</section>

			<aside class="side">
				<h2 class="eyebrow">{t('Workspace')}</h2>
				<dl class="counters">
					<div>
						<dt>{t('Studies')}</dt>
						<dd class="datum">{formatCount(overview.totals.campaignSeriesCount)}</dd>
					</div>
					<div>
						<dt>{t('Waves')}</dt>
						<dd class="datum">{formatCount(overview.totals.campaignCount)}</dd>
					</div>
					<div>
						<dt>{t('In field')}</dt>
						<dd class="datum" class:stain={overview.totals.liveCampaignCount > 0}>
							{formatCount(overview.totals.liveCampaignCount)}
						</dd>
					</div>
					<div>
						<dt>{t('Responses')}</dt>
						<dd class="datum">{formatCount(overview.totals.submittedResponseCount)}</dd>
					</div>
					<div>
						<dt>{t('Exports')}</dt>
						<dd class="datum">{formatCount(overview.totals.exportArtifactCount)}</dd>
					</div>
				</dl>

				<h2 class="eyebrow recent-h">{t('Recent studies')}</h2>
				<ul class="recent">
					{#each overview.recentSeries.slice(0, 5) as series (series.id)}
						<li>
							<a href={`/app/studies/${series.id}`}>{series.name}</a>
							<span class="datum">{t(humanizeToken(series.readinessStatus))}</span>
						</li>
					{:else}
						<li class="quiet">{t('No studies yet. Start one from Studies.')}</li>
					{/each}
				</ul>
			</aside>
		</div>
	{/if}
</LoadState>

<style>
	.head {
		margin-bottom: 2rem;
	}

	.head h1 {
		font-size: clamp(1.75rem, 3.5vw, 2.5rem);
		margin-top: 0.375rem;
	}

	.grid {
		display: grid;
		grid-template-columns: 1fr 17rem;
		gap: 3rem;
	}

	.quiet {
		font-size: 0.9375rem;
		color: var(--color-ink-3);
		margin-top: 0.75rem;
	}

	.first-run {
		margin-bottom: 2.5rem;
		padding: 1.5rem;
		border-top: 3px solid var(--color-stain);
	}

	.first-run p {
		margin-top: 0.5rem;
		font-size: 0.9375rem;
		color: var(--color-ink-2);
		max-width: 52ch;
	}

	.first-run-actions {
		margin-top: 1rem;
		display: flex;
		gap: 0.75rem;
	}

	.first-run-actions a {
		text-decoration: none;
	}

	.attention {
		margin-top: 0.75rem;
		list-style: none;
		display: flex;
		flex-direction: column;
	}

	.attention li {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1.5rem;
		padding: 1rem 0;
		border-bottom: 1px solid var(--color-line);
	}

	.attention strong {
		font-weight: 580;
	}

	.attention p {
		margin-top: 0.25rem;
		font-size: 0.875rem;
		color: var(--color-ink-2);
		max-width: 52ch;
	}

	.live-h {
		margin-top: 2.5rem;
	}

	.live {
		list-style: none;
		margin-top: 0.75rem;
	}

	.live li {
		display: flex;
		align-items: baseline;
		gap: 0.75rem;
		padding: 0.875rem 0;
		border-bottom: 1px solid var(--color-line);
	}

	.live .pip {
		width: 8px;
		height: 8px;
		border-radius: 999px;
		background: var(--color-live);
		align-self: center;
		flex-shrink: 0;
	}

	.live-name {
		font-size: 1.125rem;
		text-decoration: none;
		color: var(--color-ink);
	}

	.live-name:hover {
		color: var(--color-stain);
	}

	.live .meta {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.field-link {
		margin-left: auto;
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-stain);
		text-decoration: none;
		white-space: nowrap;
	}

	.counters {
		margin-top: 0.75rem;
		border-top: 1px solid var(--color-line);
	}

	.counters div {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		padding: 0.625rem 0;
		border-bottom: 1px solid var(--color-line);
	}

	.counters dt {
		font-size: 0.875rem;
		color: var(--color-ink-2);
	}

	.counters dd {
		font-size: 1rem;
	}

	.counters dd.stain {
		color: var(--color-stain);
		font-weight: 600;
	}

	.recent-h {
		margin-top: 2.5rem;
	}

	.recent {
		list-style: none;
		margin-top: 0.75rem;
	}

	.recent li {
		display: flex;
		justify-content: space-between;
		gap: 1rem;
		padding: 0.5rem 0;
		font-size: 0.875rem;
	}

	.recent a {
		color: var(--color-ink);
		text-decoration: none;
	}

	.recent a:hover {
		color: var(--color-stain);
	}

	.recent .datum {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	@media (max-width: 54rem) {
		.grid {
			grid-template-columns: minmax(0, 1fr);
		}
	}
</style>
