import type { CampaignSeriesOperationsWorkspaceResponse } from '$lib/api/product';
import type { EmailSuppressionResponse } from '$lib/api/setup';
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

export type SelectedSeriesCollectionStatusLaneId =
	| 'lifecycle'
	| 'responses'
	| 'audience'
	| 'reporting';

export type SelectedSeriesCollectionStatusLane = {
	id: SelectedSeriesCollectionStatusLaneId;
	label: string;
	title: string;
	status: ProductReadModelBadgeStatus;
	detail: string;
};

export type SelectedSeriesCollectionStatusSummary = {
	overallStatus: ProductReadModelBadgeStatus;
	overallLabel: string;
	headline: string;
	guidance: string;
	nextAction: string;
	lanes: SelectedSeriesCollectionStatusLane[];
};

export type RecipientSuppressionReviewRecipient = {
	email: string;
};

export type RecipientSuppressionReviewItem = {
	id: string;
	recipient: string;
	reason: string;
	reasonLabel: string;
	source: string;
	sourceLabel: string;
	note: string | null;
	createdAt: string;
};

export type RecipientSuppressionReview = {
	hasBlockedRecipients: boolean;
	blockedCount: number;
	headline: string;
	guidance: string;
	items: RecipientSuppressionReviewItem[];
};

export function toRecipientSuppressionReview(
	recipients: readonly RecipientSuppressionReviewRecipient[],
	suppressions: readonly EmailSuppressionResponse[]
): RecipientSuppressionReview {
	const activeSuppressionByRecipient = new Map(
		suppressions
			.filter((suppression) => suppression.active)
			.map((suppression) => [normalizeRecipientEmail(suppression.recipient), suppression])
	);
	const seenSuppressionIds = new Set<string>();
	const items = recipients
		.map((recipient) => activeSuppressionByRecipient.get(normalizeRecipientEmail(recipient.email)))
		.filter((suppression): suppression is EmailSuppressionResponse => Boolean(suppression))
		.filter((suppression) => {
			if (seenSuppressionIds.has(suppression.id)) {
				return false;
			}
			seenSuppressionIds.add(suppression.id);
			return true;
		})
		.map((suppression) => ({
			id: suppression.id,
			recipient: suppression.recipient,
			reason: suppression.reason,
			reasonLabel: emailSuppressionReasonLabel(suppression.reason),
			source: suppression.source,
			sourceLabel: emailSuppressionSourceLabel(suppression.source),
			note: suppression.note ?? null,
			createdAt: suppression.createdAt
		}));
	const blockedCount = items.length;

	return {
		hasBlockedRecipients: blockedCount > 0,
		blockedCount,
		headline:
			blockedCount === 1
				? '1 recipient is on the do-not-contact list'
				: blockedCount > 1
					? `${formatCount(blockedCount)} recipients are on the do-not-contact list`
					: 'No recipients are on the do-not-contact list',
		guidance:
			blockedCount > 0
				? 'Use another email, remove the recipient, or release the suppression only when you are sure future invitations are appropriate.'
				: 'Recipient list is not blocked by active do-not-contact records.',
		items
	};
}

export function emailSuppressionReasonLabel(reason: string | null | undefined) {
	switch (reason) {
		case 'recipient_unsubscribed':
			return 'Unsubscribed';
		case 'provider_bounced':
			return 'Bounced';
		case 'provider_complained':
			return 'Spam complaint';
		case 'operator_do_not_contact':
			return 'Manually suppressed';
		default:
			return titleCaseLabel(reason);
	}
}

export function emailSuppressionSourceLabel(source: string | null | undefined) {
	switch (source) {
		case 'respondent_invitation_link':
			return 'Invitation unsubscribe link';
		case 'provider_delivery_event':
			return 'Provider delivery event';
		case 'tenant_operator':
			return 'Workspace admin';
		default:
			return titleCaseLabel(source);
	}
}

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
			description: 'Confirm the questionnaire, results setup, recipients, and policies are ready.',
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
			title: 'Share access',
			description: 'Send saved invitations or create an open respondent link for this wave.',
			status: !hasCampaign
				? 'not_available'
				: launched && hasRespondentAccess
					? 'ready'
					: launched
						? 'pending'
						: 'blocked',
			available: launched && !closed,
			disabledReason: !launched
				? 'Start collection before preparing respondent access.'
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

