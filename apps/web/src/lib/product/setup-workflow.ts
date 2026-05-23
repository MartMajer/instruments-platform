import type { CampaignSeriesSetupWorkspaceResponse } from '$lib/api/product';
import type { ProductReadModelBadgeStatus } from './view-models';

export type SelectedSeriesSetupWorkflowActionId =
	| 'instrument'
	| 'template'
	| 'scoring'
	| 'campaign'
	| 'readiness';

export type SelectedSeriesSetupWorkflowLocalState = {
	instrumentId?: string | null;
	templateVersionId?: string | null;
	scoringRuleId?: string | null;
	campaignId?: string | null;
};

export type SelectedSeriesSetupWorkflowAction = {
	id: SelectedSeriesSetupWorkflowActionId;
	step: string;
	title: string;
	description: string;
	status: ProductReadModelBadgeStatus;
	available: boolean;
	disabledReason: string | null;
};

export type SelectedSeriesSetupPathStepState = 'done' | 'current' | 'blocked';

export type SelectedSeriesSetupPathStep = SelectedSeriesSetupWorkflowAction & {
	pathState: SelectedSeriesSetupPathStepState;
};

export type SelectedSeriesSetupPathStepDisplayState =
	| 'done'
	| 'current'
	| 'selected'
	| 'next'
	| 'blocked';

export type SelectedSeriesSetupPathStepDisplay = {
	state: SelectedSeriesSetupPathStepDisplayState;
	label: string;
};

export type SelectedSeriesSetupPath = {
	steps: SelectedSeriesSetupPathStep[];
	currentActionId: SelectedSeriesSetupWorkflowActionId;
	currentAction: SelectedSeriesSetupWorkflowAction;
	completedCount: number;
	totalCount: number;
};

export type SelectedSeriesSetupLaunchStateOptions = {
	readinessPassed?: boolean;
	savedRecipientSelectionCount?: number;
	savedRecipientPairCount?: number;
	savedRecipientLoading?: boolean;
	responseIdentityMode?: string | null;
};

export type SelectedSeriesSetupLaunchState = {
	statusLabel: string;
	nextActionLabel: string;
	collectionButtonLabel: string;
	collectionButtonAvailable: boolean;
	recipientSummary: string;
};

export type SelectedSeriesSetupLaunchPlanItemId =
	| 'wave'
	| 'response_mode'
	| 'recipients'
	| 'collection_handoff';

export type SelectedSeriesSetupLaunchPlanItemStatus = 'ready' | 'attention' | 'blocked';

export type SelectedSeriesSetupLaunchPlanItem = {
	id: SelectedSeriesSetupLaunchPlanItemId;
	label: string;
	status: SelectedSeriesSetupLaunchPlanItemStatus;
	detail: string;
};

export type SelectedSeriesSetupLaunchPlanOptions = {
	responseIdentityMode?: string | null;
	savedRecipientSelectionCount?: number;
	savedRecipientPairCount?: number;
	readinessPassed?: boolean;
	waveName?: string | null;
};

export type SelectedSeriesSetupLaunchPlan = {
	title: string;
	label: string;
	summary: string;
	items: SelectedSeriesSetupLaunchPlanItem[];
};

export type SelectedSeriesSetupWaveContext = {
	title: string;
	label: string;
	status: ProductReadModelBadgeStatus;
	summary: string;
	guidance: string[];
};

