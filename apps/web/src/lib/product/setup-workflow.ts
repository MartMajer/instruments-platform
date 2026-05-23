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

export function toSelectedSeriesSetupWaveContext(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	localState: SelectedSeriesSetupWorkflowLocalState = {}
): SelectedSeriesSetupWaveContext {
	const campaignId = selectSetupCampaignId(workspace, localState);
	const nextWaveName = defaultCampaignWaveName(workspace);

	if (workspace.summary.campaignCount === 0 && !campaignId) {
		return {
			title: 'Prepare Wave 1 for collection',
			label: 'First wave setup',
			status: 'pending',
			summary: 'Use this step to create the first collection wave and decide who can answer.',
			guidance: [
				'Create Wave 1 only after the questionnaire and results setup are saved.',
				'Recipient selection belongs to Wave 1 until this wave is launched.',
				'After responses arrive, review Results before planning a follow-up wave.'
			]
		};
	}

	if (campaignId) {
		const waveName = selectedSetupWaveName(workspace, campaignId) ?? nextWaveName;
		const isFirstWave = workspace.summary.campaignCount <= 1;
		return {
			title: `Prepare ${waveName} for collection`,
			label: isFirstWave ? 'Current draft wave' : 'Follow-up draft wave',
			status: 'pending',
			summary: isFirstWave
				? 'Use this step to finish the current draft wave before opening Collection.'
				: `${waveName} is a draft follow-up wave. Use it only when the next collection round is intentional.`,
			guidance: [
				`Recipient selection belongs to ${waveName} until this wave is launched.`,
				'Review the previous wave in Results before treating this as a follow-up collection.',
				'Do not assume recipients are unchanged; save the intended people or group for this wave.'
			]
		};
	}

	const existingWaveCount = Math.max(1, workspace.summary.campaignCount);
	const previousWaveName = latestSetupWaveName(workspace) ?? `Wave ${existingWaveCount}`;
	const previousWaveStatus = formatCampaignStatus(
		workspace.selectedCampaign?.status ?? workspace.campaigns.at(-1)?.status
	);
	const reviewTarget =
		existingWaveCount === 1 ? previousWaveName : `${firstSetupWaveName(workspace)} and ${previousWaveName}`;

	return {
		title:
			existingWaveCount === 1
				? `Review ${previousWaveName} before preparing ${nextWaveName}`
				: `Review existing waves before preparing ${nextWaveName}`,
		label: 'Future wave setup',
		status: 'pending',
		summary:
			existingWaveCount === 1
				? `${previousWaveName} is already ${previousWaveStatus}. Create ${nextWaveName} only when the next collection round is intentional.`
				: `${existingWaveCount} waves already exist. Create ${nextWaveName} only after the current wave results have been reviewed.`,
		guidance: [
			`Open Results to review or export ${reviewTarget} before creating ${nextWaveName}.`,
			`Create ${nextWaveName} only when the next collection round is intentional.`,
			`Recipient selection in this step will belong to the new draft wave, not to ${
				existingWaveCount === 1 ? previousWaveName : 'the previous waves'
			}.`
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

export function toSelectedSeriesSetupPathStepDisplay(
	step: Pick<SelectedSeriesSetupPathStep, 'id' | 'pathState'>,
	currentActionId: SelectedSeriesSetupWorkflowActionId,
	selectedActionId: SelectedSeriesSetupWorkflowActionId
): SelectedSeriesSetupPathStepDisplay {
	const isSelected = step.id === selectedActionId;
	const isNextUnfinished = step.id === currentActionId && step.pathState === 'current';

	if (isSelected && isNextUnfinished) {
		return { state: 'current', label: 'Current' };
	}

	if (isSelected) {
		return { state: 'selected', label: 'Selected' };
	}

	if (isNextUnfinished) {
		return { state: 'next', label: 'Next' };
	}

	if (step.pathState === 'done') {
		return { state: 'done', label: 'Done' };
	}

	return { state: 'blocked', label: 'Blocked' };
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

function firstSetupWaveName(workspace: CampaignSeriesSetupWorkspaceResponse) {
	return workspace.campaigns[0]?.name.trim() || 'Wave 1';
}

function latestSetupWaveName(workspace: CampaignSeriesSetupWorkspaceResponse) {
	return (
		workspace.selectedCampaign?.name.trim() ||
		workspace.campaigns.at(-1)?.name.trim() ||
		workspace.campaigns[0]?.name.trim() ||
		null
	);
}

function formatCampaignStatus(status: string | null | undefined) {
	return status?.replaceAll('_', ' ') || 'not editable';
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
