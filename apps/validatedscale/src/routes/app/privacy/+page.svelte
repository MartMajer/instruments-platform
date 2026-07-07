<script lang="ts">
	import { onMount } from 'svelte';
	import { createProductApi } from '$lib/api/product';
	import {
		createGovernanceApi,
		type WithdrawalRequestReviewResponse,
		type ListEmailSuppressionsResponse
	} from '$lib/api/governance';
	import { api } from '$lib/core/client';
	import { t } from '$lib/core/locale.svelte';
	import { formatCount, formatDateTime, humanizeToken } from '$lib/core/format';
	import { confirmDialog } from '$lib/ui/dialog.svelte';
	import LoadState from '$lib/ui/LoadState.svelte';

	const governance = createGovernanceApi(api());
	const product = createProductApi(api());

	let requests = $state<WithdrawalRequestReviewResponse[]>([]);
	let suppressions = $state<ListEmailSuppressionsResponse | null>(null);
	let subjectNames = $state<Record<string, string>>({});
	let loadState = $state<'loading' | 'error' | 'ready'>('loading');
	let note = $state<string | null>(null);

	async function load() {
		try {
			[requests, suppressions] = await Promise.all([
				governance.listWithdrawalRequests(),
				governance.listEmailSuppressions()
			]);
			// the review rows carry subject ids; show the person, not a guid
			const directory = await product.listSubjects().catch(() => null);
			subjectNames = Object.fromEntries(
				(directory?.subjects ?? []).map((subject) => [subject.id, subject.displayName ?? subject.id])
			);
			loadState = 'ready';
		} catch {
			loadState = 'error';
		}
	}

	function targetLabel(request: WithdrawalRequestReviewResponse): string {
		return subjectNames[request.targetId] ?? t(humanizeToken(request.targetKind));
	}

	onMount(load);

	// --- record a withdrawal request received off-platform ---
	let recordOpen = $state(false);
	let people = $state<{ id: string; displayName: string }[]>([]);
	let recordSubjectId = $state('');
	let recordAction = $state<'delete' | 'anonymize'>('delete');
	let recordBusy = $state(false);

	async function toggleRecord() {
		recordOpen = !recordOpen;
		note = null;
		if (recordOpen && people.length === 0) {
			const directory = await product.listSubjects().catch(() => null);
			people = (directory?.subjects ?? []).map((subject) => ({
				id: subject.id,
				displayName: subject.displayName ?? subject.id
			}));
		}
	}

	async function recordRequest() {
		if (recordBusy || !recordSubjectId) return;
		recordBusy = true;
		note = null;
		try {
			await governance.createWithdrawalRequest({
				targetKind: 'identified_subject',
				targetId: recordSubjectId,
				requestedAction: recordAction
			});
			note = t('Request recorded. Review and approve it below.');
			recordOpen = false;
			recordSubjectId = '';
			await load();
		} catch {
			note = t('The request could not be recorded. Check that the person has identified responses.');
		} finally {
			recordBusy = false;
		}
	}

	// --- decisions ---
	let verbBusy = $state<string | null>(null);

	async function decide(request: WithdrawalRequestReviewResponse, verb: 'approve' | 'deny' | 'execute') {
		if (verbBusy) return;
		if (verb === 'execute') {
			const proceed = await confirmDialog({
				title: t('Execute withdrawal?'),
				body: t(
					'The requested data is deleted or anonymized according to the request. This cannot be undone.'
				),
				confirmLabel: t('Execute'),
				danger: true
			});
			if (!proceed) return;
		}
		verbBusy = request.requestId;
		note = null;
		try {
			if (verb === 'approve') await governance.approveWithdrawalRequest(request.requestId);
			else if (verb === 'deny') await governance.denyWithdrawalRequest(request.requestId);
			else {
				const executed = await governance.executeWithdrawalRequest(request.requestId);
				note = `${t('Executed:')} ${formatCount(executed.dryRun.responseSessionCount)} ${t('sessions')}, ${formatCount(executed.dryRun.answerCount)} ${t('answers')}, ${formatCount(executed.dryRun.scoreCount)} ${t('scores')}.`;
			}
			await load();
		} catch {
			note = t('The action was not accepted. Reload and try again.');
		} finally {
			verbBusy = null;
		}
	}

	// --- suppressions ---
	let suppressEmail = $state('');
	let suppressBusy = $state(false);

	async function addSuppression(event: SubmitEvent) {
		event.preventDefault();
		if (suppressBusy || !suppressEmail.trim()) return;
		suppressBusy = true;
		note = null;
		try {
			await governance.addEmailSuppression({
				recipient: suppressEmail.trim(),
				reason: 'manual_do_not_contact'
			});
			suppressEmail = '';
			await load();
		} catch {
			note = t('The address could not be suppressed. Check the format and try again.');
		} finally {
			suppressBusy = false;
		}
	}

	async function release(suppressionId: string) {
		if (suppressBusy) return;
		suppressBusy = true;
		note = null;
		try {
			await governance.releaseEmailSuppression(suppressionId, 'manual_release');
			await load();
		} catch {
			note = t('The release was not accepted.');
		} finally {
			suppressBusy = false;
		}
	}

	function statusChip(status: string): string {
		const normalized = status.toLowerCase();
		if (normalized.includes('executed') || normalized.includes('approved')) return 'chip chip-live';
		if (normalized.includes('denied')) return 'chip chip-danger';
		return 'chip';
	}