export type SelectedSeriesSetupWorkflowCopy = {
	stepNumber: (number: number) => string;
	defaultWaveName: (number: number) => string;
	steps: Record<
		SelectedSeriesSetupWorkflowActionId,
		{
			title: string;
			description: string;
		}
	>;
	disabled: {
		confirmInstrument: string;
		saveQuestionnaire: string;
		createCollectionWave: string;
	};
	pathDisplay: Record<SelectedSeriesSetupPathStepDisplayState, string>;
	launchState: {
		createWaveFirstStatus: string;
		createWaveFirstNext: string;
		runLaunchCheckFirst: string;
		launchPassedSaveRecipients: string;
		launchPassedChooseAccess: string;
		saveRecipientsForIdentified: string;
		openCollectionOrSaveRecipients: string;
		launchPassedWithRecipients: string;
		openCollectionStartSavedRecipients: string;
		openCollectionLaunch: string;
		runLaunchCheck: string;
		needsAttention: string;
		resolveBeforeCollection: string;
		loadingSavedRecipients: string;
		savedSelections: (selectionCount: number, pairCount: number) => string;
		noSavedIdentified: string;
		noSavedLongitudinal: string;
		noSavedAnonymous: string;
	};
	launchPlan: {
		title: string;
		summary: string;
		draftWave: string;
		wave: string;
		responseMode: string;
		recipients: string;
		collectionHandoff: string;
		waveDraftReady: (waveName: string) => string;
		waveWillBeCreated: (waveName: string) => string;
		identifiedModeDetail: string;
		longitudinalModeDetail: string;
		anonymousModeDetail: string;
		chooseModeDetail: string;
		savedRecipientDetail: (selectionCount: number, pairCount: number) => string;
		identifiedNeedsRecipients: string;
		longitudinalNoRecipients: string;
		anonymousNoRecipients: string;
		saveRecipientsBeforeIdentifiedLaunch: string;
		launchPassedOpenCollection: string;
		runLaunchCheckBeforeCollection: string;
	};
	waveContext: {
		prepareForCollection: (waveName: string) => string;
		firstWaveSetup: string;
		currentDraftWave: string;
		followUpDraftWave: string;
		futureWaveSetup: string;
		firstWaveSummary: string;
		currentDraftSummary: string;
		followUpDraftSummary: (waveName: string) => string;
		closedOneWaveSummary: (
			previousWaveName: string,
			previousWaveStatus: string,
			nextWaveName: string
		) => string;
		multipleWaveSummary: (existingWaveCount: number, nextWaveName: string) => string;
		createFirstAfterSetup: string;
		recipientBelongsUntilLaunch: (waveName: string) => string;
		reviewResultsBeforeFollowup: string;
		doNotAssumeRecipients: string;
		reviewBeforePreparing: (previousWaveName: string, nextWaveName: string) => string;
		reviewExistingBeforePreparing: (nextWaveName: string) => string;
		openResultsBeforeCreating: (reviewTarget: string, nextWaveName: string) => string;
		createOnlyWhenIntentional: (nextWaveName: string) => string;
		recipientBelongsToNewDraft: (previousLabel: string) => string;
		previousWaves: string;
	};
	misc: {
		notEditable: string;
		and: string;
	};
};

