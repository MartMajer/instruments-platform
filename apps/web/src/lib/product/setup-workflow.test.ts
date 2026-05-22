import { describe, expect, it } from 'vitest';
import type { CampaignSeriesSetupWorkspaceResponse } from '$lib/api/product';
import {
	defaultCampaignWaveName,
	selectSetupCampaignId,
	selectSetupTemplateVersionId,
	toSelectedSeriesSetupBlueprintJourney,
	toSelectedSeriesSetupLaunchPlan,
	toSelectedSeriesSetupLaunchState,
	toSelectedSeriesSetupPath,
	toSelectedSeriesSetupWorkflowActions
} from './setup-workflow';

describe('selected-series setup workflow model', () => {
	it('names the next collection wave from the existing campaign count', () => {
		expect(defaultCampaignWaveName(emptyWorkspace)).toBe('Wave 1');
		expect(defaultCampaignWaveName(configuredWorkspace)).toBe('Wave 2');
		expect(
			defaultCampaignWaveName({
				...configuredWorkspace,
				summary: { ...configuredWorkspace.summary, campaignCount: 4 }
			})
		).toBe('Wave 5');
	});

	it('keeps empty setup actions explicit about missing prerequisites', () => {
		const actions = toSelectedSeriesSetupWorkflowActions(emptyWorkspace);

		expect(actions).toEqual([
			expect.objectContaining({
				id: 'instrument',
				status: 'pending',
				available: true,
				disabledReason: null
			}),
			expect.objectContaining({
				id: 'template',
				status: 'blocked',
				available: true,
				disabledReason: 'Confirm the instrument first.'
			}),
			expect.objectContaining({
				id: 'scoring',
				status: 'blocked',
				available: false,
				disabledReason: 'Save the questionnaire first.'
			}),
			expect.objectContaining({
				id: 'campaign',
				status: 'blocked',
				available: false,
				disabledReason: 'Save the questionnaire first.'
			}),
			expect.objectContaining({
				id: 'readiness',
				status: 'not_available',
				available: false,
				disabledReason: 'Create the collection wave first.'
			})
		]);
	});

	it('summarizes setup as a study blueprint journey', () => {
		const emptyJourney = toSelectedSeriesSetupBlueprintJourney(emptyWorkspace);
		const configuredJourney = toSelectedSeriesSetupBlueprintJourney(configuredWorkspace);

		expect(emptyJourney).toMatchObject({
			title: 'Study blueprint journey',
			summary: 'Turn the research idea into questions, results, recipients, and a launch-ready wave.',
			currentItemId: 'questionnaire'
		});
		expect(emptyJourney.items.map((item) => ({ id: item.id, state: item.state }))).toEqual([
			{ id: 'purpose', state: 'done' },
			{ id: 'questionnaire', state: 'current' },
			{ id: 'results', state: 'blocked' },
			{ id: 'wave_recipients', state: 'blocked' },
			{ id: 'launch', state: 'blocked' }
		]);
		expect(configuredJourney.currentItemId).toBe('launch');
		expect(configuredJourney.items.map((item) => ({ id: item.id, state: item.state }))).toEqual([
			{ id: 'purpose', state: 'done' },
			{ id: 'questionnaire', state: 'done' },
			{ id: 'results', state: 'done' },
			{ id: 'wave_recipients', state: 'done' },
			{ id: 'launch', state: 'current' }
		]);
	});

	it('uses configured setup-workspace template and campaign ids for later actions', () => {
		const actions = toSelectedSeriesSetupWorkflowActions(configuredWorkspace);

		expect(selectSetupTemplateVersionId(configuredWorkspace)).toBe('template-version-id');
		expect(selectSetupCampaignId(configuredWorkspace)).toBe('campaign-id');
		expect(actions.find((action) => action.id === 'scoring')).toMatchObject({
			status: 'ready',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'campaign')).toMatchObject({
			status: 'ready',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'readiness')).toMatchObject({
			status: 'blocked',
			available: true,
			disabledReason: null
		});
	});

	it('does not treat launched or closed waves as editable setup campaigns', () => {
		const closedWorkspace: CampaignSeriesSetupWorkspaceResponse = {
			...configuredWorkspace,
			summary: {
				campaignCount: 1,
				liveCampaignCount: 0,
				missingPrerequisiteCount: 0
			},
			selectedCampaign: {
				...configuredWorkspace.selectedCampaign!,
				status: 'closed',
				latestLaunchAt: '2026-05-20T10:00:00Z'
			},
			readiness: {
				campaignId: 'campaign-id',
				status: 'proof_only',
				ready: false
			},
			campaigns: [
				{
					...configuredWorkspace.campaigns[0],
					status: 'closed',
					latestLaunchAt: '2026-05-20T10:00:00Z'
				}
			]
		};

		const actions = toSelectedSeriesSetupWorkflowActions(closedWorkspace);
		const path = toSelectedSeriesSetupPath(closedWorkspace);

		expect(selectSetupCampaignId(closedWorkspace)).toBeNull();
		expect(actions.find((action) => action.id === 'campaign')).toMatchObject({
			status: 'blocked',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'readiness')).toMatchObject({
			status: 'not_available',
			available: false,
			disabledReason: 'Create the collection wave first.'
		});
		expect(path.currentActionId).toBe('campaign');
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'instrument', state: 'done' },
			{ id: 'template', state: 'done' },
			{ id: 'scoring', state: 'done' },
			{ id: 'campaign', state: 'current' },
			{ id: 'readiness', state: 'blocked' }
		]);
	});

	it('prefers local action results over setup-workspace ids', () => {
		const localState = {
			templateVersionId: 'local-template-version-id',
			campaignId: 'local-campaign-id',
			instrumentId: 'local-instrument-id',
			scoringRuleId: 'local-scoring-rule-id'
		};
		const actions = toSelectedSeriesSetupWorkflowActions(configuredWorkspace, localState);

		expect(selectSetupTemplateVersionId(configuredWorkspace, localState)).toBe(
			'local-template-version-id'
		);
		expect(selectSetupCampaignId(configuredWorkspace, localState)).toBe('local-campaign-id');
		expect(actions.find((action) => action.id === 'instrument')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'scoring')).toMatchObject({
			status: 'ready',
			available: true
		});
	});

	it('separates launch-check state from recipient and collection launch state', () => {
		const launchState = toSelectedSeriesSetupLaunchState(
			{
				...configuredWorkspace,
				readiness: {
					campaignId: 'campaign-id',
					status: 'proof_only',
					ready: true
				}
			},
			{},
			{
				readinessPassed: true,
				savedRecipientSelectionCount: 0,
				savedRecipientPairCount: 0,
				responseIdentityMode: 'anonymous'
			}
		);

		expect(launchState).toEqual({
			statusLabel: 'Launch check passed; choose public link or save recipients',
			nextActionLabel:
				'Open Collection to launch with a public link, or save recipients below before launch.',
			collectionButtonLabel: 'Open Collection launch',
			collectionButtonAvailable: true,
			recipientSummary: 'No saved recipients; launch with a public link or save recipients below.'
		});
	});

	it('summarizes saved recipients before launch', () => {
		const launchState = toSelectedSeriesSetupLaunchState(
			{
				...configuredWorkspace,
				readiness: {
					campaignId: 'campaign-id',
					status: 'proof_only',
					ready: true
				}
			},
			{},
			{
				readinessPassed: true,
				savedRecipientSelectionCount: 1,
				savedRecipientPairCount: 2,
				responseIdentityMode: 'anonymous'
			}
		);

		expect(launchState.statusLabel).toBe('Launch check passed with saved recipients');
		expect(launchState.recipientSummary).toBe('1 selection saved, 2 invitation pairs ready.');
		expect(launchState.nextActionLabel).toBe(
			'Open Collection to start the wave and send the saved recipients.'
		);
	});

	it('summarizes wave and recipient setup as one launch plan', () => {
		const plan = toSelectedSeriesSetupLaunchPlan(
			configuredWorkspace,
			{},
			{
				responseIdentityMode: 'anonymous',
				savedRecipientSelectionCount: 0,
				savedRecipientPairCount: 0,
				readinessPassed: false
			}
		);

		expect(plan).toEqual({
			title: 'Launch plan',
			label: 'Wave 1',
			summary: 'Prepare the wave, response mode, recipients, and Collection handoff before launch.',
			items: [
				{
					id: 'wave',
					label: 'Wave',
					status: 'ready',
					detail: 'Wave 1 is the draft wave for this study.'
				},
				{
					id: 'response_mode',
					label: 'Response mode',
					status: 'ready',
					detail: 'Anonymous collection can use a public link or saved email recipients.'
				},
				{
					id: 'recipients',
					label: 'Recipients',
					status: 'attention',
					detail: 'No saved recipients yet. You can still launch anonymous collection with a public link.'
				},
				{
					id: 'collection_handoff',
					label: 'Collection handoff',
					status: 'blocked',
					detail: 'Run launch check before opening Collection.'
				}
			]
		});
	});

	it('blocks identified launch plans until recipients are saved', () => {
		const plan = toSelectedSeriesSetupLaunchPlan(
			configuredWorkspace,
			{},
			{
				responseIdentityMode: 'identified',
				savedRecipientSelectionCount: 0,
				savedRecipientPairCount: 0,
				readinessPassed: true
			}
		);

		expect(plan.items.find((item) => item.id === 'recipients')).toMatchObject({
			status: 'blocked',
			detail: 'Identified collection needs saved recipients before launch.'
		});
		expect(plan.items.find((item) => item.id === 'collection_handoff')).toMatchObject({
			status: 'blocked',
			detail: 'Save recipients before opening Collection for identified launch.'
		});
	});

	it('selects instrument import as the current task for an empty setup workspace', () => {
		const path = toSelectedSeriesSetupPath(emptyWorkspace);

		expect(path.currentActionId).toBe('instrument');
		expect(path.completedCount).toBe(0);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'instrument', state: 'current' },
			{ id: 'template', state: 'blocked' },
			{ id: 'scoring', state: 'blocked' },
			{ id: 'campaign', state: 'blocked' },
			{ id: 'readiness', state: 'blocked' }
		]);
	});

	it('advances the current setup task from local action results before workspace refresh', () => {
		const path = toSelectedSeriesSetupPath(emptyWorkspace, {
			instrumentId: 'local-instrument-id',
			templateVersionId: 'local-template-version-id',
			scoringRuleId: 'local-scoring-rule-id',
			campaignId: 'local-campaign-id'
		});

		expect(path.currentActionId).toBe('readiness');
		expect(path.completedCount).toBe(4);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'instrument', state: 'done' },
			{ id: 'template', state: 'done' },
			{ id: 'scoring', state: 'done' },
			{ id: 'campaign', state: 'done' },
			{ id: 'readiness', state: 'current' }
		]);
	});

	it('treats configured template state as past the import step for path clarity', () => {
		const path = toSelectedSeriesSetupPath(configuredWorkspace);

		expect(path.currentActionId).toBe('readiness');
		expect(path.completedCount).toBe(4);
		expect(path.steps.find((step) => step.id === 'instrument')).toMatchObject({
			pathState: 'done'
		});
	});
});

