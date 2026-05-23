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

export type SelectedSeriesOperationsWorkflowCopy = {
	stepNumber: (number: number) => string;
	actions: Record<
		SelectedSeriesOperationsWorkflowActionId,
		{
			title: string;
			description: string;
		}
	>;
	disabled: {
		createWaveBeforeReadiness: string;
		createWaveBeforeStart: string;
		startBeforeAccess: string;
		startBeforeMonitor: string;
		createWaveBeforeClose: string;
		waveClosed: string;
		alreadyLive: string;
		startedThisSession: string;
		runPrelaunchAndSetup: string;
		onlyLiveClosable: string;
	};
	status: {
		lifecycleLabel: string;
		responseProgressLabel: string;
		accessLabel: string;
		reportingReadinessLabel: string;
		noWaveSelectedTitle: string;
		noWaveSelectedDetail: string;
		noResponsesYetTitle: string;
		noResponsesYetDetail: string;
		noRecipientAccessTitle: string;
		noRecipientAccessDetail: string;
		reportingNotAvailableTitle: string;
		reportingNotAvailableDetail: string;
		createWaveFirstHeadline: string;
		createWaveFirstGuidance: string;
		createWaveFirstNextAction: string;
		closedTitle: string;
		closedDetail: string;
		liveTitle: string;
		liveDetail: string;
		draftTitle: string;
		draftDetail: string;
		submittedTitle: (submitted: string) => string;
		responseActivityDetail: (started: string, drafts: string, submitted: string) => string;
		waitingForResponsesTitle: string;
		waitingForResponsesDetail: string;
		notCollectingTitle: string;
		notCollectingDetail: string;
		accessNotPreparedTitle: string;
		accessNotPreparedDetail: string;
		accessWaitsForLaunchTitle: string;
		accessWaitsForLaunchDetail: string;
		resultsPreliminaryDetail: string;
		reportingUsefulAfterSubmitted: string;
		closedOverallLabel: string;
		closedHeadline: (submitted: string, submittedCount: number) => string;
		closedGuidance: string;
		closedNextAction: string;
		liveOverallLabel: string;
		liveHeadline: (submitted: string) => string;
		liveGuidance: string;
		liveNextWithResponses: string;
		liveNextNoResponses: string;
		draftOverallLabel: string;
		draftHeadline: string;
		draftGuidance: string;
		draftNextAction: string;
		identifiedAccessTitle: string;
		inviteOnlyAccessTitle: string;
		openLinkAccessTitle: string;
		recipientAccessTitle: string;
		identifiedAccessDetail: (openLinkCount: string, pluralSuffix: string) => string;
		inviteOnlyDetail: (invitationCount: string, verb: string, boundary: string) => string;
		mixedAccessDetail: (
			openLinkCount: string,
			openPluralSuffix: string,
			invitationCount: string,
			invitationPluralSuffix: string,
			boundary: string
		) => string;
		openLinkDetail: (openLinkCount: string, verb: string) => string;
		createAccessBeforeResponses: string;
		anonymousBoundary: string;
		anonymousBoundarySentence: string;
		longitudinalBoundary: string;
		longitudinalBoundarySentence: string;
		notAvailable: string;
	};
};