export const defaultSelectedSeriesSetupWorkflowCopy: SelectedSeriesSetupWorkflowCopy = {
	stepNumber: (number) => `Step ${number}`,
	defaultWaveName: (number) => `Wave ${number}`,
	steps: {
		instrument: {
			title: 'Study source',
			description: 'Confirm what this study is based on before building the questionnaire.'
		},
		template: {
			title: 'Questionnaire',
			description: 'Build the questions respondents will answer in this study.'
		},
		scoring: {
			title: 'Results setup',
			description: 'Choose which answers become study results and how missing answers are handled.'
		},
		campaign: {
			title: 'Wave and recipients',
			description: 'Name the collection wave, choose the response mode, and prepare recipients.'
		},
		readiness: {
			title: 'Launch check',
			description:
				'Check the questionnaire, results setup, wave, recipients, and policies before collection starts.'
		}
	},
	disabled: {
		confirmInstrument: 'Confirm the instrument first.',
		saveQuestionnaire: 'Save the questionnaire first.',
		createCollectionWave: 'Create the collection wave first.'
	},
	pathDisplay: {
		done: 'Done',
		current: 'Current',
		selected: 'Selected',
		next: 'Next',
		blocked: 'Blocked'
	},
	launchState: {
		createWaveFirstStatus: 'Create collection wave first',
		createWaveFirstNext: 'Create and save the collection wave before checking launch.',
		runLaunchCheckFirst: 'Run launch check first',
		launchPassedSaveRecipients: 'Launch check passed; save recipients for identified access',
		launchPassedChooseAccess: 'Launch check passed; choose public link or save recipients',
		saveRecipientsForIdentified:
			'Save recipients below before launch so Collection can create identified access.',
		openCollectionOrSaveRecipients:
			'Open Collection to launch with a public link, or save recipients below before launch.',
		launchPassedWithRecipients: 'Launch check passed with saved recipients',
		openCollectionStartSavedRecipients:
			'Open Collection to start the wave and send the saved recipients.',
		openCollectionLaunch: 'Open Collection launch',
		runLaunchCheck: 'Run launch check',
		needsAttention: 'Needs attention',
		resolveBeforeCollection:
			'Run the launch check and resolve any listed issues before opening Collection.',
		loadingSavedRecipients: 'Loading saved recipient selection...',
		savedSelections: (selectionCount, pairCount) =>
			`${selectionCount} ${selectionCount === 1 ? 'selection' : 'selections'} saved, ${pairCount} ${
				pairCount === 1 ? 'invitation pair' : 'invitation pairs'
			} ready.`,
		noSavedIdentified: 'No saved recipients yet; save recipients before invite-only launch.',
		noSavedLongitudinal:
			'No saved recipients; save recipients for invite-only access, or use a public link and let respondents enter their repeat-participation code.',
		noSavedAnonymous: 'No saved recipients; launch with a public link or save recipients below.'
	},
	launchPlan: {
		title: 'Launch plan',
		summary: 'Prepare the wave, response mode, recipients, and Collection handoff before launch.',
		draftWave: 'Draft wave',
		wave: 'Wave',
		responseMode: 'Response mode',
		recipients: 'Recipients',
		collectionHandoff: 'Collection handoff',
		waveDraftReady: (waveName) => `${waveName} is the draft wave for this study.`,
		waveWillBeCreated: (waveName) => `${waveName} will be created when you save this step.`,
		identifiedModeDetail:
			'Identified collection requires saved recipients so each respondent can receive assigned access.',
		longitudinalModeDetail:
			'Repeat-participation collection can use public access or saved recipients; respondents use their own repeat code for comparison.',
		anonymousModeDetail: 'Anonymous collection can use a public link or saved email recipients.',
		chooseModeDetail: 'Choose how respondents should enter this wave.',
		savedRecipientDetail: (selectionCount, pairCount) =>
			`${selectionCount} saved ${selectionCount === 1 ? 'selection' : 'selections'} with ${pairCount} ${
				pairCount === 1 ? 'invitation pair' : 'invitation pairs'
			}.`,
		identifiedNeedsRecipients: 'Identified collection needs saved recipients before launch.',
		longitudinalNoRecipients:
			'No saved recipients yet. You can use a public link, or save recipients for invite-only repeat participation.',
		anonymousNoRecipients:
			'No saved recipients yet. You can still launch anonymous collection with a public link.',
		saveRecipientsBeforeIdentifiedLaunch:
			'Save recipients before opening Collection for identified launch.',
		launchPassedOpenCollection: 'Launch check passed; open Collection to start the wave.',
		runLaunchCheckBeforeCollection: 'Run launch check before opening Collection.'
	},
	waveContext: {
		prepareForCollection: (waveName) => `Prepare ${waveName} for collection`,
		firstWaveSetup: 'First wave setup',
		currentDraftWave: 'Current draft wave',
		followUpDraftWave: 'Follow-up draft wave',
		futureWaveSetup: 'Future wave setup',
		firstWaveSummary: 'Use this step to create the first collection wave and decide who can answer.',
		currentDraftSummary: 'Use this step to finish the current draft wave before opening Collection.',
		followUpDraftSummary: (waveName) =>
			`${waveName} is a draft follow-up wave. Use it only when the next collection round is intentional.`,
		closedOneWaveSummary: (previousWaveName, previousWaveStatus, nextWaveName) =>
			`${previousWaveName} is already ${previousWaveStatus}. Create ${nextWaveName} only when the next collection round is intentional.`,
		multipleWaveSummary: (existingWaveCount, nextWaveName) =>
			`${existingWaveCount} waves already exist. Create ${nextWaveName} only after the current wave results have been reviewed.`,
		createFirstAfterSetup: 'Create Wave 1 only after the questionnaire and results setup are saved.',
		recipientBelongsUntilLaunch: (waveName) =>
			`Recipient selection belongs to ${waveName} until this wave is launched.`,
		reviewResultsBeforeFollowup:
			'Review the previous wave in Results before treating this as a follow-up collection.',
		doNotAssumeRecipients:
			'Do not assume recipients are unchanged; save the intended people or group for this wave.',
		reviewBeforePreparing: (previousWaveName, nextWaveName) =>
			`Review ${previousWaveName} before preparing ${nextWaveName}`,
		reviewExistingBeforePreparing: (nextWaveName) =>
			`Review existing waves before preparing ${nextWaveName}`,
		openResultsBeforeCreating: (reviewTarget, nextWaveName) =>
			`Open Results to review or export ${reviewTarget} before creating ${nextWaveName}.`,
		createOnlyWhenIntentional: (nextWaveName) =>
			`Create ${nextWaveName} only when the next collection round is intentional.`,
		recipientBelongsToNewDraft: (previousLabel) =>
			`Recipient selection in this step will belong to the new draft wave, not to ${previousLabel}.`,
		previousWaves: 'the previous waves'
	},
	misc: {
		notEditable: 'not editable',
		and: 'and'
	}
};