</script>

<svelte:head><title>{t('Privacy')} — ValidatedScale</title></svelte:head>

<header class="head">
	<div>
		<p class="eyebrow">{t('Governance')}</p>
		<h1 class="doc-title">{t('Privacy & data requests')}</h1>
		<p class="hint">
			{t('Respondent rights, operable: withdrawal requests reviewed and executed with an audit trail, and a do-not-contact list every invitation send honors.')}
		</p>
	</div>
</header>

{#if note}<p class="note" role="status">{note}</p>{/if}

<LoadState state={loadState}>
	<section class="block">
		<div class="block-head">
			<h2 class="eyebrow">{t('Withdrawal requests')}</h2>
			<button class="btn btn-ghost" onclick={toggleRecord}>
				{recordOpen ? t('Close') : t('Record a request')}
			</button>
		</div>

		{#if recordOpen}
			<div class="panel author">
				<p class="hint">
					{t('For requests that arrive by email or in person: pick the person and what they asked for. Anonymous respondents withdraw through their response link.')}
				</p>
				<div class="author-row">
					<select bind:value={recordSubjectId} aria-label={t('Person')}>
						<option value="" disabled>{t('Choose a person')}</option>
						{#each people as person (person.id)}
							<option value={person.id}>{person.displayName}</option>
						{/each}
					</select>
					<select bind:value={recordAction} aria-label={t('Requested action')}>
						<option value="delete">{t('Delete their data')}</option>
						<option value="anonymize">{t('Anonymize their data')}</option>
					</select>
					<button class="btn btn-ink" disabled={recordBusy || !recordSubjectId} onclick={recordRequest}>
						{recordBusy ? t('Recording…') : t('Record request')}
					</button>
				</div>
			</div>
		{/if}

		{#if requests.length === 0}
			<p class="quiet">{t('No withdrawal requests. When one arrives, it appears here for review.')}</p>
		{:else}
			<div class="table-wrap">
				<table>
					<thead>
						<tr>
							<th>{t('Requested')}</th>
							<th>{t('Target')}</th>
							<th>{t('Action')}</th>
							<th>{t('Status')}</th>
							<th class="num">{t('Sessions')}</th>
							<th class="num">{t('Answers')}</th>
							<th></th>
						</tr>
					</thead>
					<tbody>
						{#each requests as request (request.requestId)}
							<tr>
								<td class="datum">{formatDateTime(request.requestedAt)}</td>
								<td>
									{targetLabel(request)}
									<span class="datum target-kind">{t(humanizeToken(request.targetKind))}</span>
								</td>
								<td>{t(humanizeToken(request.requestedAction))}</td>
								<td><span class={statusChip(request.status)}>{t(humanizeToken(request.status))}</span></td>
								<td class="num datum">{formatCount(request.responseSessionCount)}</td>
								<td class="num datum">{formatCount(request.answerCount)}</td>
								<td class="verbs">
									{#if request.canApprove}
										<button class="dl" disabled={verbBusy === request.requestId} onclick={() => decide(request, 'approve')}>{t('Approve')}</button>
									{/if}
									{#if request.canDeny}
										<button class="dl" disabled={verbBusy === request.requestId} onclick={() => decide(request, 'deny')}>{t('Deny')}</button>
									{/if}
									{#if request.canExecute}
										<button class="dl danger" disabled={verbBusy === request.requestId} onclick={() => decide(request, 'execute')}>{t('Execute')}</button>
									{/if}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{/if}
	</section>

	<section class="block">
		<div class="block-head">
			<h2 class="eyebrow">{t('Email suppressions — do not contact')}</h2>
			{#if suppressions}
				<span class="datum counts">
					{formatCount(suppressions.activeCount)} {t('active')} ·
					{formatCount(suppressions.releasedCount)} {t('released')}
				</span>
			{/if}
		</div>
		<p class="hint">
			{t('Invitations are never sent to suppressed addresses. When someone unsubscribes it applies to that study only; bounces and spam complaints apply to every study. Addresses you add here apply everywhere.')}
		</p>

		<form class="author-row suppress-form" onsubmit={addSuppression}>
			<input
				type="email"
				bind:value={suppressEmail}
				placeholder="name@example.org"
				aria-label={t('Email to suppress')}
			/>
			<button class="btn btn-ghost" type="submit" disabled={suppressBusy || !suppressEmail.trim()}>
				{t('Suppress address')}
			</button>
		</form>

		{#if suppressions && suppressions.suppressions.length > 0}
			<div class="table-wrap">
				<table>
					<thead>
						<tr>
							<th>{t('Email')}</th>
							<th>{t('Applies to')}</th>
							<th>{t('Reason')}</th>
							<th>{t('Source')}</th>
							<th>{t('Since')}</th>
							<th>{t('Status')}</th>
							<th></th>
						</tr>
					</thead>
					<tbody>
						{#each suppressions.suppressions as suppression (suppression.id)}
							<tr>
								<td>{suppression.recipient}</td>
								<td>
									{#if suppression.campaignSeriesName}
										{suppression.campaignSeriesName}
									{:else}
										<span class="dim">{t('All studies')}</span>
									{/if}
								</td>
								<td>{t(humanizeToken(suppression.reason))}</td>
								<td>{t(humanizeToken(suppression.source))}</td>
								<td class="datum">{formatDateTime(suppression.createdAt)}</td>
								<td>
									<span class={suppression.active ? 'chip chip-danger' : 'chip'}>
										{suppression.active ? t('Suppressed') : t('Released')}
									</span>
								</td>
								<td class="verbs">
									{#if suppression.active}
										<button class="dl" disabled={suppressBusy} onclick={() => release(suppression.id)}>{t('Release')}</button>
									{/if}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{:else}
			<p class="quiet">{t('No suppressed addresses.')}</p>
		{/if}
	</section>
</LoadState>

<style>
	.head {
		margin-bottom: 2rem;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.375rem;
	}

	.hint {
		margin-top: 0.5rem;
		font-size: 0.875rem;
		color: var(--color-ink-2);
		max-width: 62ch;
	}

	.note {
		margin-bottom: 1rem;
		font-size: 0.875rem;
		color: var(--color-stain);
	}

	.block {
		margin-bottom: 3rem;
	}

	.block-head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 1rem;
		margin-bottom: 0.5rem;
	}

	.counts {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.quiet {
		margin-top: 0.75rem;
		font-size: 0.9375rem;
		color: var(--color-ink-3);
	}

	.dim {
		color: var(--color-ink-3);
	}

	.panel.author {
		padding: 1.25rem;
		margin: 0.75rem 0 1rem;
	}

	.author-row {
		display: flex;
		gap: 0.75rem;
		flex-wrap: wrap;
		margin-top: 0.75rem;
	}

	.author-row select,
	.author-row input {
		font: inherit;
		padding: 0.5rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
		min-width: 16rem;
	}

	.suppress-form {
		margin-bottom: 1rem;
	}

	.table-wrap {
		overflow-x: auto;
		margin-top: 0.75rem;
	}

	table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.875rem;
	}

	th {
		text-align: left;
		font-size: 0.6875rem;
		letter-spacing: 0.08em;
		text-transform: uppercase;
		color: var(--color-ink-3);
		font-weight: 560;
		padding: 0.5rem 0.75rem 0.5rem 0;
		border-bottom: 1px solid var(--color-line-2);
	}

	td {
		padding: 0.625rem 0.75rem 0.625rem 0;
		border-bottom: 1px solid var(--color-line);
		vertical-align: baseline;
	}

	th.num,
	td.num {
		text-align: right;
	}

	.verbs {
		white-space: nowrap;
		text-align: right;
	}

	.dl {
		font: inherit;
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-stain);
		background: none;
		border: 0;
		cursor: pointer;
		padding: 0 0 0 0.75rem;
	}

	.dl.danger {
		color: var(--color-danger);
	}

	.dl:disabled {
		opacity: 0.5;
	}

	.target-kind {
		display: block;
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}
</style>
