<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import {
		createProductApi,
		type CampaignSeriesOperationsWorkspaceResponse
	} from '$lib/api/product';
	import { createSetupApi } from '$lib/api/setup';
	import { api } from '$lib/core/client';
	import { t } from '$lib/core/locale.svelte';
	import { problemMessage } from '$lib/core/problem';
	import { collectionGuidanceCopy, prerequisiteCopy } from '$lib/core/backend-copy';
	import { formatCount, formatDateTime, humanizeToken } from '$lib/core/format';
	import CoverageMeter from '$lib/ui/CoverageMeter.svelte';
	import { confirmDialog } from '$lib/ui/dialog.svelte';

	const product = createProductApi(api());
	const setup = createSetupApi(api());
	const seriesId = $derived(page.params.seriesId!);

	let mintedLink = $state<{ campaignId: string; url: string } | null>(null);
	let linkBusy = $state<string | null>(null);
	let linkError = $state<string | null>(null);
	let copied = $state(false);

	async function mintLink(campaignId: string, replace: boolean) {
		if (linkBusy) return;
		linkBusy = campaignId;
		linkError = null;
		copied = false;

		try {
			const link = replace
				? await setup.replaceCampaignOpenLink(campaignId)
				: await setup.createCampaignOpenLink(campaignId);
			mintedLink = { campaignId, url: `${location.origin}/r/${link.token}` };
			await read(true);
		} catch {
			linkError = replace
				? 'The link could not be replaced. Try again.'
				: 'The link could not be created. If one already exists, use "Replace lost link".';
		} finally {
			linkBusy = null;
		}
	}

	async function copyLink() {
		if (!mintedLink) return;
		await navigator.clipboard.writeText(mintedLink.url);
		copied = true;
	}

	type QueueLink = {
		respondentSubjectId: string;
		assignmentCount: number;
		url: string | null;
		status: string;
	};
	let queueLinks = $state<{ campaignId: string; links: QueueLink[] } | null>(null);
	let queueNames = $state<Record<string, string>>({});

	/** Personal queue links for an identified wave — one per respondent, shown once. */
	async function mintQueueLinks(campaignId: string) {
		if (linkBusy) return;
		linkBusy = campaignId;
		linkError = null;

		try {
			const access = await setup.createCampaignIdentifiedQueueAccess(campaignId);
			queueLinks = {
				campaignId,
				links: access.links.map((link) => ({
					respondentSubjectId: link.respondentSubjectId,
					assignmentCount: link.assignmentCount,
					url: link.token ? `${location.origin}/r/${link.token}` : null,
					status: link.status
				}))
			};
			// resolve respondent names so the researcher knows whose link is whose
			const directory = await product.listSubjects().catch(() => null);
			queueNames = Object.fromEntries(
				(directory?.subjects ?? []).map((subject) => [subject.id, subject.displayName ?? subject.id])
			);
			await read(true);
		} catch (cause) {
			linkError = problemMessage(
				cause,
				{
					'identified_queue.target_assignments_required': t(
						'Personal queue links need target-aware recipients (like manager review). Self-report identified waves invite by email instead.'
					)
				},
				t('Respondent links could not be created. The wave must be launched and have recipients.')
			);
		} finally {
			linkBusy = null;
		}
	}

	let closeBusy = $state<string | null>(null);

	async function closeWave(campaignId: string, name: string) {
		if (closeBusy) return;
		const proceed = await confirmDialog({
			title: `${t('Close wave')}: ${name}?`,
			body: t("Collection stops and the wave's data becomes final for reporting. This cannot be undone."),
			confirmLabel: t('Close wave'),
			danger: true
		});
		if (!proceed) return;
		closeBusy = campaignId;
		try {
			await product.closeCampaign(seriesId, campaignId);
			await read(true);
		} catch {
			linkError = 'The wave could not be closed. Try again.';
		} finally {
			closeBusy = null;
		}
	}

	let inviteEmails = $state('');
	let inviteBusy = $state(false);
	let inviteResult = $state<string | null>(null);

	async function sendInvitations(campaignId: string) {
		const recipients = inviteEmails
			.split(/[\n,;]+/)
			.map((email) => email.trim())
			.filter((email) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))
			.map((email) => ({ email }));
		if (recipients.length === 0 || inviteBusy) return;

		inviteBusy = true;
		inviteResult = null;
		try {
			const batch = await setup.createCampaignInvitationBatch(campaignId, { recipients });
			inviteResult = `${batch.createdInvitationCount} of ${batch.requestedRecipientCount} ${t('invited by email')}`;
			inviteEmails = '';
			// queued invitations don't send on their own — send them now
			if (batch.createdInvitationCount > 0) await setup.processCampaignEmailDeliveries(campaignId);
			await read(true);
			await loadDeliveries(campaignId);
		} catch (cause) {
			// surface the real refusal reason instead of a phantom "readiness" check
			inviteResult = problemMessage(
				cause,
				{
					'invitation_batch.identity_mode_not_supported': t(
						'Email invitations are for anonymous waves. This wave is identified — use “Create respondent links” to give each person their own link.'
					),
					'invitation_batch.open_link_access_active': t(
						'This wave is collecting through its open link. A wave uses one channel — remove the open link to invite by email instead.'
					),
					'campaign.not_launched': t('Launch the wave before inviting respondents.'),
					'invitation_batch.recipients_required': t('Add at least one email address.')
				},
				t('Invitations could not be queued. Try again.')
			);
		} finally {
			inviteBusy = false;
		}
	}

	// identified email: send each named recipient their own private link
	let identifiedInviteBusy = $state<string | null>(null);
	let identifiedInviteResult = $state<{ campaignId: string; text: string } | null>(null);

	async function sendIdentifiedInvitations(campaignId: string) {
		if (identifiedInviteBusy) return;
		identifiedInviteBusy = campaignId;
		identifiedInviteResult = null;
		try {
			const result = await setup.sendCampaignIdentifiedInvitations(campaignId);
			const parts = [`${formatCount(result.invitedCount)} ${t('invited by email')}`];
			if (result.noEmailCount > 0)
				parts.push(`${formatCount(result.noEmailCount)} ${t('have no email — share a link')}`);
			if (result.alreadyInvitedCount > 0)
				parts.push(`${formatCount(result.alreadyInvitedCount)} ${t('already invited')}`);
			if (result.suppressedCount > 0)
				parts.push(`${formatCount(result.suppressedCount)} ${t('on the do-not-contact list')}`);
			identifiedInviteResult = { campaignId, text: parts.join(' · ') };
			// queued invitations don't send on their own — send them now
			if (result.invitedCount > 0) await setup.processCampaignEmailDeliveries(campaignId);
			await read(true);
			await loadDeliveries(campaignId);
		} catch (cause) {
			identifiedInviteResult = {
				campaignId,
				text: problemMessage(
					cause,
					{
						'identified_invitation.no_recipients': t(
							'This wave has no recipients yet. Set them on the protocol first.'
						)
					},
					t('Invitations could not be sent. Try again.')
				)
			};
		} finally {
			identifiedInviteBusy = null;
		}
	}

	// one expand per wave: the recipients & delivery panel
	let manageFor = $state<string | null>(null);
	let deliveryBusy = $state(false);
	let deliveries = $state<
		import('$lib/api/setup').CampaignInvitationDeliveriesResponse | null
	>(null);

	async function toggleManage(campaignId: string) {
		if (manageFor === campaignId) {
			manageFor = null;
			return;
		}
		manageFor = campaignId;
		inviteEmails = '';
		inviteResult = null;
		identifiedInviteResult = null;
		await loadDeliveries(campaignId);
	}

	async function loadDeliveries(campaignId: string) {
		deliveryBusy = true;
		deliveries = null;
		try {
			deliveries = await setup.listCampaignInvitationDeliveries(campaignId);
		} catch {
			deliveries = null;
		} finally {
			deliveryBusy = false;
		}
	}

	const queuedCount = $derived(deliveries?.queuedCount ?? 0);

	// nothing auto-sends queued invitations, so the invite action triggers the
	// send itself; this also backs a manual "Send queued" retry
	let sendBusy = $state<string | null>(null);
	async function sendQueued(campaignId: string) {
		if (sendBusy) return;
		sendBusy = campaignId;
		try {
			await setup.processCampaignEmailDeliveries(campaignId);
		} catch {
			// the delivery list reflects whatever state each notification reached
		} finally {
			sendBusy = null;
			await loadDeliveries(campaignId);
		}
	}

	function deliveryStatusClass(status: string): string {
		const s = status.toLowerCase();
		if (s === 'sent') return 'st sent';
		if (s === 'bounced') return 'st bounced';
		if (s === 'failed') return 'st failed';
		return 'st queued';
	}

	let workspace = $state<CampaignSeriesOperationsWorkspaceResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'ready'>('loading');
	let lastReadAt = $state<Date | null>(null);

	const live = $derived((workspace?.summary.liveCampaignCount ?? 0) > 0);
	const selected = $derived(workspace?.selectedCampaign ?? null);

	async function read(silent = false) {
		if (!silent) loadState = 'loading';
		try {
			workspace = await product.getCampaignSeriesOperationsWorkspace(seriesId);
			lastReadAt = new Date();
			loadState = 'ready';
		} catch {
			if (!silent) loadState = 'error';
		}
	}

	onMount(() => {
		void read();
		const timer = setInterval(() => {
			if (document.visibilityState === 'visible' && live) {
				void read(true);
			}
		}, 20000);

		return () => clearInterval(timer);
	});