export function defaultCampaignWaveName(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	copy: SelectedSeriesSetupWorkflowCopy = defaultSelectedSeriesSetupWorkflowCopy
) {
	const nextWaveNumber = Math.max(0, workspace.summary.campaignCount) + 1;
	return copy.defaultWaveName(nextWaveNumber);
}

export function toSelectedSeriesSetupWorkflowActions(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {},
	copy: SelectedSeriesSetupWorkflowCopy = defaultSelectedSeriesSetupWorkflowCopy
): SelectedSeriesSetupWorkflowAction[] {
	const templateVersionId = selectSetupTemplateVersionId(workspace, localState);
	const campaignId = selectSetupCampaignId(workspace, localState);
	const instrumentConfigured = Boolean(localState.instrumentId ?? workspace.template?.instrumentId);
	const scoringConfigured = Boolean(localState.scoringRuleId ?? workspace.scoring?.id);
	const campaignConfigured = Boolean(campaignId);

	return [
		{
			id: 'instrument',
			step: copy.stepNumber(1),
			title: copy.steps.instrument.title,
			description: copy.steps.instrument.description,
			status: instrumentConfigured ? 'ready' : 'pending',
			available: true,
			disabledReason: null
		},
		{
			id: 'template',
			step: copy.stepNumber(2),
			title: copy.steps.template.title,
			description: copy.steps.template.description,
			status: templateVersionId ? 'ready' : 'blocked',
			available: true,
			disabledReason: instrumentConfigured ? null : copy.disabled.confirmInstrument
		},
		{
			id: 'scoring',
			step: copy.stepNumber(3),
			title: copy.steps.scoring.title,
			description: copy.steps.scoring.description,
			status: scoringConfigured ? 'ready' : templateVersionId ? 'blocked' : 'blocked',
			available: Boolean(templateVersionId),
			disabledReason: templateVersionId ? null : copy.disabled.saveQuestionnaire
		},
		{
			id: 'campaign',
			step: copy.stepNumber(4),
			title: copy.steps.campaign.title,
			description: copy.steps.campaign.description,
			status: campaignConfigured ? 'ready' : templateVersionId ? 'blocked' : 'blocked',
			available: Boolean(templateVersionId),
			disabledReason: templateVersionId ? null : copy.disabled.saveQuestionnaire
		},
		{
			id: 'readiness',
			step: copy.stepNumber(5),
			title: copy.steps.readiness.title,
			description: copy.steps.readiness.description,
			status: campaignId ? toActionReadinessStatus(workspace) : 'not_available',
			available: Boolean(campaignId),
			disabledReason: campaignId ? null : copy.disabled.createCollectionWave
		}
	];
}

