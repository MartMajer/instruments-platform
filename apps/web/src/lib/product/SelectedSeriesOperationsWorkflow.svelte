<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { CircleStop, LoaderCircle, Plus, RefreshCw, SearchCheck, Send } from 'lucide-svelte';
	import type {
		CampaignCloseStateResponse,
		CampaignSeriesOperationsWorkspaceResponse
	} from '$lib/api/product';
	import type {
		CampaignEmailDeliveryRepairReadinessResponse,
		CampaignInvitationBatchResponse,
		CampaignIdentifiedEntryResponse,
		CampaignOpenLinkResponse,
		EmailDeliveryReadinessResponse,
		EmailSuppressionResponse,
		LaunchCampaignResponse,
		LaunchReadinessResponse,
		ListProviderDeliveryEventsResponse,
		ProviderDeliveryEventResponse,
		ListEmailSuppressionsResponse,
		ProcessCampaignEmailDeliveriesResponse,
		RequeueFailedCampaignEmailDeliveriesResponse
	} from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import {
		toSelectedSeriesCollectionStatusSummary,
		toSelectedSeriesOperationsPath,
		type SelectedSeriesOperationsPathStep,
		type SelectedSeriesOperationsWorkflowActionId
	} from './operations-workflow';
	import {
		appendRecipientImportEntry,
		keepValidRecipientImportRows,
		maxRecipientImportRecipients,
		readRecipientImportFile,
		reviewRecipientImport
	} from './recipient-import';
	import { createProductApiFromEnv, createSetupApiFromEnv } from './route-state';
	import { toProductApiErrorMessage } from './view-models';

	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';
	type ReadinessIssue = LaunchReadinessResponse['issues'][number];
	type ReadinessIssueGuidance = {
		title: string;
		detail: string;
		severity: string;
	};

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
	let invitationBatchResult = $state<CampaignInvitationBatchResponse | null>(null);
	let deliveryResult = $state<ProcessCampaignEmailDeliveriesResponse | null>(null);
	let requeueFailedResult = $state<RequeueFailedCampaignEmailDeliveriesResponse | null>(null);
	let recipientImportText = $state('');
	let recipientImportFileError = $state<string | null>(null);
	let manualRecipientName = $state('');
	let manualRecipientEmail = $state('');
	let manualRecipientError = $state<string | null>(null);
	let suppressionEmail = $state('');
	let suppressionNote = $state('');
	let suppressionListResult = $state<ListEmailSuppressionsResponse | null>(null);
	let emailReadinessResult = $state<EmailDeliveryReadinessResponse | null>(null);
	let providerDeliveryEventsResult = $state<ListProviderDeliveryEventsResponse | null>(null);
	let repairReadinessResult = $state<CampaignEmailDeliveryRepairReadinessResponse | null>(null);
	let retryFailedDeliveryAcknowledged = $state(false);
	let localQueuedInvitationOverride = $state<number | null>(null);
	let localSentInvitationOverride = $state<number | null>(null);
	let localFailedInvitationOverride = $state<number | null>(null);
	let localBouncedInvitationOverride = $state<number | null>(null);
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
	const selectedCampaignSupportsEmailInvites = $derived(
		selectedCampaign?.responseIdentityMode === 'anonymous' ||
			selectedCampaign?.responseIdentityMode === 'anonymous_longitudinal'
	);
	const localState = $derived({
		readinessReady: readinessResult?.ready === true,
		launched: Boolean(launchResult),
		openLinkCreated: Boolean(
			openLinkResult || identifiedEntryResult || invitationBatchResult || deliveryResult
		),
		closed: Boolean(closeResult)
	});
	const operationsPath = $derived(toSelectedSeriesOperationsPath(workspace, localState));
	const collectionStatus = $derived(toSelectedSeriesCollectionStatusSummary(workspace, localState));
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
	const preparedInvitationCount = $derived(
		(selectedCampaign?.queuedInvitationCount ?? 0) +
			(selectedCampaign?.sentInvitationCount ?? 0) +
			(selectedCampaign?.failedInvitationCount ?? 0) +
			(selectedCampaign?.bouncedInvitationCount ?? 0)
	);
	const queuedInvitationCount = $derived(selectedCampaign?.queuedInvitationCount ?? 0);
	const sentInvitationCount = $derived(selectedCampaign?.sentInvitationCount ?? 0);
	const failedInvitationCount = $derived(selectedCampaign?.failedInvitationCount ?? 0);
	const bouncedInvitationCount = $derived(selectedCampaign?.bouncedInvitationCount ?? 0);
	const platformDeliveryAttemptCount = $derived(selectedCampaign?.deliveryAttemptCount ?? 0);
	const providerAcceptedEventCount = $derived(selectedCampaign?.providerAcceptedEventCount ?? 0);
	const providerDeliveredEventCount = $derived(selectedCampaign?.providerDeliveredEventCount ?? 0);
	const providerBouncedEventCount = $derived(selectedCampaign?.providerBouncedEventCount ?? 0);
	const providerComplainedEventCount = $derived(selectedCampaign?.providerComplainedEventCount ?? 0);
	const providerDeliveryEventCount = $derived(
		providerAcceptedEventCount +
			providerDeliveredEventCount +
			providerBouncedEventCount +
			providerComplainedEventCount
	);
	const newlyQueuedInvitationCount = $derived(invitationBatchResult?.createdInvitationCount ?? 0);
	const locallyQueuedInvitationCount = $derived(
		localQueuedInvitationOverride ?? Math.max(queuedInvitationCount, newlyQueuedInvitationCount)
	);
	const locallySentInvitationCount = $derived(localSentInvitationOverride ?? sentInvitationCount);
	const locallyFailedInvitationCount = $derived(localFailedInvitationOverride ?? failedInvitationCount);
	const locallyBouncedInvitationCount = $derived(
		localBouncedInvitationOverride ?? bouncedInvitationCount
	);
	const locallyPreparedInvitationCount = $derived(
		Math.max(
			preparedInvitationCount,
			locallyQueuedInvitationCount +
				locallySentInvitationCount +
				locallyFailedInvitationCount +
				locallyBouncedInvitationCount
		)
	);
	const openLinkAccessActive = $derived(
		Boolean(openLinkResult) || (selectedCampaign?.openLinkAssignmentCount ?? 0) > 0
	);
	const emailInviteAccessActive = $derived(
		selectedCampaignSupportsEmailInvites &&
			locallyPreparedInvitationCount > 0
	);
	const recipientImportReview = $derived(reviewRecipientImport(recipientImportText));
	const canCreateEmailInvitations = $derived(
		Boolean(selectedCampaign) &&
			selectedCampaignSupportsEmailInvites &&
			!openLinkAccessActive &&
			recipientImportReview.validRecipientCount > 0 &&
			!recipientImportReview.hasBlockingIssues
	);
	const emailReadinessBlockingIssues = $derived(
		emailReadinessResult?.issues.filter((issue) => issue.severity === 'blocking') ?? []
	);
	const canSendEmailNow = $derived(
		Boolean(selectedCampaign) &&
			locallyQueuedInvitationCount > 0 &&
			Boolean(emailReadinessResult) &&
			emailReadinessBlockingIssues.length === 0
	);
	const canRetryFailedEmails = $derived(
		Boolean(selectedCampaign) &&
			locallyFailedInvitationCount > 0 &&
			retryFailedDeliveryAcknowledged &&
			Boolean(emailReadinessResult) &&
			emailReadinessBlockingIssues.length === 0
	);
	const activeEmailSuppressions = $derived(
		suppressionListResult?.suppressions.filter((suppression) => suppression.active) ?? []
	);
	const providerDeliveryEventRows = $derived(providerDeliveryEventsResult?.events ?? []);
	const latestResponseActivity = $derived(
		workspace.summary.latestResponseSubmittedAt ?? workspace.summary.latestResponseStartedAt ?? null
	);
	const latestProviderEventAt = $derived(selectedCampaign?.latestProviderEventAt ?? null);
	const setupHref = $derived(`/app/campaign-series/${workspace.series.id}/setup`);
	const resultsHref = $derived(`/app/campaign-series/${workspace.series.id}/reports`);
	const readinessIssueGuidance = $derived(
		readinessResult?.issues.length
			? readinessResult.issues.map(toReadinessIssueGuidance)
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
			invitationBatchResult = null;
			deliveryResult = null;
			requeueFailedResult = null;
			localQueuedInvitationOverride = null;
			localSentInvitationOverride = null;
			localFailedInvitationOverride = null;
			localBouncedInvitationOverride = null;
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

		if (!selectedCampaignIsIdentified && emailInviteAccessActive) {
			actionErrors = {
				...actionErrors,
				openLink:
					'This wave already uses private email invitations. Open links are disabled so access stays invite-only.'
			};
			return;
		}

		if (!selectedCampaignIsIdentified && openLinkAccessActive) {
			actionErrors = {
				...actionErrors,
				openLink:
					'This wave already has an open respondent link. Keep using the link you created, or create a new wave if the link was lost.'
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

	async function replaceOpenRespondentLink() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				openLink: 'Create a collection wave before replacing the open respondent link.'
			};
			return;
		}

		if (selectedCampaignIsIdentified || !openLinkAccessActive || emailInviteAccessActive) {
			actionErrors = {
				...actionErrors,
				openLink:
					'Open respondent link replacement is available only for anonymous open-link collection.'
			};
			return;
		}

		const result = await runAction('openLink', () =>
			setupApi.replaceCampaignOpenLink(selectedCampaign.id)
		);

		if (result) {
			openLinkResult = result;
			identifiedEntryResult = null;
		}
	}

	async function createEmailInvitations() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				openLink: 'Create a collection wave before preparing invitations.'
			};
			return;
		}

		if (!selectedCampaignSupportsEmailInvites) {
			actionErrors = {
				...actionErrors,
				openLink:
					'Email invitations are available for anonymous or repeat-participation waves.'
			};
			return;
		}

		if (openLinkAccessActive) {
			actionErrors = {
				...actionErrors,
				openLink:
					'This wave already has an open respondent link. Private email invitations are disabled for open-link collection.'
			};
			return;
		}

		if (!canCreateEmailInvitations) {
			actionErrors = {
				...actionErrors,
				openLink:
					'Review the recipient list first. Remove invalid or duplicate emails before creating invitations.'
			};
			return;
		}

		const result = await runAction('openLink', () =>
			setupApi.createCampaignInvitationBatch(selectedCampaign.id, {
				recipients: recipientImportReview.recipients
			})
		);

		if (result) {
			invitationBatchResult = result;
			deliveryResult = null;
			localQueuedInvitationOverride =
				(localQueuedInvitationOverride ?? queuedInvitationCount) + result.createdInvitationCount;
			recipientImportText = '';
			manualRecipientName = '';
			manualRecipientEmail = '';
		}
	}

	async function loadRecipientImportFile(file: File | null | undefined) {
		recipientImportFileError = null;
		if (!file) {
			return;
		}

		try {
			recipientImportText = await readRecipientImportFile(file);
		} catch (error) {
			recipientImportFileError =
				error instanceof Error ? error.message : 'Recipient file could not be read.';
		}
	}

	function addManualRecipient() {
		manualRecipientError = null;
		const candidateReview = reviewRecipientImport(
			appendRecipientImportEntry('', {
				displayName: manualRecipientName,
				email: manualRecipientEmail
			})
		);
		const recipient = candidateReview.recipients[0];

		if (!recipient || candidateReview.hasBlockingIssues) {
			manualRecipientError = 'Enter one valid email address.';
			return;
		}

		if (recipientImportReview.recipients.some((item) => item.email === recipient.email)) {
			manualRecipientError = 'This recipient is already in the wave list.';
			return;
		}

		recipientImportText = appendRecipientImportEntry(recipientImportText, {
			displayName: manualRecipientName,
			email: recipient.email
		});
		manualRecipientName = '';
		manualRecipientEmail = '';
	}

	function keepOnlyValidRecipients() {
		recipientImportText = keepValidRecipientImportRows(recipientImportText);
		recipientImportFileError = null;
		manualRecipientError = null;
	}

	function clearRecipientImport() {
		recipientImportText = '';
		recipientImportFileError = null;
		manualRecipientError = null;
	}

	async function sendQueuedEmails() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				openLink: 'Create a collection wave before sending invitations.'
			};
			return;
		}
		if (!emailReadinessResult) {
			actionErrors = {
				...actionErrors,
				openLink: 'Check email sending setup before sending invitation emails.'
			};
			return;
		}
		if (emailReadinessBlockingIssues.length > 0) {
			actionErrors = {
				...actionErrors,
				openLink:
					emailReadinessBlockingIssues[0]?.message ??
					'Resolve email sending setup blockers before sending.'
			};
			return;
		}

		const result = await runAction('openLink', () =>
			setupApi.processCampaignEmailDeliveries(selectedCampaign.id, { batchSize: 25 })
		);

		if (result) {
			deliveryResult = result;
			localQueuedInvitationOverride = Math.max(
				0,
				(localQueuedInvitationOverride ?? queuedInvitationCount) - result.processedCount
			);
			localSentInvitationOverride =
				(localSentInvitationOverride ?? sentInvitationCount) + result.sentCount;
			localFailedInvitationOverride =
				(localFailedInvitationOverride ?? failedInvitationCount) + result.failedCount;
			localBouncedInvitationOverride =
				(localBouncedInvitationOverride ?? bouncedInvitationCount) + (result.bouncedCount ?? 0);
		}
	}

	async function retryFailedEmails() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				openLink: 'Create a collection wave before retrying failed emails.'
			};
			return;
		}

		if (!retryFailedDeliveryAcknowledged) {
			actionErrors = {
				...actionErrors,
				openLink:
					'Confirm another invitation email is appropriate before requeueing.'
			};
			return;
		}
		if (!emailReadinessResult) {
			actionErrors = {
				...actionErrors,
				openLink: 'Check email sending setup before retrying failed invitation emails.'
			};
			return;
		}
		if (emailReadinessBlockingIssues.length > 0) {
			actionErrors = {
				...actionErrors,
				openLink:
					emailReadinessBlockingIssues[0]?.message ??
					'Resolve email sending setup blockers before retrying failed invitations.'
			};
			return;
		}

		const result = await runAction('openLink', () =>
			setupApi.requeueFailedCampaignEmailDeliveries(selectedCampaign.id, {
				batchSize: 25,
				confirmedAnotherEmailAppropriate: retryFailedDeliveryAcknowledged
			})
		);

		if (result) {
			requeueFailedResult = result;
			deliveryResult = null;
			localQueuedInvitationOverride =
				(localQueuedInvitationOverride ?? queuedInvitationCount) + result.requeuedCount;
			localFailedInvitationOverride = Math.max(
				0,
				(localFailedInvitationOverride ?? failedInvitationCount) - result.requeuedCount
			);
			retryFailedDeliveryAcknowledged = false;
		}
	}

	async function loadEmailSuppressions() {
		const result = await runAction('openLink', () => setupApi.listEmailSuppressions(50, false));
		if (result) {
			suppressionListResult = result;
		}
	}

	async function loadEmailDeliveryReadiness() {
		const result = await runAction('openLink', () => setupApi.getEmailDeliveryReadiness());
		if (result) {
			emailReadinessResult = result;
		}
	}

	async function loadProviderDeliveryEvents() {
		const result = await runAction('monitor', () => setupApi.listProviderDeliveryEvents(25));
		if (result) {
			providerDeliveryEventsResult = result;
		}
	}

	async function loadRepairReadiness() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				monitor: 'Create a collection wave before checking email repair readiness.'
			};
			return;
		}

		const result = await runAction('monitor', () =>
			setupApi.getCampaignEmailDeliveryRepairReadiness(selectedCampaign.id)
		);
		if (result) {
			repairReadinessResult = result;
		}
	}

	function providerEventStateSummary(event: ProviderDeliveryEventResponse) {
		const evidence = [];
		if (event.hasProviderEventId) {
			evidence.push('provider event evidence');
		}
		if (event.hasProviderMessageId) {
			evidence.push('provider message evidence');
		}

		return `${humanize(event.notificationStatus)} notification, ${humanize(event.deliveryAttemptStatus)} delivery attempt; ${
			evidence.length > 0 ? evidence.join(' and ') : 'no provider ids exposed'
		}`;
	}

	async function addEmailSuppression() {
		const email = suppressionEmail.trim();
		if (!email) {
			actionErrors = {
				...actionErrors,
				openLink: 'Enter an email address before adding it to do-not-contact.'
			};
			return;
		}

		const result = await runAction('openLink', () =>
			setupApi.addEmailSuppression({
				recipient: email,
				reason: 'operator_do_not_contact',
				note: suppressionNote.trim() || null
			})
		);

		if (result) {
			suppressionEmail = '';
			suppressionNote = '';
			upsertEmailSuppression(result);
		}
	}

	async function releaseEmailSuppression(suppression: EmailSuppressionResponse) {
		const result = await runAction('openLink', () =>
			setupApi.releaseEmailSuppression(suppression.id, {
				reason: 'operator_released'
			})
		);

		if (result) {
			upsertEmailSuppression(result);
		}
	}

	function upsertEmailSuppression(suppression: EmailSuppressionResponse) {
		const current = suppressionListResult?.suppressions ?? [];
		const suppressions = [
			suppression,
			...current.filter((candidate) => candidate.id !== suppression.id)
		];
		const activeCount = suppressions.filter((candidate) => candidate.active).length;
		const releasedCount = suppressions.filter((candidate) => !candidate.active).length;
		suppressionListResult = {
			requestedLimit: suppressionListResult?.requestedLimit ?? 50,
			activeCount,
			releasedCount,
			suppressions
		};
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

		const date = new Date(normalizeTimestampForDate(value));
		if (Number.isNaN(date.getTime())) {
			return value;
		}

		return dateTimeFormatter.format(date);
	}

	function normalizeTimestampForDate(value: string) {
		return value.replace(/\.(\d{3})\d+(?=(Z|[+-]\d{2}:?\d{2})$)/, '.$1');
	}

	function humanize(value: string | null | undefined) {
		return value ? value.replaceAll('_', ' ') : 'Not available';
	}

	function emailSubject() {
		return 'Study invitation';
	}

	function emailBody() {
		return `You have been invited to complete a study.\n\nFor privacy, this email does not include the study title or topic. The link opens the study page before you decide whether to respond.\n\nOpen your study link:\n[unique respondent link]\n\nIf you already responded, you can ignore this email.\n\nIf you should not receive future study invitations from this workspace, unsubscribe here:\n[unsubscribe link]\n\n[workspace invitation footer]`;
	}

	function deliveryBatchSummary(result: ProcessCampaignEmailDeliveriesResponse) {
		const bouncedCount = result.bouncedCount ?? 0;
		return `${formatCount(result.sentCount)} sent, ${formatCount(result.failedCount)} failed, ${formatCount(bouncedCount)} suppressed`;
	}

	function deliveryBatchGuidance(result: ProcessCampaignEmailDeliveriesResponse) {
		if (result.failedCount > 0) {
			const errors = new Set(
				result.deliveries
					.map((delivery) => delivery.error)
					.filter((error): error is string => Boolean(error))
			);

			if (errors.has('ses_sandbox_recipient_not_verified')) {
				return 'AWS SES rejected at least one recipient because the account is still in sandbox. Verify the lowercase recipient email in the same SES region, or wait for SES production access, then use Retry failed emails.';
			}

			if (errors.has('ses_sender_identity_not_verified')) {
				return 'AWS SES rejected the sender identity. Check the verified sender domain/from address in SES, then use Retry failed emails.';
			}

			if (errors.has('ses_identity_not_verified')) {
				return 'AWS SES rejected a verified-identity check. Confirm sender and sandbox recipient identities in the configured SES region, then retry failed emails.';
			}

			if (errors.has('smtp_auth_failed')) {
				return 'The SMTP provider rejected authentication. Check the SES SMTP username/password on the server, then retry failed emails.';
			}

			if (errors.has('smtp_tls_failed')) {
				return 'The SMTP TLS handshake failed. Check provider host, port, and TLS settings before retrying failed emails.';
			}

			if (errors.has('ses_throttled')) {
				return 'AWS SES throttled this batch. Wait for the provider limit window to clear, then retry failed emails.';
			}

			if (errors.has('recipient_suppressed')) {
				return 'At least one recipient is on the workspace do-not-contact list. Review suppressions before retrying.';
			}

			return 'The provider rejected at least one invitation. Check email setup and provider status, then use Retry failed emails when another send is appropriate.';
		}

		if (result.sentCount > 0) {
			return 'Sent means the message was accepted by the SMTP handoff. Delivery, bounce, and complaint evidence appears later under Provider delivery evidence.';
		}

		return null;
	}

	function repairReadinessGuidance(result: CampaignEmailDeliveryRepairReadinessResponse) {
		if (result.retryableFailedNotificationCount > 0) {
			return 'Failed invitations can be retried after the provider issue is corrected. Use Retry failed emails in the respondent access step.';
		}

		if (result.suppressedFailedNotificationCount > 0) {
			return 'Some failed invitations are suppressed by do-not-contact or provider feedback. Review the suppression list before sending again.';
		}

		if (result.stalePreparedAttemptCount > 0 || result.ambiguousFailedNotificationCount > 0) {
			return 'Some handoffs are ambiguous. Treat them as possibly sent and retry only after checking provider evidence.';
		}

		if (result.providerEventCount > 0) {
			return 'Provider events have reconciled for this campaign. Load recent provider events to inspect accepted, delivered, bounced, or complained counts.';
		}

		return 'No email delivery cleanup is currently needed for this wave.';
	}

	function emailReadinessBadgeStatus() {
		if (!emailReadinessResult) {
			return 'neutral';
		}

		if (emailReadinessResult.canSendRealEmail) {
			return 'ready';
		}

		return emailReadinessResult.issues.some((issue) => issue.severity === 'blocking')
			? 'blocked'
			: 'pending';
	}

	function emailReadinessBadgeLabel() {
		if (!emailReadinessResult) {
			return 'Not checked';
		}

		if (emailReadinessResult.canSendRealEmail) {
			return emailReadinessResult.webhookConfigured ? 'SMTP ready' : 'SMTP send ready';
		}

		return emailReadinessResult.mode === 'local_dev' ? 'Local proof mode' : 'Needs config';
	}

	function emailSendDisabledReason() {
		if (!selectedCampaign) {
			return 'Create a collection wave before sending invitations.';
		}
		if (locallyQueuedInvitationCount <= 0) {
			return 'No queued invitation emails are waiting to send.';
		}
		if (!emailReadinessResult) {
			return 'Check email sending setup before sending invitation emails.';
		}
		if (emailReadinessBlockingIssues.length > 0) {
			return (
				emailReadinessBlockingIssues[0]?.message ??
				'Resolve email sending setup blockers before sending.'
			);
		}

		return '';
	}

	function emailRetryDisabledReason() {
		if (!selectedCampaign) {
			return 'Create a collection wave before retrying failed emails.';
		}
		if (locallyFailedInvitationCount <= 0) {
			return 'No retryable failed invitation emails are waiting.';
		}
		if (!retryFailedDeliveryAcknowledged) {
			return 'Confirm another invitation email is appropriate before requeueing.';
		}
		if (!emailReadinessResult) {
			return 'Check email sending setup before retrying failed invitation emails.';
		}
		if (emailReadinessBlockingIssues.length > 0) {
			return (
				emailReadinessBlockingIssues[0]?.message ??
				'Resolve email sending setup blockers before retrying failed invitations.'
			);
		}

		return '';
	}

	function toReadinessIssueGuidance(issue: ReadinessIssue): ReadinessIssueGuidance {
		const code = issue.code.toLowerCase();
		const normalized = `${issue.code} ${issue.message}`.toLowerCase();

		if (code === 'campaign.status_not_launchable') {
			return {
				title: 'Use a draft collection wave',
				detail:
					'This wave is no longer draft or scheduled. Open Setup, select or create a draft collection wave, then run this check again.',
				severity: issue.severity
			};
		}

		if (code === 'identity.unknown') {
			return {
				title: 'Choose the response mode',
				detail:
					'Open Setup and save the Collection setup step with a valid response mode before starting collection.',
				severity: issue.severity
			};
		}

		if (code === 'template_version.missing') {
			return {
				title: 'Connect the questionnaire to this wave',
				detail:
					'Open Setup, save the Questionnaire step, then save the Collection setup step so the wave uses that questionnaire.',
				severity: issue.severity
			};
		}

		if (code.startsWith('template.')) {
			return {
				title: 'Finish the questionnaire',
				detail:
					'Open Setup and add at least one questionnaire section and question before starting collection.',
				severity: issue.severity
			};
		}

		if (code.startsWith('scoring_rule.') || normalized.includes('scoring')) {
			return {
				title: 'Finish Results setup',
				detail:
					'Open Setup and save the Results setup step so reports know which answers become scores.',
				severity: issue.severity
			};
		}

		if (
			code.includes('consent') ||
			code.includes('retention') ||
			code.includes('disclosure') ||
			normalized.includes('policy')
		) {
			return {
				title: 'Complete study policies',
				detail:
					'Open Setup and save the consent, retention, and disclosure policies for this study before launch.',
				severity: issue.severity
			};
		}

		if (code === 'respondent_rule.identity_mode_not_supported') {
			return {
				title: 'Switch response mode',
				detail:
					'Saved specific-email lists are available for anonymous or repeat-participation waves. Open Setup and change the response mode, or remove the saved recipient list.',
				severity: issue.severity
			};
		}

		if (code === 'respondent_rule.email_required') {
			return {
				title: 'Add recipient email addresses',
				detail:
					'Open Directory and add email addresses for everyone in the saved recipient selection, then rerun the pre-launch check.',
				severity: issue.severity
			};
		}

		if (code === 'respondent_rule.no_recipients') {
			return {
				title: 'Select at least one recipient',
				detail:
					'Open Setup and save at least one recipient selection that resolves to active people.',
				severity: issue.severity
			};
		}

		if (code === 'respondent_rule_preview.audience_missing') {
			return {
				title: 'Recipient selection is empty',
				detail:
					'Add active people to the selected group in Setup, or remove the saved recipient selection if this wave should use a general respondent link.',
				severity: issue.severity
			};
		}

		if (code.startsWith('respondent_rule_preview.')) {
			return {
				title: 'Fix who can answer',
				detail:
					'Open Setup and adjust the recipient selection until the preview finds the people you expect.',
				severity: issue.severity
			};
		}

		if (code.startsWith('instrument.')) {
			return {
				title: 'Review the instrument',
				detail:
					'Open Setup and save the Instrument and Questionnaire steps again so this wave uses a launchable study instrument.',
				severity: issue.severity
			};
		}

		return {
			title: 'Review setup',
			detail: issue.message,
			severity: issue.severity
		};
	}