</script>

<svelte:head><title>Field — ValidatedScale</title></svelte:head>

<div class="console" class:idle={!live}>
	<div class="inner">
		<header class="head">
			<p class="eyebrow crumbs">
				<a href="/app/studies">{t('Studies')}</a> /
				<a href={`/app/studies/${seriesId}`}>{workspace?.series.name ?? t('Study')}</a>
			</p>

			<div class="title-row">
				<h1 class="doc-title">{t('Field')}</h1>
				{#if loadState === 'ready'}
					<span class="live-flag" class:on={live}>
						<span class="pip" aria-hidden="true"></span>
						{live ? t('Collecting') : t('Not collecting')}
					</span>
				{/if}
				{#if lastReadAt}
					<span class="datum read-at">{t('read')} {lastReadAt.toLocaleTimeString('en-GB')}</span>
				{/if}
			</div>

			<nav class="phases" aria-label="Study phases">
				<a class="phase" href={`/app/studies/${seriesId}`}>{t('Protocol')}</a>
				<span class="phase current">{t('Field')}</span>
				<a class="phase" href={`/app/studies/${seriesId}/evidence`}>{t('Evidence')}</a>
			</nav>
		</header>

		{#if loadState === 'loading'}
			<p class="reading-note" role="status">{t('Reading the field…')}</p>
		{:else if loadState === 'error'}
			<p class="reading-note" role="alert">
				{t('The field did not respond.')} <button class="retry" onclick={() => read()}>{t('Read again')}</button>
			</p>
		{:else if workspace}
			<section class="board">
				<div class="tile">
					<span class="eyebrow dim-label">{t('Invited')}</span>
					<span class="datum value">
						{formatCount(
							workspace.summary.sentInvitationCount + workspace.summary.openLinkAssignmentCount
						)}
					</span>
					<span class="sub">
						{formatCount(workspace.summary.sentInvitationCount)} {t('sent')} ·
						{formatCount(workspace.summary.openLinkAssignmentCount)} {t('open link')}
					</span>
				</div>
				<div class="tile">
					<span class="eyebrow dim-label">{t('Started')}</span>
					<span class="datum value">{formatCount(workspace.summary.startedResponseCount)}</span>
					<span class="sub">{formatCount(workspace.summary.draftResponseCount)} {t('in draft')}</span>
				</div>
				<div class="tile accent">
					<span class="eyebrow dim-label">{t('Submitted')}</span>
					<span class="datum value">{formatCount(workspace.summary.submittedResponseCount)}</span>
					<span class="sub">
						{t('last')} {formatDateTime(workspace.summary.latestResponseSubmittedAt)}
					</span>
				</div>
				<div class="tile">
					<span class="eyebrow dim-label">{t('Delivery')}</span>
					<span class="datum value">
						{formatCount(workspace.summary.failedInvitationCount)}
						<span class="value-unit">{t('failed')}</span>
					</span>
					<span class="sub">
						{formatCount(workspace.summary.bouncedInvitationCount ?? 0)} {t('bounced')} ·
						{formatCount(workspace.summary.deliveryAttemptCount)} {t('attempts')}
					</span>
				</div>
			</section>

			<p class="guidance">
				{collectionGuidanceCopy(
					workspace.summary.collectionStatus,
					workspace.summary.reportVisibilityStatus,
					workspace.summary.collectionGuidance
				)}
			</p>

			{#if workspace.groupCoverage && workspace.groupCoverage.groups.length > 0}
				<section class="coverage">
					<h2 class="eyebrow dim-label">
						{t('Group coverage — reporting threshold')} k = {workspace.groupCoverage.kMin}
					</h2>
					<div class="meters">
						{#each workspace.groupCoverage.groups as group (group.groupId)}
							<CoverageMeter
								label={group.groupName}
								submitted={group.submittedCount}
								invited={group.invitedCount}
								kMin={workspace.groupCoverage.kMin}
								meets={group.meetsThreshold}
							/>
						{/each}
					</div>
					{#if workspace.groupCoverage.unattributedSubmittedCount > 0}
						<p class="unattributed">
							{formatCount(workspace.groupCoverage.unattributedSubmittedCount)}
							{t('submissions are not attributed to a group.')}
						</p>
					{/if}
				</section>
			{/if}

			<section class="waves">
				<h2 class="eyebrow dim-label">{t('Waves')}</h2>
				<ul>
					{#each workspace.campaigns as wave (wave.id)}
						<li class:selected={wave.id === selected?.id}>
							<div class="wave-main">
								<strong>{wave.name}</strong>
								<span class="datum wave-meta">
									{t(humanizeToken(wave.status))} · {t(humanizeToken(wave.responseIdentityMode))}
									{#if wave.latestLaunchAt}· {t('launched')} {formatDateTime(wave.latestLaunchAt)}{/if}
								</span>
							</div>
							<span class="datum wave-count">
								{formatCount(wave.submittedResponseCount)}
								<span class="dim">{t('submitted')}</span>
							</span>
							{#if wave.status.toLowerCase() === 'live'}
								<span class="link-actions">
									<button class="link-btn" onclick={() => toggleManage(wave.id)}>
										{manageFor === wave.id ? t('Hide') : t('Invite & deliver')}
									</button>
									<button class="link-btn" disabled={closeBusy === wave.id} onclick={() => closeWave(wave.id, wave.name)}>
										{closeBusy === wave.id ? t('Closing…') : t('Close wave')}
									</button>
								</span>
							{/if}
						</li>
						{#if manageFor === wave.id}
							<li class="minted">
								<div class="manage-panel">
									<div class="manage-section">
										<span class="eyebrow dim-label">{t('Invite')}</span>
										{#if wave.responseIdentityMode.toLowerCase() === 'identified'}
											<p class="invite-hint">{t('Email each person their own private link, or get the links to share yourself.')}</p>
											<div class="manage-actions">
												<button class="link-btn primary" disabled={identifiedInviteBusy === wave.id} onclick={() => sendIdentifiedInvitations(wave.id)}>
													{identifiedInviteBusy === wave.id ? t('Sending…') : t('Invite people by email')}
												</button>
												<button class="link-btn" disabled={linkBusy === wave.id} onclick={() => mintQueueLinks(wave.id)}>
													{t('Get links to share myself')}
												</button>
											</div>
											{#if identifiedInviteResult?.campaignId === wave.id}
												<p class="invite-result" role="status">{identifiedInviteResult.text}</p>
											{/if}
										{:else}
											<p class="invite-hint">{t('A wave collects one way: email invitations or a shared open link, not both.')}</p>
											<textarea
												rows="3"
												bind:value={inviteEmails}
												placeholder={'ana@example.org\nmarko@example.org'}
												aria-label="Recipient emails"
											></textarea>
											<div class="manage-actions">
												<button class="link-btn primary" disabled={inviteBusy} onclick={() => sendInvitations(wave.id)}>
													{inviteBusy ? t('Sending…') : t('Invite by email')}
												</button>
												{#if wave.openLinkAssignmentCount > 0}
													<button class="link-btn" disabled={linkBusy === wave.id} onclick={() => mintLink(wave.id, true)}>
														{t('Replace lost link')}
													</button>
												{:else}
													<button class="link-btn" disabled={linkBusy === wave.id} onclick={() => mintLink(wave.id, false)}>
														{t('Create open link')}
													</button>
												{/if}
											</div>
											{#if inviteResult}<p class="invite-result" role="status">{inviteResult}</p>{/if}
										{/if}
									</div>

									{#if mintedLink?.campaignId === wave.id}
										<div class="minted-inner">
											<span class="eyebrow dim-label">{t('Respondent link — shown once, save it now')}</span>
											<code class="datum minted-url">{mintedLink.url}</code>
											<button class="link-btn" onclick={copyLink}>{copied ? t('Copied') : t('Copy link')}</button>
										</div>
									{/if}
									{#if queueLinks?.campaignId === wave.id}
										<div class="minted-inner">
											<span class="eyebrow dim-label">
												{t('Personal respondent links — shown once, deliver each to its person')}
											</span>
											<ul class="queue-links">
												{#each queueLinks.links as link (link.respondentSubjectId)}
													<li>
														<span class="queue-who">
															{queueNames[link.respondentSubjectId] ?? link.respondentSubjectId}
															<span class="datum queue-count">
																{formatCount(link.assignmentCount)} {t('to answer')}
															</span>
														</span>
														{#if link.url}
															<code class="datum minted-url">{link.url}</code>
														{:else}
															<span class="datum queue-count">{t(humanizeToken(link.status))}</span>
														{/if}
													</li>
												{/each}
											</ul>
										</div>
									{/if}

									<div class="manage-section">
										<div class="manage-head">
											<span class="eyebrow dim-label">{t('Delivery — who was invited and where it landed')}</span>
											{#if queuedCount > 0}
												<button class="link-btn" disabled={sendBusy === wave.id} onclick={() => sendQueued(wave.id)}>
													{sendBusy === wave.id ? t('Sending…') : `${t('Send queued')} (${formatCount(queuedCount)})`}
												</button>
											{/if}
										</div>
										{#if deliveryBusy}
											<p class="hint-line">{t('Loading…')}</p>
										{:else if deliveries && deliveries.deliveries.length > 0}
											<ul class="delivery-list">
												{#each deliveries.deliveries as row (row.notificationId)}
													<li>
														<span class="delivery-who">
															{#if row.displayName}<strong>{row.displayName}</strong>{/if}
															<span class="datum delivery-email">{row.recipient}</span>
														</span>
														<span class={deliveryStatusClass(row.status)}>{t(humanizeToken(row.status))}</span>
														<span class="datum delivery-when">
															{#if row.error}{t(humanizeToken(row.error))}{:else if row.lastEventAt}{formatDateTime(row.lastEventAt)}{/if}
														</span>
													</li>
												{/each}
											</ul>
										{:else}
											<p class="hint-line">{t('No email invitations on this wave yet.')}</p>
										{/if}
									</div>
								</div>
							</li>
						{/if}
					{:else}
						<li class="none">{t('No waves launched yet. Launch from the protocol.')}</li>
					{/each}
				</ul>
				{#if linkError}<p class="link-error" role="alert">{linkError}</p>{/if}
			</section>

			{#if workspace.missingPrerequisites.length > 0}
				<section class="prereqs">
					<h2 class="eyebrow dim-label">{t('Field notes')}</h2>
					<ul>
						{#each workspace.missingPrerequisites as item (item.code)}
							<li>{prerequisiteCopy(item.code, item.message).text}</li>
						{/each}
					</ul>
				</section>
			{/if}
		{/if}
	</div>
</div>

<style>
	/* The console claims the full canvas: the app's one dark surface. */
	.console {
		margin-top: -2rem;
		margin-bottom: -4rem;
		margin-left: calc(50% - 50vw);
		margin-right: calc(50% - 50vw);
		min-height: calc(100dvh - 3.25rem);
		background: var(--color-console);
		color: var(--color-console-ink);
	}

	.inner {
		max-width: 74rem;
		margin: 0 auto;
		padding: 2rem 1.5rem 4rem;
	}

	.crumbs,
	.crumbs a {
		color: var(--color-console-dim);
		text-decoration: none;
	}

	.crumbs a:hover {
		color: var(--color-console-ink);
	}

	.title-row {
		display: flex;
		align-items: baseline;
		gap: 1.25rem;
		margin-top: 0.5rem;
	}

	.title-row h1 {
		font-size: 2.25rem;
		color: var(--color-console-ink);
	}

	.live-flag {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-console-dim);
	}

	.live-flag .pip {
		width: 9px;
		height: 9px;
		border-radius: 999px;
		background: var(--color-console-line);
	}

	.live-flag.on {
		color: var(--color-console-ink);
	}

	.live-flag.on .pip {
		background: var(--color-chart-violet-dark);
		box-shadow: 0 0 0 4px color-mix(in oklab, var(--color-chart-violet-dark) 22%, transparent);
		animation: breathe 2.4s ease-in-out infinite;
	}

	@keyframes breathe {
		0%,
		100% {
			box-shadow: 0 0 0 3px color-mix(in oklab, var(--color-chart-violet-dark) 16%, transparent);
		}
		50% {
			box-shadow: 0 0 0 6px color-mix(in oklab, var(--color-chart-violet-dark) 28%, transparent);
		}
	}

	.read-at {
		margin-left: auto;
		font-size: 0.6875rem;
		color: var(--color-console-dim);
	}

	.phases {
		display: flex;
		gap: 0.25rem;
		margin-top: 1.5rem;
		border-bottom: 1px solid var(--color-console-line);
	}

	.phase {
		padding: 0.5rem 0.875rem;
		font-size: 0.875rem;
		font-weight: 540;
		color: var(--color-console-dim);
		text-decoration: none;
		border-bottom: 2px solid transparent;
		margin-bottom: -1px;
	}

	.phase:hover {
		color: var(--color-console-ink);
	}

	.phase.current {
		color: var(--color-chart-violet-dark);
		border-bottom-color: var(--color-chart-violet-dark);
	}

	.reading-note {
		margin-top: 3rem;
		color: var(--color-console-dim);
	}

	.retry {
		background: none;
		border: none;
		color: var(--color-chart-violet-dark);
		font: inherit;
		cursor: pointer;
		text-decoration: underline;
	}

	.board {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: 1px;
		background: var(--color-console-line);
		border: 1px solid var(--color-console-line);
		border-radius: var(--radius-instrument);
		overflow: hidden;
		margin-top: 2rem;
	}

	.tile {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
		padding: 1.25rem;
		background: var(--color-console-2);
	}

	.tile.accent {
		box-shadow: inset 0 3px 0 var(--color-chart-violet-dark);
	}

	.dim-label {
		color: var(--color-console-dim);
	}

	.value {
		font-size: 2rem;
		font-weight: 500;
		color: var(--color-console-ink);
	}

	.value-unit {
		font-size: 0.8125rem;
		color: var(--color-console-dim);
	}

	.sub {
		font-size: 0.75rem;
		color: var(--color-console-dim);
	}

	.guidance {
		margin-top: 1.25rem;
		font-size: 0.9375rem;
		color: var(--color-console-dim);
		max-width: 64ch;
	}

	.coverage {
		margin-top: 2.5rem;
	}

	.meters {
		margin-top: 1rem;
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
		max-width: 44rem;
	}

	.unattributed {
		margin-top: 1rem;
		font-size: 0.8125rem;
		color: var(--color-console-dim);
	}

	.waves {
		margin-top: 2.75rem;
	}

	.waves ul {
		list-style: none;
		margin-top: 0.75rem;
	}

	.waves li {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 1.5rem;
		padding: 0.875rem 0.875rem;
		border-bottom: 1px solid var(--color-console-line);
	}

	.waves li.selected {
		background: var(--color-console-2);
		border-radius: var(--radius-instrument);
	}

	.waves li.none {
		color: var(--color-console-dim);
		font-size: 0.9375rem;
	}

	.wave-main {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.wave-main strong {
		font-weight: 560;
		color: var(--color-console-ink);
	}

	.wave-meta {
		font-size: 0.75rem;
		color: var(--color-console-dim);
	}

	.wave-count {
		font-size: 1rem;
		color: var(--color-console-ink);
		white-space: nowrap;
	}

	.wave-count .dim {
		font-size: 0.75rem;
		color: var(--color-console-dim);
	}

	.link-actions {
		flex-shrink: 0;
	}

	.link-btn {
		background: none;
		border: 1px solid var(--color-console-line);
		border-radius: var(--radius-instrument);
		color: var(--color-chart-violet-dark);
		font: inherit;
		font-size: 0.8125rem;
		font-weight: 560;
		padding: 0.375rem 0.75rem;
		cursor: pointer;
		white-space: nowrap;
	}

	.link-btn:hover {
		border-color: var(--color-chart-violet-dark);
	}

	.minted {
		background: var(--color-console-2);
		border-radius: var(--radius-instrument);
	}

	.minted-inner {
		display: flex;
		align-items: center;
		gap: 1rem;
		flex-wrap: wrap;
		width: 100%;
	}

	.minted-url {
		font-size: 0.8125rem;
		color: var(--color-console-ink);
		word-break: break-all;
		flex: 1;
		min-width: 16rem;
	}

	.queue-links {
		list-style: none;
		width: 100%;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.queue-links li {
		display: flex;
		align-items: baseline;
		gap: 1rem;
		flex-wrap: wrap;
		padding: 0;
		border: 0;
	}

	.queue-who {
		min-width: 14rem;
		color: var(--color-console-ink);
		font-size: 0.875rem;
	}

	.queue-count {
		margin-left: 0.5rem;
		font-size: 0.75rem;
		color: var(--color-console-ink-2, rgba(255, 255, 255, 0.55));
	}

	.link-error {
		margin-top: 0.75rem;
		font-size: 0.8125rem;
		color: #f0b429;
	}

	.invite-inner {
		display: flex;
		flex-direction: column;
		gap: 0.625rem;
		width: 100%;
	}

	.manage-section textarea {
		font-family: var(--font-mono);
		font-size: 0.8125rem;
		background: var(--color-console);
		border: 1px solid var(--color-console-line);
		border-radius: var(--radius-instrument);
		color: var(--color-console-ink);
		padding: 0.625rem;
		resize: vertical;
	}

	.invite-row {
		display: flex;
		align-items: center;
		gap: 1rem;
	}

	.invite-result {
		font-size: 0.8125rem;
		color: var(--color-console-dim);
	}

	.invite-hint {
		font-size: 0.75rem;
		color: var(--color-console-dim);
		margin: -0.25rem 0 0;
	}

	.manage-panel {
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
		width: 100%;
	}

	.manage-section {
		display: flex;
		flex-direction: column;
		gap: 0.6rem;
	}

	.manage-head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 1rem;
		flex-wrap: wrap;
	}

	.manage-actions {
		display: flex;
		gap: 0.6rem;
		flex-wrap: wrap;
	}

	.link-btn.primary {
		background: var(--color-stain);
		border-color: var(--color-stain);
		color: #fff;
	}

	.link-btn.primary:hover:not(:disabled) {
		filter: brightness(1.08);
	}

	.delivery-list {
		list-style: none;
		width: 100%;
		display: flex;
		flex-direction: column;
		gap: 0.4rem;
	}

	.delivery-list li {
		display: grid;
		grid-template-columns: 1fr auto auto;
		align-items: baseline;
		gap: 1rem;
		padding: 0;
		border: 0;
	}

	.delivery-who {
		display: flex;
		flex-direction: column;
	}

	.delivery-email {
		font-size: 0.75rem;
		color: var(--color-console-dim);
	}

	.delivery-when {
		font-size: 0.7rem;
		color: var(--color-console-dim);
		white-space: nowrap;
	}

	.st {
		font-family: var(--font-mono);
		font-size: 0.68rem;
		letter-spacing: 0.04em;
		text-transform: uppercase;
		padding: 0.15rem 0.5rem;
		border-radius: 999px;
		white-space: nowrap;
	}

	.st.queued {
		color: #b8b8c4;
		background: rgba(255, 255, 255, 0.07);
	}

	.st.sent {
		color: #8fb8ff;
		background: rgba(120, 150, 255, 0.14);
	}

	.st.bounced {
		color: #eec06a;
		background: rgba(220, 160, 50, 0.16);
	}

	.st.failed {
		color: #f0a3a0;
		background: rgba(220, 90, 80, 0.16);
	}

	.prereqs {
		margin-top: 2.75rem;
	}

	.prereqs ul {
		list-style: none;
		margin-top: 0.75rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		font-size: 0.875rem;
		color: var(--color-console-dim);
	}

	.prereqs li {
		padding-left: 0.875rem;
		border-left: 2px solid var(--color-console-line);
	}

	@media (max-width: 54rem) {
		.board {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.live-flag.on .pip {
			animation: none;
		}
	}
</style>