export function toSelectedSeriesCollectionStatusSummary(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	localState: SelectedSeriesOperationsWorkflowLocalState = {}
): SelectedSeriesCollectionStatusSummary {
	const selectedCampaign = workspace.selectedCampaign;

	if (!selectedCampaign) {
		const lanes: SelectedSeriesCollectionStatusLane[] = [
			{
				id: 'lifecycle',
				label: 'Collection lifecycle',
				title: 'No wave selected',
				status: 'not_available',
				detail: 'Create or select a collection wave before collecting responses.'
			},
			{
				id: 'responses',
				label: 'Response progress',
				title: 'No responses yet',
				status: 'not_available',
				detail: 'Response counts appear after a wave is started.'
			},
			{
				id: 'audience',
				label: 'Access',
				title: 'No recipient access prepared',
				status: 'not_available',
				detail: 'Choose recipients or create respondent access after setup is ready.'
			},
			{
				id: 'reporting',
				label: 'Reporting readiness',
				title: 'Not available',
				status: 'not_available',
				detail: 'Reporting readiness appears after collection has a selected wave.'
			}
		];

		return {
			overallStatus: 'not_available',
			overallLabel: 'No wave selected',
			headline: 'Create a collection wave first',
			guidance: 'Collection starts after setup has a campaign wave.',
			nextAction: 'Open Setup and create a collection wave.',
			lanes
		};
	}

	const isLive = selectedCampaign.status === 'live';
	const isClosed = selectedCampaign.status === 'closed' || Boolean(selectedCampaign.closedAt);
	const hasLaunchEvidence = Boolean(
		selectedCampaign.latestLaunchSnapshotId || selectedCampaign.latestLaunchAt
	);
	const launched = Boolean(localState.launched || isLive || isClosed || hasLaunchEvidence);
	const closed = Boolean(localState.closed || isClosed);
	const submitted = workspace.summary.submittedResponseCount;
	const started = workspace.summary.startedResponseCount;
	const drafts = workspace.summary.draftResponseCount;
	const preparedInvitations =
		workspace.summary.queuedInvitationCount +
		workspace.summary.sentInvitationCount +
		workspace.summary.failedInvitationCount +
		(workspace.summary.bouncedInvitationCount ?? 0);
	const providerEventCount =
		(workspace.summary.providerAcceptedEventCount ?? 0) +
		(workspace.summary.providerDeliveredEventCount ?? 0) +
		(workspace.summary.providerBouncedEventCount ?? 0) +
		(workspace.summary.providerComplainedEventCount ?? 0);
	const openLinks = workspace.summary.openLinkAssignmentCount;
	const hasRespondentAccess = Boolean(localState.openLinkCreated || preparedInvitations || openLinks);
	const hasResponseActivity = Boolean(started || drafts || submitted);

	const lifecycleLane: SelectedSeriesCollectionStatusLane = closed
		? {
				id: 'lifecycle',
				label: 'Collection lifecycle',
				title: 'Closed',
				status: 'closed',
				detail: 'This wave no longer accepts new responses.'
			}
		: isLive || localState.launched
			? {
					id: 'lifecycle',
					label: 'Collection lifecycle',
					title: 'Live: accepting responses',
					status: 'live',
					detail: 'Respondents can still submit. Results remain preliminary until collection closes.'
				}
			: {
					id: 'lifecycle',
					label: 'Collection lifecycle',
					title: 'Draft: not collecting yet',
					status: 'pending',
					detail: 'Run the pre-launch check, then start collection.'
				};

	const responsesLane: SelectedSeriesCollectionStatusLane = hasResponseActivity
		? {
				id: 'responses',
				label: 'Response progress',
				title: `${formatCount(submitted)} submitted`,
				status: submitted > 0 ? 'ready' : 'pending',
				detail: `${formatCount(started)} started, ${formatCount(drafts)} in progress, ${formatCount(submitted)} submitted.`
			}
		: launched
			? {
					id: 'responses',
					label: 'Response progress',
					title: 'Waiting for responses',
					status: 'pending',
					detail: 'Collection is open, but no response activity has been recorded yet.'
				}
			: {
					id: 'responses',
					label: 'Response progress',
					title: 'Not collecting yet',
					status: 'blocked',
					detail: 'Start collection before monitoring responses.'
				};

	const audienceLane: SelectedSeriesCollectionStatusLane = hasRespondentAccess
		? {
				id: 'audience',
				label: 'Access',
				title: 'Recipient access prepared',
				status: 'ready',
				detail: `${formatCount(openLinks)} respondent link${openLinks === 1 ? '' : 's'} and ${formatCount(preparedInvitations)} prepared invitation${preparedInvitations === 1 ? '' : 's'}. ${
					providerEventCount > 0
						? `Provider reported ${formatCount(providerEventCount)} delivery event${providerEventCount === 1 ? '' : 's'}.`
						: workspace.summary.sentInvitationCount > 0
							? 'Provider delivery events have not been reconciled yet.'
							: 'Provider events appear after sent email invitations are reconciled.'
				} Anonymous reports keep respondent identity out of results.`
			}
		: launched
			? {
					id: 'audience',
					label: 'Access',
					title: 'Access not prepared',
					status: 'pending',
					detail: 'Create a respondent link or prepare invitations before expecting responses.'
				}
			: {
					id: 'audience',
					label: 'Access',
					title: 'Access waits for launch',
					status: 'blocked',
					detail: 'Save recipients in Setup before launch, or start collection before creating an open respondent link.'
				};

	const reportingLane: SelectedSeriesCollectionStatusLane = {
		id: 'reporting',
		label: 'Reporting readiness',
		title: humanize(workspace.summary.reportVisibilityStatus),
		status: toReportingStatus(workspace.summary.reportVisibilityStatus, submitted),
		detail:
			submitted > 0
				? 'Results can be reviewed, but live collection data should be treated as preliminary until closed.'
				: 'Reporting becomes useful after submitted responses are available.'
	};

	const lanes = [lifecycleLane, responsesLane, audienceLane, reportingLane];

	if (closed) {
		return {
			overallStatus: 'closed',
			overallLabel: 'Closed',
			headline: `Closed: ${formatCount(submitted)} submitted response${submitted === 1 ? '' : 's'}`,
			guidance: 'Collection is closed. Submitted responses are stable for Results review.',
			nextAction: 'Open Results to review findings and exports.',
			lanes
		};
	}

	if (isLive || localState.launched) {
		return {
			overallStatus: 'live',
			overallLabel: 'Live',
			headline: `Live: accepting responses with ${formatCount(submitted)} submitted`,
			guidance:
				'Use this page to monitor response progress and recipient access. Close collection when the response window is finished.',
			nextAction:
				submitted > 0
					? 'Keep collecting, review preliminary Results, or close collection when ready.'
					: 'Share respondent access and wait for submitted responses.',
			lanes
		};
	}

	return {
		overallStatus: 'pending',
		overallLabel: 'Draft',
		headline: 'Draft: collection has not started',
		guidance: 'Run the pre-launch check before sharing respondent access.',
		nextAction: 'Run the pre-launch check.',
		lanes
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

function formatCount(value: number | null | undefined) {
	return new Intl.NumberFormat('hr-HR').format(value ?? 0);
}

function humanize(value: string | null | undefined) {
	return value ? value.replaceAll('_', ' ') : 'Not available';
}

function normalizeRecipientEmail(value: string) {
	return value.trim().toLowerCase();
}

function titleCaseLabel(value: string | null | undefined) {
	const label = humanize(value).trim();
	if (!label || label === 'Not available') {
		return 'Not available';
	}

	return label.charAt(0).toUpperCase() + label.slice(1);
}

function toReportingStatus(
	reportVisibilityStatus: string | null | undefined,
	submittedResponseCount: number
): ProductReadModelBadgeStatus {
	if (submittedResponseCount <= 0) {
		return 'pending';
	}

	if (reportVisibilityStatus === 'reportable' || reportVisibilityStatus === 'visible') {
		return 'ready';
	}

	if (reportVisibilityStatus === 'blocked') {
		return 'blocked';
	}

	return 'pending';
}
