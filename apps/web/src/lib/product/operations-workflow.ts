import type { CampaignSeriesOperationsWorkspaceResponse } from '$lib/api/product';
import type { ProductReadModelBadgeStatus } from './view-models';

export type SelectedSeriesOperationsWorkflowActionId =
	| 'readiness'
	| 'launch'
	| 'openLink'
	| 'invitations'
	| 'delivery'
	| 'close';

export type SelectedSeriesOperationsWorkflowLocalState = {
	readinessReady?: boolean;
	launched?: boolean;
	openLinkCreated?: boolean;
	invitationsQueued?: boolean;
	deliveryProcessed?: boolean;
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
	const isIdentified = selectedCampaign?.responseIdentityMode === 'identified';
	const supportsAnonymousInvitationFlow = selectedCampaign?.responseIdentityMode === 'anonymous';
	const skipsInvitationFlow = hasCampaign && !supportsAnonymousInvitationFlow;
	const closed = Boolean(localState.closed || isClosed);
	const closeable = isLive || Boolean(localState.launched);
	const hasLaunchEvidence = Boolean(
		selectedCampaign?.latestLaunchSnapshotId || selectedCampaign?.latestLaunchAt
	);
	const launched = Boolean(localState.launched || isLive || closed || hasLaunchEvidence);
	const readinessReady = Boolean(localState.readinessReady || launched);
	const hasOpenLink = Boolean(
		localState.openLinkCreated || selectedCampaign?.openLinkAssignmentCount
	);
	const hasQueuedInvitations = Boolean(
		localState.invitationsQueued || selectedCampaign?.queuedInvitationCount
	);
	const deliveryProcessed = Boolean(
		localState.deliveryProcessed || selectedCampaign?.deliveryAttemptCount
	);
	const entryTitle = isIdentified ? 'Identified entry' : 'Open-link entry';
	const entryDescription = isIdentified
		? 'Create an identified entry token for the selected assignment.'
		: 'Create a public entry link for the selected campaign.';

	return [
		{
			id: 'readiness',
			step: 'Step 1',
			title: 'Launch readiness',
			description: 'Run launch-readiness diagnostics for the selected campaign.',
			status: !hasCampaign ? 'not_available' : readinessReady ? 'ready' : 'pending',
			available: hasCampaign,
			disabledReason: hasCampaign ? null : 'Create or select a campaign before running operations.'
		},
		{
			id: 'launch',
			step: 'Step 2',
			title: 'Launch campaign',
			description: 'Freeze launch provenance and move the selected campaign live.',
			status: toLaunchStatus(hasCampaign, isLive, closed, launched, localState),
			available: hasCampaign && readinessReady && !launched && !closed,
			disabledReason: toLaunchDisabledReason(hasCampaign, isLive, closed, launched, localState)
		},
		{
			id: 'openLink',
			step: 'Step 3',
			title: entryTitle,
			description: entryDescription,
			status: !hasCampaign
				? 'not_available'
				: launched && hasOpenLink
					? 'ready'
					: launched
						? 'pending'
						: 'blocked',
			available: launched && !closed,
			disabledReason: !launched
				? 'Launch the selected campaign before creating an open link.'
				: closed
					? 'Selected campaign is closed.'
					: null
		},
		{
			id: 'invitations',
			step: 'Step 4',
			title: 'Invitation batch',
			description: 'Queue email invitation intents for the selected campaign.',
			status: !hasCampaign || skipsInvitationFlow
				? 'not_available'
				: launched && hasQueuedInvitations
					? 'ready'
					: launched
						? 'pending'
						: 'blocked',
			available: launched && !closed && supportsAnonymousInvitationFlow,
			disabledReason: skipsInvitationFlow
				? 'Email invitation batches support anonymous campaigns only.'
				: !launched
				? 'Launch the selected campaign before queuing invitations.'
				: closed
					? 'Selected campaign is closed.'
					: null
		},
		{
			id: 'delivery',
			step: 'Step 5',
			title: 'Local delivery',
			description: 'Process queued invitations through the local/dev delivery boundary.',
			status: !hasCampaign || skipsInvitationFlow
				? 'not_available'
				: deliveryProcessed
					? 'ready'
					: hasQueuedInvitations
					? 'pending'
					: 'blocked',
			available: hasQueuedInvitations && !closed && supportsAnonymousInvitationFlow,
			disabledReason: skipsInvitationFlow
				? 'Local delivery requires queued anonymous invitations.'
				: !hasQueuedInvitations
				? 'Queue invitations before processing local delivery.'
				: closed
					? 'Selected campaign is closed.'
					: null
		},
		{
			id: 'close',
			step: 'Step 6',
			title: 'Close campaign',
			description: 'Stop public collection while keeping submitted data reportable.',
			status: !hasCampaign ? 'not_available' : closed ? 'closed' : closeable ? 'pending' : 'blocked',
			available: closeable && !closed,
			disabledReason: !hasCampaign
				? 'Launch the selected campaign before closing collection.'
				: closed
				? null
				: closeable
					? null
					: 'Only live campaigns can be closed.'
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
	const supportsAnonymousInvitationFlow = selectedCampaign?.responseIdentityMode === 'anonymous';
	const skipsInvitationFlow = Boolean(selectedCampaign) && !supportsAnonymousInvitationFlow;
	const closed = Boolean(localState.closed || isClosed);
	const hasLaunchEvidence = Boolean(
		selectedCampaign?.latestLaunchSnapshotId || selectedCampaign?.latestLaunchAt
	);
	const launched = Boolean(localState.launched || isLive || closed || hasLaunchEvidence);
	const readinessReady = Boolean(localState.readinessReady || launched);
	const hasOpenLink = Boolean(
		localState.openLinkCreated || selectedCampaign?.openLinkAssignmentCount
	);
	const hasQueuedInvitations = Boolean(
		localState.invitationsQueued || selectedCampaign?.queuedInvitationCount
	);
	const deliveryProcessed = Boolean(
		localState.deliveryProcessed || selectedCampaign?.deliveryAttemptCount
	);
	const doneByActionId: Record<SelectedSeriesOperationsWorkflowActionId, boolean> = {
		readiness: readinessReady,
		launch: launched,
		openLink: hasOpenLink,
		invitations: skipsInvitationFlow || hasQueuedInvitations,
		delivery: skipsInvitationFlow || deliveryProcessed,
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
		return 'Create or select a campaign before launch.';
	}

	if (closed) {
		return 'Selected campaign is closed.';
	}

	if (isLive) {
		return 'Selected campaign is already live.';
	}

	if (launched || localState.launched) {
		return 'Selected campaign was launched in this session.';
	}

	return localState.readinessReady ? null : 'Run launch readiness and resolve blockers first.';
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