export function toSelectedSeriesSetupLaunchState(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {},
	options: SelectedSeriesSetupLaunchStateOptions = {},
	copy: SelectedSeriesSetupWorkflowCopy = defaultSelectedSeriesSetupWorkflowCopy
): SelectedSeriesSetupLaunchState {
	const campaignId = selectSetupCampaignId(workspace, localState);
	const readinessPassed = Boolean(options.readinessPassed ?? workspace.readiness.ready);
	const recipientSummary = toRecipientSummary(options, copy);

	if (!campaignId) {
		return {
			statusLabel: copy.launchState.createWaveFirstStatus,
			nextActionLabel: copy.launchState.createWaveFirstNext,
			collectionButtonLabel: copy.launchState.runLaunchCheckFirst,
			collectionButtonAvailable: false,
			recipientSummary
		};
	}

	if (readinessPassed) {
		const hasSavedRecipients = (options.savedRecipientSelectionCount ?? 0) > 0;
		const mode = options.responseIdentityMode;
		const noRecipientStatus =
			mode === 'identified'
				? copy.launchState.launchPassedSaveRecipients
				: copy.launchState.launchPassedChooseAccess;
		const noRecipientNextAction =
			mode === 'identified'
				? copy.launchState.saveRecipientsForIdentified
				: copy.launchState.openCollectionOrSaveRecipients;
		return {
			statusLabel: hasSavedRecipients
				? copy.launchState.launchPassedWithRecipients
				: noRecipientStatus,
			nextActionLabel: hasSavedRecipients
				? copy.launchState.openCollectionStartSavedRecipients
				: noRecipientNextAction,
			collectionButtonLabel: copy.launchState.openCollectionLaunch,
			collectionButtonAvailable: mode === 'identified' ? hasSavedRecipients : true,
			recipientSummary
		};
	}

	return {
		statusLabel:
			workspace.readiness.status === 'not_available'
				? copy.launchState.runLaunchCheck
				: copy.launchState.needsAttention,
		nextActionLabel: copy.launchState.resolveBeforeCollection,
		collectionButtonLabel: copy.launchState.runLaunchCheckFirst,
		collectionButtonAvailable: false,
		recipientSummary
	};
}

