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
			disabledReason: null
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
			title: 'Draft campaign',
			description: 'Create a draft campaign inside the selected route campaign series.',
			status: campaignConfigured ? 'ready' : templateVersionId ? 'blocked' : 'blocked',
			available: Boolean(templateVersionId),
			disabledReason: templateVersionId ? null : 'Save the questionnaire first.'
		},
		{
			id: 'readiness',
			step: 'Step 5',
			title: 'Launch check',
			description: 'Run launch-readiness diagnostics against the selected campaign draft.',
			status: campaignId ? toActionReadinessStatus(workspace) : 'not_available',
			available: Boolean(campaignId),
			disabledReason: campaignId ? null : 'Create a campaign draft first.'
		}
	];
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
	return localState.campaignId ?? workspace.selectedCampaign?.id ?? null;
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
