import { describe, expect, it } from 'vitest';
import type { CampaignSeriesSetupWorkspaceResponse } from '$lib/api/product';
import { routePageCopy } from '$lib/i18n/route-copy';
import {
	defaultCampaignWaveName,
	selectSetupCampaignId,
	selectSetupTemplateVersionId,
	toSelectedSeriesSetupLaunchPlan,
	toSelectedSeriesSetupLaunchState,
	toSelectedSeriesSetupDesignMap,
	toSelectedSeriesSetupPath,
	toSelectedSeriesSetupPathStepDisplay,
	toSelectedSeriesSetupWaveContext,
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
				disabledReason: 'Choose the questionnaire source first.'
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

	it('summarizes the saved study design from actual setup artifacts', () => {
		const design = toSelectedSeriesSetupDesignMap(configuredWorkspace);

		expect(design).toEqual({
			title: 'How this study is built',
			summary:
				'Study is the project container. Source material seeds the questionnaire; result outputs interpret answers; waves collect responses.',
			items: [
				{
					id: 'source',
					label: 'Questionnaire source',
					status: 'ready',
					detail: 'Source material is ready for this questionnaire.'
				},
				{
					id: 'questionnaire',
					label: 'Questionnaire',
					status: 'ready',
					detail: 'Tenant burnout pulse template is saved with 5 questions.'
				},
				{
					id: 'results',
					label: 'Result outputs',
					status: 'ready',
					detail: 'Result outputs are saved as burnout.total.'
				},
				{
					id: 'waves',
					label: 'Collection waves',
					status: 'pending',
					detail: '1 draft wave is prepared; launch readiness still needs attention.'
				}
			]
		});
	});

	it('labels an editable campaign as current-wave setup instead of future-wave setup', () => {
		const context = toSelectedSeriesSetupWaveContext(configuredWorkspace);

		expect(context).toMatchObject({
			title: 'Prepare Wave 1 for collection',
			label: 'Current draft wave',
			status: 'pending',
			summary: 'Use this step to finish the current draft wave before opening Collection.'
		});
		expect(context.guidance).toContain(
			'Recipient selection belongs to Wave 1 until this wave is launched.'
		);
	});

	it('separates next-wave setup from review of a locked previous wave', () => {
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
			campaigns: [
				{
					...configuredWorkspace.campaigns[0],
					status: 'closed',
					latestLaunchAt: '2026-05-20T10:00:00Z'
				}
			]
		};

		const context = toSelectedSeriesSetupWaveContext(closedWorkspace);

		expect(context).toMatchObject({
			title: 'Review Wave 1 before preparing Wave 2',
			label: 'Future wave setup',
			status: 'pending',
			summary:
				'Wave 1 is already closed. Create Wave 2 only when the next collection round is intentional.'
		});
		expect(context.guidance).toContain('Open Results to review or export Wave 1 before creating Wave 2.');
		expect(context.guidance).toContain(
			'Recipient selection in this step will belong to the new draft wave, not to Wave 1.'
		);
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
		expect(path.steps[0]).toMatchObject({
			title: 'Questionnaire source',
			description:
				'Choose reusable or imported source material. It seeds the questionnaire; it is not the study and not the final questionnaire.'
		});
		expect(path.steps[1]).toMatchObject({
			title: 'Questionnaire',
			description: 'Build the saved question set respondents will answer for this study.'
		});
		expect(path.steps[2]).toMatchObject({
			title: 'Result outputs',
			description:
				'Choose which questionnaire answers become result outputs and how missing answers are handled.'
		});
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

	it('separates the viewed setup step from the next unfinished setup step', () => {
		const path = toSelectedSeriesSetupPath(configuredWorkspace);
		const selectedStep = path.steps.find((step) => step.id === 'scoring')!;
		const nextStep = path.steps.find((step) => step.id === path.currentActionId)!;

		expect(toSelectedSeriesSetupPathStepDisplay(selectedStep, path.currentActionId, 'scoring')).toEqual({
			label: 'Selected',
			state: 'selected'
		});
		expect(toSelectedSeriesSetupPathStepDisplay(nextStep, path.currentActionId, 'scoring')).toEqual({
			label: 'Next',
			state: 'next'
		});
		expect(toSelectedSeriesSetupPathStepDisplay(nextStep, path.currentActionId, nextStep.id)).toEqual({
			label: 'Current',
			state: 'current'
		});
	});

	it('localizes setup workflow model copy for Croatian route context', () => {
		const copy = routePageCopy('hr-HR').selectedStudy.setupWorkflow;
		const path = toSelectedSeriesSetupPath(emptyWorkspace, {}, copy);
		const plan = toSelectedSeriesSetupLaunchPlan(
			configuredWorkspace,
			{},
			{
				responseIdentityMode: 'anonymous',
				savedRecipientSelectionCount: 0,
				savedRecipientPairCount: 0,
				readinessPassed: false
			},
			copy
		);

		expect(defaultCampaignWaveName(emptyWorkspace, copy)).toBe('Val 1');
		expect(path.steps[0]).toMatchObject({
			step: '1',
			title: 'Izvor upitnika',
			description:
				'Odaberite višekratni ili uvezeni izvorni materijal. On pokreće upitnik, ali nije studija ni završni upitnik.'
		});
		expect(path.steps[1]?.disabledReason).toBe('Prvo odaberite izvor upitnika.');
		expect(toSelectedSeriesSetupPathStepDisplay(path.steps[0]!, path.currentActionId, 'template', copy)).toEqual({
			label: 'Sljedeće',
			state: 'next'
		});
		expect(plan).toMatchObject({
			title: 'Plan pokretanja',
			summary: 'Pripremite val, način odgovaranja, primatelje i prijenos u Prikupljanje prije pokretanja.'
		});
		expect(plan.items.map((item) => item.label)).toEqual([
			'Val',
			'Način odgovaranja',
			'Primatelji',
			'Prijenos u Prikupljanje'
		]);
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
