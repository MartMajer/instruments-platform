import type { CampaignSeriesOperationsWorkspaceResponse } from '$lib/api/product';
import type { ProductReadModelBadgeStatus } from './view-models';

export type SelectedSeriesOperationsWorkflowActionId =
	| 'readiness'
	| 'launch'
	| 'openLink'
	| 'monitor'
	| 'close';

export type SelectedSeriesOperationsWorkflowLocalState = {
	readinessReady?: boolean;
	launched?: boolean;
	openLinkCreated?: boolean;
	closed?: boolean;
};

export type SelectedSeriesOperationsWorkflowAction = {
	id: SelectedSeriesOperationsWorkflowActionId;
	step: string;
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	available: boolean;
	disabledReason: string | null;
};

export type SelectedSeriesOperationsPathStepState = 'done' | 'current' | 'blocked';

export type SelectedSeriesOperationsPathStep = SelectedSeriesOperationsWorkflowAction & {
	pathState: SelectedSeriesOperationsPathStepState;
};

export type SelectedSeriesOperationsPath = {
	steps: SelectedSeriesOperationsPathStep[];
	currentActionId: SelectedSeriesOperationsWorkflowActionId;
	currentAction: SelectedSeriesOperationsWorkflowAction;
	completedCount: number;
	totalCount: number;
};

export function toSelectedSeriesOperationsWorkflowActions(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	localState: SelectedSeriesOperationsWorkflowLocalState = {}
): SelectedSeriesOperationsWorkflowAction[] {
	const selectedCampaign = workspace.selectedCampaign;
	const hasCampaign = Boolean(selectedCampaign);
	const isLive = selectedCampaign?.status === 'live';
	const isClosed = selectedCampaign?.status === 'closed' || Boolean(selectedCampaign?.closedAt);
	const closed = Boolean(localState.closed || isClosed);
	const closeable = isLive || Boolean(localState.launched);
	const hasLaunchEvidence = Boolean(
		selectedCampaign?.latestLaunchSnapshotId || selectedCampaign?.latestLaunchAt
	);
	const launched = Boolean(localState.launched || isLive || closed || hasLaunchEvidence);
	const readinessReady = Boolean(localState.readinessReady || launched);
	const hasRespondentAccess = Boolean(
		localState.openLinkCreated ||
			selectedCampaign?.openLinkAssignmentCount ||
			selectedCampaign?.queuedInvitationCount ||
			selectedCampaign?.sentInvitationCount
	);
	const hasResponseActivity = Boolean(
		workspace.summary.startedResponseCount ||
			workspace.summary.draftResponseCount ||
			workspace.summary.submittedResponseCount
	);

	return [
		{
			id: 'readiness',
			step: 'Step 1',
			title: 'Pre-launch check',
			description: 'Confirm the questionnaire, scoring, audience, and policies are ready.',
			status: !hasCampaign ? 'not_available' : readinessReady ? 'ready' : 'pending',
			available: hasCampaign,
			disabledReason: hasCampaign
				? null
				: 'Create a collection wave in setup before checking readiness.'
		},
		{
			id: 'launch',
			step: 'Step 2',
			title: 'Start collection',
			description: 'Open this wave for responses and record the setup used for reporting.',
			status: toLaunchStatus(hasCampaign, isLive, closed, launched, localState),
			available: hasCampaign && readinessReady && !launched && !closed,
			disabledReason: toLaunchDisabledReason(hasCampaign, isLive, closed, launched, localState)
		},
		{
			id: 'openLink',
			step: 'Step 3',
			title: 'Respondent access',
			description: 'Create the entry link respondents use to answer this wave.',
			status: !hasCampaign
				? 'not_available'
				: launched && hasRespondentAccess
					? 'ready'
					: launched
						? 'pending'
						: 'blocked',
			available: launched && !closed,
			disabledReason: !launched
				? 'Start collection before creating respondent access.'
				: closed
					? 'This collection wave is closed.'
					: null
		},
		{
			id: 'monitor',
			step: 'Step 4',
			title: 'Monitor responses',
			description: 'Track starts, drafts, submissions, and report readiness while collection runs.',
			status: !hasCampaign
				? 'not_available'
				: closed
					? 'closed'
					: launched && hasResponseActivity
						? 'ready'
						: launched
							? 'pending'
							: 'blocked',
			available: launched,
			disabledReason: !launched ? 'Start collection before monitoring responses.' : null
		},
		{
			id: 'close',
			step: 'Step 5',
			title: 'Close collection',
			description: 'Stop accepting new responses while keeping submitted data reportable.',
			status: !hasCampaign ? 'not_available' : closed ? 'closed' : closeable ? 'pending' : 'blocked',
			available: closeable && !closed,
			disabledReason: !hasCampaign
				? 'Create a collection wave before closing collection.'
				: closed
					? null
					: closeable
						? null
						: 'Only a live collection wave can be closed.'
		}
	];
}