export const defaultSelectedSeriesOperationsWorkflowCopy: SelectedSeriesOperationsWorkflowCopy = {
	stepNumber: (number) => `Step ${number}`,
	actions: {
		readiness: {
			title: 'Pre-launch check',
			description: 'Confirm the questionnaire, results setup, recipients, and policies are ready.'
		},
		launch: {
			title: 'Start collection',
			description: 'Open this wave for responses and record the setup used for reporting.'
		},
		openLink: {
			title: 'Share access',
			description: 'Send saved invitations or create an open respondent link for this wave.'
		},
		monitor: {
			title: 'Monitor responses',
			description: 'Track starts, drafts, submissions, and report readiness while collection runs.'
		},
		close: {
			title: 'Close collection',
			description: 'Stop accepting new responses while keeping submitted data reportable.'
		}
	},
	disabled: {
		createWaveBeforeReadiness: 'Create a collection wave in setup before checking readiness.',
		createWaveBeforeStart: 'Create a collection wave before starting collection.',
		startBeforeAccess: 'Start collection before preparing respondent access.',
		startBeforeMonitor: 'Start collection before monitoring responses.',
		createWaveBeforeClose: 'Create a collection wave before closing collection.',
		waveClosed: 'This collection wave is closed.',
		alreadyLive: 'Collection is already live.',
		startedThisSession: 'Collection was started in this session.',
		runPrelaunchAndSetup:
			'Run the pre-launch check. If it says Blocked, open Setup and finish the listed items first.',
		onlyLiveClosable: 'Only a live collection wave can be closed.'
	},
	status: {
		lifecycleLabel: 'Collection lifecycle',
		responseProgressLabel: 'Response progress',
		accessLabel: 'Access',
		reportingReadinessLabel: 'Reporting readiness',
		noWaveSelectedTitle: 'No wave selected',
		noWaveSelectedDetail: 'Create or select a collection wave before collecting responses.',
		noResponsesYetTitle: 'No responses yet',
		noResponsesYetDetail: 'Response counts appear after a wave is started.',
		noRecipientAccessTitle: 'No recipient access prepared',
		noRecipientAccessDetail: 'Choose recipients or create respondent access after setup is ready.',
		reportingNotAvailableTitle: 'Not available',
		reportingNotAvailableDetail: 'Reporting readiness appears after collection has a selected wave.',
		createWaveFirstHeadline: 'Create a collection wave first',
		createWaveFirstGuidance: 'Collection starts after setup has a campaign wave.',
		createWaveFirstNextAction: 'Open Setup and create a collection wave.',
		closedTitle: 'Closed',
		closedDetail: 'This wave no longer accepts new responses.',
		liveTitle: 'Live: accepting responses',
		liveDetail: 'Respondents can still submit. Results remain preliminary until collection closes.',
		draftTitle: 'Draft: not collecting yet',
		draftDetail: 'Run the pre-launch check, then start collection.',
		submittedTitle: (submitted) => `${submitted} submitted`,
		responseActivityDetail: (started, drafts, submitted) =>
			`${started} started, ${drafts} in progress, ${submitted} submitted.`,
		waitingForResponsesTitle: 'Waiting for responses',
		waitingForResponsesDetail: 'Collection is open, but no response activity has been recorded yet.',
		notCollectingTitle: 'Not collecting yet',
		notCollectingDetail: 'Start collection before monitoring responses.',
		accessNotPreparedTitle: 'Access not prepared',
		accessNotPreparedDetail: 'Create a respondent link or prepare invitations before expecting responses.',
		accessWaitsForLaunchTitle: 'Access waits for launch',
		accessWaitsForLaunchDetail:
			'Save recipients in Setup before launch, or start collection before creating an open respondent link.',
		resultsPreliminaryDetail:
			'Results can be reviewed, but live collection data should be treated as preliminary until closed.',
		reportingUsefulAfterSubmitted: 'Reporting becomes useful after submitted responses are available.',
		closedOverallLabel: 'Closed',
		closedHeadline: (submitted, submittedCount) =>
			`Closed: ${submitted} submitted response${submittedCount === 1 ? '' : 's'}`,
		closedGuidance: 'Collection is closed. Submitted responses are stable for Results review.',
		closedNextAction: 'Open Results to review findings and exports.',
		liveOverallLabel: 'Live',
		liveHeadline: (submitted) => `Live: accepting responses with ${submitted} submitted`,
		liveGuidance:
			'Use this page to monitor response progress and recipient access. Close collection when the response window is finished.',
		liveNextWithResponses:
			'Keep collecting, review preliminary Results, or close collection when ready.',
		liveNextNoResponses: 'Share respondent access and wait for submitted responses.',
		draftOverallLabel: 'Draft',
		draftHeadline: 'Draft: collection has not started',
		draftGuidance: 'Run the pre-launch check before sharing respondent access.',
		draftNextAction: 'Run the pre-launch check.',
		identifiedAccessTitle: 'Identified access prepared',
		inviteOnlyAccessTitle: 'Invite-only access prepared',
		openLinkAccessTitle: 'Open-link access prepared',
		recipientAccessTitle: 'Recipient access prepared',
		identifiedAccessDetail: (openLinkCount, pluralSuffix) =>
			`${openLinkCount} identified access link${pluralSuffix} prepared. Respondents are connected to known subject records for this wave.`,
		inviteOnlyDetail: (invitationCount, verb, boundary) =>
			`${invitationCount} saved email invitation${verb} ready for this wave. Only saved recipients receive private access, and ${boundary}`,
		mixedAccessDetail: (
			openLinkCount,
			openPluralSuffix,
			invitationCount,
			invitationPluralSuffix,
			boundary
		) =>
			`${openLinkCount} open respondent link${openPluralSuffix} and ${invitationCount} saved email invitation${invitationPluralSuffix}. Open-link access is broad; invite-only email access limits entry to saved recipients. ${boundary}`,
		openLinkDetail: (openLinkCount, verb) =>
			`${openLinkCount} open respondent link${verb} active. Anyone with the link can enter this wave; use saved invitations when access should be limited.`,
		createAccessBeforeResponses:
			'Create a respondent link or saved email invitations before expecting responses.',
		anonymousBoundary: 'anonymous reports still do not show who answered.',
		anonymousBoundarySentence: 'Anonymous reports still keep respondent identity out of results.',
		longitudinalBoundary:
			'repeat-participation results use participant codes instead of showing who answered.',
		longitudinalBoundarySentence:
			'Repeat-participation comparison uses participant codes; email recipient lists are not shown in results.',
		notAvailable: 'Not available'
	}
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
	localState: SelectedSeriesOperationsWorkflowLocalState = {},
	copy: SelectedSeriesOperationsWorkflowCopy = defaultSelectedSeriesOperationsWorkflowCopy
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
			step: copy.stepNumber(1),
			title: copy.actions.readiness.title,
			description: copy.actions.readiness.description,
			status: !hasCampaign ? 'not_available' : readinessReady ? 'ready' : 'pending',
			available: hasCampaign,
			disabledReason: hasCampaign
				? null
				: copy.disabled.createWaveBeforeReadiness
		},
		{
			id: 'launch',
			step: copy.stepNumber(2),
			title: copy.actions.launch.title,
			description: copy.actions.launch.description,
			status: toLaunchStatus(hasCampaign, isLive, closed, launched, localState),
			available: hasCampaign && readinessReady && !launched && !closed,
			disabledReason: toLaunchDisabledReason(hasCampaign, isLive, closed, launched, localState, copy)
		},
		{
			id: 'openLink',
			step: copy.stepNumber(3),
			title: copy.actions.openLink.title,
			description: copy.actions.openLink.description,
			status: !hasCampaign
				? 'not_available'
				: launched && hasRespondentAccess
					? 'ready'
					: launched
						? 'pending'
						: 'blocked',
			available: launched && !closed,
			disabledReason: !launched
				? copy.disabled.startBeforeAccess
				: closed
					? copy.disabled.waveClosed
					: null
		},
		{
			id: 'monitor',
			step: copy.stepNumber(4),
			title: copy.actions.monitor.title,
			description: copy.actions.monitor.description,
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
			disabledReason: !launched ? copy.disabled.startBeforeMonitor : null
		},
		{
			id: 'close',
			step: copy.stepNumber(5),
			title: copy.actions.close.title,
			description: copy.actions.close.description,
			status: !hasCampaign ? 'not_available' : closed ? 'closed' : closeable ? 'pending' : 'blocked',
			available: closeable && !closed,
			disabledReason: !hasCampaign
				? copy.disabled.createWaveBeforeClose
				: closed
					? null
					: closeable
						? null
						: copy.disabled.onlyLiveClosable
		}
	];
}