export function toSelectedSeriesSetupLaunchPlan(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {},
	options: SelectedSeriesSetupLaunchPlanOptions = {},
	copy: SelectedSeriesSetupWorkflowCopy = defaultSelectedSeriesSetupWorkflowCopy
): SelectedSeriesSetupLaunchPlan {
	const campaignId = selectSetupCampaignId(workspace, localState);
	const waveName =
		options.waveName?.trim() ||
		(campaignId && workspace.selectedCampaign?.id === campaignId
			? workspace.selectedCampaign.name.trim()
			: '') ||
		(campaignId ? copy.launchPlan.draftWave : defaultCampaignWaveName(workspace, copy));
	const mode = options.responseIdentityMode ?? workspace.selectedCampaign?.responseIdentityMode ?? null;
	const savedRecipientSelectionCount = options.savedRecipientSelectionCount ?? 0;
	const savedRecipientPairCount = options.savedRecipientPairCount ?? 0;
	const readinessPassed = Boolean(options.readinessPassed ?? workspace.readiness.ready);
	const hasSavedRecipients = savedRecipientSelectionCount > 0 || savedRecipientPairCount > 0;
	const identifiedNeedsRecipients = mode === 'identified' && !hasSavedRecipients;

	return {
		title: copy.launchPlan.title,
		label: waveName,
		summary: copy.launchPlan.summary,
		items: [
			{
				id: 'wave',
				label: copy.launchPlan.wave,
				status: campaignId ? 'ready' : 'attention',
				detail: campaignId
					? copy.launchPlan.waveDraftReady(waveName)
					: copy.launchPlan.waveWillBeCreated(waveName)
			},
			{
				id: 'response_mode',
				label: copy.launchPlan.responseMode,
				status: mode ? 'ready' : 'attention',
				detail: responseModeLaunchPlanDetail(mode, copy)
			},
			{
				id: 'recipients',
				label: copy.launchPlan.recipients,
				status: identifiedNeedsRecipients ? 'blocked' : hasSavedRecipients ? 'ready' : 'attention',
				detail: recipientLaunchPlanDetail(
					mode,
					savedRecipientSelectionCount,
					savedRecipientPairCount,
					copy
				)
			},
			{
				id: 'collection_handoff',
				label: copy.launchPlan.collectionHandoff,
				status: readinessPassed && !identifiedNeedsRecipients ? 'ready' : 'blocked',
				detail: collectionHandoffLaunchPlanDetail(
					readinessPassed,
					identifiedNeedsRecipients,
					copy
				)
			}
		]
	};
}

export function toSelectedSeriesSetupWaveContext(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {},
	copy: SelectedSeriesSetupWorkflowCopy = defaultSelectedSeriesSetupWorkflowCopy
): SelectedSeriesSetupWaveContext {
	const campaignId = selectSetupCampaignId(workspace, localState);
	const nextWaveName = defaultCampaignWaveName(workspace, copy);

	if (workspace.summary.campaignCount === 0 && !campaignId) {
		return {
			title: copy.waveContext.prepareForCollection(copy.defaultWaveName(1)),
			label: copy.waveContext.firstWaveSetup,
			status: 'pending',
			summary: copy.waveContext.firstWaveSummary,
			guidance: [
				copy.waveContext.createFirstAfterSetup,
				copy.waveContext.recipientBelongsUntilLaunch(copy.defaultWaveName(1)),
				copy.waveContext.reviewResultsBeforeFollowup
			]
		};
	}

	if (campaignId) {
		const waveName = selectedSetupWaveName(workspace, campaignId) ?? nextWaveName;
		const isFirstWave = workspace.summary.campaignCount <= 1;
		return {
			title: copy.waveContext.prepareForCollection(waveName),
			label: isFirstWave ? copy.waveContext.currentDraftWave : copy.waveContext.followUpDraftWave,
			status: 'pending',
			summary: isFirstWave
				? copy.waveContext.currentDraftSummary
				: copy.waveContext.followUpDraftSummary(waveName),
			guidance: [
				copy.waveContext.recipientBelongsUntilLaunch(waveName),
				copy.waveContext.reviewResultsBeforeFollowup,
				copy.waveContext.doNotAssumeRecipients
			]
		};
	}

	const existingWaveCount = Math.max(1, workspace.summary.campaignCount);
	const previousWaveName = latestSetupWaveName(workspace) ?? copy.defaultWaveName(existingWaveCount);
	const previousWaveStatus = formatCampaignStatus(
		workspace.selectedCampaign?.status ?? workspace.campaigns.at(-1)?.status,
		copy
	);
	const reviewTarget =
		existingWaveCount === 1
			? previousWaveName
			: `${firstSetupWaveName(workspace, copy)} ${copy.misc.and} ${previousWaveName}`;

	return {
		title:
			existingWaveCount === 1
				? copy.waveContext.reviewBeforePreparing(previousWaveName, nextWaveName)
				: copy.waveContext.reviewExistingBeforePreparing(nextWaveName),
		label: copy.waveContext.futureWaveSetup,
		status: 'pending',
		summary:
			existingWaveCount === 1
				? copy.waveContext.closedOneWaveSummary(previousWaveName, previousWaveStatus, nextWaveName)
				: copy.waveContext.multipleWaveSummary(existingWaveCount, nextWaveName),
		guidance: [
			copy.waveContext.openResultsBeforeCreating(reviewTarget, nextWaveName),
			copy.waveContext.createOnlyWhenIntentional(nextWaveName),
			copy.waveContext.recipientBelongsToNewDraft(
				existingWaveCount === 1 ? previousWaveName : copy.waveContext.previousWaves
			)
		]
	};
}

