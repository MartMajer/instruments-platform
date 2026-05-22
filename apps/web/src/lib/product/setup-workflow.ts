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

export type SelectedSeriesSetupBlueprintJourneyItemId =
	| 'purpose'
	| 'questionnaire'
	| 'results'
	| 'wave_recipients'
	| 'launch';

export type SelectedSeriesSetupBlueprintJourneyItemState = 'done' | 'current' | 'blocked';

export type SelectedSeriesSetupBlueprintJourneyItem = {
	id: SelectedSeriesSetupBlueprintJourneyItemId;
	label: string;
	description: string;
	state: SelectedSeriesSetupBlueprintJourneyItemState;
};

export type SelectedSeriesSetupBlueprintJourney = {
	title: string;
	summary: string;
	currentItemId: SelectedSeriesSetupBlueprintJourneyItemId;
	items: SelectedSeriesSetupBlueprintJourneyItem[];
};

export function defaultCampaignWaveName(workspace: CampaignSeriesSetupWorkspaceResponse) {
	const nextWaveNumber = Math.max(0, workspace.summary.campaignCount) + 1;
	return `Wave ${nextWaveNumber}`;
}

export function toSelectedSeriesSetupWorkflowActions(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {}
): SelectedSeriesSetupWorkflowAction[] {
	const templateVersionId = selectSetupTemplateVersionId(workspace, localState);
	const campaignId = selectSetupCampaignId(workspace, localState);
	const instrumentConfigured = Boolean(localState.instrumentId ?? workspace.template?.instrumentId);
	const scoringConfigured = Boolean(localState.scoringRuleId ?? workspace.scoring?.id);
	const campaignConfigured = Boolean(campaignId);

	return [
		{
			id: 'instrument',
			step: 'Step 1',
			title: 'Study source',
			description: 'Confirm what this study is based on before building the questionnaire.',
			status: instrumentConfigured ? 'ready' : 'pending',
			available: true,
			disabledReason: null
		},
		{
			id: 'template',
			step: 'Step 2',
			title: 'Questionnaire',
			description:
				'Build the questions respondents will answer in this study.',
			status: templateVersionId ? 'ready' : 'blocked',
			available: true,
			disabledReason: instrumentConfigured ? null : 'Confirm the instrument first.'
		},
		{
			id: 'scoring',
			step: 'Step 3',
			title: 'Results setup',
			description: 'Choose which answers become study results and how missing answers are handled.',
			status: scoringConfigured ? 'ready' : templateVersionId ? 'blocked' : 'blocked',
			available: Boolean(templateVersionId),
			disabledReason: templateVersionId ? null : 'Save the questionnaire first.'
		},
		{
			id: 'campaign',
			step: 'Step 4',
			title: 'Wave and recipients',
			description: 'Name the collection wave, choose the response mode, and prepare recipients.',
			status: campaignConfigured ? 'ready' : templateVersionId ? 'blocked' : 'blocked',
			available: Boolean(templateVersionId),
			disabledReason: templateVersionId ? null : 'Save the questionnaire first.'
		},
		{
			id: 'readiness',
			step: 'Step 5',
			title: 'Launch check',
			description: 'Check the questionnaire, results setup, wave, recipients, and policies before collection starts.',
			status: campaignId ? toActionReadinessStatus(workspace) : 'not_available',
			available: Boolean(campaignId),
			disabledReason: campaignId ? null : 'Create the collection wave first.'
		}
	];
}

