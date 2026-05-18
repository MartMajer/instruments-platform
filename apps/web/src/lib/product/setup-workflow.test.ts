import { describe, expect, it } from 'vitest';
import type { CampaignSeriesSetupWorkspaceResponse } from '$lib/api/product';
import {
	selectSetupCampaignId,
	selectSetupTemplateVersionId,
	toSelectedSeriesSetupPath,
	toSelectedSeriesSetupWorkflowActions
} from './setup-workflow';

describe('selected-series setup workflow model', () => {
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
				disabledReason: null
			}),
			expect.objectContaining({
				id: 'scoring',
				status: 'blocked',
				available: false,
				disabledReason: 'Create or select a template version first.'
			}),
			expect.objectContaining({
				id: 'campaign',
				status: 'blocked',
				available: false,
				disabledReason: 'Create or select a template version first.'
			}),
			expect.objectContaining({
				id: 'readiness',
				status: 'not_available',
				available: false,
				disabledReason: 'Create a campaign draft first.'
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
