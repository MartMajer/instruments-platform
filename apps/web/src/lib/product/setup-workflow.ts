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
			title: 'Instrument',
			description: 'Confirm the instrument this study will use before building the questionnaire.',
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
			title: 'Collection setup',
			description: 'Name the collection wave and choose how respondents will answer.',
			status: campaignConfigured ? 'ready' : templateVersionId ? 'blocked' : 'blocked',
			available: Boolean(templateVersionId),
			disabledReason: templateVersionId ? null : 'Save the questionnaire first.'
		},
		{
			id: 'readiness',
			step: 'Step 5',
			title: 'Launch check',
			description: 'Check the questionnaire, results, collection wave, recipients, and policies before launch.',
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
				? 'Save a recipient selection below before launch so Collection can create identified access.'
				: 'Open Collection to launch with a public link, or save recipients below before launch.';
		return {
			statusLabel: hasSavedRecipients
				? 'Launch check passed with saved recipients'
				: noRecipientStatus,
			nextActionLabel: hasSavedRecipients
				? 'Open Collection to start the wave with the saved recipient selection.'
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
		return 'No saved recipients yet; save a recipient selection before invite-only launch.';
	}

	if (options.responseIdentityMode === 'anonymous_longitudinal') {
		return 'No saved recipients; save recipients for invite-only access, or use a public link and let respondents enter their repeat-participation code.';
	}

	return 'No saved recipients; launch with a public link or save recipients below.';
}