export function toSelectedSeriesSetupLaunchState(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {},
	options: SelectedSeriesSetupLaunchStateOptions = {}
): SelectedSeriesSetupLaunchState {
	const campaignId = selectSetupCampaignId(workspace, localState);
	const readinessPassed = Boolean(options.readinessPassed ?? workspace.readiness.ready);
	const recipientSummary = toRecipientSummary(options);

	if (!campaignId) {
		return {
			statusLabel: 'Create collection wave first',
			nextActionLabel: 'Create and save the collection wave before checking launch.',
			collectionButtonLabel: 'Run launch check first',
			collectionButtonAvailable: false,
			recipientSummary
		};
	}

	if (readinessPassed) {
		const hasSavedRecipients = (options.savedRecipientSelectionCount ?? 0) > 0;
		const mode = options.responseIdentityMode;
		const noRecipientStatus =
			mode === 'identified'
				? 'Launch check passed; save recipients for identified access'
				: 'Launch check passed; choose public link or save recipients';
		const noRecipientNextAction =
			mode === 'identified'
				? 'Save recipients below before launch so Collection can create identified access.'
				: 'Open Collection to launch with a public link, or save recipients below before launch.';
		return {
			statusLabel: hasSavedRecipients
				? 'Launch check passed with saved recipients'
				: noRecipientStatus,
			nextActionLabel: hasSavedRecipients
				? 'Open Collection to start the wave and send the saved recipients.'
				: noRecipientNextAction,
			collectionButtonLabel: 'Open Collection launch',
			collectionButtonAvailable: mode === 'identified' ? hasSavedRecipients : true,
			recipientSummary
		};
	}

	return {
		statusLabel:
			workspace.readiness.status === 'not_available' ? 'Run launch check' : 'Needs attention',
		nextActionLabel: 'Run the launch check and resolve any listed issues before opening Collection.',
		collectionButtonLabel: 'Run launch check first',
		collectionButtonAvailable: false,
		recipientSummary
	};
}

export function toSelectedSeriesSetupLaunchPlan(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {},
	options: SelectedSeriesSetupLaunchPlanOptions = {}
): SelectedSeriesSetupLaunchPlan {
	const campaignId = selectSetupCampaignId(workspace, localState);
	const waveName =
		options.waveName?.trim() ||
		(campaignId && workspace.selectedCampaign?.id === campaignId
			? workspace.selectedCampaign.name.trim()
			: '') ||
		(campaignId ? 'Draft wave' : defaultCampaignWaveName(workspace));
	const mode = options.responseIdentityMode ?? workspace.selectedCampaign?.responseIdentityMode ?? null;
	const savedRecipientSelectionCount = options.savedRecipientSelectionCount ?? 0;
	const savedRecipientPairCount = options.savedRecipientPairCount ?? 0;
	const readinessPassed = Boolean(options.readinessPassed ?? workspace.readiness.ready);
	const hasSavedRecipients = savedRecipientSelectionCount > 0 || savedRecipientPairCount > 0;
	const identifiedNeedsRecipients = mode === 'identified' && !hasSavedRecipients;

	return {
		title: 'Launch plan',
		label: waveName,
		summary: 'Prepare the wave, response mode, recipients, and Collection handoff before launch.',
		items: [
			{
				id: 'wave',
				label: 'Wave',
				status: campaignId ? 'ready' : 'attention',
				detail: campaignId
					? `${waveName} is the draft wave for this study.`
					: `${waveName} will be created when you save this step.`
			},
			{
				id: 'response_mode',
				label: 'Response mode',
				status: mode ? 'ready' : 'attention',
				detail: responseModeLaunchPlanDetail(mode)
			},
			{
				id: 'recipients',
				label: 'Recipients',
				status: identifiedNeedsRecipients ? 'blocked' : hasSavedRecipients ? 'ready' : 'attention',
				detail: recipientLaunchPlanDetail(mode, savedRecipientSelectionCount, savedRecipientPairCount)
			},
			{
				id: 'collection_handoff',
				label: 'Collection handoff',
				status: readinessPassed && !identifiedNeedsRecipients ? 'ready' : 'blocked',
				detail: collectionHandoffLaunchPlanDetail(readinessPassed, identifiedNeedsRecipients)
			}
		]
	};
}