export function toSelectedSeriesOperationsPath(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	localState: SelectedSeriesOperationsWorkflowLocalState = {},
	copy: SelectedSeriesOperationsWorkflowCopy = defaultSelectedSeriesOperationsWorkflowCopy
): SelectedSeriesOperationsPath {
	const actions = toSelectedSeriesOperationsWorkflowActions(workspace, localState, copy);
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
	localState: SelectedSeriesOperationsWorkflowLocalState = {},
	copy: SelectedSeriesOperationsWorkflowCopy = defaultSelectedSeriesOperationsWorkflowCopy
): SelectedSeriesCollectionStatusSummary {
	const selectedCampaign = workspace.selectedCampaign;

	if (!selectedCampaign) {
		const lanes: SelectedSeriesCollectionStatusLane[] = [
			{
				id: 'lifecycle',
				label: copy.status.lifecycleLabel,
				title: copy.status.noWaveSelectedTitle,
				status: 'not_available',
				detail: copy.status.noWaveSelectedDetail
			},
			{
				id: 'responses',
				label: copy.status.responseProgressLabel,
				title: copy.status.noResponsesYetTitle,
				status: 'not_available',
				detail: copy.status.noResponsesYetDetail
			},
			{
				id: 'audience',
				label: copy.status.accessLabel,
				title: copy.status.noRecipientAccessTitle,
				status: 'not_available',
				detail: copy.status.noRecipientAccessDetail
			},
			{
				id: 'reporting',
				label: copy.status.reportingReadinessLabel,
				title: copy.status.reportingNotAvailableTitle,
				status: 'not_available',
				detail: copy.status.reportingNotAvailableDetail
			}
		];

		return {
			overallStatus: 'not_available',
			overallLabel: copy.status.noWaveSelectedTitle,
			headline: copy.status.createWaveFirstHeadline,
			guidance: copy.status.createWaveFirstGuidance,
			nextAction: copy.status.createWaveFirstNextAction,
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
	const openLinks = workspace.summary.openLinkAssignmentCount;
	const hasRespondentAccess = Boolean(localState.openLinkCreated || preparedInvitations || openLinks);
	const hasResponseActivity = Boolean(started || drafts || submitted);

	const lifecycleLane: SelectedSeriesCollectionStatusLane = closed
		? {
				id: 'lifecycle',
				label: copy.status.lifecycleLabel,
				title: copy.status.closedTitle,
				status: 'closed',
				detail: copy.status.closedDetail
			}
		: isLive || localState.launched
			? {
					id: 'lifecycle',
					label: copy.status.lifecycleLabel,
					title: copy.status.liveTitle,
					status: 'live',
					detail: copy.status.liveDetail
				}
			: {
					id: 'lifecycle',
					label: copy.status.lifecycleLabel,
					title: copy.status.draftTitle,
					status: 'pending',
					detail: copy.status.draftDetail
				};

	const responsesLane: SelectedSeriesCollectionStatusLane = hasResponseActivity
		? {
				id: 'responses',
				label: copy.status.responseProgressLabel,
				title: copy.status.submittedTitle(formatCount(submitted)),
				status: submitted > 0 ? 'ready' : 'pending',
				detail: copy.status.responseActivityDetail(
					formatCount(started),
					formatCount(drafts),
					formatCount(submitted)
				)
			}
		: launched
			? {
					id: 'responses',
					label: copy.status.responseProgressLabel,
					title: copy.status.waitingForResponsesTitle,
					status: 'pending',
					detail: copy.status.waitingForResponsesDetail
				}
			: {
					id: 'responses',
					label: copy.status.responseProgressLabel,
					title: copy.status.notCollectingTitle,
					status: 'blocked',
					detail: copy.status.notCollectingDetail
				};

	const audienceLane: SelectedSeriesCollectionStatusLane = hasRespondentAccess
		? {
				id: 'audience',
				label: copy.status.accessLabel,
				title: audienceAccessTitle(
					selectedCampaign.responseIdentityMode,
					openLinks,
					preparedInvitations,
					copy
				),
				status: 'ready',
				detail: audienceAccessDetail(
					selectedCampaign.responseIdentityMode,
					openLinks,
					preparedInvitations,
					localState.openLinkCreated,
					copy
				)
			}
		: launched
			? {
					id: 'audience',
					label: copy.status.accessLabel,
					title: copy.status.accessNotPreparedTitle,
					status: 'pending',
					detail: copy.status.accessNotPreparedDetail
				}
			: {
					id: 'audience',
					label: copy.status.accessLabel,
					title: copy.status.accessWaitsForLaunchTitle,
					status: 'blocked',
					detail: copy.status.accessWaitsForLaunchDetail
				};

	const reportingLane: SelectedSeriesCollectionStatusLane = {
		id: 'reporting',
		label: copy.status.reportingReadinessLabel,
		title: humanize(workspace.summary.reportVisibilityStatus),
		status: toReportingStatus(workspace.summary.reportVisibilityStatus, submitted),
		detail:
			submitted > 0
				? copy.status.resultsPreliminaryDetail
				: copy.status.reportingUsefulAfterSubmitted
	};

	const lanes = [lifecycleLane, responsesLane, audienceLane, reportingLane];

	if (closed) {
		return {
			overallStatus: 'closed',
			overallLabel: copy.status.closedOverallLabel,
			headline: copy.status.closedHeadline(formatCount(submitted), submitted),
			guidance: copy.status.closedGuidance,
			nextAction: copy.status.closedNextAction,
			lanes
		};
	}

	if (isLive || localState.launched) {
		return {
			overallStatus: 'live',
			overallLabel: copy.status.liveOverallLabel,
			headline: copy.status.liveHeadline(formatCount(submitted)),
			guidance: copy.status.liveGuidance,
			nextAction:
				submitted > 0
					? copy.status.liveNextWithResponses
					: copy.status.liveNextNoResponses,
			lanes
		};
	}

	return {
		overallStatus: 'pending',
		overallLabel: copy.status.draftOverallLabel,
		headline: copy.status.draftHeadline,
		guidance: copy.status.draftGuidance,
		nextAction: copy.status.draftNextAction,
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
	localState: SelectedSeriesOperationsWorkflowLocalState,
	copy: SelectedSeriesOperationsWorkflowCopy
) {
	if (!hasCampaign) {
		return copy.disabled.createWaveBeforeStart;
	}

	if (closed) {
		return copy.disabled.waveClosed;
	}

	if (isLive) {
		return copy.disabled.alreadyLive;
	}

	if (launched || localState.launched) {
		return copy.disabled.startedThisSession;
	}

	return localState.readinessReady ? null : copy.disabled.runPrelaunchAndSetup;
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

function audienceAccessTitle(
	responseIdentityMode: string | null | undefined,
	openLinkCount: number,
	invitationCount: number,
	copy: SelectedSeriesOperationsWorkflowCopy
) {
	if (responseIdentityMode === 'identified') {
		return copy.status.identifiedAccessTitle;
	}

	if (invitationCount > 0 && openLinkCount === 0) {
		return copy.status.inviteOnlyAccessTitle;
	}

	if (openLinkCount > 0 && invitationCount === 0) {
		return copy.status.openLinkAccessTitle;
	}

	return copy.status.recipientAccessTitle;
}

function audienceAccessDetail(
	responseIdentityMode: string | null | undefined,
	openLinkCount: number,
	invitationCount: number,
	openLinkCreated: boolean | undefined,
	copy: SelectedSeriesOperationsWorkflowCopy
) {
	const effectiveOpenLinkCount = openLinkCount || (openLinkCreated ? 1 : 0);

	if (responseIdentityMode === 'identified') {
		return copy.status.identifiedAccessDetail(
			formatCount(effectiveOpenLinkCount),
			effectiveOpenLinkCount === 1 ? '' : 's'
		);
	}

	if (invitationCount > 0 && effectiveOpenLinkCount === 0) {
		return copy.status.inviteOnlyDetail(
			formatCount(invitationCount),
			invitationCount === 1 ? ' is' : 's are',
			anonymousResultBoundary(responseIdentityMode, false, copy)
		);
	}

	if (effectiveOpenLinkCount > 0 && invitationCount > 0) {
		return copy.status.mixedAccessDetail(
			formatCount(effectiveOpenLinkCount),
			effectiveOpenLinkCount === 1 ? '' : 's',
			formatCount(invitationCount),
			invitationCount === 1 ? '' : 's',
			anonymousResultBoundary(responseIdentityMode, true, copy)
		);
	}

	if (effectiveOpenLinkCount > 0) {
		return copy.status.openLinkDetail(
			formatCount(effectiveOpenLinkCount),
			effectiveOpenLinkCount === 1 ? ' is' : 's are'
		);
	}

	return copy.status.createAccessBeforeResponses;
}

function anonymousResultBoundary(
	responseIdentityMode: string | null | undefined,
	startSentence = false,
	copy: SelectedSeriesOperationsWorkflowCopy
) {
	if (responseIdentityMode === 'anonymous_longitudinal') {
		return startSentence ? copy.status.longitudinalBoundarySentence : copy.status.longitudinalBoundary;
	}

	return startSentence ? copy.status.anonymousBoundarySentence : copy.status.anonymousBoundary;
}
