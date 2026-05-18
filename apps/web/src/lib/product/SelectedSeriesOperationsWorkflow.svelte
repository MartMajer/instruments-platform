<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { CircleStop, LoaderCircle, SearchCheck, Send } from 'lucide-svelte';
	import type {
		CampaignCloseStateResponse,
		CampaignSeriesOperationsWorkspaceResponse
	} from '$lib/api/product';
	import type {
		CampaignIdentifiedEntryResponse,
		CampaignInvitationBatchResponse,
		CampaignOpenLinkResponse,
		LaunchCampaignResponse,
		LaunchReadinessResponse,
		ProcessCampaignEmailDeliveriesResponse
	} from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import {
		toSelectedSeriesOperationsPath,
		type SelectedSeriesOperationsWorkflowActionId
	} from './operations-workflow';
	import { createProductApiFromEnv, createSetupApiFromEnv } from './route-state';
	import { toProductApiErrorMessage } from './view-models';

	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';

	let {
		workspace,
		canManageSetup = true,
		onWorkspaceRefresh
	}: {
		workspace: CampaignSeriesOperationsWorkspaceResponse;
		canManageSetup?: boolean;
		onWorkspaceRefresh?: () => Promise<boolean>;
	} = $props();

	const productApi = createProductApiFromEnv(env);
	const setupApi = createSetupApiFromEnv(env);
	const recipientSuffix = generateOperationsRunSuffix();

	let readinessResult = $state<LaunchReadinessResponse | null>(null);
	let launchResult = $state<LaunchCampaignResponse | null>(null);
	let openLinkResult = $state<CampaignOpenLinkResponse | null>(null);
	let identifiedEntryResult = $state<CampaignIdentifiedEntryResponse | null>(null);
	let invitationBatchResult = $state<CampaignInvitationBatchResponse | null>(null);
	let deliveryResult = $state<ProcessCampaignEmailDeliveriesResponse | null>(null);
	let closeResult = $state<CampaignCloseStateResponse | null>(null);
	let refreshWarning = $state<string | null>(null);
	let actionStates = $state<Record<SelectedSeriesOperationsWorkflowActionId, StepState>>({
		readiness: 'idle',
		launch: 'idle',
		openLink: 'idle',
		invitations: 'idle',
		delivery: 'idle',
		close: 'idle'
	});
	let actionErrors = $state<Record<SelectedSeriesOperationsWorkflowActionId, string | null>>({
		readiness: null,
		launch: null,
		openLink: null,
		invitations: null,
		delivery: null,
		close: null
	});

	const selectedCampaign = $derived(workspace.selectedCampaign);
	const localState = $derived({
		readinessReady: readinessResult?.ready === true,
		launched: Boolean(launchResult),
		openLinkCreated: Boolean(openLinkResult || identifiedEntryResult),
		invitationsQueued: Boolean(invitationBatchResult),
		deliveryProcessed: Boolean(deliveryResult),
		closed: Boolean(closeResult)
	});
	const operationsPath = $derived(toSelectedSeriesOperationsPath(workspace, localState));
	const workflowActions = $derived(operationsPath.steps);
	const currentAction = $derived(operationsPath.currentAction);
	const hasOperationResults = $derived(
		Boolean(
			readinessResult ||
			launchResult ||
			openLinkResult ||
			invitationBatchResult ||
			deliveryResult ||
			identifiedEntryResult ||
			closeResult
		)
	);
	const selectedCampaignIsIdentified = $derived(
		selectedCampaign?.responseIdentityMode === 'identified'
	);

	async function checkLaunchReadiness() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				readiness: 'Create or select a campaign before running operations.'
			};
			return;
		}

		const result = await runAction('readiness', () =>
			setupApi.getLaunchReadiness(selectedCampaign.id)
		);

		if (result) {
			readinessResult = result;
		}
	}

	async function launchCampaign() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				launch: 'Create or select a campaign before launch.'
			};
			return;
		}

		const result = await runAction('launch', () => setupApi.launchCampaign(selectedCampaign.id));

		if (result) {
			launchResult = result;
			openLinkResult = null;
			identifiedEntryResult = null;
			invitationBatchResult = null;
			deliveryResult = null;
			closeResult = null;
		}
	}

	async function createOpenLink() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				openLink: 'Create or select a campaign before creating an open link.'
			};
			return;
		}

		const result = await runAction('openLink', () =>
			selectedCampaignIsIdentified
				? setupApi.createCampaignIdentifiedEntry(selectedCampaign.id)
				: setupApi.createCampaignOpenLink(selectedCampaign.id)
		);

		if (result) {
			if (selectedCampaignIsIdentified) {
				identifiedEntryResult = result as CampaignIdentifiedEntryResponse;
				openLinkResult = null;
			} else {
				openLinkResult = result as CampaignOpenLinkResponse;
				identifiedEntryResult = null;
			}
		}
	}

	async function queueEmailInvitations() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				invitations: 'Create or select a campaign before queuing invitations.'
			};
			return;
		}

		const result = await runAction('invitations', () =>
			setupApi.createCampaignInvitationBatch(selectedCampaign.id, {
				recipients: [
					{ email: `ada.ops.${recipientSuffix}@example.com` },
					{ email: `bo.ops.${recipientSuffix}@example.com` }
				]
			})
		);

		if (result) {
			invitationBatchResult = result;
			deliveryResult = null;
		}
	}

	async function processLocalDelivery() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				delivery: 'Create or select a campaign before processing delivery.'
			};
			return;
		}

		const result = await runAction('delivery', () =>
			setupApi.processCampaignEmailDeliveries(selectedCampaign.id, { batchSize: 25 })
		);

		if (result) {
			deliveryResult = result;
		}
	}

	async function closeCampaign() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				close: 'Create or select a campaign before closing collection.'
			};
			return;
		}

		const result = await runAction('close', () =>
			productApi.closeCampaign(workspace.series.id, selectedCampaign.id, {})
		);

		if (result) {
			closeResult = result;
		}
	}

	async function runAction<T>(
		actionId: SelectedSeriesOperationsWorkflowActionId,
		action: () => Promise<T>
	) {
		actionStates = { ...actionStates, [actionId]: 'submitting' };
		actionErrors = { ...actionErrors, [actionId]: null };
		refreshWarning = null;

		try {
			const result = await action();
			actionStates = { ...actionStates, [actionId]: 'succeeded' };
			const refreshed = await onWorkspaceRefresh?.();
			if (refreshed === false) {
				refreshWarning = 'Operations action saved, but the operations workspace refresh failed.';
			}
			return result;
		} catch (error) {
			actionStates = { ...actionStates, [actionId]: 'failed' };
			actionErrors = {
				...actionErrors,
				[actionId]: toProductApiErrorMessage(error, 'Operations action failed.')
			};
			return null;
		}
	}

	function workflowAction(id: SelectedSeriesOperationsWorkflowActionId) {
		return workflowActions.find((action) => action.id === id) ?? workflowActions[0];
	}

	function isActionDisabled(id: SelectedSeriesOperationsWorkflowActionId) {
		const action = workflowAction(id);
		return !action.available || actionStates[id] === 'submitting';
	}

	function stepLabel(state: StepState) {
		if (state === 'submitting') {
			return 'Working';
		}

		if (state === 'succeeded') {
			return 'Saved';
		}

		if (state === 'failed') {
			return 'Failed';
		}

		return 'Ready';
	}

	function pathStateLabel(state: 'done' | 'current' | 'blocked') {
		if (state === 'done') {
			return 'Done';
		}

		if (state === 'current') {
			return 'Current';
		}

		return 'Blocked';
	}

	function generateOperationsRunSuffix() {
		if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
			return crypto.randomUUID().slice(0, 8).toLowerCase();
		}

		return Math.random().toString(36).slice(2, 10);
	}
