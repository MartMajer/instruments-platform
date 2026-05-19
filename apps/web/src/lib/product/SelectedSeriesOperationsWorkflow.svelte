<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { CircleStop, LoaderCircle, RefreshCw, SearchCheck, Send } from 'lucide-svelte';
	import type {
		CampaignCloseStateResponse,
		CampaignSeriesOperationsWorkspaceResponse
	} from '$lib/api/product';
	import type {
		CampaignIdentifiedEntryResponse,
		CampaignOpenLinkResponse,
		LaunchCampaignResponse,
		LaunchReadinessResponse
	} from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import {
		toSelectedSeriesOperationsPath,
		type SelectedSeriesOperationsPathStep,
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
	const countFormatter = new Intl.NumberFormat('hr-HR');
	const dateTimeFormatter = new Intl.DateTimeFormat('hr-HR', {
		day: '2-digit',
		month: '2-digit',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		hour12: false
	});

	let readinessResult = $state<LaunchReadinessResponse | null>(null);
	let launchResult = $state<LaunchCampaignResponse | null>(null);
	let openLinkResult = $state<CampaignOpenLinkResponse | null>(null);
	let identifiedEntryResult = $state<CampaignIdentifiedEntryResponse | null>(null);
	let closeResult = $state<CampaignCloseStateResponse | null>(null);
	let refreshWarning = $state<string | null>(null);
	let activeActionId = $state<SelectedSeriesOperationsWorkflowActionId | null>(null);
	let actionStates = $state<Record<SelectedSeriesOperationsWorkflowActionId, StepState>>({
		readiness: 'idle',
		launch: 'idle',
		openLink: 'idle',
		monitor: 'idle',
		close: 'idle'
	});
	let actionErrors = $state<Record<SelectedSeriesOperationsWorkflowActionId, string | null>>({
		readiness: null,
		launch: null,
		openLink: null,
		monitor: null,
		close: null
	});

	const selectedCampaign = $derived(workspace.selectedCampaign);
	const selectedCampaignIsIdentified = $derived(
		selectedCampaign?.responseIdentityMode === 'identified'
	);
	const localState = $derived({
		readinessReady: readinessResult?.ready === true,
		launched: Boolean(launchResult),
		openLinkCreated: Boolean(openLinkResult || identifiedEntryResult),
		closed: Boolean(closeResult)
	});
	const operationsPath = $derived(toSelectedSeriesOperationsPath(workspace, localState));
	const workflowActions = $derived(operationsPath.steps);
	const recommendedAction = $derived(operationsPath.currentAction);
	const activeAction = $derived(
		workflowActions.find((action) => action.id === activeActionId) ?? recommendedAction
	);
	const activeActionIndex = $derived(
		Math.max(
			0,
			workflowActions.findIndex((action) => action.id === activeAction.id)
		)
	);
	const respondentEntry = $derived(identifiedEntryResult ?? openLinkResult);
	const latestResponseActivity = $derived(
		workspace.summary.latestResponseSubmittedAt ?? workspace.summary.latestResponseStartedAt ?? null
	);
	const readinessIssueGuidance = $derived(
		readinessResult?.issues.length
			? readinessResult.issues.map((issue) => toReadinessIssueGuidance(issue.message))
			: []
	);

	async function checkLaunchReadiness() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				readiness: 'Create a collection wave before running the pre-launch check.'
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
				launch: 'Create a collection wave before starting collection.'
			};
			return;
		}

		const result = await runAction('launch', () => setupApi.launchCampaign(selectedCampaign.id));

		if (result) {
			launchResult = result;
			openLinkResult = null;
			identifiedEntryResult = null;
			closeResult = null;
		}
	}

	async function createRespondentAccess() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				openLink: 'Create a collection wave before creating respondent access.'
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

	async function refreshCollectionStatus() {
		actionStates = { ...actionStates, monitor: 'submitting' };
		actionErrors = { ...actionErrors, monitor: null };
		refreshWarning = null;

		try {
			const refreshed = await onWorkspaceRefresh?.();
			if (refreshed === false) {
				throw new Error('Collection status could not be refreshed.');
			}
			actionStates = { ...actionStates, monitor: 'succeeded' };
		} catch (error) {
			actionStates = { ...actionStates, monitor: 'failed' };
			actionErrors = {
				...actionErrors,
				monitor: toProductApiErrorMessage(error, 'Collection status refresh failed.')
			};
		}
	}

	async function closeCampaign() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				close: 'Create a collection wave before closing collection.'
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
				refreshWarning = 'The action was saved, but this collection view could not refresh.';
			}
			return result;
		} catch (error) {
			actionStates = { ...actionStates, [actionId]: 'failed' };
			actionErrors = {
				...actionErrors,
				[actionId]: toProductApiErrorMessage(error, 'Collection action failed.')
			};
			return null;
		}
	}

	function selectAction(id: SelectedSeriesOperationsWorkflowActionId) {
		activeActionId = id;
	}

	function selectRelativeAction(offset: number) {
		const nextAction = workflowActions[activeActionIndex + offset];
		if (nextAction) {
			activeActionId = nextAction.id;
		}
	}

	function workflowAction(id: SelectedSeriesOperationsWorkflowActionId) {
		return workflowActions.find((action) => action.id === id) ?? workflowActions[0];
	}

	function isActionDisabled(id: SelectedSeriesOperationsWorkflowActionId) {
		const action = workflowAction(id);
		return !action.available || actionStates[id] === 'submitting';
	}

	function displayedPathState(action: SelectedSeriesOperationsPathStep) {
		return action.id === activeAction.id ? 'current' : action.pathState;
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

	function formatCount(value: number | null | undefined) {
		return countFormatter.format(value ?? 0);
	}

	function formatDateTime(value: string | null | undefined) {
		if (!value) {
			return 'Not available';
		}

		const date = new Date(value);
		if (Number.isNaN(date.getTime())) {
			return value;
		}

		return dateTimeFormatter.format(date);
	}

	function humanize(value: string | null | undefined) {
		return value ? value.replaceAll('_', ' ') : 'Not available';
	}

	function toReadinessIssueGuidance(message: string) {
		const normalized = message.toLowerCase();

		if (normalized.includes('campaign') && normalized.includes('template')) {
			return 'Go back to Setup and attach a questionnaire template to this collection wave.';
		}

		if (normalized.includes('template')) {
			return 'Go back to Setup and finish the questionnaire/template step.';
		}

		if (normalized.includes('scoring')) {
			return 'Go back to Setup and finish the result/scoring rule step.';
		}

		if (normalized.includes('policy') || normalized.includes('consent') || normalized.includes('retention') || normalized.includes('disclosure')) {
			return 'Go back to Setup and complete the study policy records.';
		}

		if (normalized.includes('audience') || normalized.includes('respondent')) {
			return 'Go back to Setup and define who can answer this collection wave.';
		}

		return message;
	}
</script>

<section class="product-panel" role="group" aria-label="Collection workflow">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Study collection</p>
			<h3 class="product-title">Collect responses</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Start the wave, share respondent access, monitor submissions, and close collection when
				the study is finished.
			</p>
		</div>
		<div class="grid justify-items-end gap-2">
			<StatusBadge status={recommendedAction.status} label={recommendedAction.title} />
			<p class="text-xs font-semibold text-[var(--color-text-muted)]">
				{operationsPath.completedCount}/{operationsPath.totalCount} steps complete
			</p>
		</div>
	</div>

	{#if refreshWarning}
		<p class="error-line">{refreshWarning}</p>
	{/if}

	<div class="setup-path" role="list" aria-label="Collection path">
		{#each operationsPath.steps as action, index (action.id)}
			<div role="listitem">
				<button
					type="button"
					class="setup-path__item"
					data-state={displayedPathState(action)}
					aria-current={displayedPathState(action) === 'current' ? 'step' : undefined}
					onclick={() => selectAction(action.id)}
				>
					<span class="setup-path__marker">{index + 1}</span>
					<span class="setup-path__content">
						<span class="setup-path__title">{action.title}</span>
						<span class="setup-path__description">{action.description}</span>
					</span>
					<span class="setup-path__state">{pathStateLabel(displayedPathState(action))}</span>
				</button>
			</div>
		{/each}
	</div>

	{#if !canManageSetup}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">Read-only access</strong>
			<span>Collection actions require workspace management access.</span>
		</p>
	{:else}
		<article class="record-row setup-current-task" role="region" aria-label="Collection step">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">{activeAction.step}</p>
					<h4 class="setup-current-task__title">{activeAction.title}</h4>
					<p class="text-sm text-[var(--color-text-muted)]">{activeAction.description}</p>
				</div>
				<StatusBadge status={activeAction.status} />
			</div>
			{#if activeAction.disabledReason}
				<p class="text-sm text-[var(--color-text-muted)]">{activeAction.disabledReason}</p>
			{/if}

			<div class="setup-current-task__body">
				{#if activeAction.id === 'readiness'}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						Use this before opening collection. The check confirms that the questionnaire, scoring,
						audience, and policy setup can support responses and reporting.
					</p>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Collection wave</dt>
							<dd class="record-field__value">{selectedCampaign?.name ?? 'Missing'}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Setup check</dt>
							<dd class="record-field__value">
								{readinessResult ? (readinessResult.ready ? 'Ready' : 'Blocked') : 'Not checked'}
							</dd>
						</div>
					</dl>
					{#if readinessResult?.issues.length}
						<div class="record-row" aria-label="Readiness issues">
							<h5 class="record-row__title">Before collection can start</h5>
							<p class="text-sm text-[var(--color-text-muted)]">
								Fix these setup items, then run the pre-launch check again.
							</p>
							<ul class="grid gap-2">
								{#each readinessIssueGuidance as guidance}
									<li class="text-sm text-[var(--color-text-muted)]">{guidance}</li>
								{/each}
							</ul>
						</div>
					{/if}
					{@render ActionFooter({
						id: 'readiness',
						label: 'Run pre-launch check',
						resultLabel: 'Setup check',
						resultValue: readinessResult ? (readinessResult.ready ? 'Ready' : 'Blocked') : null,
						onclick: checkLaunchReadiness
					})}
				{:else if activeAction.id === 'launch'}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						Starting collection opens the selected wave for responses and records the setup version
						that reports will use later.
					</p>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Collection wave</dt>
							<dd class="record-field__value">{selectedCampaign?.name ?? 'Missing'}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Status</dt>
							<dd class="record-field__value">
								{humanize(launchResult?.status ?? selectedCampaign?.status)}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Started</dt>
							<dd class="record-field__value">
								{formatDateTime(selectedCampaign?.latestLaunchAt)}
							</dd>
						</div>
					</dl>
					{@render ActionFooter({
						id: 'launch',
						label: 'Start collection',
						resultLabel: 'Collection',
						resultValue: launchResult?.status ? humanize(launchResult.status) : null,
						onclick: launchCampaign
					})}
				{:else if activeAction.id === 'openLink'}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						Create the respondent entry link for this collection wave. Share it with the intended
						respondents once collection is live.
					</p>
					{#if respondentEntry}
						<div class="record-row">
							<div class="record-row__header">
								<h5 class="record-row__title">Respondent link ready</h5>
								<span class="step-pill" data-state="succeeded">Created</span>
							</div>
							<p class="result-line">
								<span>Share link</span>
								<code>{respondentEntry.respondentPath}</code>
							</p>
						</div>
					{/if}
					{@render ActionFooter({
						id: 'openLink',
						label: selectedCampaignIsIdentified ? 'Create identified entry' : 'Create respondent link',
						resultLabel: 'Share link',
						resultValue: respondentEntry?.respondentPath ?? null,
						onclick: createRespondentAccess
					})}
				{:else if activeAction.id === 'monitor'}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						Watch response movement while collection is open. These numbers refresh from the
						workspace state and do not change study setup.
					</p>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Started</dt>
							<dd class="record-field__value">{formatCount(workspace.summary.startedResponseCount)}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">In progress</dt>
							<dd class="record-field__value">{formatCount(workspace.summary.draftResponseCount)}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Submitted</dt>
							<dd class="record-field__value">
								{formatCount(workspace.summary.submittedResponseCount)}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Latest activity</dt>
							<dd class="record-field__value">{formatDateTime(latestResponseActivity)}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Report readiness</dt>
							<dd class="record-field__value">{humanize(workspace.summary.reportVisibilityStatus)}</dd>
						</div>
					</dl>
					{@render ActionFooter({
						id: 'monitor',
						label: 'Refresh status',
						resultLabel: 'Latest activity',
						resultValue: formatDateTime(latestResponseActivity),
						onclick: refreshCollectionStatus
					})}
				{:else}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						Close collection when the response window is finished. Submitted responses remain
						available for scoring and reports.
					</p>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Collection wave</dt>
							<dd class="record-field__value">{selectedCampaign?.name ?? 'Missing'}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Status</dt>
							<dd class="record-field__value">
								{humanize(closeResult?.status ?? selectedCampaign?.status)}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Closed</dt>
							<dd class="record-field__value">
								{formatDateTime(closeResult?.closedAt ?? selectedCampaign?.closedAt)}
							</dd>
						</div>
					</dl>
					{@render ActionFooter({
						id: 'close',
						label: 'Close collection',
						resultLabel: 'Closed',
						resultValue: closeResult?.closedAt
							? formatDateTime(closeResult.closedAt)
							: selectedCampaign?.closedAt
								? formatDateTime(selectedCampaign.closedAt)
								: null,
						onclick: closeCampaign
					})}
				{/if}

				<div class="action-row" aria-label="Collection step navigation">
					<button
						type="button"
						class="secondary-button"
						disabled={activeActionIndex === 0}
						onclick={() => selectRelativeAction(-1)}
					>
						Previous step
					</button>
					<button
						type="button"
						class="secondary-button"
						disabled={activeActionIndex >= workflowActions.length - 1}
						onclick={() => selectRelativeAction(1)}
					>
						Next step
					</button>
				</div>
			</div>
		</article>

	{/if}
</section>

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
			{:else if id === 'monitor'}
				<RefreshCw size={17} aria-hidden="true" />
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
	{#if value && value !== 'Not available'}
		<p class="result-line">
			<span>{label}</span>
			<code>{value}</code>
		</p>
	{/if}
{/snippet}