</script>

<section class="product-panel" role="group" aria-label="Study collection flow">
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
			<StatusBadge status={collectionStatus.overallStatus} label={collectionStatus.overallLabel} />
			<p class="text-xs font-semibold text-[var(--color-text-muted)]">
				{operationsPath.completedCount}/{operationsPath.totalCount} steps complete
			</p>
		</div>
	</div>

	{#if refreshWarning}
		<p class="error-line">{refreshWarning}</p>
	{/if}

	<article class="score-result-panel report-proof-panel" role="region" aria-label="Collection status">
		<div class="score-result-panel__header">
			<div>
				<p class="product-kicker">Collection status</p>
				<h4 class="record-row__title">{collectionStatus.headline}</h4>
				<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
					{collectionStatus.guidance}
				</p>
			</div>
			<StatusBadge status={collectionStatus.overallStatus} label={collectionStatus.overallLabel} />
		</div>
		<dl class="record-grid">
			{#each collectionStatus.lanes as lane (lane.id)}
				<div class="record-field">
					<dt class="record-field__label">{lane.label}</dt>
					<dd class="record-field__value">{lane.title}</dd>
					<dd class="text-sm text-[var(--color-text-muted)]">{lane.detail}</dd>
				</div>
			{/each}
		</dl>
		<p class="result-line">
			<span>Next action</span>
			<span>{collectionStatus.nextAction}</span>
		</p>
	</article>

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
						recipients, and policy setup can support responses and reporting.
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
							<h5 class="record-row__title">
								{readinessResult.ready ? 'Setup warnings' : 'Before collection can start'}
							</h5>
							<p class="text-sm text-[var(--color-text-muted)]">
								{readinessResult.ready
									? 'These items do not block collection, but they should be reviewed before sharing access.'
									: 'Fix the blocking setup items, then run the pre-launch check again.'}
							</p>
							<ul class="grid gap-2">
								{#each readinessIssueGuidance as guidance}
									<li class="grid gap-1 text-sm">
										<span class="font-semibold text-[var(--color-text)]">
											{guidance.title}
											<span class="text-xs uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
												{guidance.severity === 'blocker' ? 'Blocking' : 'Warning'}
											</span>
										</span>
										<span class="text-[var(--color-text-muted)]">{guidance.detail}</span>
									</li>
								{/each}
							</ul>
							<div class="flex flex-wrap items-center gap-3">
								<a class="secondary-button" href={setupHref}>Open Setup</a>
								<span class="text-xs font-semibold text-[var(--color-text-muted)]">
									Return here and run the check again after saving setup.
								</span>
							</div>
						</div>
					{:else if readinessResult && !readinessResult.ready}
						<div class="record-row" aria-label="Readiness blocked">
							<h5 class="record-row__title">Setup is blocked</h5>
							<p class="text-sm text-[var(--color-text-muted)]">
								The check did not return itemized blockers. Open Setup, review incomplete steps,
								save changes, then run this check again.
							</p>
							<a class="secondary-button" href={setupHref}>Open Setup</a>
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
						Choose how respondents enter this wave. Directory and group selections saved in Setup
						become private invitations at launch. Use the one-off importer here only to add ad hoc
						recipients after launch, or create an open respondent link when anyone with the link may
						answer.
					</p>
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">Email sending setup</p>
								<h5 class="record-row__title">Check delivery configuration before sending</h5>
							</div>
							<StatusBadge
								status={emailReadinessBadgeStatus()}
								label={emailReadinessBadgeLabel()}
							/>
						</div>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">
							This check shows whether the current environment can send real SMTP invitations, or
							whether it is still in local test mode or missing required email settings. It never
							exposes provider secrets or SMTP credentials.
						</p>
						{#if emailReadinessResult}
							<dl class="record-grid">
								<div class="record-field">
									<dt class="record-field__label">Mode</dt>
									<dd class="record-field__value">{humanize(emailReadinessResult.mode)}</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Real email send</dt>
									<dd class="record-field__value">
										{emailReadinessResult.canSendRealEmail ? 'Available' : 'Not available'}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Provider events</dt>
									<dd class="record-field__value">
										{emailReadinessResult.webhookConfigured ? 'Webhook configured' : 'Webhook disabled'}
									</dd>
								</div>
							</dl>
							{#if emailReadinessResult.issues.length > 0}
								<div class="grid gap-2">
									{#each emailReadinessResult.issues as issue (issue.code)}
										<div class="record-field">
											<p class="record-field__label">{humanize(issue.severity)}</p>
											<p class="text-sm text-[var(--color-text-muted)]">{issue.message}</p>
										</div>
									{/each}
								</div>
							{/if}
						{/if}
						<div class="action-row">
							<button
								type="button"
								class="secondary-button"
								disabled={actionStates.openLink === 'submitting'}
								onclick={loadEmailDeliveryReadiness}
							>
								<SearchCheck size={16} aria-hidden="true" />
								<span>Check email setup</span>
							</button>
						</div>
					</div>
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">Do-not-contact list</p>
								<h5 class="record-row__title">Suppress emails before inviting</h5>
							</div>
							<StatusBadge
								status={(suppressionListResult?.activeCount ?? 0) > 0 ? 'pending' : 'neutral'}
								label={
									suppressionListResult
										? `${formatCount(suppressionListResult.activeCount)} active`
										: 'Not loaded'
								}
							/>
						</div>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">
							Add addresses that must not receive campaign invitations from this tenant. Suppression is
							checked when invitations are created, when saved recipient lists are launched, and again before email
							delivery. Releasing an address only allows future explicit invitations; it does not resend
							or restore old suppressed emails.
						</p>
						<div class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto]">
							<label class="field">
								<span>Email to suppress</span>
								<input
									type="email"
									value={suppressionEmail}
									placeholder="do.not.contact@example.com"
									oninput={(event) => (suppressionEmail = event.currentTarget.value)}
								/>
							</label>
							<label class="field">
								<span>Internal note</span>
								<input
									value={suppressionNote}
									placeholder="Requested by tenant admin"
									oninput={(event) => (suppressionNote = event.currentTarget.value)}
								/>
							</label>
							<button
								type="button"
								class="secondary-button self-end"
								disabled={actionStates.openLink === 'submitting'}
								onclick={addEmailSuppression}
							>
								<CircleStop size={16} aria-hidden="true" />
								<span>Add to do-not-contact</span>
							</button>
						</div>
						<div class="action-row">
							<button
								type="button"
								class="secondary-button"
								disabled={actionStates.openLink === 'submitting'}
								onclick={loadEmailSuppressions}
							>
								<RefreshCw size={16} aria-hidden="true" />
								<span>Refresh do-not-contact list</span>
							</button>
						</div>
						{#if suppressionListResult}
							{#if activeEmailSuppressions.length > 0}
								<div class="grid gap-2">
									{#each activeEmailSuppressions.slice(0, 6) as suppression (suppression.id)}
										<div class="record-field">
											<p class="record-field__label">{suppression.reason.replaceAll('_', ' ')}</p>
											<div class="flex flex-wrap items-center justify-between gap-3">
												<p class="record-field__value">{suppression.recipient}</p>
												<button
													type="button"
													class="secondary-button"
													disabled={actionStates.openLink === 'submitting'}
													title="Allow future explicit invitations. Existing suppressed emails stay closed."
													onclick={() => releaseEmailSuppression(suppression)}
												>
													Allow future invites
												</button>
											</div>
											{#if suppression.note}
												<p class="text-sm text-[var(--color-text-muted)]">{suppression.note}</p>
											{/if}
										</div>
									{/each}
									{#if activeEmailSuppressions.length > 6}
										<p class="text-sm text-[var(--color-text-muted)]">
											Showing first 6 of {formatCount(activeEmailSuppressions.length)} active
											do-not-contact records.
										</p>
									{/if}
								</div>
							{:else}
								<p class="text-sm text-[var(--color-text-muted)]">
									No active do-not-contact records were returned for this tenant.
								</p>
							{/if}
						{:else}
							<p class="text-sm text-[var(--color-text-muted)]">
								Refresh the list before a live send when you need to review tenant-level suppression
								records.
							</p>
						{/if}
					</div>
					{#if locallyPreparedInvitationCount > 0}
						<div class="record-row">
							<div class="record-row__header">
								<h5 class="record-row__title">Email invitation status</h5>
								<span class="step-pill" data-state="succeeded">Ready</span>
							</div>
							<dl class="record-grid">
								<div class="record-field">
									<dt class="record-field__label">Queued</dt>
									<dd class="record-field__value">
										{formatCount(locallyQueuedInvitationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Sent</dt>
									<dd class="record-field__value">
										{formatCount(locallySentInvitationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Retryable failures</dt>
									<dd class="record-field__value">
										{formatCount(locallyFailedInvitationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Suppressed</dt>
									<dd class="record-field__value">
										{formatCount(locallyBouncedInvitationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Send attempts</dt>
									<dd class="record-field__value">
										{formatCount(platformDeliveryAttemptCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Provider accepted</dt>
									<dd class="record-field__value">
										{formatCount(providerAcceptedEventCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Provider delivered</dt>
									<dd class="record-field__value">
										{formatCount(providerDeliveredEventCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Provider bounced</dt>
									<dd class="record-field__value">
										{formatCount(providerBouncedEventCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Complaints</dt>
									<dd class="record-field__value">
										{formatCount(providerComplainedEventCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Latest provider event</dt>
									<dd class="record-field__value">{formatDateTime(latestProviderEventAt)}</dd>
								</div>
							</dl>
							<p class="text-sm text-[var(--color-text-muted)]">
								Sent means the platform handed the email to the configured provider. Provider counts
								show later accepted, delivered, bounced, or complaint events when they have been
								reconciled{providerDeliveryEventCount > 0 ? '.' : '; no provider events are recorded yet.'}
								Anonymous answers stay disconnected from named recipients in reports and exports.
							</p>
						</div>
					{/if}
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">Invited email access</p>
								<h5 class="record-row__title">Add one-off recipients after launch</h5>
							</div>
							<StatusBadge
								status={selectedCampaignSupportsEmailInvites && !openLinkAccessActive ? 'neutral' : 'blocked'}
								label={
									selectedCampaignSupportsEmailInvites && !openLinkAccessActive
										? 'Anonymous invite-only'
										: openLinkAccessActive
											? 'Open link active'
											: 'Unavailable'
								}
							/>
						</div>
						{#if selectedCampaignSupportsEmailInvites}
							{#if openLinkAccessActive}
								<p class="error-line" role="alert">
									This wave already has an open respondent link. Keep using open-link collection or
									create a new wave for private email invitations.
								</p>
							{/if}
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">Post-launch additions</p>
										<h6 class="record-row__title">Add one-time recipients to this wave</h6>
									</div>
									<span
										class="step-pill"
										data-state={recipientImportReview.hasBlockingIssues ? 'failed' : 'idle'}
									>
										{formatCount(recipientImportReview.validRecipientCount)} ready
									</span>
								</div>
								<div class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto]">
									<label class="field">
										<span>Name for review</span>
										<input
											value={manualRecipientName}
											placeholder="Bo Horvat"
											oninput={(event) => (manualRecipientName = event.currentTarget.value)}
										/>
									</label>
									<label class="field">
										<span>Email</span>
										<input
											type="email"
											value={manualRecipientEmail}
											placeholder="bo@example.com"
											oninput={(event) => (manualRecipientEmail = event.currentTarget.value)}
										/>
									</label>
									<button type="button" class="secondary-button self-end" onclick={addManualRecipient}>
										<Plus size={16} aria-hidden="true" />
										<span>Add to review list</span>
									</button>
								</div>
								{#if manualRecipientError}
									<p class="error-line" role="alert">{manualRecipientError}</p>
								{/if}
								<label class="field">
									<span>Import recipients</span>
									<input
										type="file"
										accept=".csv,.txt,text/csv,text/plain"
										onchange={(event) => loadRecipientImportFile(event.currentTarget.files?.[0])}
									/>
									<span class="text-sm text-[var(--color-text-muted)]">
										Use this for late additions or a one-time list after launch. For the normal study
										recipient list, use Directory groups or the saved recipient selection in Setup before
										launch. Review happens in this browser before private invitation links are created.
										Limit: {formatCount(maxRecipientImportRecipients)} recipients per wave update.
									</span>
								</label>
								<details>
									<summary class="record-row__title">Review or paste source list</summary>
									<label class="field mt-3">
										<span>Recipient source</span>
										<textarea
											rows="5"
											value={recipientImportText}
											placeholder={'ada@example.com\nBo Horvat <bo@example.com>\ncarla@example.com; diego@example.com'}
											oninput={(event) => (recipientImportText = event.currentTarget.value)}
										></textarea>
										<span class="text-sm text-[var(--color-text-muted)]">
											Use this for copied spreadsheet columns or cleanup after import. One row can be an
											email address or Name &lt;email@example.com&gt;. This does not create Directory
											people or groups.
										</span>
									</label>
								</details>
							</div>
							{#if recipientImportFileError}
								<p class="error-line" role="alert">{recipientImportFileError}</p>
							{/if}
							{#if recipientImportReview.rows.length > 0}
								<div class="record-row">
									<div class="record-row__header">
										<h6 class="record-row__title">Import review</h6>
										<span
											class="step-pill"
											data-state={recipientImportReview.hasBlockingIssues ? 'failed' : 'succeeded'}
										>
											{formatCount(recipientImportReview.validRecipientCount)} ready
										</span>
									</div>
									<dl class="record-grid">
										<div class="record-field">
											<dt class="record-field__label">Ready</dt>
											<dd class="record-field__value">
												{formatCount(recipientImportReview.validRecipientCount)}
											</dd>
										</div>
										<div class="record-field">
											<dt class="record-field__label">Invalid</dt>
											<dd class="record-field__value">
												{formatCount(recipientImportReview.invalidCount)}
											</dd>
										</div>
										<div class="record-field">
											<dt class="record-field__label">Duplicates</dt>
											<dd class="record-field__value">
												{formatCount(recipientImportReview.duplicateCount)}
											</dd>
										</div>
									</dl>
									<div class="grid gap-2">
										{#each recipientImportReview.rows.slice(0, 8) as row (row.id)}
											<div class="record-field">
												<p class="record-field__label">
													{row.displayName ?? row.sourceText}
												</p>
												<p class="record-field__value">{row.email || 'No email found'}</p>
												<p class="text-sm text-[var(--color-text-muted)]">{row.reason}</p>
											</div>
										{/each}
										{#if recipientImportReview.rows.length > 8}
											<p class="text-sm text-[var(--color-text-muted)]">
												Showing first 8 of {formatCount(recipientImportReview.rows.length)} parsed rows.
											</p>
										{/if}
									</div>
									<div class="action-row">
										<button
											type="button"
											class="secondary-button"
											disabled={!recipientImportReview.hasBlockingIssues}
											onclick={keepOnlyValidRecipients}
										>
											<RefreshCw size={16} aria-hidden="true" />
											<span>Keep valid only</span>
										</button>
										<button type="button" class="secondary-button" onclick={clearRecipientImport}>
											<CircleStop size={16} aria-hidden="true" />
											<span>Clear list</span>
										</button>
									</div>
								</div>
							{/if}
							<details class="record-row">
								<summary class="record-row__title">Invitation email preview</summary>
								<div class="mt-3 record-field">
									<p class="record-field__label">Subject</p>
									<p class="record-field__value">{emailSubject()}</p>
									<p class="mt-2 whitespace-pre-wrap text-sm text-[var(--color-text-muted)]">
										{emailBody()}
									</p>
								</div>
								<p class="mt-3 text-sm text-[var(--color-text-muted)]">
									Each recipient gets their own link. Delivery status stays operational; reports and
									exports do not expose recipient identity for anonymous or repeat-participation
									waves. Unsubscribe requests add the recipient to this workspace's do-not-contact
									list. Sender identity, footer text, and reminder cadence are governed by email
									delivery settings and future reminder workflow.
								</p>
							</details>
							{#if locallyFailedInvitationCount > 0}
								<div class="field">
									<span>Retry safety check</span>
									<label class="inline-flex items-start gap-2 text-sm text-[var(--color-text-muted)]">
										<input
											type="checkbox"
											checked={retryFailedDeliveryAcknowledged}
											onchange={(event) =>
												(retryFailedDeliveryAcknowledged = event.currentTarget.checked)}
										/>
										<span>
											I checked the failed recipients and confirm another email is appropriate.
											Requeueing sends another valid invite link; earlier sent links stay valid.
										</span>
									</label>
								</div>
							{/if}
							<div class="action-row">
								<button
									type="button"
									class="primary-button"
									disabled={!canCreateEmailInvitations || actionStates.openLink === 'submitting'}
									onclick={createEmailInvitations}
								>
									<Send size={17} aria-hidden="true" />
									<span>Create ad hoc invitations</span>
								</button>
								<button
									type="button"
									class="secondary-button"
									disabled={actionStates.openLink === 'submitting'}
									onclick={loadEmailDeliveryReadiness}
								>
									<SearchCheck size={17} aria-hidden="true" />
									<span>Check email setup</span>
								</button>
								<button
									type="button"
									class="secondary-button"
									disabled={!canSendEmailNow || actionStates.openLink === 'submitting'}
									title={emailSendDisabledReason()}
									onclick={sendQueuedEmails}
								>
									<Send size={17} aria-hidden="true" />
									<span>Send next email batch</span>
								</button>
								<button
									type="button"
									class="secondary-button"
									disabled={!canRetryFailedEmails || actionStates.openLink === 'submitting'}
									title={emailRetryDisabledReason()}
									onclick={retryFailedEmails}
								>
									<RefreshCw size={17} aria-hidden="true" />
									<span>Retry failed emails</span>
								</button>
								<p class="step-pill" data-state={actionStates.openLink}>
									{stepLabel(actionStates.openLink)}
								</p>
							</div>
							{#if !emailReadinessResult && (locallyQueuedInvitationCount > 0 || locallyFailedInvitationCount > 0)}
								<p class="text-sm text-[var(--color-text-muted)]">
									Check email sending setup before sending or retrying invitation emails.
								</p>
							{:else if emailReadinessBlockingIssues.length > 0}
								<p class="error-line" role="alert">
									{emailReadinessBlockingIssues[0]?.message}
								</p>
							{/if}
							{#if invitationBatchResult}
								<p class="result-line">
									<span>Invitations created</span>
									<code>{formatCount(invitationBatchResult.createdInvitationCount)}</code>
								</p>
							{/if}
							{#if deliveryResult}
								<p class="result-line">
									<span>Email delivery batch</span>
									<code>{deliveryBatchSummary(deliveryResult)}</code>
								</p>
								{#if deliveryBatchGuidance(deliveryResult)}
									<p class={deliveryResult.failedCount > 0 ? 'error-line' : 'text-sm text-[var(--color-text-muted)]'}>
										{deliveryBatchGuidance(deliveryResult)}
									</p>
								{/if}
							{/if}
							{#if requeueFailedResult}
								<p class="result-line">
									<span>Failed emails requeued</span>
									<code>{formatCount(requeueFailedResult.requeuedCount)}</code>
								</p>
							{/if}
						{:else}
							<p class="text-sm text-[var(--color-text-muted)]">
								Email invitations currently require an anonymous or repeat-participation collection
								wave. Use Setup to change the response mode, or use the access option below for this
								wave.
							</p>
						{/if}
					</div>
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">
									{selectedCampaignIsIdentified
										? 'Identified entry'
										: emailInviteAccessActive
											? 'Invite-only access'
											: openLinkAccessActive
												? 'Open respondent link'
											: 'Open respondent link'}
								</p>
								<h5 class="record-row__title">
									{selectedCampaignIsIdentified
										? 'Create an identified respondent entry'
										: emailInviteAccessActive
											? 'Private invitations are active'
											: openLinkAccessActive
												? 'Open link already created'
											: 'Create a shareable link'}
								</h5>
							</div>
							<StatusBadge
								status={emailInviteAccessActive ? 'blocked' : openLinkAccessActive ? 'ready' : 'neutral'}
								label={
									emailInviteAccessActive
										? 'Open link disabled'
										: openLinkAccessActive
											? 'Open link active'
											: undefined
								}
							/>
						</div>
						<p class="text-sm text-[var(--color-text-muted)]">
							{selectedCampaignIsIdentified
								? 'Use this only when respondents should be connected to known subject records.'
								: emailInviteAccessActive
									? 'This wave already has private email invitations. Open links are disabled so participation stays limited to invited recipients.'
									: openLinkAccessActive
										? 'This wave already has one active open link. If the link was lost, replace it here. The old link will stop accepting new respondents; existing response sessions can still finish through their private session handles.'
								: 'Use this when broad anonymous participation is acceptable and you do not need an invite-only recipient list.'}
						</p>
						{#if emailInviteAccessActive && !selectedCampaignIsIdentified}
							<div class="action-row">
								<button type="button" class="secondary-button" disabled>
									<Send size={17} aria-hidden="true" />
									<span>Open link disabled</span>
								</button>
								<p class="step-pill" data-state="idle">
									Invite-only
								</p>
							</div>
							{#if actionErrors.openLink}
								<p class="error-line">{actionErrors.openLink}</p>
							{/if}
						{:else if openLinkAccessActive && !selectedCampaignIsIdentified}
							<div class="action-row">
								<button
									type="button"
									class="secondary-button"
									disabled={actionStates.openLink === 'submitting'}
									onclick={replaceOpenRespondentLink}
								>
									{#if actionStates.openLink === 'submitting'}
										<LoaderCircle size={17} aria-hidden="true" />
									{:else}
										<RefreshCw size={17} aria-hidden="true" />
									{/if}
									<span>Replace lost link</span>
								</button>
								<p class="step-pill" data-state={actionStates.openLink}>
									{actionStates.openLink === 'succeeded' ? 'Replaced' : 'One active link'}
								</p>
							</div>
							{#if actionErrors.openLink}
								<p class="error-line">{actionErrors.openLink}</p>
							{/if}
						{:else}
							{@render ActionFooter({
								id: 'openLink',
								label: selectedCampaignIsIdentified
									? 'Create identified access link'
									: 'Create respondent link',
								resultLabel: 'Share link',
								resultValue: respondentEntry?.respondentPath ?? null,
								onclick: createRespondentAccess
							})}
						{/if}
					</div>
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
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">Provider delivery evidence</p>
								<h5 class="record-row__title">Recent email provider events</h5>
							</div>
							<StatusBadge
								status={providerDeliveryEventCount > 0 ? 'ready' : 'pending'}
								label={
									providerDeliveryEventCount > 0
										? `${formatCount(providerDeliveryEventCount)} reconciled`
										: 'No events yet'
								}
							/>
						</div>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">
							Use this to confirm whether SES or another provider has reported accepted, delivered,
							bounced, or complained events. The list intentionally hides recipients, internal ids,
							provider ids, and provider reason text.
						</p>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">Accepted</dt>
								<dd class="record-field__value">{formatCount(providerAcceptedEventCount)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Delivered</dt>
								<dd class="record-field__value">{formatCount(providerDeliveredEventCount)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Bounced</dt>
								<dd class="record-field__value">{formatCount(providerBouncedEventCount)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Complained</dt>
								<dd class="record-field__value">{formatCount(providerComplainedEventCount)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Latest provider event</dt>
								<dd class="record-field__value">{formatDateTime(latestProviderEventAt)}</dd>
							</div>
						</dl>
						<div class="action-row">
							<button
								type="button"
								class="secondary-button"
								disabled={actionStates.monitor === 'submitting'}
								onclick={loadProviderDeliveryEvents}
							>
								{#if actionStates.monitor === 'submitting'}
									<LoaderCircle size={17} aria-hidden="true" />
								{:else}
									<RefreshCw size={17} aria-hidden="true" />
								{/if}
								<span>Load recent provider events</span>
							</button>
							{#if providerDeliveryEventsResult}
								<p class="step-pill" data-state="succeeded">
									{formatCount(providerDeliveryEventRows.length)} loaded
								</p>
							{/if}
						</div>
						{#if providerDeliveryEventRows.length > 0}
							<div class="grid gap-3">
								{#each providerDeliveryEventRows as event, index (`${event.provider}-${event.eventType}-${event.receivedAt}-${index}`)}
									<div class="record-field">
										<p class="record-field__label">
											{humanize(event.eventType)} from {humanize(event.provider)}
										</p>
										<p class="record-field__value">
											{formatDateTime(event.occurredAt)}
										</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{providerEventStateSummary(event)}. Received
											{formatDateTime(event.receivedAt)}.
										</p>
									</div>
								{/each}
							</div>
						{:else if providerDeliveryEventsResult}
							<p class="text-sm text-[var(--color-text-muted)]">
								No recent provider events are recorded for this workspace yet.
							</p>
						{/if}
						{#if actionErrors.monitor}
							<p class="error-line">{actionErrors.monitor}</p>
						{/if}
					</div>
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">Email delivery cleanup</p>
								<h5 class="record-row__title">Repair readiness</h5>
							</div>
							<StatusBadge
								status={repairReadinessResult?.hasRepairWork ? 'pending' : repairReadinessResult ? 'ready' : 'neutral'}
								label={
									repairReadinessResult?.hasRepairWork
										? 'Needs review'
										: repairReadinessResult
											? 'No cleanup'
											: 'Not checked'
								}
							/>
						</div>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">
							Check this before retrying failed invitation emails. It separates stale prepared
							handoffs, ambiguous failures, retryable failures, and suppressed recipients without
							changing delivery state.
						</p>
						{#if repairReadinessResult}
							<p class="text-sm leading-6 text-[var(--color-text-muted)]">
								{repairReadinessGuidance(repairReadinessResult)}
							</p>
							<dl class="record-grid">
								<div class="record-field">
									<dt class="record-field__label">Stale handoffs</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.stalePreparedAttemptCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Ambiguous failures</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.ambiguousFailedNotificationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Retryable failures</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.retryableFailedNotificationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Suppressed failures</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.suppressedFailedNotificationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Provider evidence</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.providerEventCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Latest provider event</dt>
									<dd class="record-field__value">
										{formatDateTime(repairReadinessResult.latestProviderEventAt)}
									</dd>
								</div>
							</dl>
							{#if repairReadinessResult.issues.length > 0}
								<div class="grid gap-2">
									{#each repairReadinessResult.issues as issue (issue.code)}
										<p class={issue.severity === 'blocking' ? 'error-line' : 'text-sm text-[var(--color-text-muted)]'}>
											{issue.message}
										</p>
									{/each}
								</div>
							{/if}
						{/if}
						<div class="action-row">
							<button
								type="button"
								class="secondary-button"
								disabled={!selectedCampaign || actionStates.monitor === 'submitting'}
								onclick={loadRepairReadiness}
							>
								{#if actionStates.monitor === 'submitting'}
									<LoaderCircle size={17} aria-hidden="true" />
								{:else}
									<SearchCheck size={17} aria-hidden="true" />
								{/if}
								<span>Check cleanup readiness</span>
							</button>
							{#if repairReadinessResult?.canRetryFailed}
								<p class="step-pill" data-state="pending">Retry possible</p>
							{/if}
						</div>
					</div>
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
					{#if activeAction.id === 'close'}
						<a class="secondary-button" href={resultsHref}>Go to results</a>
					{:else}
						<button
							type="button"
							class="secondary-button"
							disabled={activeActionIndex >= workflowActions.length - 1}
							onclick={() => selectRelativeAction(1)}
						>
							Next step
						</button>
					{/if}
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
