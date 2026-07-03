<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import {
		createProductApi,
		type CampaignSeriesHubResponse,
		type CampaignSeriesSetupWorkspaceResponse
	} from '$lib/api/product';
	import { createSetupApi, type LaunchReadinessResponse } from '$lib/api/setup';
	import { api } from '$lib/core/client';
	import { formatCount, formatDate, formatDateTime, humanizeToken } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';
	import WaveRail from '$lib/ui/WaveRail.svelte';

	const product = createProductApi(api());
	const setup = createSetupApi(api());

	const seriesId = $derived(page.params.seriesId!);

	let hub = $state<CampaignSeriesHubResponse | null>(null);
	let workspace = $state<CampaignSeriesSetupWorkspaceResponse | null>(null);
	let readiness = $state<LaunchReadinessResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'ready'>('loading');
	let launching = $state(false);
	let launchError = $state<string | null>(null);

	const chapters = [
		{ n: '01', id: 'design', title: 'Design' },
		{ n: '02', id: 'instrument', title: 'Instrument' },
		{ n: '03', id: 'scoring', title: 'Scoring' },
		{ n: '04', id: 'policies', title: 'Policies' },
		{ n: '05', id: 'waves', title: 'Waves' }
	];

	const launchCandidate = $derived(
		workspace?.selectedCampaign ??
			workspace?.campaigns.find((c) => c.latestLaunchAt === null) ??
			null
	);

	const waveMarks = $derived(
		(hub?.campaigns ?? []).map((wave) => ({
			id: wave.id,
			label: wave.name,
			state:
				wave.status.toLowerCase() === 'live'
					? ('live' as const)
					: wave.latestLaunchAt
						? ('done' as const)
						: ('planned' as const)
		}))
	);

	async function load() {
		try {
			const [hubResponse, setupResponse] = await Promise.all([
				product.getCampaignSeriesHub(seriesId),
				product.getCampaignSeriesSetupWorkspace(seriesId)
			]);
			hub = hubResponse;
			workspace = setupResponse;
			loadState = 'ready';

			const candidate =
				setupResponse.selectedCampaign ??
				setupResponse.campaigns.find((c) => c.latestLaunchAt === null) ??
				null;
			if (candidate) {
				readiness = await setup.getLaunchReadiness(candidate.id).catch(() => null);
			}
		} catch {
			loadState = 'error';
		}
	}

	onMount(load);

	async function launch() {
		if (!launchCandidate || launching) return;
		if (!confirm(`Launch "${launchCandidate.name}"? Instrument and scoring are locked at launch.`)) {
			return;
		}

		launching = true;
		launchError = null;

		try {
			await setup.launchCampaign(launchCandidate.id);
			await load();
		} catch {
			launchError = 'Launch failed. Check the readiness issues and try again.';
		} finally {
			launching = false;
		}
	}

	function policyChip(status: string): string {
		const normalized = status.toLowerCase();
		if (normalized.includes('ready') || normalized.includes('active')) return 'chip chip-live';
		if (normalized.includes('missing')) return 'chip chip-danger';
		return 'chip';
	}
</script>

<svelte:head><title>{hub?.name ?? 'Protocol'} — Spectra</title></svelte:head>