const emptyWorkspace: CampaignSeriesSetupWorkspaceResponse = {
	series: {
		id: 'series-id',
		name: 'Quarterly pulse',
		studyKind: 'own',
		isSample: false,
		sampleScenario: null,
		readOnlyReason: null,
		createdAt: '2026-05-08T08:00:00Z',
		updatedAt: '2026-05-08T08:00:00Z'
	},
	summary: {
		campaignCount: 0,
		liveCampaignCount: 0,
		missingPrerequisiteCount: 6
	},
	selectedCampaign: null,
	template: null,
	scoring: null,
	policies: {
		consent: { id: null, version: null, status: 'not_configured' },
		retention: { id: null, version: null, status: 'not_configured' },
		disclosure: { id: null, version: null, status: 'not_configured' }
	},
	readiness: {
		campaignId: null,
		status: 'not_available',
		ready: false
	},
	missingPrerequisites: [
		{
			code: 'campaign.missing',
			label: 'Campaign',
			message: 'Add a campaign to this series.',
			severity: 'blocking'
		},
		{
			code: 'template.missing',
			label: 'Template',
			message: 'Attach a survey template version to the selected campaign.',
			severity: 'blocking'
		}
	],
	campaigns: []
};

const configuredWorkspace: CampaignSeriesSetupWorkspaceResponse = {
	...emptyWorkspace,
	summary: {
		campaignCount: 1,
		liveCampaignCount: 0,
		missingPrerequisiteCount: 1
	},
	selectedCampaign: {
		id: 'campaign-id',
		name: 'Wave 1',
		status: 'draft',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en',
		templateVersionId: 'template-version-id',
		latestLaunchAt: null
	},
	template: {
		templateId: 'template-id',
		templateVersionId: 'template-version-id',
		templateName: 'Tenant burnout pulse template',
		semver: '1.0.0',
		status: 'draft',
		defaultLocale: 'en',
		instrumentId: null,
		questionCount: 5
	},
	scoring: {
		id: 'scoring-rule-id',
		ruleKey: 'burnout.total',
		ruleVersion: '1.0.0',
		status: 'draft',
		source: 'template_version'
	},
	readiness: {
		campaignId: 'campaign-id',
		status: 'blocked',
		ready: false
	},
	missingPrerequisites: [
		{
			code: 'disclosure_policy.missing',
			label: 'Disclosure policy',
			message: 'Add a disclosure policy for this series.',
			severity: 'blocking'
		}
	],
	campaigns: [
		{
			id: 'campaign-id',
			name: 'Wave 1',
			status: 'draft',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			templateVersionId: 'template-version-id',
			latestLaunchAt: null
		}
	]
};
