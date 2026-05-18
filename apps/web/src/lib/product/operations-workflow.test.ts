import { describe, expect, it } from 'vitest';
import type { CampaignSeriesOperationsWorkspaceResponse } from '$lib/api/product';
import {
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
				disabledReason: 'Create or select a campaign before running operations.'
			}),
			expect.objectContaining({
				id: 'launch',
				status: 'not_available',
				available: false,
				disabledReason: 'Create or select a campaign before launch.'
			}),
			expect.objectContaining({
				id: 'openLink',
				status: 'not_available',
				available: false,
				disabledReason: 'Launch the selected campaign before creating an open link.'
			}),
			expect.objectContaining({
				id: 'invitations',
				status: 'not_available',
				available: false,
				disabledReason: 'Launch the selected campaign before queuing invitations.'
			}),
			expect.objectContaining({
				id: 'delivery',
				status: 'not_available',
				available: false,
				disabledReason: 'Queue invitations before processing local delivery.'
			}),
			expect.objectContaining({
				id: 'close',
				status: 'not_available',
				available: false,
				disabledReason: 'Launch the selected campaign before closing collection.'
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
			disabledReason: 'Run launch readiness and resolve blockers first.'
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

	it('enables entry and invitation actions for a live campaign', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(liveWorkspace);

		expect(actions.find((action) => action.id === 'launch')).toMatchObject({
			status: 'live',
			available: false,
			disabledReason: 'Selected campaign is already live.'
		});
		expect(actions.find((action) => action.id === 'openLink')).toMatchObject({
			status: 'ready',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'invitations')).toMatchObject({
			status: 'ready',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'delivery')).toMatchObject({
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

	it('uses identified entry and skips invitation delivery actions for identified campaigns', () => {
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
			title: 'Identified entry',
			status: 'pending',
			available: true,
			disabledReason: null
		});
		expect(actions.find((action) => action.id === 'invitations')).toMatchObject({
			status: 'not_available',
			available: false,
			disabledReason: 'Email invitation batches support anonymous campaigns only.'
		});
		expect(actions.find((action) => action.id === 'delivery')).toMatchObject({
			status: 'not_available',
			available: false,
			disabledReason: 'Local delivery requires queued anonymous invitations.'
		});
		expect(path.currentActionId).toBe('close');
	});

	it('skips invitation delivery actions for anonymous-longitudinal campaigns', () => {
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

		expect(actions.find((action) => action.id === 'invitations')).toMatchObject({
			status: 'not_available',
			available: false
		});
		expect(actions.find((action) => action.id === 'delivery')).toMatchObject({
			status: 'not_available',
			available: false
		});
		expect(path.currentActionId).toBe('close');
	});

	it('uses local action results to advance open-link, invitation, and delivery state', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(draftWorkspace, {
			readinessReady: true,
			launched: true,
			openLinkCreated: true,
			invitationsQueued: true,
			deliveryProcessed: true
		});

		expect(actions.find((action) => action.id === 'launch')).toMatchObject({
			status: 'ready',
			available: false,
			disabledReason: 'Selected campaign was launched in this session.'
		});
		expect(actions.find((action) => action.id === 'openLink')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'invitations')).toMatchObject({
			status: 'ready',
			available: true
		});
		expect(actions.find((action) => action.id === 'delivery')).toMatchObject({
			status: 'ready',
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
			{ id: 'invitations', state: 'blocked' },
			{ id: 'delivery', state: 'blocked' },
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
			{ id: 'invitations', state: 'blocked' },
			{ id: 'delivery', state: 'blocked' },
			{ id: 'close', state: 'blocked' }
		]);
	});

	it('selects invitation batch for a live campaign that already has open-link entry', () => {
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

		expect(path.currentActionId).toBe('invitations');
		expect(path.completedCount).toBe(3);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'done' },
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'invitations', state: 'current' },
			{ id: 'delivery', state: 'blocked' },
			{ id: 'close', state: 'blocked' }
		]);
	});

	it('selects local delivery when invitations are queued', () => {
		const path = toSelectedSeriesOperationsPath(liveWorkspace);

		expect(path.currentActionId).toBe('delivery');
		expect(path.completedCount).toBe(4);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'done' },
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'invitations', state: 'done' },
			{ id: 'delivery', state: 'current' },
			{ id: 'close', state: 'blocked' }
		]);
	});

	it('selects close after local delivery has been processed', () => {
		const path = toSelectedSeriesOperationsPath(liveWorkspace, {
			deliveryProcessed: true
		});

		expect(path.currentActionId).toBe('close');
		expect(path.completedCount).toBe(5);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'done' },
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'invitations', state: 'done' },
			{ id: 'delivery', state: 'done' },
			{ id: 'close', state: 'current' }
		]);
	});

	it('treats closed campaigns as completed collection paths', () => {
		const path = toSelectedSeriesOperationsPath(closedWorkspace);

		expect(path.currentActionId).toBe('close');
		expect(path.completedCount).toBe(6);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'readiness', state: 'done' },
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'invitations', state: 'done' },
			{ id: 'delivery', state: 'done' },
			{ id: 'close', state: 'done' }
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