<LoadState state={loadState}>
	{#if hub && workspace}
		<header class="head">
			<p class="eyebrow">
				<a href="/app/studies">Studies</a> / Protocol
			</p>
			<h1 class="doc-title">{hub.name}</h1>
			<p class="datum registered">
				Registered {formatDate(hub.createdAt)} · {formatCount(hub.totals.campaignCount)}
				{hub.totals.campaignCount === 1 ? 'wave' : 'waves'} ·
				{formatCount(hub.totals.submittedResponseCount)} responses
			</p>

			<nav class="phases" aria-label="Study phases">
				<span class="phase current">Protocol</span>
				<a class="phase" href={`/app/studies/${seriesId}/field`}>Field</a>
				<a class="phase" href={`/app/studies/${seriesId}/evidence`}>Evidence</a>
			</nav>
		</header>

		<div class="body">
			<!-- Margin ruler: the manuscript's chapter rail -->
			<aside class="ruler" aria-label="Protocol chapters">
				<div class="ruler-track rail-v" aria-hidden="true"></div>
				<ol>
					{#each chapters as chapter (chapter.id)}
						<li>
							<a href={`#${chapter.id}`}>
								<span class="datum">{chapter.n}</span>
								{chapter.title}
							</a>
						</li>
					{/each}
				</ol>
			</aside>

			<article class="doc">
				<section id="design">
					<h2><span class="datum ch">01</span> Design</h2>
					{#if launchCandidate}
						<dl class="facts">
							<div>
								<dt>Identity mode</dt>
								<dd>{humanizeToken(launchCandidate.responseIdentityMode)}</dd>
							</div>
							<div>
								<dt>Default locale</dt>
								<dd class="datum">{launchCandidate.defaultLocale}</dd>
							</div>
							<div>
								<dt>Current wave</dt>
								<dd>{launchCandidate.name} — {humanizeToken(launchCandidate.status)}</dd>
							</div>
						</dl>
					{:else if hub.campaigns.length > 0}
						<p class="prose">
							All {formatCount(hub.campaigns.length)} waves of this study have been launched.
							Design settings are locked in each wave's snapshot.
						</p>
					{:else}
						<p class="prose">
							No wave is defined yet. The study design becomes binding when the first wave
							launches.
						</p>
					{/if}
				</section>

				<section id="instrument">
					<h2><span class="datum ch">02</span> Instrument</h2>
					{#if workspace.template}
						<dl class="facts">
							<div>
								<dt>Template</dt>
								<dd>
									{workspace.template.templateName}
									<span class="datum quiet">v{workspace.template.semver}</span>
								</dd>
							</div>
							<div>
								<dt>Items</dt>
								<dd class="datum">{formatCount(workspace.template.questionCount)}</dd>
							</div>
							<div>
								<dt>Status</dt>
								<dd>{humanizeToken(workspace.template.status)}</dd>
							</div>
						</dl>
					{:else}
						<p class="prose">No instrument attached yet. Choose one from the library.</p>
					{/if}
				</section>

				<section id="scoring">
					<h2><span class="datum ch">03</span> Scoring</h2>
					{#if workspace.scoring}
						<dl class="facts">
							<div>
								<dt>Rule</dt>
								<dd class="datum">
									{workspace.scoring.ruleKey} v{workspace.scoring.ruleVersion}
								</dd>
							</div>
							<div>
								<dt>Status</dt>
								<dd>{humanizeToken(workspace.scoring.status)}</dd>
							</div>
							<div>
								<dt>Source</dt>
								<dd>{humanizeToken(workspace.scoring.source)}</dd>
							</div>
						</dl>
					{:else}
						<p class="prose">
							No scoring rule bound. Responses will be stored but not scored until one is
							attached.
						</p>
					{/if}
				</section>

				<section id="policies">
					<h2><span class="datum ch">04</span> Policies</h2>
					<ul class="policies">
						<li>
							<span class={policyChip(workspace.policies.consent.status)}>Consent</span>
							<span class="policy-detail">
								{humanizeToken(workspace.policies.consent.status)}
								{#if workspace.policies.consent.version}
									<span class="datum quiet">v{workspace.policies.consent.version}</span>
								{/if}
							</span>
						</li>
						<li>
							<span class={policyChip(workspace.policies.retention.status)}>Retention</span>
							<span class="policy-detail">{humanizeToken(workspace.policies.retention.status)}</span>
						</li>
						<li>
							<span class={policyChip(workspace.policies.disclosure.status)}>Disclosure</span>
							<span class="policy-detail">
								{humanizeToken(workspace.policies.disclosure.status)}
								{#each workspace.policies.disclosure.details ?? [] as detail (detail.label)}
									· {detail.label}: <span class="datum">{detail.value}</span>
								{/each}
							</span>
						</li>
					</ul>
				</section>

				<section id="waves">
					<h2><span class="datum ch">05</span> Waves</h2>
					{#if waveMarks.length > 0}
						<div class="waves-rail">
							<WaveRail marks={waveMarks} />
						</div>
						<ul class="waves">
							{#each hub.campaigns as wave (wave.id)}
								<li>
									<strong>{wave.name}</strong>
									<span class="datum meta">
										{humanizeToken(wave.status)}
										{#if wave.latestLaunchAt}· launched {formatDateTime(wave.latestLaunchAt)}{/if}
										· {formatCount(wave.submittedResponseCount)} responses
									</span>
								</li>
							{/each}
						</ul>
					{:else}
						<p class="prose">No waves defined yet.</p>
					{/if}
				</section>
			</article>

			<aside class="margin">
				<div class="panel launch">
					<h2 class="eyebrow">Launch check</h2>
					{#if readiness}
						{#if readiness.ready}
							<p class="ready">
								<span class="chip chip-live">Ready</span>
								All prerequisites hold.
							</p>
						{:else}
							<ul class="issues">
								{#each readiness.issues as issue (issue.code)}
									<li class:severe={issue.severity.toLowerCase() === 'error'}>
										{issue.message}
									</li>
								{/each}
							</ul>
						{/if}
						{#if launchCandidate && readiness.ready}
							<button class="btn btn-stain launch-btn" disabled={launching} onclick={launch}>
								{launching ? 'Launching…' : `Launch ${launchCandidate.name}`}
							</button>
							<p class="lock-note">Instrument and scoring lock at launch.</p>
						{/if}
						{#if launchError}<p class="error" role="alert">{launchError}</p>{/if}
					{:else if workspace.missingPrerequisites.length > 0}
						<ul class="issues">
							{#each workspace.missingPrerequisites as missing (missing.code)}
								<li class:severe={missing.severity.toLowerCase() === 'error'}>
									{missing.message}
								</li>
							{/each}
						</ul>
					{:else}
						<p class="quiet-note">No wave is awaiting launch.</p>
					{/if}
				</div>

				<div class="panel governance">
					<h2 class="eyebrow">Governance</h2>
					<dl class="gov">
						<div><dt>Consent</dt><dd>{humanizeToken(hub.governance.consentStatus)}</dd></div>
						<div><dt>Retention</dt><dd>{humanizeToken(hub.governance.retentionStatus)}</dd></div>
						<div><dt>Disclosure</dt><dd>{humanizeToken(hub.governance.disclosureStatus)}</dd></div>
						<div><dt>Scoring</dt><dd>{humanizeToken(hub.governance.scoringStatus)}</dd></div>
					</dl>
				</div>
			</aside>
		</div>
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
		font-size: clamp(1.9rem, 4vw, 2.75rem);
		margin-top: 0.5rem;
		max-width: 24ch;
	}

	.registered {
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

	.phase:hover {
		color: var(--color-ink);
	}

	.phase.current {
		color: var(--color-stain);
		border-bottom-color: var(--color-stain);
	}

	.body {
		display: grid;
		grid-template-columns: 8.5rem minmax(0, 1fr) 17rem;
		gap: 2.5rem;
	}

	/* margin ruler */
	.ruler {
		position: relative;
	}

	.ruler-track {
		position: absolute;
		inset: 0.25rem auto 0.25rem 0;
		width: 6px;
		--rail-pitch: 8px;
	}

	.ruler ol {
		list-style: none;
		position: sticky;
		top: 1.5rem;
		display: flex;
		flex-direction: column;
		gap: 1rem;
		padding-left: 1.125rem;
	}

	.ruler a {
		display: flex;
		flex-direction: column;
		font-size: 0.8125rem;
		font-weight: 540;
		color: var(--color-ink-2);
		text-decoration: none;
	}

	.ruler a:hover {
		color: var(--color-stain);
	}

	.ruler .datum {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	/* manuscript */
	.doc section {
		padding-bottom: 2.25rem;
		margin-bottom: 2.25rem;
		border-bottom: 1px solid var(--color-line);
	}

	.doc section:last-child {
		border-bottom: none;
	}

	.doc h2 {
		font-family: var(--font-doc);
		font-size: 1.375rem;
		font-weight: 580;
		display: flex;
		align-items: baseline;
		gap: 0.75rem;
		margin-bottom: 1rem;
	}

	.ch {
		font-size: 0.75rem;
		color: var(--color-stain);
	}

	.prose {
		font-size: 0.9375rem;
		line-height: 1.6;
		color: var(--color-ink-2);
		max-width: 56ch;
	}

	.facts {
		display: flex;
		flex-direction: column;
	}

	.facts div {
		display: grid;
		grid-template-columns: 10rem 1fr;
		gap: 1rem;
		padding: 0.5rem 0;
		border-bottom: 1px dashed var(--color-line);
		font-size: 0.9375rem;
	}

	.facts div:last-child {
		border-bottom: none;
	}

	.facts dt {
		color: var(--color-ink-3);
	}

	.quiet {
		color: var(--color-ink-3);
		font-size: 0.8125rem;
	}

	.policies {
		list-style: none;
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}

	.policies li {
		display: flex;
		align-items: baseline;
		gap: 0.75rem;
	}

	.policy-detail {
		font-size: 0.9375rem;
		color: var(--color-ink-2);
	}

	.waves-rail {
		margin: 0.75rem 0 1.5rem;
	}

	.waves {
		list-style: none;
		display: flex;
		flex-direction: column;
	}

	.waves li {
		display: flex;
		align-items: baseline;
		gap: 0.875rem;
		padding: 0.625rem 0;
		border-bottom: 1px dashed var(--color-line);
		font-size: 0.9375rem;
	}

	.waves li:last-child {
		border-bottom: none;
	}

	.waves .meta {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	/* action margin */
	.margin {
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
	}

	.launch,
	.governance {
		padding: 1.25rem;
	}

	.launch {
		border-top: 3px solid var(--color-stain);
		position: sticky;
		top: 1.5rem;
	}

	.ready {
		display: flex;
		align-items: center;
		gap: 0.625rem;
		margin-top: 0.75rem;
		font-size: 0.875rem;
		color: var(--color-ink-2);
	}

	.issues {
		list-style: none;
		margin-top: 0.75rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		font-size: 0.8125rem;
		color: var(--color-ink-2);
	}

	.issues li {
		padding-left: 0.875rem;
		border-left: 2px solid var(--color-warn);
	}

	.issues li.severe {
		border-left-color: var(--color-danger);
	}

	.launch-btn {
		margin-top: 1rem;
		width: 100%;
		justify-content: center;
	}

	.lock-note {
		margin-top: 0.5rem;
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.quiet-note {
		margin-top: 0.75rem;
		font-size: 0.875rem;
		color: var(--color-ink-3);
	}

	.error {
		margin-top: 0.5rem;
		font-size: 0.8125rem;
		color: var(--color-danger);
	}

	.gov {
		margin-top: 0.5rem;
	}

	.gov div {
		display: flex;
		justify-content: space-between;
		padding: 0.4375rem 0;
		font-size: 0.8125rem;
		border-bottom: 1px dashed var(--color-line);
	}

	.gov div:last-child {
		border-bottom: none;
	}

	.gov dt {
		color: var(--color-ink-3);
	}

	@media (max-width: 62rem) {
		.body {
			grid-template-columns: 1fr;
		}

		.ruler {
			display: none;
		}

		.launch {
			position: static;
		}
	}
</style>