export function toSelectedSeriesSetupPath(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {}
): SelectedSeriesSetupPath {
	const actions = toSelectedSeriesSetupWorkflowActions(workspace, localState);
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

export function toSelectedSeriesSetupBlueprintJourney(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {}
): SelectedSeriesSetupBlueprintJourney {
	const templateVersionId = selectSetupTemplateVersionId(workspace, localState);
	const scoringConfigured = Boolean(localState.scoringRuleId ?? workspace.scoring?.id);
	const campaignId = selectSetupCampaignId(workspace, localState);
	const questionnaireDone = Boolean(templateVersionId);
	const resultsDone = scoringConfigured;
	const waveRecipientsDone = Boolean(campaignId);
	const launchDone = workspace.readiness.ready;

	const items: SelectedSeriesSetupBlueprintJourneyItem[] = [
		{
			id: 'purpose',
			label: 'Purpose',
			description: 'Keep the study name and intent clear before authoring questions.',
			state: 'done'
		},
		{
			id: 'questionnaire',
			label: 'Questionnaire',
			description: 'Write the questions and answer formats respondents will see.',
			state: questionnaireDone ? 'done' : 'current'
		},
		{
			id: 'results',
			label: 'Results setup',
			description: 'Decide which answers become scores or summaries.',
			state: resultsDone ? 'done' : questionnaireDone ? 'current' : 'blocked'
		},
		{
			id: 'wave_recipients',
			label: 'Wave and recipients',
			description: 'Create the collection wave and save who should receive it.',
			state: waveRecipientsDone ? 'done' : resultsDone ? 'current' : 'blocked'
		},
		{
			id: 'launch',
			label: 'Launch check',
			description: 'Confirm the study can move into collection.',
			state: launchDone ? 'done' : waveRecipientsDone ? 'current' : 'blocked'
		}
	];
	const currentItem = items.find((item) => item.state === 'current') ?? items.at(-1) ?? items[0];

	return {
		title: 'Study blueprint journey',
		summary: 'Turn the research idea into questions, results, recipients, and a launch-ready wave.',
		currentItemId: currentItem.id,
		items
	};
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

function toRecipientSummary(options: SelectedSeriesSetupLaunchStateOptions) {
	if (options.savedRecipientLoading) {
		return 'Loading saved recipient selection...';
	}

	const selectionCount = options.savedRecipientSelectionCount ?? 0;
	if (selectionCount > 0) {
		const pairCount = options.savedRecipientPairCount ?? 0;
		return `${selectionCount} ${selectionCount === 1 ? 'selection' : 'selections'} saved, ${pairCount} ${
			pairCount === 1 ? 'invitation pair' : 'invitation pairs'
		} ready.`;
	}

	if (options.responseIdentityMode === 'identified') {
		return 'No saved recipients yet; save recipients before invite-only launch.';
	}

	if (options.responseIdentityMode === 'anonymous_longitudinal') {
		return 'No saved recipients; save recipients for invite-only access, or use a public link and let respondents enter their repeat-participation code.';
	}

	return 'No saved recipients; launch with a public link or save recipients below.';
}

function responseModeLaunchPlanDetail(mode: string | null | undefined) {
	if (mode === 'identified') {
		return 'Identified collection requires saved recipients so each respondent can receive assigned access.';
	}

	if (mode === 'anonymous_longitudinal') {
		return 'Repeat-participation collection can use public access or saved recipients; respondents use their own repeat code for comparison.';
	}

	if (mode === 'anonymous') {
		return 'Anonymous collection can use a public link or saved email recipients.';
	}

	return 'Choose how respondents should enter this wave.';
}

function recipientLaunchPlanDetail(
	mode: string | null | undefined,
	selectionCount: number,
	pairCount: number
) {
	if (selectionCount > 0 || pairCount > 0) {
		return `${selectionCount} saved ${selectionCount === 1 ? 'selection' : 'selections'} with ${pairCount} ${
			pairCount === 1 ? 'invitation pair' : 'invitation pairs'
		}.`;
	}

	if (mode === 'identified') {
		return 'Identified collection needs saved recipients before launch.';
	}

	if (mode === 'anonymous_longitudinal') {
		return 'No saved recipients yet. You can use a public link, or save recipients for invite-only repeat participation.';
	}

	return 'No saved recipients yet. You can still launch anonymous collection with a public link.';
}

function collectionHandoffLaunchPlanDetail(readinessPassed: boolean, identifiedNeedsRecipients: boolean) {
	if (identifiedNeedsRecipients) {
		return 'Save recipients before opening Collection for identified launch.';
	}

	if (readinessPassed) {
		return 'Launch check passed; open Collection to start the wave.';
	}

	return 'Run launch check before opening Collection.';
}