export function toSelectedSeriesOperationsPath(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	localState: SelectedSeriesOperationsWorkflowLocalState = {}
): SelectedSeriesOperationsPath {
	const actions = toSelectedSeriesOperationsWorkflowActions(workspace, localState);
	const selectedCampaign = workspace.selectedCampaign;
	const isLive = selectedCampaign?.status === 'live';
	const isClosed = selectedCampaign?.status === 'closed' || Boolean(selectedCampaign?.closedAt);
	const closed = Boolean(localState.closed || isClosed);
	const hasLaunchEvidence = Boolean(
		selectedCampaign?.latestLaunchSnapshotId || selectedCampaign?.latestLaunchAt
	);
	const launched = Boolean(localState.launched || isLive || closed || hasLaunchEvidence);
	const readinessReady = Boolean(localState.readinessReady || launched);
	const hasRespondentAccess = Boolean(
		localState.openLinkCreated ||
			selectedCampaign?.openLinkAssignmentCount ||
			selectedCampaign?.queuedInvitationCount ||
			selectedCampaign?.sentInvitationCount
	);
	const hasResponseActivity = Boolean(
		workspace.summary.startedResponseCount ||
			workspace.summary.draftResponseCount ||
			workspace.summary.submittedResponseCount
	);
	const doneByActionId: Record<SelectedSeriesOperationsWorkflowActionId, boolean> = {
		readiness: readinessReady,
		launch: launched,
		openLink: hasRespondentAccess,
		monitor: closed || hasResponseActivity,
		close: closed
	};
	const currentAction =
		actions.find((action) => !doneByActionId[action.id] && action.available) ??
		actions.find((action) => !doneByActionId[action.id]) ??
		actions.at(-1) ??
		actions[0];
	const steps = actions.map((action) => ({
		...action,
		pathState: toPathStepState(action.id, currentAction.id, doneByActionId)
	}));

	return {
		steps,
		currentActionId: currentAction.id,
		currentAction,
		completedCount: steps.filter((step) => step.pathState === 'done').length,
		totalCount: steps.length
	};
}

function toLaunchStatus(
	hasCampaign: boolean,
	isLive: boolean,
	closed: boolean,
	launched: boolean,
	localState: SelectedSeriesOperationsWorkflowLocalState
): ProductReadModelBadgeStatus {
	if (!hasCampaign) {
		return 'not_available';
	}

	if (closed) {
		return 'closed';
	}

	if (isLive) {
		return 'live';
	}

	if (launched || localState.launched) {
		return 'ready';
	}

	return localState.readinessReady ? 'pending' : 'blocked';
}

function toLaunchDisabledReason(
	hasCampaign: boolean,
	isLive: boolean,
	closed: boolean,
	launched: boolean,
	localState: SelectedSeriesOperationsWorkflowLocalState
) {
	if (!hasCampaign) {
		return 'Create a collection wave before starting collection.';
	}

	if (closed) {
		return 'This collection wave is closed.';
	}

	if (isLive) {
		return 'Collection is already live.';
	}

	if (launched || localState.launched) {
		return 'Collection was started in this session.';
	}

	return localState.readinessReady
		? null
		: 'Run the pre-launch check. If it says Blocked, open Setup and finish the listed items first.';
}

function toPathStepState(
	actionId: SelectedSeriesOperationsWorkflowActionId,
	currentActionId: SelectedSeriesOperationsWorkflowActionId,
	doneByActionId: Record<SelectedSeriesOperationsWorkflowActionId, boolean>
): SelectedSeriesOperationsPathStepState {
	if (doneByActionId[actionId]) {
		return 'done';
	}

	if (actionId === currentActionId) {
		return 'current';
	}

	return 'blocked';
}
