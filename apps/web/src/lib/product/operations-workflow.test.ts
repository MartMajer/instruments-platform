import { describe, expect, it } from 'vitest';
import type { CampaignSeriesOperationsWorkspaceResponse } from '$lib/api/product';
import { routePageCopy } from '$lib/i18n/route-copy';
import {
	emailSuppressionReasonLabel,
	emailSuppressionSourceLabel,
	toSelectedSeriesCollectionStatusSummary,
	toSelectedSeriesOperationsPath,
	toSelectedSeriesOperationsPathStepDisplay,
	toRecipientSuppressionReview,
	toSelectedSeriesOperationsWorkflowActions
} from './operations-workflow';

describe('selected-series operations workflow model', () => {
	it('blocks operations actions when no campaign is selected', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(emptyWorkspace);

		expect(actions).toEqual([
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
				disabledReason: 'Start collection before preparing respondent access.'
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

	it('blocks launch for a draft campaign until readiness passes', () => {
		const actions = toSelectedSeriesOperationsWorkflowActions(draftWorkspace);

		expect(actions.find((action) => action.id === 'launch')).toMatchObject({
			status: 'blocked',
			available: false,
			disabledReason: 'Finish setup before starting collection.'
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

	it('selects launch as the current task for a draft campaign', () => {
		const path = toSelectedSeriesOperationsPath(draftWorkspace);

		expect(path.currentActionId).toBe('launch');
		expect(path.completedCount).toBe(0);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'launch', state: 'current' },
			{ id: 'openLink', state: 'blocked' },
			{ id: 'monitor', state: 'blocked' },
			{ id: 'close', state: 'blocked' }
		]);
	});

	it('keeps launch current after local readiness passes', () => {
		const path = toSelectedSeriesOperationsPath(draftWorkspace, {
			readinessReady: true
		});

		expect(path.currentActionId).toBe('launch');
		expect(path.completedCount).toBe(0);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
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
		expect(path.completedCount).toBe(2);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'monitor', state: 'current' },
			{ id: 'close', state: 'blocked' }
		]);
	});

	it('selects close after response activity exists', () => {
		const path = toSelectedSeriesOperationsPath(liveWithResponsesWorkspace);

		expect(path.currentActionId).toBe('close');
		expect(path.completedCount).toBe(3);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'monitor', state: 'done' },
			{ id: 'close', state: 'current' }
		]);
	});

	it('treats closed campaigns as completed collection paths', () => {
		const path = toSelectedSeriesOperationsPath(closedWorkspace);

		expect(path.currentActionId).toBe('close');
		expect(path.completedCount).toBe(4);
		expect(path.steps.map((step) => ({ id: step.id, state: step.pathState }))).toEqual([
			{ id: 'launch', state: 'done' },
			{ id: 'openLink', state: 'done' },
			{ id: 'monitor', state: 'done' },
			{ id: 'close', state: 'done' }
		]);
	});

	it('labels selected, next, done, and blocked path badges like setup', () => {
		const path = toSelectedSeriesOperationsPath(liveWorkspace);
		const selectedActionId = 'launch';

		expect(
			path.steps.map((step) =>
				toSelectedSeriesOperationsPathStepDisplay(step, path.currentActionId, selectedActionId)
			)
		).toEqual([
			{ state: 'selected', label: 'Selected' },
			{ state: 'done', label: 'Done' },
			{ state: 'next', label: 'Next' },
			{ state: 'blocked', label: 'Blocked' }
		]);

		expect(
			toSelectedSeriesOperationsPathStepDisplay(
				path.steps[2]!,
				path.currentActionId,
				path.currentActionId
			)
		).toEqual({ state: 'current', label: 'Current' });
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
				label: 'Access',
				title: 'Recipient access prepared',
				status: 'ready',
				detail:
					'1 open respondent link and 21 saved email invitations. Open-link access is broad; invite-only email access limits entry to saved recipients. Anonymous reports still keep respondent identity out of results.'
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

	it('keeps provider event reconciliation out of the primary audience status lane', () => {
		const inviteOnlyWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
			...liveWithResponsesWorkspace,
			summary: {
				...liveWithResponsesWorkspace.summary,
				openLinkAssignmentCount: 0,
				queuedInvitationCount: 21,
				sentInvitationCount: 0,
				providerAcceptedEventCount: 8,
				providerDeliveredEventCount: 7
			},
			selectedCampaign: {
				...liveWithResponsesWorkspace.selectedCampaign!,
				openLinkAssignmentCount: 0,
				queuedInvitationCount: 21,
				sentInvitationCount: 0,
				providerAcceptedEventCount: 8,
				providerDeliveredEventCount: 7
			}
		};

		const summary = toSelectedSeriesCollectionStatusSummary(inviteOnlyWorkspace);
		const accessLane = summary.lanes.find((lane) => lane.id === 'audience');

		expect(accessLane).toMatchObject({
			title: 'Invite-only access prepared',
			status: 'ready',
			detail:
				'21 saved email invitations are ready for this wave. Only saved recipients receive private access, and anonymous reports still do not show who answered.'
		});
		expect(accessLane?.detail).not.toContain('Provider');
		expect(accessLane?.detail).not.toContain('delivery event');
	});

	it('explains which imported recipients are blocked by active email suppressions', () => {
		const review = toRecipientSuppressionReview(
			[
				{ email: 'Ada@Example.com' },
				{ email: 'bo@example.com' },
				{ email: 'released@example.com' }
			],
			[
				{
					id: 'suppression-1',
					recipient: 'ada@example.com',
					reason: 'recipient_unsubscribed',
					source: 'respondent_invitation_link',
					note: null,
					createdAt: '2026-05-21T23:11:52Z',
					releasedAt: null,
					releaseReason: null,
					active: true
				},
				{
					id: 'suppression-2',
					recipient: 'bo@example.com',
					reason: 'provider_complained',
					source: 'provider_delivery_event',
					note: 'SES complaint event',
					createdAt: '2026-05-22T08:00:00Z',
					releasedAt: null,
					releaseReason: null,
					active: true
				},
				{
					id: 'suppression-3',
					recipient: 'released@example.com',
					reason: 'operator_do_not_contact',
					source: 'tenant_operator',
					note: null,
					createdAt: '2026-05-22T09:00:00Z',
					releasedAt: '2026-05-22T10:00:00Z',
					releaseReason: 'owner test release',
					active: false
				}
			]
		);

		expect(review).toMatchObject({
			hasBlockedRecipients: true,
			blockedCount: 2,
			headline: '2 recipients are on the do-not-contact list',
			guidance:
				'Use another email, remove the recipient, or release the suppression only when you are sure future invitations are appropriate.'
		});
		expect(review.items).toEqual([
			expect.objectContaining({
				id: 'suppression-1',
				recipient: 'ada@example.com',
				reasonLabel: 'Unsubscribed',
				sourceLabel: 'Invitation unsubscribe link'
			}),
			expect.objectContaining({
				id: 'suppression-2',
				recipient: 'bo@example.com',
				reasonLabel: 'Spam complaint',
				sourceLabel: 'Provider delivery event',
				note: 'SES complaint event'
			})
		]);
	});

	it('labels suppression reasons and sources in operator language', () => {
		expect(emailSuppressionReasonLabel('recipient_unsubscribed')).toBe('Unsubscribed');
		expect(emailSuppressionReasonLabel('provider_bounced')).toBe('Bounced');
		expect(emailSuppressionReasonLabel('provider_complained')).toBe('Spam complaint');
		expect(emailSuppressionReasonLabel('operator_do_not_contact')).toBe('Manually suppressed');
		expect(emailSuppressionReasonLabel('custom_reason')).toBe('Custom reason');
		expect(emailSuppressionSourceLabel('respondent_invitation_link')).toBe(
			'Invitation unsubscribe link'
		);
		expect(emailSuppressionSourceLabel('provider_delivery_event')).toBe(
			'Provider delivery event'
		);
		expect(emailSuppressionSourceLabel('tenant_operator')).toBe('Workspace admin');
	});

	it('localizes recipient suppression review labels for Croatian route context', () => {
		const review = toRecipientSuppressionReview(
			[{ email: 'Ada@Example.com' }],
			[
				{
					id: 'suppression-1',
					recipient: 'ada@example.com',
					reason: 'recipient_unsubscribed',
					source: 'respondent_invitation_link',
					note: null,
					createdAt: '2026-05-21T23:11:52Z',
					releasedAt: null,
					releaseReason: null,
					active: true
				}
			],
			'hr-HR'
		);

		expect(review).toMatchObject({
			hasBlockedRecipients: true,
			blockedCount: 1,
			headline: '1 primatelj je na popisu osoba koje ne treba kontaktirati',
			guidance:
				'Koristite drugu adresu e-pošte, uklonite primatelja ili maknite blokadu samo ako ste sigurni da je budući poziv opravdan.'
		});
		expect(review.items[0]).toMatchObject({
			reasonLabel: 'Odjavljeno',
			sourceLabel: 'Poveznica za odjavu iz poziva'
		});
		expect(emailSuppressionReasonLabel('provider_bounced', 'hr-HR')).toBe('Odbijena isporuka');
		expect(emailSuppressionReasonLabel('provider_complained', 'hr-HR')).toBe(
			'Prijava neželjene pošte'
		);
		expect(emailSuppressionReasonLabel('operator_do_not_contact', 'hr-HR')).toBe('Ručno blokirano');
		expect(emailSuppressionSourceLabel('provider_delivery_event', 'hr-HR')).toBe(
			'Događaj isporuke od pružatelja'
		);
		expect(emailSuppressionSourceLabel('tenant_operator', 'hr-HR')).toBe(
			'Administrator radnog prostora'
		);
	});

	it('localizes operations workflow and collection status copy for Croatian route context', () => {
		const copy = routePageCopy('hr-HR').selectedStudy.operationsWorkflow;
		const path = toSelectedSeriesOperationsPath(draftWorkspace, {}, copy);
		const summary = toSelectedSeriesCollectionStatusSummary(liveWithResponsesWorkspace, {}, copy);

		expect(path.steps[0]).toMatchObject({
			step: '1',
			title: 'Pokretanje prikupljanja',
			description: 'Otvorite ovo mjerenje za odgovore i zabilježite postavke korištene za izvještavanje.'
		});
		expect(path.steps[0]?.disabledReason).toBe('Dovršite postavljanje prije pokretanja prikupljanja.');
		expect(summary).toMatchObject({
			overallLabel: 'Aktivno',
			headline: 'Aktivno: prihvaća odgovore; predano: 21 odgovor',
			nextAction:
				'Nastavite prikupljati, pregledajte preliminarne Rezultate ili zatvorite prikupljanje kad ste spremni.'
		});
		expect(summary.lanes[0]).toMatchObject({
			label: 'Životni ciklus prikupljanja',
			title: 'Aktivno: prihvaća odgovore'
		});
		expect(summary.lanes[2]).toMatchObject({
			label: 'Pristup',
			title: 'Pristup primatelja pripremljen'
		});
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