</script>

<section class="product-panel" role="group" aria-label="Collection actions">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Collection workflow</p>
			<h3 class="product-title">Selected-series collection workflow</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				The path targets the selected campaign and keeps only the current collection task active.
			</p>
		</div>
		<div class="grid justify-items-end gap-2">
			<StatusBadge status="proof_only" label="Proof/local" />
			<p class="text-xs font-semibold text-[var(--color-text-muted)]">
				{operationsPath.completedCount}/{operationsPath.totalCount} done
			</p>
		</div>
	</div>

	{#if refreshWarning}
		<p class="error-line">{refreshWarning}</p>
	{/if}

	<div class="setup-path" role="list" aria-label="Collection action path">
		{#each operationsPath.steps as action, index (action.id)}
			<div
				class="setup-path__item"
				data-state={action.pathState}
				role="listitem"
				aria-current={action.pathState === 'current' ? 'step' : undefined}
			>
				<span class="setup-path__marker">{index + 1}</span>
				<div class="setup-path__content">
					<p class="setup-path__title">{action.title}</p>
					<p class="setup-path__description">{action.description}</p>
				</div>
				<span class="setup-path__state">{pathStateLabel(action.pathState)}</span>
			</div>
		{/each}
	</div>

	{#if !canManageSetup}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">Read-only access</strong>
			<span>Collection actions require setup management access.</span>
		</p>
	{:else}
		<article
			class="record-row setup-current-task"
			role="region"
			aria-label="Current collection task"
		>
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">{currentAction.step}</p>
					<h4 class="setup-current-task__title">Current collection task</h4>
					<p class="record-row__title">{currentAction.title}</p>
					<p class="text-sm text-[var(--color-text-muted)]">{currentAction.description}</p>
				</div>
				<StatusBadge status={currentAction.status} />
			</div>
			{#if currentAction.disabledReason}
				<p class="text-sm text-[var(--color-text-muted)]">{currentAction.disabledReason}</p>
			{/if}

			<div class="setup-current-task__body">
				{#if currentAction.id === 'readiness'}
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Selected campaign</dt>
							<dd class="record-field__value">{selectedCampaign?.name ?? 'Missing'}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Campaign id</dt>
							<dd class="record-field__value">{selectedCampaign?.id ?? 'Missing'}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Readiness</dt>
							<dd class="record-field__value">
								{readinessResult ? (readinessResult.ready ? 'ready' : 'blocked') : 'not checked'}
							</dd>
						</div>
					</dl>
					{#if readinessResult?.issues.length}
						<ul class="grid gap-2" aria-label="Launch readiness issues">
							{#each readinessResult.issues as issue}
								<li class="text-sm text-[var(--color-text-muted)]">
									<strong>{issue.code}</strong>: {issue.message}
								</li>
							{/each}
						</ul>
					{/if}
					{@render ActionFooter({
						id: 'readiness',
						label: 'Check launch readiness',
						resultLabel: 'Campaign',
						resultValue: readinessResult?.campaignId ?? selectedCampaign?.id ?? null,
						onclick: checkLaunchReadiness
					})}
				{:else if currentAction.id === 'launch'}
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Launch status</dt>
							<dd class="record-field__value">
								{launchResult?.status ?? selectedCampaign?.status ?? 'Missing'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Launch snapshot</dt>
							<dd class="record-field__value">
								{launchResult?.launchSnapshotId ??
									selectedCampaign?.latestLaunchSnapshotId ??
									'Not available'}
							</dd>
						</div>
					</dl>
					{@render ActionFooter({
						id: 'launch',
						label: 'Launch campaign',
						resultLabel: 'Launch snapshot',
						resultValue:
							launchResult?.launchSnapshotId ?? selectedCampaign?.latestLaunchSnapshotId ?? null,
						onclick: launchCampaign
					})}
				{:else if currentAction.id === 'openLink'}
					{#if openLinkResult || identifiedEntryResult}
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">Assignment</dt>
								<dd class="record-field__value">
									{(identifiedEntryResult ?? openLinkResult)?.assignmentId}
								</dd>
							</div>
							{#if identifiedEntryResult}
								<div class="record-field">
									<dt class="record-field__label">Subject</dt>
									<dd class="record-field__value">{identifiedEntryResult.subjectId}</dd>
								</div>
							{/if}
							<div class="record-field">
								<dt class="record-field__label">Proof/local path</dt>
								<dd class="record-field__value">
									{(identifiedEntryResult ?? openLinkResult)?.respondentPath}
								</dd>
							</div>
						</dl>
					{/if}
					{@render ActionFooter({
						id: 'openLink',
						label: selectedCampaignIsIdentified ? 'Create identified entry' : 'Create open link',
						resultLabel: 'Proof/local path',
						resultValue: (identifiedEntryResult ?? openLinkResult)?.respondentPath ?? null,
						onclick: createOpenLink
					})}
				{:else if currentAction.id === 'invitations'}
					{#if invitationBatchResult}
						{@render InvitationResults()}
					{/if}
					{@render ActionFooter({
						id: 'invitations',
						label: 'Queue email invitations',
						resultLabel: 'Queued invitations',
						resultValue: invitationBatchResult
							? String(invitationBatchResult.createdInvitationCount)
							: null,
						onclick: queueEmailInvitations
					})}
				{:else if currentAction.id === 'delivery'}
					{#if deliveryResult}
						{@render DeliveryResults()}
					{/if}
					{@render ActionFooter({
						id: 'delivery',
						label: 'Process local delivery',
						resultLabel: 'Sent',
						resultValue: deliveryResult ? String(deliveryResult.sentCount) : null,
						onclick: processLocalDelivery
					})}
				{:else}
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Campaign status</dt>
							<dd class="record-field__value">
								{closeResult?.status ?? selectedCampaign?.status ?? 'Missing'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Closed at</dt>
							<dd class="record-field__value">
								{closeResult?.closedAt ?? selectedCampaign?.closedAt ?? 'Not available'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Closed by</dt>
							<dd class="record-field__value">
								{closeResult?.closedByUserId ?? selectedCampaign?.closedByUserId ?? 'Not available'}
							</dd>
						</div>
					</dl>
					{@render ActionFooter({
						id: 'close',
						label: 'Close campaign',
						resultLabel: 'Closed at',
						resultValue: closeResult?.closedAt ?? selectedCampaign?.closedAt ?? null,
						onclick: closeCampaign
					})}
				{/if}
			</div>
		</article>

		{#if hasOperationResults}
			<div class="record-list" aria-label="Latest operations results">
				{#if readinessResult}
					<article class="record-row">
						<div class="record-row__header">
							<h4 class="record-row__title">Launch readiness result</h4>
							<span class="step-pill" data-state={readinessResult.ready ? 'succeeded' : 'failed'}>
								{readinessResult.ready ? 'ready' : 'blocked'}
							</span>
						</div>
						{@render ResultLine({
							label: 'Campaign',
							value: readinessResult.campaignId
						})}
					</article>
				{/if}
				{#if launchResult}
					<article class="record-row">
						<div class="record-row__header">
							<h4 class="record-row__title">Launch result</h4>
							<span class="step-pill" data-state="succeeded">{launchResult.status}</span>
						</div>
						{@render ResultLine({
							label: 'Launch snapshot',
							value: launchResult.launchSnapshotId
						})}
					</article>
				{/if}
				{#if openLinkResult}
					<article class="record-row">
						<div class="record-row__header">
							<h4 class="record-row__title">Open-link result</h4>
							<span class="step-pill" data-state="succeeded">created</span>
						</div>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">Assignment</dt>
								<dd class="record-field__value">{openLinkResult.assignmentId}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Proof/local path</dt>
								<dd class="record-field__value">{openLinkResult.respondentPath}</dd>
							</div>
						</dl>
					</article>
				{/if}
				{#if invitationBatchResult}
					<article class="record-row">
						<div class="record-row__header">
							<h4 class="record-row__title">Invitation batch result</h4>
							<span class="step-pill" data-state="succeeded">
								{invitationBatchResult.createdInvitationCount} queued
							</span>
						</div>
						{@render InvitationResults()}
					</article>
				{/if}
				{#if deliveryResult}
					<article class="record-row">
						<div class="record-row__header">
							<h4 class="record-row__title">Local delivery result</h4>
							<span class="step-pill" data-state="succeeded">{deliveryResult.sentCount} sent</span>
						</div>
						{@render DeliveryResults()}
					</article>
				{/if}
				{#if closeResult}
					<article class="record-row">
						<div class="record-row__header">
							<h4 class="record-row__title">Close result</h4>
							<span class="step-pill" data-state="succeeded">{closeResult.status}</span>
						</div>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">Closed at</dt>
								<dd class="record-field__value">{closeResult.closedAt ?? 'Not available'}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Closed by</dt>
								<dd class="record-field__value">{closeResult.closedByUserId ?? 'Not available'}</dd>
							</div>
						</dl>
					</article>
				{/if}
				{#if identifiedEntryResult}
					<article class="record-row">
						<div class="record-row__header">
							<h4 class="record-row__title">Identified entry result</h4>
							<span class="step-pill" data-state="succeeded">created</span>
						</div>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">Assignment</dt>
								<dd class="record-field__value">{identifiedEntryResult.assignmentId}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Subject</dt>
								<dd class="record-field__value">{identifiedEntryResult.subjectId}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Proof/local path</dt>
								<dd class="record-field__value">{identifiedEntryResult.respondentPath}</dd>
							</div>
						</dl>
					</article>
				{/if}
			</div>
		{/if}
	{/if}
</section>

{#snippet InvitationResults()}
	<div class="record-list" aria-label="Queued invitation intents">
		{#each invitationBatchResult?.invitations ?? [] as invitation (invitation.notificationId)}
			<div class="record-row">
				<p class="font-semibold text-[var(--color-text)]">{invitation.recipient}</p>
				<p class="result-line">
					<span>Proof/local path</span>
					<code>{invitation.respondentPath}</code>
				</p>
				<span class="step-pill" data-state="succeeded">{invitation.status}</span>
			</div>
		{/each}
	</div>
{/snippet}

{#snippet DeliveryResults()}
	<div class="record-list" aria-label="Local delivery">
		{#each deliveryResult?.deliveries ?? [] as delivery (delivery.notificationId)}
			<div class="record-row">
				<div class="record-row__header">
					<div>
						<p class="font-semibold text-[var(--color-text)]">{delivery.recipient}</p>
						<p class="text-xs text-[var(--color-text-muted)]">{delivery.provider}</p>
					</div>
					<span class="step-pill" data-state={delivery.status === 'sent' ? 'succeeded' : 'failed'}>
						{delivery.status}
					</span>
				</div>
				{#if delivery.respondentPath ?? delivery.providerMessageId}
					<p class="result-line">
						<span>Proof/local output</span>
						<code>{delivery.respondentPath ?? delivery.providerMessageId}</code>
					</p>
				{/if}
				{#if delivery.error}
					<p class="error-line">{delivery.error}</p>
				{/if}
			</div>
		{/each}
	</div>
{/snippet}

{#snippet ActionFooter({
	id,
	label,
	resultLabel,
	resultValue,
	onclick
}: {
	id: SelectedSeriesOperationsWorkflowActionId;
	label: string;
	resultLabel: string;
	resultValue: string | null | undefined;
	onclick: () => void | Promise<void>;
})}
	<div class="action-row">
		<button
			type="button"
			class="primary-button"
			disabled={isActionDisabled(id)}
			title={workflowAction(id).disabledReason ?? undefined}
			{onclick}
		>
			{#if actionStates[id] === 'submitting'}
				<LoaderCircle size={17} aria-hidden="true" />
			{:else if id === 'readiness'}
				<SearchCheck size={17} aria-hidden="true" />
			{:else if id === 'close'}
				<CircleStop size={17} aria-hidden="true" />
			{:else}
				<Send size={17} aria-hidden="true" />
			{/if}
			<span>{label}</span>
		</button>
		<p class="step-pill" data-state={actionStates[id]}>{stepLabel(actionStates[id])}</p>
		{@render ResultLine({ label: resultLabel, value: resultValue })}
	</div>
	{#if actionErrors[id]}
		<p class="error-line">{actionErrors[id]}</p>
	{/if}
{/snippet}

{#snippet ResultLine({ label, value }: { label: string; value: string | null | undefined })}
	{#if value}
		<p class="result-line">
			<span>{label}</span>
			<code>{value}</code>
		</p>
	{/if}
{/snippet}
