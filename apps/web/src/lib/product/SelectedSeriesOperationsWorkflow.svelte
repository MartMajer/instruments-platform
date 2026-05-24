<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
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
		CreateCampaignTestResponsesResponse,
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
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import {
		emailSuppressionReasonLabel,
		emailSuppressionSourceLabel,
		toSelectedSeriesCollectionStatusSummary,
		toSelectedSeriesOperationsPath,
		toRecipientSuppressionReview,
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
	const appLocale = $derived(appLocaleFromPageData(page.data));
	const operationsWorkflowCopy = $derived(routePageCopy(appLocale).selectedStudy.operationsWorkflow);
	const operationsBodyCopy = $derived(routePageCopy(appLocale).selectedStudy.operationsBody);
	const countFormatter = $derived(new Intl.NumberFormat(appLocale));
	const dateTimeFormatter = $derived(
		new Intl.DateTimeFormat(appLocale, {
			day: '2-digit',
			month: '2-digit',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
			hour12: false
		})
	);

	let readinessResult = $state<LaunchReadinessResponse | null>(null);
	let launchResult = $state<LaunchCampaignResponse | null>(null);
	let openLinkResult = $state<CampaignOpenLinkResponse | null>(null);
	let identifiedEntryResult = $state<CampaignIdentifiedEntryResponse | null>(null);
	let invitationBatchResult = $state<CampaignInvitationBatchResponse | null>(null);
	let deliveryResult = $state<ProcessCampaignEmailDeliveriesResponse | null>(null);
	let requeueFailedResult = $state<RequeueFailedCampaignEmailDeliveriesResponse | null>(null);
	let testResponseResult = $state<CreateCampaignTestResponsesResponse | null>(null);
	let testResponseCount = $state(25);
	let testResponseTargetOutcome = $state(7);
	let testResponseVariation = $state<'tight' | 'normal' | 'noisy'>('normal');
	let testResponseIncludeComments = $state(true);
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
			openLinkResult ||
				identifiedEntryResult ||
				invitationBatchResult ||
				deliveryResult ||
				testResponseResult
		),
		closed: Boolean(closeResult)
	});
	const operationsPath = $derived(
		toSelectedSeriesOperationsPath(workspace, localState, operationsWorkflowCopy)
	);
	const collectionStatus = $derived(
		toSelectedSeriesCollectionStatusSummary(workspace, localState, operationsWorkflowCopy)
	);
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
	const recipientSuppressionReview = $derived(
		toRecipientSuppressionReview(recipientImportReview.recipients, activeEmailSuppressions)
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
			testResponseResult = null;
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

		const suppressionReviewResult = await loadEmailSuppressionsForInviteReview();
		if (!suppressionReviewResult) {
			return;
		}

		const latestSuppressionReview = toRecipientSuppressionReview(
			recipientImportReview.recipients,
			suppressionReviewResult.suppressions.filter((suppression) => suppression.active)
		);
		if (latestSuppressionReview.hasBlockedRecipients) {
			actionErrors = {
				...actionErrors,
				openLink: `${latestSuppressionReview.headline}. ${latestSuppressionReview.guidance}`
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

	async function loadEmailSuppressionsForInviteReview() {
		const result = await runAction('openLink', () => setupApi.listEmailSuppressions(100, false));
		if (result) {
			suppressionListResult = result;
		}
		return result;
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

	async function simulateTestResponses() {
		if (!selectedCampaign) {
			actionErrors = {
				...actionErrors,
				openLink: 'Create and start a collection wave before simulating responses.'
			};
			return;
		}

		const result = await runAction('openLink', () =>
			setupApi.createCampaignTestResponses(selectedCampaign.id, {
				responseCount: clampNumber(testResponseCount, 1, 1000),
				targetOutcome: clampNumber(testResponseTargetOutcome, 0, 10),
				variation: testResponseVariation,
				includeComments: testResponseIncludeComments
			})
		);

		if (result) {
			testResponseResult = result;
			deliveryResult = null;
			localQueuedInvitationOverride = Math.max(
				0,
				(localQueuedInvitationOverride ?? queuedInvitationCount) - result.markedEmailSentCount
			);
			localSentInvitationOverride =
				(localSentInvitationOverride ?? sentInvitationCount) + result.markedEmailSentCount;
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
		const confirmed =
			typeof window === 'undefined'
				? true
				: window.confirm(
						`Allow future invitations to ${suppression.recipient}?\n\nOnly do this when the recipient should receive new explicit invitations. Old suppressed emails stay closed.`
					);
		if (!confirmed) {
			return;
		}

		const result = await runAction('openLink', () =>
			setupApi.releaseEmailSuppression(suppression.id, {
				reason: 'owner_operator_allow_future_invitations'
			})
		);

		if (result) {
			upsertEmailSuppression(result);
		}
	}

	async function releaseRecipientSuppressionReviewItem(id: string) {
		const suppression = activeEmailSuppressions.find((candidate) => candidate.id === id);
		if (!suppression) {
			actionErrors = {
				...actionErrors,
				openLink: 'Refresh the do-not-contact list before releasing this recipient.'
			};
			return;
		}

		await releaseEmailSuppression(suppression);
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
			return operationsBodyCopy.stepStatus.working;
		}

		if (state === 'succeeded') {
			return operationsBodyCopy.stepStatus.saved;
		}

		if (state === 'failed') {
			return operationsBodyCopy.stepStatus.failed;
		}

		return operationsBodyCopy.stepStatus.ready;
	}

	function pathStateLabel(state: 'done' | 'current' | 'blocked') {
		if (state === 'done') {
			return operationsBodyCopy.pathStatus.done;
		}

		if (state === 'current') {
			return operationsBodyCopy.pathStatus.current;
		}

		return operationsBodyCopy.pathStatus.blocked;
	}

	function formatCount(value: number | null | undefined) {
		return countFormatter.format(value ?? 0);
	}

	function clampNumber(value: number, min: number, max: number) {
		if (!Number.isFinite(value)) {
			return min;
		}

		return Math.min(Math.max(Math.round(value), min), max);
	}

	function formatDateTime(value: string | null | undefined) {
		if (!value) {
			return operationsBodyCopy.common.notAvailable;
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
		return value ? value.replaceAll('_', ' ') : operationsBodyCopy.common.notAvailable;
	}

	function emailSubject() {
		return operationsBodyCopy.email.subject;
	}

	function emailBody() {
		return operationsBodyCopy.email.body;
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
				title: 'Finish result outputs',
				detail:
					'Open Setup and save result outputs so reports know which answers become scores.',
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

<section class="product-panel" role="group" aria-label={operationsBodyCopy.progressAriaLabel}>
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">{operationsBodyCopy.progressKicker}</p>
			<h3 class="product-title">{operationsBodyCopy.progressTitle}</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				{operationsBodyCopy.progressBody}
			</p>
		</div>
		<div class="grid justify-items-end gap-2">
			<StatusBadge status={collectionStatus.overallStatus} label={collectionStatus.overallLabel} />
			<p class="text-xs font-semibold text-[var(--color-text-muted)]">
				{operationsBodyCopy.stepsComplete(operationsPath.completedCount, operationsPath.totalCount)}
			</p>
		</div>
	</div>

	{#if refreshWarning}
		<p class="error-line">{refreshWarning}</p>
	{/if}

	<article class="score-result-panel report-proof-panel" role="region" aria-label={operationsBodyCopy.statusKicker}>
		<div class="score-result-panel__header">
			<div>
				<p class="product-kicker">{operationsBodyCopy.statusKicker}</p>
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
			<span>{operationsBodyCopy.nextAction}</span>
			<span>{collectionStatus.nextAction}</span>
		</p>
	</article>

	<div class="setup-path" role="list" aria-label={operationsBodyCopy.pathAriaLabel}>
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
			<strong class="record-row__title">{operationsBodyCopy.readOnlyTitle}</strong>
			<span>{operationsBodyCopy.readOnlyBody}</span>
		</p>
	{:else}
		<article class="record-row setup-current-task" role="region" aria-label={operationsBodyCopy.stepAriaLabel}>
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
							<dt class="record-field__label">{operationsBodyCopy.common.collectionWave}</dt>
							<dd class="record-field__value">{selectedCampaign?.name ?? operationsBodyCopy.common.missing}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.setupCheck}</dt>
							<dd class="record-field__value">
								{readinessResult ? (readinessResult.ready ? operationsBodyCopy.common.ready : operationsBodyCopy.common.blocked) : operationsBodyCopy.common.notChecked}
							</dd>
						</div>
					</dl>
					{#if readinessResult?.issues.length}
						<div class="record-row" aria-label={operationsBodyCopy.readiness.issuesAria}>
							<h5 class="record-row__title">
								{readinessResult.ready ? operationsBodyCopy.readiness.warningsTitle : operationsBodyCopy.readiness.blockersTitle}
							</h5>
							<p class="text-sm text-[var(--color-text-muted)]">
								{readinessResult.ready
									? operationsBodyCopy.readiness.warningsBody
									: operationsBodyCopy.readiness.blockersBody}
							</p>
							<ul class="grid gap-2">
								{#each readinessIssueGuidance as guidance}
									<li class="grid gap-1 text-sm">
										<span class="font-semibold text-[var(--color-text)]">
											{guidance.title}
											<span class="text-xs uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
												{guidance.severity === 'blocker' ? operationsBodyCopy.readiness.blocking : operationsBodyCopy.readiness.warning}
											</span>
										</span>
										<span class="text-[var(--color-text-muted)]">{guidance.detail}</span>
									</li>
								{/each}
							</ul>
							<div class="flex flex-wrap items-center gap-3">
								<a class="secondary-button" href={setupHref}>{operationsBodyCopy.readiness.openSetup}</a>
								<span class="text-xs font-semibold text-[var(--color-text-muted)]">
									{operationsBodyCopy.readiness.returnAndCheck}
								</span>
							</div>
						</div>
					{:else if readinessResult && !readinessResult.ready}
						<div class="record-row" aria-label="Readiness blocked">
							<h5 class="record-row__title">{operationsBodyCopy.readiness.blockedTitle}</h5>
							<p class="text-sm text-[var(--color-text-muted)]">
								The check did not return itemized blockers. Open Setup, review incomplete steps,
								save changes, then run this check again.
							</p>
							<a class="secondary-button" href={setupHref}>{operationsBodyCopy.readiness.openSetup}</a>
						</div>
					{/if}
					{@render ActionFooter({
						id: 'readiness',
						label: operationsBodyCopy.readiness.runCheck,
						resultLabel: operationsBodyCopy.common.setupCheck,
						resultValue: readinessResult ? (readinessResult.ready ? 'Ready' : 'Blocked') : null,
						onclick: checkLaunchReadiness
					})}
				{:else if activeAction.id === 'launch'}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						{operationsBodyCopy.launch.body}
					</p>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.collectionWave}</dt>
							<dd class="record-field__value">{selectedCampaign?.name ?? operationsBodyCopy.common.missing}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.status}</dt>
							<dd class="record-field__value">
								{humanize(launchResult?.status ?? selectedCampaign?.status)}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.started}</dt>
							<dd class="record-field__value">
								{formatDateTime(selectedCampaign?.latestLaunchAt)}
							</dd>
						</div>
					</dl>
					{@render ActionFooter({
						id: 'launch',
						label: operationsBodyCopy.launch.start,
						resultLabel: operationsBodyCopy.launch.resultLabel,
						resultValue: launchResult?.status ? humanize(launchResult.status) : null,
						onclick: launchCampaign
					})}
				{:else if activeAction.id === 'openLink'}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						{operationsBodyCopy.shareAccess.body}
					</p>
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">{operationsBodyCopy.emailSetup.label}</p>
								<h5 class="record-row__title">{operationsBodyCopy.emailSetup.title}</h5>
							</div>
							<StatusBadge
								status={emailReadinessBadgeStatus()}
								label={emailReadinessBadgeLabel()}
							/>
						</div>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">
							{operationsBodyCopy.emailSetup.body}
						</p>
						{#if emailReadinessResult}
							<dl class="record-grid">
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.emailSetup.mode}</dt>
									<dd class="record-field__value">{humanize(emailReadinessResult.mode)}</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.emailSetup.realEmailSend}</dt>
									<dd class="record-field__value">
										{emailReadinessResult.canSendRealEmail ? operationsBodyCopy.common.available : operationsBodyCopy.common.notAvailable}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.emailSetup.providerEvents}</dt>
									<dd class="record-field__value">
										{emailReadinessResult.webhookConfigured ? operationsBodyCopy.emailSetup.webhookConfigured : operationsBodyCopy.emailSetup.webhookDisabled}
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
								<span>{operationsBodyCopy.emailSetup.checkEmailSetup}</span>
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
											<p class="record-field__label">
												{emailSuppressionReasonLabel(suppression.reason)}
											</p>
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
											<p class="text-sm text-[var(--color-text-muted)]">
												{emailSuppressionSourceLabel(suppression.source)} - Added {formatDateTime(suppression.createdAt)}
											</p>
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
									<dt class="record-field__label">{operationsBodyCopy.cleanup.retryableFailures}</dt>
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
									<dt class="record-field__label">{operationsBodyCopy.monitor.latestProviderEvent}</dt>
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
											? operationsBodyCopy.shareAccess.openLinkActive
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
							{#if recipientSuppressionReview.hasBlockedRecipients}
								<div class="record-row">
									<div class="record-row__header">
										<div>
											<p class="record-field__label">Do-not-contact match</p>
											<h6 class="record-row__title">{recipientSuppressionReview.headline}</h6>
										</div>
										<StatusBadge
											status="blocked"
											label={`${formatCount(recipientSuppressionReview.blockedCount)} blocked`}
										/>
									</div>
									<p class="error-line" role="alert">{recipientSuppressionReview.guidance}</p>
									<div class="grid gap-2">
										{#each recipientSuppressionReview.items as item (item.id)}
											<div class="record-field">
												<p class="record-field__label">{item.reasonLabel}</p>
												<div class="flex flex-wrap items-center justify-between gap-3">
													<p class="record-field__value">{item.recipient}</p>
													<button
														type="button"
														class="secondary-button"
														disabled={actionStates.openLink === 'submitting'}
														title="Allow future explicit invitations. Existing suppressed emails stay closed."
														onclick={() => releaseRecipientSuppressionReviewItem(item.id)}
													>
														Allow future invites
													</button>
												</div>
												<p class="text-sm text-[var(--color-text-muted)]">
													{item.sourceLabel} - Added {formatDateTime(item.createdAt)}
												</p>
												{#if item.note}
													<p class="text-sm text-[var(--color-text-muted)]">{item.note}</p>
												{/if}
											</div>
										{/each}
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
									disabled={!canCreateEmailInvitations ||
										recipientSuppressionReview.hasBlockedRecipients ||
										actionStates.openLink === 'submitting'}
									title={recipientSuppressionReview.hasBlockedRecipients
										? recipientSuppressionReview.headline
										: undefined}
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
									<span>{operationsBodyCopy.emailSetup.checkEmailSetup}</span>
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
								<p class="record-field__label">Demo/test data</p>
								<h5 class="record-row__title">Simulate collection responses</h5>
								<p class="text-sm text-[var(--color-text-muted)]">
									Use this for staging demos and workflow checks when real email delivery or manual
									respondents would slow you down. It submits marked synthetic answers through the
									selected wave and updates queued test invitations as sent.
								</p>
							</div>
							<StatusBadge
								status={testResponseResult ? 'ready' : 'neutral'}
								label={testResponseResult ? 'Responses created' : 'Staging/demo'}
							/>
						</div>
						<div class="grid gap-3 lg:grid-cols-[minmax(7rem,10rem)_minmax(7rem,10rem)_minmax(9rem,12rem)_auto]">
							<label class="field">
								<span>Responses</span>
								<input
									type="number"
									min="1"
									max="1000"
									bind:value={testResponseCount}
									disabled={actionStates.openLink === 'submitting'}
								/>
							</label>
							<label class="field">
								<span>Average target</span>
								<input
									type="number"
									min="0"
									max="10"
									bind:value={testResponseTargetOutcome}
									disabled={actionStates.openLink === 'submitting'}
								/>
							</label>
							<label class="field">
								<span>Variation</span>
								<select
									bind:value={testResponseVariation}
									disabled={actionStates.openLink === 'submitting'}
								>
									<option value="tight">Tight</option>
									<option value="normal">Normal</option>
									<option value="noisy">Noisy</option>
								</select>
							</label>
							<button
								type="button"
								class="secondary-button self-end"
								disabled={!selectedCampaign || actionStates.openLink === 'submitting'}
								onclick={simulateTestResponses}
							>
								{#if actionStates.openLink === 'submitting'}
									<LoaderCircle size={17} aria-hidden="true" />
								{:else}
									<Plus size={17} aria-hidden="true" />
								{/if}
								<span>{operationsBodyCopy.simulation.simulateCollection}</span>
							</button>
						</div>
						<label class="inline-flex items-start gap-2 text-sm text-[var(--color-text-muted)]">
							<input
								type="checkbox"
								checked={testResponseIncludeComments}
								disabled={actionStates.openLink === 'submitting'}
								onchange={(event) => (testResponseIncludeComments = event.currentTarget.checked)}
							/>
							<span>{operationsBodyCopy.simulation.includeComments}</span>
						</label>
						{#if testResponseResult}
							<div class="record-grid">
								<div class="record-field">
									<p class="record-field__label">{operationsBodyCopy.common.submitted}</p>
									<p class="record-field__value">
										{formatCount(testResponseResult.submittedResponseCount)}
									</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">{operationsBodyCopy.simulation.answersSaved}</p>
									<p class="record-field__value">{formatCount(testResponseResult.answerCount)}</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">{operationsBodyCopy.simulation.scoredResponses}</p>
									<p class="record-field__value">
										{formatCount(testResponseResult.scoredResponseCount)}
									</p>
								</div>
							</div>
						{/if}
					</div>
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">
									{selectedCampaignIsIdentified
										? operationsBodyCopy.shareAccess.identifiedEntryLabel
										: emailInviteAccessActive
											? operationsBodyCopy.shareAccess.inviteOnlyLabel
											: openLinkAccessActive
												? operationsBodyCopy.shareAccess.openLinkLabel
											: operationsBodyCopy.shareAccess.openLinkLabel}
								</p>
								<h5 class="record-row__title">
									{selectedCampaignIsIdentified
										? operationsBodyCopy.shareAccess.identifiedEntryTitle
										: emailInviteAccessActive
											? operationsBodyCopy.shareAccess.privateInvitationsTitle
											: openLinkAccessActive
												? operationsBodyCopy.shareAccess.openLinkReadyTitle
											: operationsBodyCopy.shareAccess.createShareableLinkTitle}
								</h5>
							</div>
							<StatusBadge
								status={emailInviteAccessActive ? 'blocked' : openLinkAccessActive ? 'ready' : 'neutral'}
								label={
									emailInviteAccessActive
										? operationsBodyCopy.shareAccess.openLinkDisabled
										: openLinkAccessActive
											? operationsBodyCopy.shareAccess.openLinkActive
											: operationsBodyCopy.shareAccess.openLinkNotCreated
								}
							/>
						</div>
						<p class="text-sm text-[var(--color-text-muted)]">
							{selectedCampaignIsIdentified
								? operationsBodyCopy.shareAccess.identifiedHelp
								: emailInviteAccessActive
									? operationsBodyCopy.shareAccess.inviteOnlyHelp
									: openLinkAccessActive
										? operationsBodyCopy.shareAccess.openLinkReadyHelp
								: operationsBodyCopy.shareAccess.openLinkHelp}
						</p>
						{#if emailInviteAccessActive && !selectedCampaignIsIdentified}
							<div class="action-row">
								<button type="button" class="secondary-button" disabled>
									<Send size={17} aria-hidden="true" />
									<span>{operationsBodyCopy.shareAccess.openLinkDisabled}</span>
								</button>
								<p class="step-pill" data-state="idle">
									{operationsBodyCopy.shareAccess.inviteOnly}
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
									<span>{operationsBodyCopy.shareAccess.replaceLostLink}</span>
								</button>
								<p class="step-pill" data-state={actionStates.openLink}>
									{actionStates.openLink === 'succeeded' ? operationsBodyCopy.shareAccess.replaced : operationsBodyCopy.shareAccess.oneActiveLink}
								</p>
							</div>
							{#if actionErrors.openLink}
								<p class="error-line">{actionErrors.openLink}</p>
							{/if}
						{:else}
							{@render ActionFooter({
								id: 'openLink',
								label: selectedCampaignIsIdentified
									? operationsBodyCopy.shareAccess.createIdentifiedAccessLink
									: operationsBodyCopy.shareAccess.createRespondentLink,
								resultLabel: operationsBodyCopy.shareAccess.shareLink,
								resultValue: respondentEntry?.respondentPath ?? null,
								onclick: createRespondentAccess
							})}
						{/if}
					</div>
					{#if respondentEntry}
						<div class="record-row">
							<div class="record-row__header">
								<h5 class="record-row__title">{operationsBodyCopy.shareAccess.respondentLinkReady}</h5>
								<span class="step-pill" data-state="succeeded">{operationsBodyCopy.common.created}</span>
							</div>
							<p class="result-line">
								<span>Share link</span>
								<code>{respondentEntry.respondentPath}</code>
							</p>
						</div>
					{/if}
				{:else if activeAction.id === 'monitor'}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						{operationsBodyCopy.monitor.body}
					</p>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.started}</dt>
							<dd class="record-field__value">{formatCount(workspace.summary.startedResponseCount)}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.inProgress}</dt>
							<dd class="record-field__value">{formatCount(workspace.summary.draftResponseCount)}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.submitted}</dt>
							<dd class="record-field__value">
								{formatCount(workspace.summary.submittedResponseCount)}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.latestActivity}</dt>
							<dd class="record-field__value">{formatDateTime(latestResponseActivity)}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.reportReadiness}</dt>
							<dd class="record-field__value">{humanize(workspace.summary.reportVisibilityStatus)}</dd>
						</div>
					</dl>
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">{operationsBodyCopy.monitor.deliveryDiagnostics}</p>
								<h5 class="record-row__title">{operationsBodyCopy.monitor.recentEmailEvents}</h5>
							</div>
							<StatusBadge
								status={providerDeliveryEventCount > 0 ? 'ready' : 'pending'}
								label={
									providerDeliveryEventCount > 0
										? `${formatCount(providerDeliveryEventCount)} reconciled`
										: operationsBodyCopy.monitor.noEventsYet
								}
							/>
						</div>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">
							{operationsBodyCopy.monitor.providerEventsBody}
						</p>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">{operationsBodyCopy.monitor.accepted}</dt>
								<dd class="record-field__value">{formatCount(providerAcceptedEventCount)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{operationsBodyCopy.monitor.delivered}</dt>
								<dd class="record-field__value">{formatCount(providerDeliveredEventCount)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{operationsBodyCopy.monitor.bounced}</dt>
								<dd class="record-field__value">{formatCount(providerBouncedEventCount)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{operationsBodyCopy.monitor.complained}</dt>
								<dd class="record-field__value">{formatCount(providerComplainedEventCount)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{operationsBodyCopy.monitor.latestProviderEvent}</dt>
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
								<span>{operationsBodyCopy.monitor.loadProviderEvents}</span>
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
								{operationsBodyCopy.monitor.noRecentProviderEvents}
							</p>
						{/if}
						{#if actionErrors.monitor}
							<p class="error-line">{actionErrors.monitor}</p>
						{/if}
					</div>
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">{operationsBodyCopy.cleanup.label}</p>
								<h5 class="record-row__title">{operationsBodyCopy.cleanup.title}</h5>
							</div>
							<StatusBadge
								status={repairReadinessResult?.hasRepairWork ? 'pending' : repairReadinessResult ? 'ready' : 'neutral'}
								label={
									repairReadinessResult?.hasRepairWork
										? operationsBodyCopy.cleanup.needsReview
										: repairReadinessResult
											? operationsBodyCopy.cleanup.noCleanup
											: operationsBodyCopy.cleanup.notChecked
								}
							/>
						</div>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">
							{operationsBodyCopy.cleanup.body}
						</p>
						{#if repairReadinessResult}
							<p class="text-sm leading-6 text-[var(--color-text-muted)]">
								{repairReadinessGuidance(repairReadinessResult)}
							</p>
							<dl class="record-grid">
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.cleanup.staleHandoffs}</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.stalePreparedAttemptCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.cleanup.ambiguousFailures}</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.ambiguousFailedNotificationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.cleanup.retryableFailures}</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.retryableFailedNotificationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.cleanup.suppressedFailures}</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.suppressedFailedNotificationCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.cleanup.deliveryEvents}</dt>
									<dd class="record-field__value">
										{formatCount(repairReadinessResult.providerEventCount)}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{operationsBodyCopy.monitor.latestProviderEvent}</dt>
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
								<span>{operationsBodyCopy.cleanup.checkCleanupReadiness}</span>
							</button>
							{#if repairReadinessResult?.canRetryFailed}
								<p class="step-pill" data-state="pending">{operationsBodyCopy.cleanup.retryPossible}</p>
							{/if}
						</div>
					</div>
					{@render ActionFooter({
						id: 'monitor',
						label: operationsBodyCopy.monitor.refreshStatus,
						resultLabel: operationsBodyCopy.common.latestActivity,
						resultValue: formatDateTime(latestResponseActivity),
						onclick: refreshCollectionStatus
					})}
				{:else}
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">
						{operationsBodyCopy.close.body}
					</p>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.collectionWave}</dt>
							<dd class="record-field__value">{selectedCampaign?.name ?? operationsBodyCopy.common.missing}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.status}</dt>
							<dd class="record-field__value">
								{humanize(closeResult?.status ?? selectedCampaign?.status)}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{operationsBodyCopy.common.closed}</dt>
							<dd class="record-field__value">
								{formatDateTime(closeResult?.closedAt ?? selectedCampaign?.closedAt)}
							</dd>
						</div>
					</dl>
					{@render ActionFooter({
						id: 'close',
						label: operationsBodyCopy.close.closeCollection,
						resultLabel: operationsBodyCopy.common.closed,
						resultValue: closeResult?.closedAt
							? formatDateTime(closeResult.closedAt)
							: selectedCampaign?.closedAt
								? formatDateTime(selectedCampaign.closedAt)
								: null,
						onclick: closeCampaign
					})}
				{/if}

				<div class="action-row" aria-label={operationsBodyCopy.navigation.ariaLabel}>
					<button
						type="button"
						class="secondary-button"
						disabled={activeActionIndex === 0}
						onclick={() => selectRelativeAction(-1)}
					>
						Previous step
					</button>
					{#if activeAction.id === 'close'}
						<a class="secondary-button" href={resultsHref}>{operationsBodyCopy.navigation.goToResults}</a>
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
