import { describe, expect, it } from 'vitest';
import type { CampaignSeriesOperationsWorkspaceResponse } from '$lib/api/product';
import {
	toSelectedSeriesCollectionStatusSummary,
	toSelectedSeriesOperationsPath,
	toSelectedSeriesOperationsWorkflowActions
} from './operations-workflow';

describe('selected-series operations workflow model', () => {
	it('blocks operations actions when no campaign is selected', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(emptyWorkspace);

		expect(actions).toEqual([
			expect.objectContaining({
				id: 'readiness',
				status: 'not_available',
				available: false,
				disabledReason: 'Create a collection wave in setup before checking readiness.'
			}),
			expect.objectContaining({
				id: 'launch',
				status: 'not_available',
				available: false,
				disabledReason: 'Create a collection wave before starting collection.'
			}),
			expect.objectContaining({
				id: 'openLink',
				status: 'not_available',
				available: false,
				disabledReason: 'Start collection before creating respondent access.'
			}),
			expect.objectContaining({
				id: 'monitor',
				status: 'not_available',
				available: false,
				disabledReason: 'Start collection before monitoring responses.'
			}),
			expect.objectContaining({
				id: 'close',
				status: 'not_available',
				available: false,
				disabledReason: 'Create a collection wave before closing collection.'
			})
		]);
	});

	it('allows readiness for a draft campaign and blocks later operations until readiness passes', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(draftWorkspace);

		expect(actions.find((action) => action.id === 'readiness')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'launch')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason:
				'Run the pre-launch check. If it says Blocked, open Setup and finish the listed items first.'
		});
		expect(actions.find((action) => action.id === 'openLink')).toMatchObject({
			status: 'blocked',
			available: false
		});
		expect(actions.find((action) => action.id === 'close')).toMatchObject({
			status: 'blocked',
			available: false
		});
	});

	it('enables launch after local readiness passes', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(draftWorkspace, {
			readinessReady: true
		});

		expect(actions.find((action) => action.id === 'readiness')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'launch')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
	});

	it('enables respondent access and monitoring actions for a live campaign', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(liveWorkspace);

		expect(actions.find((action) => action.id === 'launch')).toMatchObject({
			status: 'live',
			available: false,
			disabledReason: 'Collection is already live.'
		});
		expect(actions.find((action) => action.id === 'openLink')).toMatchObject({
			status: 'ready',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'monitor')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'close')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
	});

	it('uses identified entry wording for identified campaigns', () => {
		const identifiedWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
			...liveWorkspace,
			selectedCampaign: {
				...liveWorkspace.selectedCampaign!,
				responseIdentityMode: 'identified',
				openLinkAssignmentCount: 0,
				queuedInvitationCount: 0,
				deliveryAttemptCount: 0
			}
		};

		const actions = toSelectedSeriesOperationsWorkflowActions(identifiedWorkspace);
		const path = toSelectedSeriesOperationsPath(identifiedWorkspace, {
			openLinkCreated: true
		});

		expect(actions.find((action) => action.id === 'openLink')).toMatchObject({
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(path.currentActionId).toBe('monitor');
	});

	it('keeps anonymous-longitudinal campaigns on the same collection path', () => {
		const longitudinalWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
			...liveWorkspace,
			selectedCampaign: {
				...liveWorkspace.selectedCampaign!,
				responseIdentityMode: 'anonymous_longitudinal',
				queuedInvitationCount: 0,
				deliveryAttemptCount: 0
			}
		};

		const actions = toSelectedSeriesOperationsWorkflowActions(longitudinalWorkspace);
		const path = toSelectedSeriesOperationsPath(longitudinalWorkspace);

		expect(actions.find((action) => action.id === 'openLink')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(path.currentActionId).toBe('monitor');
	});

	it('uses local action results to advance open-link and collection state', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(draftWorkspace, {
			readinessReady: true,
			launched: true,
			openLinkCreated: true
		});

		expect(actions.find((action) => action.id === 'launch')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Collection was started in this session.'
		});
		expect(actions.find((action) => action.id === 'openLink')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'monitor')).toMatchObject({
			status: 'pending',
			available: true
		});
		expect(actions.find((action) => action.id === 'close')).toMatchObject({
			status: 'pending',
			available: true
		});
	});

	it('selects launch readiness as the current task for a draft campaign', () => {
		const path = toSelectedSeriesOperationsPath(draftWorkspace);

		expect(path.currentActionId).toBe('readiness');
		expect(path.completedCount).toBe(0);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'current' },
			{ id: 'launch', state: 'blocked' },
			{ id: 'openLink', state: 'blocked' },
			{ id: 'monitor', state: 'blocked' },
			{ id: 'close', state: 'blocked' }
		]);
	});

	it('advances the current task to launch after local readiness passes', () => {
		const path = toSelectedSeriesOperationsPath(draftWorkspace, {
			readinessReady: true
		});

		expect(path.currentActionId).toBe('launch');
		expect(path.completedCount).toBe(1);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'done' },
			{ id: 'launch', state: 'current' },
			{ id: 'openLink', state: 'blocked' },
			{ id: 'monitor', state: 'blocked' },
			{ id: 'close', state: 'blocked' }
		]);
	});

	it('selects monitor for a live campaign that already has respondent access', () => {
		const path = toSelectedSeriesOperationsPath({
			...liveWorkspace,
			summary: {
				...liveWorkspace.summary,
				queuedInvitationCount: 0
			},
			selectedCampaign: {
				...liveWorkspace.selectedCampaign!,
				queuedInvitationCount: 0
			}
		});

		expect(path.currentActionId).toBe('monitor');
		expect(path.completedCount).toBe(3);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'done' },
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'monitor', state: 'current' },
			{ id: 'close', state: 'blocked' }
		]);
	});

	it('selects close after response activity exists', () => {
		const path = toSelectedSeriesOperationsPath(liveWithResponsesWorkspace);

		expect(path.currentActionId).toBe('close');
		expect(path.completedCount).toBe(4);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'done' },
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'monitor', state: 'done' },
			{ id: 'close', state: 'current' }
		]);
	});

	it('treats closed campaigns as completed collection paths', () => {
		const path = toSelectedSeriesOperationsPath(closedWorkspace);

		expect(path.currentActionId).toBe('close');
		expect(path.completedCount).toBe(5);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'done' },
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'monitor', state: 'done' },
			{ id: 'close', state: 'done' }
		]);
	});

	it('summarizes live collection with response progress and recipient context', () => {
		const summary = toSelectedSeriesCollectionStatusSummary(liveWithResponsesWorkspace);

		expect(summary).toMatchObject({
			overallStatus: 'live',
			overallLabel: 'Live',
			headline: 'Live: accepting responses with 21 submitted',
			guidance:
				'Use this page to monitor response progress and recipient access. Close collection when the response window is finished.',
			nextAction: 'Keep collecting, review preliminary Results, or close collection when ready.'
		});
		expect(summary.lanes).toEqual([
			expect.objectContaining({
				id: 'lifecycle',
				label: 'Collection lifecycle',
				title: 'Live: accepting responses',
				status: 'live',
				detail: 'Respondents can still submit. Results remain preliminary until collection closes.'
			}),
			expect.objectContaining({
				id: 'responses',
				label: 'Response progress',
				title: '21 submitted',
				status: 'ready',
				detail: '24 started, 3 in progress, 21 submitted.'
			}),
			expect.objectContaining({
				id: 'audience',
				label: 'Audience and access',
				title: 'Recipient access prepared',
				status: 'ready',
				detail:
					'1 respondent link and 21 prepared invitations. Anonymous reports keep respondent identity out of results.'
			}),
			expect.objectContaining({
				id: 'reporting',
				label: 'Reporting readiness',
				title: 'reportable',
				status: 'ready',
				detail:
					'Results can be reviewed, but live collection data should be treated as preliminary until closed.'
			})
		]);
	});
});

const emptyWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
	series: {
		id: 'series-id',
		name: 'Quarterly burnout pulse',
		studyKind: 'own',
		isSample: false,
		sampleScenario: null,
		readOnlyReason: null,
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-02T09:00:00Z'
	},
	summary: {
		campaignCount: 0,
		liveCampaignCount: 0,
		openLinkAssignmentCount: 0,
		queuedInvitationCount: 0,
		sentInvitationCount: 0,
		failedInvitationCount: 0,
		deliveryAttemptCount: 0,
		startedResponseCount: 0,
		draftResponseCount: 0,
		submittedResponseCount: 0,
		latestResponseStartedAt: null,
		latestResponseSubmittedAt: null,
		collectionStatus: 'not_started',
		reportVisibilityStatus: 'unknown_policy',
		collectionGuidance: 'Share the public link or send invitations.',
		missingPrerequisiteCount: 1
	},
	selectedCampaign: null,
	missingPrerequisites: [],
	campaigns: []
};

const draftWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
	...emptyWorkspace,
	summary: {
		...emptyWorkspace.summary,
		campaignCount: 1,
		missingPrerequisiteCount: 0
	},
	selectedCampaign: {
		id: 'campaign-id',
		name: 'Wave 1',
		status: 'draft',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en',
		latestLaunchSnapshotId: null,
		latestLaunchAt: null,
		closedAt: null,
		closedByUserId: null,
		closeReason: null,
		startedResponseCount: 0,
		draftResponseCount: 0,
		submittedResponseCount: 0,
		latestResponseStartedAt: null,
		latestResponseSubmittedAt: null,
		collectionStatus: 'not_started',
		reportVisibilityStatus: 'unknown_policy',
		collectionGuidance: 'Share the public link or send invitations.',
		openLinkAssignmentCount: 0,
		queuedInvitationCount: 0,
		sentInvitationCount: 0,
		failedInvitationCount: 0,
		deliveryAttemptCount: 0,
		latestDeliveryAttemptAt: null
	},
	campaigns: []
};

const liveWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
	...draftWorkspace,
	summary: {
		...draftWorkspace.summary,
		liveCampaignCount: 1,
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 2,
		deliveryAttemptCount: 0
	},
	selectedCampaign: {
		...draftWorkspace.selectedCampaign!,
		status: 'live',
		latestLaunchSnapshotId: 'launch-snapshot-id',
		latestLaunchAt: '2026-05-05T08:30:00Z',
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 2,
		deliveryAttemptCount: 0,
		latestDeliveryAttemptAt: null
	}
};

const liveWithResponsesWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
	...liveWorkspace,
	summary: {
		...liveWorkspace.summary,
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 21,
		startedResponseCount: 24,
		draftResponseCount: 3,
		submittedResponseCount: 21,
		latestResponseStartedAt: '2026-05-20T19:30:00Z',
		latestResponseSubmittedAt: '2026-05-20T19:34:00Z',
		reportVisibilityStatus: 'reportable'
	},
	selectedCampaign: {
		...liveWorkspace.selectedCampaign!,
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 21,
		startedResponseCount: 24,
		draftResponseCount: 3,
		submittedResponseCount: 21,
		latestResponseStartedAt: '2026-05-20T19:30:00Z',
		latestResponseSubmittedAt: '2026-05-20T19:34:00Z',
		reportVisibilityStatus: 'reportable'
	}
};

const closedWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
	...liveWorkspace,
	summary: {
		...liveWorkspace.summary,
		liveCampaignCount: 0,
		deliveryAttemptCount: 2
	},
	selectedCampaign: {
		...liveWorkspace.selectedCampaign!,
		status: 'closed',
		closedAt: '2026-05-11T14:30:00Z',
		closedByUserId: 'actor-id',
		closeReason: 'Collection complete',
		deliveryAttemptCount: 2,
		latestDeliveryAttemptAt: '2026-05-11T14:20:00Z'
	}
};
