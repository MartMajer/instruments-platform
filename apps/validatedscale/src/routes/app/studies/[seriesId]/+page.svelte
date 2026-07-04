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
	import { t, waveWord } from '$lib/core/locale.svelte';
	import { formatCount, formatDate, formatDateTime, humanizeToken } from '$lib/core/format';
	import Composer from '$lib/protocol/Composer.svelte';
	import { confirmDialog, promptDialog } from '$lib/ui/dialog.svelte';
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

	let waveName = $state('');
	let waveIdentityMode = $state('anonymous');
	let waveLocale = $state('en');
	let waveBusy = $state(false);
	let waveError = $state<string | null>(null);

	async function addWave(event: SubmitEvent) {
		event.preventDefault();
		if (!workspace?.template || waveBusy) return;
		waveBusy = true;
		waveError = null;

		try {
			await setup.createCampaign({
				templateVersionId: workspace.template.templateVersionId,
				name: waveName.trim() || `Wave ${(hub?.campaigns.length ?? 0) + 1}`,
				responseIdentityMode: waveIdentityMode,
				campaignSeriesId: seriesId,
				schedule: '{}',
				defaultLocale: waveLocale
			});
			waveName = '';
			await load();
		} catch {
			waveError = 'The wave could not be created. Try again.';
		} finally {
			waveBusy = false;
		}
	}

	const chapters = [
		{ n: '01', id: 'design', title: t('Design') },
		{ n: '02', id: 'instrument', title: t('Instrument') },
		{ n: '03', id: 'scoring', title: t('Scoring') },
		{ n: '04', id: 'policies', title: t('Policies') },
		{ n: '05', id: 'waves', title: t('Waves') }
	];

	const launchCandidate = $derived(
		workspace?.campaigns.find((c) => c.latestLaunchAt === null) ??
			workspace?.selectedCampaign ??
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
				setupResponse.campaigns.find((c) => c.latestLaunchAt === null) ??
				setupResponse.selectedCampaign ??
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
		const proceed = await confirmDialog({
			title: `${t('Launch')} ${launchCandidate.name}?`,
			body: t('Instrument, scoring and policies lock at launch. Collection starts immediately.'),
			confirmLabel: t('Launch')
		});
		if (!proceed) return;

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

	let reviseOpen = $state(false);
	let itemsOpen = $state(false);
	let templateItems = $state<{ code: string; text: string }[] | null>(null);

	async function toggleItems() {
		itemsOpen = !itemsOpen;
		if (itemsOpen && !templateItems && workspace?.template) {
			const detail = await setup
				.getTemplateVersion(workspace.template.templateVersionId)
				.catch(() => null);
			templateItems = (detail?.questions ?? [])
				.slice()
				.sort((a, b) => a.ordinal - b.ordinal)
				.map((q) => ({ code: q.code, text: q.textDefault }));
		}
	}

	let consentOpen = $state(false);
	let consentLocale = $state('en');
	let consentVersion = $state('');
	let consentTitle = $state('');
	let consentBody = $state('');
	let consentBusy = $state(false);
	let consentNote = $state<string | null>(null);

	async function publishConsent(event: SubmitEvent) {
		event.preventDefault();
		if (consentBusy) return;
		consentBusy = true;
		consentNote = null;

		try {
			const published = await setup.publishCampaignSeriesConsentDocument(seriesId, {
				locale: consentLocale,
				version: consentVersion.trim(),
				title: consentTitle.trim(),
				bodyMarkdown: consentBody.trim()
			});
			consentNote = `Published v${published.version} (${published.locale}); ${published.retiredCount} previous version${published.retiredCount === 1 ? '' : 's'} retired. Applies to waves launched from now on.`;
			consentOpen = false;
			consentVersion = '';
			consentTitle = '';
			consentBody = '';
			await load();
		} catch {
			consentNote =
				'Publishing failed. The version must be new for this study and language, and sample studies are read-only.';
		} finally {
			consentBusy = false;
		}
	}

	let groups = $state<{ id: string; name: string; memberCount: number }[]>([]);
	let recipientsFor = $state<string | null>(null);
	let recipientKind = $state<'self' | 'all_in_group'>('self');
	let recipientGroupId = $state('');
	let recipientBusy = $state(false);
	let recipientNote = $state<string | null>(null);

	async function toggleRecipients(campaignId: string) {
		recipientsFor = recipientsFor === campaignId ? null : campaignId;
		recipientNote = null;
		if (recipientsFor && groups.length === 0) {
			const list = await product.listSubjectGroups().catch(() => null);
			groups = (list?.groups ?? []).map((g) => ({ id: g.id, name: g.name, memberCount: g.memberCount }));
		}
	}

	async function saveRecipients(campaignId: string) {
		if (recipientBusy) return;
		recipientBusy = true;
		recipientNote = null;
		try {
			const rule =
				recipientKind === 'all_in_group'
					? { kind: 'all_in_group', role: 'group_member', group_id: recipientGroupId }
					: { kind: 'self', role: 'self' };
			const saved = await setup.updateCampaignRespondentRules(campaignId, {
				rules: [{ rule: JSON.stringify(rule) }]
			});
			const pairs = saved.rules?.[0]?.assignmentPairCount ?? null;
			recipientNote = pairs != null ? `Saved — ${pairs} recipients resolved from the directory.` : 'Recipients saved.';
			await load();
		} catch {
			recipientNote = 'Recipients could not be saved. Anonymous open-link waves do not need them.';
		} finally {
			recipientBusy = false;
		}
	}

	let lifecycleBusy = $state(false);
	let lifecycleError = $state<string | null>(null);

	async function duplicateStudy() {
		if (!hub || lifecycleBusy) return;
		const name = await promptDialog({
			title: t('Duplicate as my study'),
			body: t('Copies this example into an editable study of your own — protocol included, responses not.'),
			confirmLabel: t('Duplicate'),
			initialValue: `${hub.name} (copy)`
		});
		if (!name) return;
		lifecycleBusy = true;
		lifecycleError = null;
		try {
			const copy = await product.duplicateCampaignSeries(seriesId, { name: name.trim() });
			location.assign(`/app/studies/${copy.id}`);
		} catch {
			lifecycleError = 'Duplication failed. Try again.';
			lifecycleBusy = false;
		}
	}

	async function toggleArchive() {
		if (!hub || lifecycleBusy) return;
		if (!hub.archived) {
			const proceed = await confirmDialog({
				title: `${t('Archive')} ${hub.name}?`,
				body: t('The study leaves the portfolio but stays readable and can be restored at any time.'),
				confirmLabel: t('Archive'),
				danger: true
			});
			if (!proceed) return;
		}
		lifecycleBusy = true;
		lifecycleError = null;
		try {
			if (hub.archived) await product.restoreCampaignSeries(seriesId);
			else await product.archiveCampaignSeries(seriesId);
			await load();
		} catch {
			lifecycleError = 'The archive state could not be changed.';
		} finally {
			lifecycleBusy = false;
		}
	}

	/** Backend prerequisites speak backend ("campaign"); translate + point at the right chapter. */
	function prerequisiteHint(code: string, message: string): { text: string; anchor: string | null } {
		const lowered = `${code} ${message}`.toLowerCase();
		if (lowered.includes('campaign')) {
			return { text: t('This study has no wave yet. Add one in chapter 05 — Waves.'), anchor: '#waves' };
		}
		if (lowered.includes('template') || lowered.includes('instrument') || lowered.includes('question')) {
			return { text: t('No instrument is attached yet. Compose or pick one in chapter 02 — Instrument.'), anchor: '#instrument' };
		}
		if (lowered.includes('scoring')) {
			return { text: t('No scoring rule is bound. It is created with the questionnaire in chapter 02.'), anchor: '#instrument' };
		}
		if (lowered.includes('consent') || lowered.includes('retention') || lowered.includes('disclosure')) {
			return { text: message, anchor: '#policies' };
		}
		return { text: message, anchor: null };
	}

	function policyDetail(details: { label: string; value: string }[] | null | undefined, label: string): string | null {
		return details?.find((d) => d.label.toLowerCase() === label.toLowerCase())?.value ?? null;
	}

	function policyChip(status: string): string {
		const normalized = status.toLowerCase();
		if (normalized.includes('ready') || normalized.includes('active')) return 'chip chip-live';
		if (normalized.includes('missing')) return 'chip chip-danger';
		return 'chip';
	}
</script>

<svelte:head><title>{hub?.name ?? 'Protocol'} — ValidatedScale</title></svelte:head>

<LoadState state={loadState}>
	{#if hub && workspace}
		<header class="head">
			<p class="eyebrow">
				<a href="/app/studies">Studies</a> / Protocol
			</p>
			<h1 class="doc-title">{hub.name}</h1>
			<p class="datum registered">
				{t('Registered')} {formatDate(hub.createdAt)} · {formatCount(hub.totals.campaignCount)}
				{waveWord(hub.totals.campaignCount)} ·
				{formatCount(hub.totals.submittedResponseCount)} {t('responses')}
			</p>

			<nav class="phases" aria-label="Study phases">
				<span class="phase current">{t('Protocol')}</span>
				<a class="phase" href={`/app/studies/${seriesId}/field`}>{t('Field')}</a>
				<a class="phase" href={`/app/studies/${seriesId}/evidence`}>{t('Evidence')}</a>
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
					<h2><span class="datum ch">01</span> {t('Design')}</h2>
					{#if launchCandidate}
						<dl class="facts">
							<div>
								<dt>{t('Identity mode')}</dt>
								<dd>{t(humanizeToken(launchCandidate.responseIdentityMode))}</dd>
							</div>
							<div>
								<dt>{t('Default locale')}</dt>
								<dd class="datum">{launchCandidate.defaultLocale}</dd>
							</div>
							<div>
								<dt>{t('Current wave')}</dt>
								<dd>{launchCandidate.name} — {t(humanizeToken(launchCandidate.status))}</dd>
							</div>
						</dl>
					{:else if hub.campaigns.length > 0}
						<p class="prose">
							All {formatCount(hub.campaigns.length)} waves of this study have been launched.
							Design settings are locked in each wave's snapshot.
						</p>
					{:else}
						<p class="prose">
							{t('No wave is defined yet. The study design becomes binding when the first wave launches.')}
						</p>
					{/if}
				</section>

				<section id="participation">
					<h2><span class="datum ch">·</span> {t('Who takes part')}</h2>
					<p class="prose">
						{t('People reach this study through Field once a wave is live: share an open link (anonymous or with a participant code), or queue email invitations. For invite lists, keep your cohort in')} <a href="/app/people">{t('People')}</a>.
					</p>
					{#if hub.campaigns.some((c) => c.status.toLowerCase() === 'live')}
						<p class="prose">
							<a href={`/app/studies/${seriesId}/field`}>Open Field →</a>
						</p>
					{/if}
				</section>

				<section id="instrument">
					<h2><span class="datum ch">02</span> {t('Instrument')}</h2>
					{#if workspace.template}
						<dl class="facts">
							<div>
								<dt>{t('Template')}</dt>
								<dd>
									{workspace.template.templateName}
									<span class="datum quiet">v{workspace.template.semver}</span>
								</dd>
							</div>
							<div>
								<dt>{t('Items')}</dt>
								<dd class="datum">{formatCount(workspace.template.questionCount)}</dd>
							</div>
							<div>
								<dt>{t('Status')}</dt>
								<dd>{t(humanizeToken(workspace.template.status))}</dd>
							</div>
						</dl>
						<div class="instrument-actions">
							<button class="quiet-action" onclick={toggleItems}>
								{itemsOpen ? t('Hide items') : t('View the items respondents will see')}
							</button>
							<button class="quiet-action" onclick={() => (reviseOpen = !reviseOpen)}>
								{reviseOpen ? t('Close editor') : t('Revise questionnaire')}
							</button>
						</div>
						{#if reviseOpen && workspace.template}
							<div class="compose-wrap">
								<Composer
									{seriesId}
									seriesName={hub.name}
									revise={{ templateVersionId: workspace.template.templateVersionId, semver: workspace.template.semver }}
									onDone={() => { reviseOpen = false; templateItems = null; itemsOpen = false; void load(); }}
								/>
							</div>
						{/if}
						{#if itemsOpen}
							{#if templateItems}
								<ol class="item-preview">
									{#each templateItems as item (item.code)}
										<li><span class="datum item-preview-code">{item.code}</span>{item.text}</li>
									{/each}
								</ol>
							{:else}
								<p class="prose">Loading items…</p>
							{/if}
						{/if}
					{:else}
						<p class="prose">
							{t('No instrument attached yet. Compose the questionnaire here — or import a validated instrument from the')} <a href="/app/instruments">{t('library')}</a>.
						</p>
						<div class="compose-wrap">
							<Composer {seriesId} seriesName={hub.name} onDone={load} />
						</div>
					{/if}
				</section>

				<section id="scoring">
					<h2><span class="datum ch">03</span> {t('Scoring')}</h2>
					{#if workspace.scoring}
						<dl class="facts">
							<div>
								<dt>{t('Rule')}</dt>
								<dd class="datum">
									{workspace.scoring.ruleKey} v{workspace.scoring.ruleVersion}
								</dd>
							</div>
							<div>
								<dt>{t('Status')}</dt>
								<dd>{t(humanizeToken(workspace.scoring.status))}</dd>
							</div>
							<div>
								<dt>{t('Source')}</dt>
								<dd>{t(humanizeToken(workspace.scoring.source))}</dd>
							</div>
						</dl>
					{:else}
						<p class="prose">
							{t('No scoring rule bound. Responses will be stored but not scored until one is attached.')}
						</p>
					{/if}
				</section>

				<section id="policies">
					<h2><span class="datum ch">04</span> {t('Consent & data guarantees')}</h2>

					<div class="consent-card">
						<div class="policy-head">
							<span class={policyChip(workspace.policies.consent.status)}>Consent</span>
							{#if workspace.policies.consent.version}
								<span class="datum quiet">v{workspace.policies.consent.version}</span>
							{/if}
							{#if policyDetail(workspace.policies.consent.details, 'Title')}
								<span class="consent-current">{policyDetail(workspace.policies.consent.details, 'Title')}</span>
							{/if}
						</div>
						<p class="policy-explain">
							{t('What respondents agree to before answering — your words, versioned. Publish a new version below; earlier waves keep the version they launched with.')}
						</p>
						<div class="policy-actions">
							<button class="quiet-action" onclick={() => (consentOpen = !consentOpen)}>
								{consentOpen ? t('Close consent editor') : t('Publish new consent version')}
							</button>
							{#if consentNote}<p class="consent-note" role="status">{consentNote}</p>{/if}
						</div>
						{#if consentOpen}
							<form class="consent-editor" onsubmit={publishConsent}>
								<div class="consent-row">
									<div class="field">
										<label class="eyebrow" for="c-locale">{t('Language')}</label>
										<select id="c-locale" bind:value={consentLocale}>
											<option value="en">English</option>
											<option value="hr-HR">Hrvatski</option>
										</select>
									</div>
									<div class="field">
										<label class="eyebrow" for="c-version">{t('Version')}</label>
										<input id="c-version" class="datum" required bind:value={consentVersion} placeholder="2.0.0" />
									</div>
									<div class="field grow">
										<label class="eyebrow" for="c-title">{t('Title')}</label>
										<input id="c-title" required bind:value={consentTitle} placeholder="Informed consent — nurse workload study" />
									</div>
								</div>
								<div class="field">
									<label class="eyebrow" for="c-body">{t('Consent text (shown to every respondent before items)')}</label>
									<textarea id="c-body" rows="7" required bind:value={consentBody} placeholder={'What the study measures, who runs it, that answers are anonymous, the k-threshold for reporting, the right to stop at any time, your ethics reference…'}></textarea>
								</div>
								<button class="btn btn-stain consent-submit" type="submit" disabled={consentBusy}>
									{consentBusy ? t('Publishing…') : t('Publish — retires the current version')}
								</button>
								<p class="consent-hint">
									Launched waves keep the consent version they launched with; this version binds
									to waves launched after publishing. Grants carry over unchanged.
								</p>
							</form>
						{/if}
					</div>

					<p class="guarantees">
						<span class="eyebrow guarantees-label">{t('Platform guarantees')}</span>
						{t('Responses are anonymized')}
						<span class="datum">{policyDetail(workspace.policies.retention.details, 'Retain for') ?? '2 years'}</span>
						{t('after each wave closes, and nothing is ever reported for groups smaller than')}
						<span class="datum">{policyDetail(workspace.policies.disclosure.details, 'Minimum group size') ?? '5'}</span>
						{t('people. These hold for every study and bind at launch.')}
					</p>
				</section>

				<section id="waves">
					<h2><span class="datum ch">05</span> {t('Waves')}</h2>
					{#if waveMarks.length > 0}
						<div class="waves-rail">
							<WaveRail marks={waveMarks} />
						</div>
						<ul class="waves">
							{#each hub.campaigns as wave (wave.id)}
								<li>
									<strong>{wave.name}</strong>
									<span class="datum meta">
										{t(humanizeToken(wave.status))}
										{#if wave.latestLaunchAt}· launched {formatDateTime(wave.latestLaunchAt)}{/if}
										· {formatCount(wave.submittedResponseCount)} responses
									</span>
									{#if !wave.latestLaunchAt && wave.responseIdentityMode.toLowerCase() === 'identified'}
										<button class="recipients-btn" onclick={() => toggleRecipients(wave.id)}>
											{t('Recipients')}
										</button>
									{/if}
								</li>
								{#if recipientsFor === wave.id}
									<li class="recipients-row">
										<div class="recipients-form">
											<span class="eyebrow">{t('Who answers this wave')}</span>
											<div class="recipients-controls">
												<select bind:value={recipientKind} aria-label="Recipient rule">
													<option value="self">{t('Everyone in the directory (about themselves)')}</option>
													<option value="all_in_group">{t('Members of a group (about themselves)')}</option>
												</select>
												{#if recipientKind === 'all_in_group'}
													<select bind:value={recipientGroupId} aria-label="Group">
														<option value="" disabled>{t('Choose a group')}</option>
														{#each groups as group (group.id)}
															<option value={group.id}>{group.name} ({group.memberCount})</option>
														{/each}
													</select>
												{/if}
												<button
													class="btn btn-ghost"
													disabled={recipientBusy || (recipientKind === 'all_in_group' && !recipientGroupId)}
													onclick={() => saveRecipients(wave.id)}
												>
													{recipientBusy ? t('Saving…') : t('Save recipients')}
												</button>
											</div>
											{#if recipientNote}<p class="recipients-note" role="status">{recipientNote}</p>{/if}
											<p class="recipients-note">
												{t('Recipients come from')} <a href="/app/people">{t('People')}</a>. {t('Anonymous waves skip this — they collect through open links or email invitations in Field.')}
											</p>
										</div>
									</li>
								{/if}
							{/each}
						</ul>
					{:else if !workspace.template}
						<p class="prose">{t('Waves become available once an instrument is attached.')}</p>
					{/if}

					{#if workspace.template}
						<form class="add-wave" onsubmit={addWave}>
							<input
								bind:value={waveName}
								placeholder={`Wave ${(hub.campaigns.length ?? 0) + 1} — e.g. Baseline`}
								aria-label="New wave name"
							/>
							<select bind:value={waveIdentityMode} aria-label="Identity mode">
								<option value="anonymous">Anonymous</option>
								<option value="anonymous_longitudinal">Anonymous longitudinal</option>
								<option value="identified">Identified</option>
							</select>
							<select bind:value={waveLocale} aria-label="Wave language">
								<option value="en">English</option>
								<option value="hr-HR">Hrvatski</option>
							</select>
							<button class="btn btn-ghost" type="submit" disabled={waveBusy}>
								{waveBusy ? t('Adding…') : t('Add wave')}
							</button>
						</form>
						{#if waveError}<p class="error" role="alert">{waveError}</p>{/if}
					{/if}
				</section>
			</article>

			<aside class="margin">
				<div class="panel launch">
					<h2 class="eyebrow">{t('Launch check')}</h2>
					{#if readiness}
						{#if readiness.ready}
							<p class="ready">
								<span class="chip chip-live">{t('Ready')}</span>
								{t('All prerequisites hold.')}
							</p>
						{:else}
							<ul class="issues">
								{#each readiness.issues as issue (issue.code)}
									{@const hint = prerequisiteHint(issue.code, issue.message)}
									<li class:severe={issue.severity.toLowerCase() === 'error'}>
										{#if hint.anchor}<a href={hint.anchor}>{hint.text}</a>{:else}{hint.text}{/if}
									</li>
								{/each}
							</ul>
						{/if}
						{#if launchCandidate && readiness.ready}
							<button class="btn btn-stain launch-btn" disabled={launching} onclick={launch}>
								{launching ? t('Launching…') : `${t('Launch')} ${launchCandidate.name}`}
							</button>
							<p class="lock-note">{t('Instrument and scoring lock at launch.')}</p>
						{/if}
						{#if launchError}<p class="error" role="alert">{launchError}</p>{/if}
					{:else if workspace.missingPrerequisites.length > 0}
						<ul class="issues">
							{#each workspace.missingPrerequisites as missing (missing.code)}
								{@const hint = prerequisiteHint(missing.code, missing.message)}
								<li class:severe={missing.severity.toLowerCase() === 'error'}>
									{#if hint.anchor}<a href={hint.anchor}>{hint.text}</a>{:else}{hint.text}{/if}
								</li>
							{/each}
						</ul>
					{:else}
						<p class="quiet-note">{t('No wave is awaiting launch.')}</p>
					{/if}
				</div>

				<div class="panel governance">
					<h2 class="eyebrow">{t('Study')}</h2>
					<div class="lifecycle-actions">
						{#if hub.isSample}
							<button class="quiet-action" disabled={lifecycleBusy} onclick={duplicateStudy}>
								{t('Duplicate as my study')}
							</button>
						{/if}
						<button class="quiet-action" disabled={lifecycleBusy} onclick={toggleArchive}>
							{hub.archived ? t('Restore from archive') : t('Archive study')}
						</button>
					</div>
					{#if lifecycleError}<p class="error" role="alert">{lifecycleError}</p>{/if}
					<h2 class="eyebrow governance-h">{t('Governance')}</h2>
					<dl class="gov">
						<div><dt>{t('Consent')}</dt><dd>{t(humanizeToken(hub.governance.consentStatus))}</dd></div>
						<div><dt>{t('Retention')}</dt><dd>{t(humanizeToken(hub.governance.retentionStatus))}</dd></div>
						<div><dt>{t('Disclosure')}</dt><dd>{t(humanizeToken(hub.governance.disclosureStatus))}</dd></div>
						<div><dt>{t('Scoring')}</dt><dd>{t(humanizeToken(hub.governance.scoringStatus))}</dd></div>
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

	.policy-actions {
		margin-top: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		align-items: flex-start;
	}

	.consent-note {
		font-size: 0.8125rem;
		color: var(--color-ink-2);
	}

	.consent-editor {
		margin-top: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.875rem;
		padding: 1.25rem;
		border: 1px solid var(--color-stain-line);
		background: var(--color-stain-wash);
		border-radius: var(--radius-instrument);
	}

	.consent-row {
		display: flex;
		gap: 0.875rem;
		flex-wrap: wrap;
	}

	.consent-editor .field {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
	}

	.consent-editor .field.grow {
		flex: 1;
		min-width: 14rem;
	}

	.consent-editor input,
	.consent-editor select,
	.consent-editor textarea {
		font: inherit;
		font-size: 0.9375rem;
		padding: 0.5rem 0.625rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	.consent-editor textarea {
		font-family: var(--font-doc);
		line-height: 1.6;
		resize: vertical;
	}

	.consent-submit {
		align-self: flex-start;
	}

	.consent-hint {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.consent-card {
		border: 1px solid var(--color-line);
		border-radius: var(--radius-instrument);
		padding: 1.125rem 1.25rem;
		background: var(--color-surface);
		margin-top: 1rem;
	}

	.consent-current {
		font-size: 0.8125rem;
		color: var(--color-ink-2);
	}

	.guarantees {
		margin-top: 1.25rem;
		font-size: 0.875rem;
		line-height: 1.7;
		color: var(--color-ink-2);
		max-width: 56ch;
	}

	.guarantees-label {
		display: block;
		margin-bottom: 0.25rem;
	}

	.policy-head {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		margin-bottom: 0.625rem;
	}

	.policy-explain {
		font-size: 0.8125rem;
		line-height: 1.55;
		color: var(--color-ink-2);
		margin-bottom: 0.75rem;
	}


	.waves-rail {
		margin: 0.75rem 0 1.5rem;
	}

	.compose-wrap {
		margin-top: 1.25rem;
	}

	.instrument-actions {
		margin-top: 1rem;
		display: flex;
		gap: 0.625rem;
		flex-wrap: wrap;
	}

	.item-preview {
		margin: 0.875rem 0 0 0;
		list-style: none;
		display: flex;
		flex-direction: column;
	}

	.item-preview li {
		display: flex;
		gap: 0.75rem;
		padding: 0.5rem 0;
		border-bottom: 1px dashed var(--color-line);
		font-size: 0.9375rem;
		line-height: 1.5;
	}

	.item-preview li:last-child {
		border-bottom: none;
	}

	.item-preview-code {
		font-size: 0.6875rem;
		color: var(--color-stain);
		flex-shrink: 0;
		width: 4.5rem;
	}

	.prose a {
		color: var(--color-stain);
	}

	.add-wave {
		display: flex;
		gap: 0.625rem;
		margin-top: 1.25rem;
		flex-wrap: wrap;
	}

	.add-wave input,
	.add-wave select {
		font: inherit;
		font-size: 0.875rem;
		padding: 0.5rem 0.625rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	.add-wave input {
		flex: 1;
		min-width: 12rem;
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

	.recipients-btn {
		margin-left: auto;
		background: none;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		font: inherit;
		font-size: 0.75rem;
		font-weight: 560;
		color: var(--color-stain);
		padding: 0.3125rem 0.625rem;
		cursor: pointer;
	}

	.recipients-row {
		background: var(--color-stain-wash);
		border-radius: var(--radius-instrument);
		padding: 0.875rem !important;
	}

	.recipients-form {
		display: flex;
		flex-direction: column;
		gap: 0.625rem;
		width: 100%;
	}

	.recipients-controls {
		display: flex;
		gap: 0.5rem;
		flex-wrap: wrap;
	}

	.recipients-controls select {
		font: inherit;
		font-size: 0.875rem;
		padding: 0.4375rem 0.625rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	.recipients-note {
		font-size: 0.8125rem;
		color: var(--color-ink-2);
	}

	.recipients-note a {
		color: var(--color-stain);
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

	.issues a {
		color: var(--color-stain);
		text-decoration: none;
	}

	.issues a:hover {
		text-decoration: underline;
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

	.lifecycle-actions {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
		margin: 0.75rem 0 1.25rem;
	}

	.quiet-action {
		background: none;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		font: inherit;
		font-size: 0.8125rem;
		font-weight: 540;
		color: var(--color-ink-2);
		padding: 0.4375rem 0.625rem;
		cursor: pointer;
		text-align: left;
	}

	.quiet-action:hover {
		border-color: var(--color-ink);
		color: var(--color-ink);
	}

	.governance-h {
		margin-top: 0.5rem;
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