export function toSelectedSeriesSetupPath(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {},
	copy: SelectedSeriesSetupWorkflowCopy = defaultSelectedSeriesSetupWorkflowCopy
): SelectedSeriesSetupPath {
	const actions = toSelectedSeriesSetupWorkflowActions(workspace, localState, copy);
	const templateVersionId = selectSetupTemplateVersionId(workspace, localState);
	const campaignId = selectSetupCampaignId(workspace, localState);
	const instrumentDone = Boolean(
		localState.instrumentId ?? workspace.template?.instrumentId ?? templateVersionId
	);
	const doneByActionId: Record<SelectedSeriesSetupWorkflowActionId, boolean> = {
		instrument: instrumentDone,
		template: Boolean(templateVersionId),
		scoring: Boolean(localState.scoringRuleId ?? workspace.scoring?.id),
		campaign: Boolean(campaignId),
		readiness: workspace.readiness.ready
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

export function toSelectedSeriesSetupPathStepDisplay(
	step: Pick<SelectedSeriesSetupPathStep, 'id' | 'pathState'>,
	currentActionId: SelectedSeriesSetupWorkflowActionId,
	selectedActionId: SelectedSeriesSetupWorkflowActionId,
	copy: SelectedSeriesSetupWorkflowCopy = defaultSelectedSeriesSetupWorkflowCopy
): SelectedSeriesSetupPathStepDisplay {
	const isSelected = step.id === selectedActionId;
	const isNextUnfinished = step.id === currentActionId && step.pathState === 'current';

	if (isSelected && isNextUnfinished) {
		return { state: 'current', label: copy.pathDisplay.current };
	}

	if (isSelected) {
		return { state: 'selected', label: copy.pathDisplay.selected };
	}

	if (isNextUnfinished) {
		return { state: 'next', label: copy.pathDisplay.next };
	}

	if (step.pathState === 'done') {
		return { state: 'done', label: copy.pathDisplay.done };
	}

	return { state: 'blocked', label: copy.pathDisplay.blocked };
}

export function selectSetupTemplateVersionId(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {}
) {
	return localState.templateVersionId ?? workspace.template?.templateVersionId ?? null;
}

export function selectSetupCampaignId(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {}
) {
	if (localState.campaignId) {
		return localState.campaignId;
	}

	return isEditableSetupCampaign(workspace.selectedCampaign?.status)
		? (workspace.selectedCampaign?.id ?? null)
		: null;
}

function isEditableSetupCampaign(status: string | null | undefined) {
	return status === 'draft' || status === 'scheduled';
}

function selectedSetupWaveName(workspace: CampaignSeriesSetupWorkspaceResponse, campaignId: string) {
	return (
		workspace.campaigns.find((campaign) => campaign.id === campaignId)?.name.trim() ||
		(workspace.selectedCampaign?.id === campaignId ? workspace.selectedCampaign.name.trim() : '') ||
		null
	);
}

function firstSetupWaveName(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	copy: SelectedSeriesSetupWorkflowCopy
) {
	return workspace.campaigns[0]?.name.trim() || copy.defaultWaveName(1);
}

function latestSetupWaveName(workspace: CampaignSeriesSetupWorkspaceResponse) {
	return (
		workspace.selectedCampaign?.name.trim() ||
		workspace.campaigns.at(-1)?.name.trim() ||
		workspace.campaigns[0]?.name.trim() ||
		null
	);
}

function formatCampaignStatus(
	status: string | null | undefined,
	copy: SelectedSeriesSetupWorkflowCopy
) {
	return status?.replaceAll('_', ' ') || copy.misc.notEditable;
}

function toActionReadinessStatus(
	workspace: CampaignSeriesSetupWorkspaceResponse
): ProductReadModelBadgeStatus {
	if (workspace.readiness.ready) {
		return 'ready';
	}

	if (workspace.readiness.status === 'proof_only') {
		return 'proof_only';
	}

	if (workspace.readiness.status === 'not_available') {
		return 'not_available';
	}

	return 'blocked';
}

function toPathStepState(
	actionId: SelectedSeriesSetupWorkflowActionId,
	currentActionId: SelectedSeriesSetupWorkflowActionId,
	doneByActionId: Record<SelectedSeriesSetupWorkflowActionId, boolean>
): SelectedSeriesSetupPathStepState {
	if (doneByActionId[actionId]) {
		return 'done';
	}

	if (actionId === currentActionId) {
		return 'current';
	}

	return 'blocked';
}

function toRecipientSummary(
	options: SelectedSeriesSetupLaunchStateOptions,
	copy: SelectedSeriesSetupWorkflowCopy
) {
	if (options.savedRecipientLoading) {
		return copy.launchState.loadingSavedRecipients;
	}

	const selectionCount = options.savedRecipientSelectionCount ?? 0;
	if (selectionCount > 0) {
		const pairCount = options.savedRecipientPairCount ?? 0;
		return copy.launchState.savedSelections(selectionCount, pairCount);
	}

	if (options.responseIdentityMode === 'identified') {
		return copy.launchState.noSavedIdentified;
	}

	if (options.responseIdentityMode === 'anonymous_longitudinal') {
		return copy.launchState.noSavedLongitudinal;
	}

	return copy.launchState.noSavedAnonymous;
}

function responseModeLaunchPlanDetail(
	mode: string | null | undefined,
	copy: SelectedSeriesSetupWorkflowCopy
) {
	if (mode === 'identified') {
		return copy.launchPlan.identifiedModeDetail;
	}

	if (mode === 'anonymous_longitudinal') {
		return copy.launchPlan.longitudinalModeDetail;
	}

	if (mode === 'anonymous') {
		return copy.launchPlan.anonymousModeDetail;
	}

	return copy.launchPlan.chooseModeDetail;
}

function recipientLaunchPlanDetail(
	mode: string | null | undefined,
	selectionCount: number,
	pairCount: number,
	copy: SelectedSeriesSetupWorkflowCopy
) {
	if (selectionCount > 0 || pairCount > 0) {
		return copy.launchPlan.savedRecipientDetail(selectionCount, pairCount);
	}

	if (mode === 'identified') {
		return copy.launchPlan.identifiedNeedsRecipients;
	}

	if (mode === 'anonymous_longitudinal') {
		return copy.launchPlan.longitudinalNoRecipients;
	}

	return copy.launchPlan.anonymousNoRecipients;
}

function collectionHandoffLaunchPlanDetail(
	readinessPassed: boolean,
	identifiedNeedsRecipients: boolean,
	copy: SelectedSeriesSetupWorkflowCopy
) {
	if (identifiedNeedsRecipients) {
		return copy.launchPlan.saveRecipientsBeforeIdentifiedLaunch;
	}

	if (readinessPassed) {
		return copy.launchPlan.launchPassedOpenCollection;
	}

	return copy.launchPlan.runLaunchCheckBeforeCollection;
}
