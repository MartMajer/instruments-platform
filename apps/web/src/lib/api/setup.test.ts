import { describe, expect, it, vi } from 'vitest';
import type { ApiClient } from './client';
import { createSetupApi } from './setup';

describe('createSetupApi', () => {
	it('calls respondent-rule setup endpoints with safe assignment responses', async () => {
		const request = vi.fn(async <T>(path: string): Promise<T> => responseFor(path) as T);
		const api = createSetupApi({
			request: request as ApiClient['request'],
			requestText: vi.fn() as ApiClient['requestText']
		});

		const listedRules = await api.listCampaignRespondentRules('campaign-id');
		const savedRules = await api.updateCampaignRespondentRules('campaign-id', {
			rules: [{ rule: '{"kind":"self","role":"self"}' }]
		});
		const assignments = await api.listCampaignAssignments('campaign-id');

		expect(request).toHaveBeenNthCalledWith(1, '/campaigns/campaign-id/respondent-rules');
		expect(request).toHaveBeenNthCalledWith(
			2,
			'/campaigns/campaign-id/respondent-rules',
			expect.objectContaining({
				method: 'PUT',
				body: JSON.stringify({ rules: [{ rule: '{"kind":"self","role":"self"}' }] })
			})
		);
		expect(request).toHaveBeenNthCalledWith(3, '/campaigns/campaign-id/assignments');
		expect(listedRules.rules[0].ruleKind).toBe('self');
		expect(savedRules.rules[0].assignmentPairCount).toBe(1);
		expect(assignments.assignmentCount).toBe(1);
		expect(assignments.assignments[0].respondent?.label).toBe('Ana Analyst');

		const serialized = JSON.stringify(assignments);
		expect(serialized).not.toContain('token');
		expect(serialized).not.toContain('hash');
		expect(serialized).not.toContain('recipient');
		expect(serialized).not.toContain('answer');
	});

	it('calls identified entry setup and respondent endpoints', async () => {
		const request = vi.fn(async <T>(path: string): Promise<T> => responseFor(path) as T);
		const api = createSetupApi({
			request: request as ApiClient['request'],
			requestText: vi.fn() as ApiClient['requestText']
		});

		const created = await api.createCampaignIdentifiedEntry('campaign-id');
		const entry = await api.getIdentifiedEntry('identified-token');
		const session = await api.createIdentifiedEntrySession('identified-token', {
			locale: 'en',
			acceptedConsentDocumentId: 'consent-document-id',
			acceptedGrants: ['data_processing']
		});

		expect(request).toHaveBeenNthCalledWith(
			1,
			'/campaigns/campaign-id/identified-entry',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			2,
			'/respondent/identified-entries/identified-token'
		);
		expect(request).toHaveBeenNthCalledWith(
			3,
			'/respondent/identified-entries/identified-token/sessions',
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify({
					locale: 'en',
					acceptedConsentDocumentId: 'consent-document-id',
					acceptedGrants: ['data_processing']
				})
			})
		);
		expect(created.subjectId).toBe('subject-id');
		expect(entry.responseIdentityMode).toBe('identified');
		expect(session.publicHandle).toBe('public-session-handle');
	});

	it('calls the GF05 setup endpoints with the expected methods', async () => {
		const request = vi.fn(async <T>(path: string): Promise<T> => responseFor(path) as T);
		const requestText = vi.fn(async () => ({
			body: 'campaign_id,dimension_code,disclosure\r\n',
			contentType: 'text/csv',
			contentDisposition: 'attachment; filename="campaign-report-proof.csv"',
			byteSize: 36
		}));
		const api = createSetupApi({
			request: request as ApiClient['request'],
			requestText: requestText as ApiClient['requestText']
		});

		const session = await api.getCurrentSession();
		await api.getCsrfToken();
		await api.createPrivateInstrumentImport(sampleInstrumentRequest);
		await api.listInstruments();
		await api.createTemplateVersion(sampleTemplateRequest);
		await api.getTemplateVersion('template-version-id');
		await api.createScoringRule(sampleScoringRuleRequest);
		await api.createCampaignSeries({ name: 'Quarterly pulse' });
		await api.getCampaignSeriesTwoWaveProof('series-id');
		await api.getCampaignSeriesWaveComparisonProof('series-id');
		await api.createCampaign(sampleCampaignRequest);
		await api.getLaunchReadiness('campaign-id');
		const launch = await api.launchCampaign('campaign-id');
		await api.createCampaignOpenLink('campaign-id');
		const invitationBatch = await api.createCampaignInvitationBatch('campaign-id', {
			recipients: [{ email: 'ada@example.com' }, { email: 'bo@example.com' }]
		});
		const delivery = await api.processCampaignEmailDeliveries('campaign-id', { batchSize: 5 });
		await api.getRespondentCampaign('campaign-id');
		await api.createLabAssignment('campaign-id');
		await api.createResponseSession({ assignmentId: 'assignment-id', locale: 'en' });
		await api.saveAnswers('session-id', {
			answers: [{ questionId: 'question-id', value: '4' }]
		});
		await api.submitResponseSession('session-id', { timeTakenMs: 1200 });
		await api.computeResponseScores('session-id');
		await api.getOpenLinkEntry('open-link-token');
		await api.createOpenLinkSession('open-link-token', {
			locale: 'en',
			acceptedConsentDocumentId: 'consent-document-id',
			acceptedGrants: ['data_processing', 'research_participation']
		});
		await api.saveOpenLinkAnswers('open-link-token', 'session-id', {
			answers: [{ questionId: 'question-id', value: '4' }]
		});
		await api.submitOpenLinkSession('open-link-token', 'session-id', { timeTakenMs: 1200 });
		await api.createCampaignReportProofExport('campaign-id');
		await api.createCampaignSeriesResponseExport('series-id');
		const artifact = await api.getExportArtifact('artifact-id');
		const download = await api.downloadExportArtifactCsv('artifact-id');

		expect(request).toHaveBeenNthCalledWith(1, '/auth/session');
		expect(request).toHaveBeenNthCalledWith(2, '/auth/csrf');
		expect(request).toHaveBeenNthCalledWith(
			3,
			'/instruments/private-imports',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(4, '/instruments');
		expect(request).toHaveBeenNthCalledWith(
			5,
			'/template-versions',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(6, '/template-versions/template-version-id');
		expect(request).toHaveBeenNthCalledWith(
			7,
			'/scoring-rules',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			8,
			'/campaign-series',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(9, '/campaign-series/series-id/two-wave-proof');
		expect(request).toHaveBeenNthCalledWith(10, '/campaign-series/series-id/wave-comparison-proof');
		expect(request).toHaveBeenNthCalledWith(
			11,
			'/campaigns',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(12, '/campaigns/campaign-id/launch-readiness');
		expect(request).toHaveBeenNthCalledWith(
			13,
			'/campaigns/campaign-id/launch',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			14,
			'/campaigns/campaign-id/open-link',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			15,
			'/campaigns/campaign-id/invitation-batches',
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify({
					recipients: [{ email: 'ada@example.com' }, { email: 'bo@example.com' }]
				})
			})
		);
		expect(request).toHaveBeenNthCalledWith(
			16,
			'/campaigns/campaign-id/notification-deliveries/process',
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify({ batchSize: 5 })
			})
		);
		expect(request).toHaveBeenNthCalledWith(17, '/respondent/campaigns/campaign-id');
		expect(request).toHaveBeenNthCalledWith(
			18,
			'/respondent/campaigns/campaign-id/lab-assignment',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			19,
			'/respondent/sessions',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			20,
			'/respondent/sessions/session-id/answers',
			expect.objectContaining({ method: 'PUT' })
		);
		expect(request).toHaveBeenNthCalledWith(
			21,
			'/respondent/sessions/session-id/submit',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			22,
			'/respondent/sessions/session-id/scores',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(23, '/respondent/open-links/open-link-token');
		expect(request).toHaveBeenNthCalledWith(
			24,
			'/respondent/open-links/open-link-token/sessions',
			expect.objectContaining({
				method: 'POST',
				body: JSON.stringify({
					locale: 'en',
					acceptedConsentDocumentId: 'consent-document-id',
					acceptedGrants: ['data_processing', 'research_participation']
				})
			})
		);
		expect(request).toHaveBeenNthCalledWith(
			25,
			'/respondent/open-links/open-link-token/sessions/session-id/answers',
			expect.objectContaining({ method: 'PUT' })
		);
		expect(request).toHaveBeenNthCalledWith(
			26,
			'/respondent/open-links/open-link-token/sessions/session-id/submit',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			27,
			'/campaigns/campaign-id/report-proof/exports',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(
			28,
			'/campaign-series/series-id/response-exports',
			expect.objectContaining({ method: 'POST' })
		);
		expect(request).toHaveBeenNthCalledWith(29, '/export-artifacts/artifact-id');
		expect(request).toHaveBeenCalledTimes(29);
		expect(requestText).toHaveBeenCalledWith(
			'/export-artifacts/artifact-id/download',
			expect.objectContaining({
				headers: { accept: 'text/csv' }
			})
		);
		expect(session.tenantId).toBe('tenant-id');
		expect(session.permissions).toContain('setup.manage');
		expect(launch.retentionPolicyId).toBe('retention-policy-id');
		expect(launch.disclosurePolicyId).toBe('disclosure-policy-id');
		expect(invitationBatch.createdInvitationCount).toBe(2);
		expect(invitationBatch.invitations[0].status).toBe('queued');
		expect(delivery.sentCount).toBe(2);
		expect(artifact.id).toBe('artifact-id');
		expect(download.fileName).toBe('campaign-report-proof.csv');
	});
});

const sampleInstrumentRequest = {
	code: 'tenant-burnout-pulse',
	version: '1.0.0',
	fullName: 'Tenant burnout pulse',
	domain: 'psychometric',
	provenanceNote: 'Tenant provided item text.',
	rightsStatus: 'attested_by_tenant',
	validityLabel: 'tenant_provided',
	licenseType: 'unknown'
};

const sampleTemplateRequest = {
	templateName: 'Tenant burnout pulse template',
	semver: '1.0.0',
	defaultLocale: 'en',
	instrumentId: null,
	sections: [{ ordinal: 1, code: 'core', titleDefault: 'Core' }],
	scales: [
		{
			code: 'agreement',
			type: 'likert',
			minValue: 1,
			maxValue: 5,
			step: 1,
			naAllowed: false,
			anchors: '[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]'
		}
	],
	questions: [
		{
			ordinal: 1,
			code: 'q01',
			type: 'likert',
			textDefault: 'After work, I need time to recover mentally.',
			sectionCode: 'core',
			scaleCode: 'agreement',
			required: true,
			reverseCoded: false,
			measurementLevel: 'ordinal',
			payload: '{}',
			missingCodes: '[]'
		}
	]
};

const sampleScoringRuleRequest = {
	templateVersionId: 'template-version-id',
	ruleKey: 'tenant-burnout.total',
	ruleVersion: '1.0.0',
	schemaVersion: 'scoring-rule/v1',
	engineMinVersion: 'engine/v1',
	document: JSON.stringify({
		rule_id: 'tenant-burnout.total',
		rule_version: '1.0.0',
		schema_version: '1.0.0',
		engine_min_version: '1.0.0',
		scale_defaults: {
			agreement: { min: 1, max: 5 }
		},
		inputs: [{ id: 'core_items', kind: 'answers', items: ['q01', 'q02', 'q03'] }],
		nodes: [
			{ id: 'core_answers', op: 'select_answers', input: 'core_items' },
			{
				id: 'scored_answers',
				op: 'reverse_code',
				input: 'core_answers',
				scale: 'agreement',
				reverse_flag_source: 'explicit_list',
				explicit_reverse_items: ['q03']
			},
			{ id: 'total', op: 'mean', input: 'scored_answers' }
		],
		outputs: [{ code: 'total', node: 'total' }],
		missing_data: {
			defaults: { strategy: 'require_all' }
		}
	}),
	produces: '{"scores":["total"]}',
	compatibility: '{}'
};

const sampleCampaignRequest = {
	templateVersionId: 'template-version-id',
	name: 'Wave 1',
	responseIdentityMode: 'anonymous',
	campaignSeriesId: 'series-id',
	schedule: '{}',
	defaultLocale: 'en'
};

function responseFor(path: string) {
	if (path === '/auth/session') {
		return {
			userId: 'user-id',
			tenantId: 'tenant-id',
			permissions: ['setup.manage']
		};
	}

	if (path === '/instruments') {
		return [];
	}

	if (path.includes('launch-readiness')) {
		return { campaignId: 'campaign-id', ready: false, issues: [] };
	}

	if (path === '/campaigns/campaign-id/respondent-rules') {
		return {
			campaignId: 'campaign-id',
			rules: [
				{
					id: 'respondent-rule-id',
					ordinal: 1,
					rule: '{"kind":"self","role":"self"}',
					ruleKind: 'self',
					role: 'self',
					targetSubjectId: null,
					groupId: null,
					assignmentPairCount: 1,
					issues: []
				}
			]
		};
	}

	if (path === '/campaigns/campaign-id/assignments') {
		return {
			campaignId: 'campaign-id',
			assignmentCount: 1,
			assignments: [
				{
					id: 'assignment-id',
					role: 'self',
					status: 'pending',
					anonymous: false,
					targetSubjectId: 'subject-1',
					target: {
						id: 'subject-1',
						label: 'Ana Analyst',
						displayName: 'Ana Analyst',
						email: 'ana@example.test',
						externalId: 'ANA-001'
					},
					respondentSubjectId: 'subject-1',
					respondent: {
						id: 'subject-1',
						label: 'Ana Analyst',
						displayName: 'Ana Analyst',
						email: 'ana@example.test',
						externalId: 'ANA-001'
					},
					dueAt: null,
					createdAt: '2026-05-15T12:00:00Z'
				}
			]
		};
	}

	if (path.includes('/launch')) {
		return {
			campaignId: 'campaign-id',
			status: 'live',
			launchSnapshotId: 'launch-snapshot-id',
			templateVersionId: 'template-version-id',
			scoringRuleId: 'scoring-rule-id',
			retentionPolicyId: 'retention-policy-id',
			disclosurePolicyId: 'disclosure-policy-id',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			launchedAt: '2026-05-07T10:15:00Z'
		};
	}

	if (path.includes('template-versions')) {
		return {
			templateId: 'template-id',
			templateVersionId: 'template-version-id',
			templateName: 'Tenant burnout pulse template',
			semver: '1.0.0',
			status: 'draft',
			defaultLocale: 'en',
			instrumentId: null,
			sections: [],
			scales: [],
			questions: []
		};
	}

	if (path === '/campaigns') {
		return {
			id: 'campaign-id',
			campaignSeriesId: 'series-id',
			templateVersionId: 'template-version-id',
			name: 'Wave 1',
			status: 'draft',
			responseIdentityMode: 'anonymous'
		};
	}

	if (path === '/campaign-series/series-id/two-wave-proof') {
		return {
			campaignSeriesId: 'series-id',
			proofStatus: 'ready',
			expectedWaveCount: 2,
			launchedWaveCount: 2,
			submittedWaveCount: 2,
			linkedTrajectoryCount: 1,
			completeTrajectoryCount: 1,
			waves: [
				{
					campaignId: 'campaign-1',
					name: 'Wave 1',
					status: 'live',
					responseIdentityMode: 'anonymous_longitudinal',
					submittedResponseCount: 1
				},
				{
					campaignId: 'campaign-2',
					name: 'Wave 2',
					status: 'live',
					responseIdentityMode: 'anonymous_longitudinal',
					submittedResponseCount: 1
				}
			]
		};
	}

	if (path === '/campaign-series/series-id/wave-comparison-proof') {
		return {
			campaignSeriesId: 'series-id',
			proofStatus: 'ready',
			interpretationStatus: 'not_validated_interpretation',
			baselineWave: {
				campaignId: 'campaign-1',
				name: 'Wave 1',
				status: 'live',
				responseIdentityMode: 'anonymous_longitudinal',
				launchedAt: '2026-05-08T10:00:00Z',
				scoringRuleId: 'scoring-rule-id',
				scoringRuleKey: 'tenant-burnout.total',
				scoringRuleVersion: '1.0.0',
				scoringRuleDocumentHash: 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
				submittedResponseCount: 5
			},
			comparisonWave: {
				campaignId: 'campaign-2',
				name: 'Wave 2',
				status: 'live',
				responseIdentityMode: 'anonymous_longitudinal',
				launchedAt: '2026-05-08T11:00:00Z',
				scoringRuleId: 'scoring-rule-id',
				scoringRuleKey: 'tenant-burnout.total',
				scoringRuleVersion: '1.0.0',
				scoringRuleDocumentHash: 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
				submittedResponseCount: 5
			},
			disclosurePolicy: {
				id: 'disclosure-policy-id',
				version: '1.0.0',
				kMin: 5,
				suppressionStrategy: 'hide_cell'
			},
			scores: [
				{
					dimensionCode: 'total',
					compatibilityStatus: 'compatible',
					disclosure: 'visible',
					baselineSubmittedResponseCount: 5,
					comparisonSubmittedResponseCount: 5,
					linkedPairCount: 5,
					baselineScoreCount: 5,
					comparisonScoreCount: 5,
					baselineMean: 3.8,
					comparisonMean: 4.6,
					aggregateDelta: 0.8,
					pairedDeltaMean: 0.8,
					suppressionReason: null,
					compatibilityReason: null
				}
			]
		};
	}

	if (path === '/respondent/campaigns/campaign-id') {
		return {
			campaignId: 'campaign-id',
			templateVersionId: 'template-version-id',
			name: 'Wave 1',
			status: 'draft',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			questions: [
				{
					id: 'question-id',
					ordinal: 1,
					code: 'q01',
					type: 'likert',
					textDefault: 'After work, I need time to recover mentally.',
					required: true
				}
			]
		};
	}

	if (path === '/respondent/campaigns/campaign-id/lab-assignment') {
		return {
			assignmentId: 'assignment-id',
			campaignId: 'campaign-id',
			responseIdentityMode: 'anonymous'
		};
	}

	if (path === '/campaigns/campaign-id/open-link') {
		return {
			campaignId: 'campaign-id',
			assignmentId: 'assignment-id',
			token: 'open-link-token',
			respondentPath: '/r/open-link-token'
		};
	}

	if (path === '/campaigns/campaign-id/identified-entry') {
		return {
			campaignId: 'campaign-id',
			assignmentId: 'assignment-id',
			subjectId: 'subject-id',
			token: 'identified-token',
			respondentPath: '/r/identified-token'
		};
	}

	if (path === '/campaigns/campaign-id/invitation-batches') {
		return {
			campaignId: 'campaign-id',
			requestedRecipientCount: 2,
			createdInvitationCount: 2,
			invitations: [
				{
					assignmentId: 'invite-assignment-1',
					invitationTokenId: 'invite-token-1',
					notificationId: 'notification-1',
					recipient: 'ada@example.com',
					token: 'inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDE',
					respondentPath: '/r/inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDE',
					status: 'queued'
				},
				{
					assignmentId: 'invite-assignment-2',
					invitationTokenId: 'invite-token-2',
					notificationId: 'notification-2',
					recipient: 'bo@example.com',
					token: 'inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDF',
					respondentPath: '/r/inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDF',
					status: 'queued'
				}
			]
		};
	}

	if (path === '/campaigns/campaign-id/notification-deliveries/process') {
		return {
			campaignId: 'campaign-id',
			requestedBatchSize: 5,
			processedCount: 2,
			sentCount: 2,
			failedCount: 0,
			deliveries: [
				{
					notificationId: 'notification-1',
					recipient: 'ada@example.com',
					status: 'sent',
					provider: 'local-dev',
					providerMessageId: 'local-dev-message-1',
					respondentPath:
						'/r/inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDE2',
					error: null
				},
				{
					notificationId: 'notification-2',
					recipient: 'bo@example.com',
					status: 'sent',
					provider: 'local-dev',
					providerMessageId: 'local-dev-message-2',
					respondentPath:
						'/r/inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDF2',
					error: null
				}
			]
		};
	}

	if (path === '/respondent/open-links/open-link-token') {
		return {
			campaignId: 'campaign-id',
			assignmentId: 'assignment-id',
			templateVersionId: 'template-version-id',
			name: 'Wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			consentDocument: {
				id: 'consent-document-id',
				locale: 'en',
				version: '1.0.0',
				title: 'Default participant disclosure',
				bodyMarkdown: 'Consent body',
				requiredGrants: ['data_processing', 'research_participation'],
				optionalGrants: []
			},
			questions: [
				{
					id: 'question-id',
					ordinal: 1,
					code: 'q01',
					type: 'likert',
					textDefault: 'After work, I need time to recover mentally.',
					required: true
				}
			]
		};
	}

	if (path === '/respondent/identified-entries/identified-token') {
		return {
			campaignId: 'campaign-id',
			assignmentId: 'assignment-id',
			templateVersionId: 'template-version-id',
			name: 'Identified wave',
			status: 'live',
			responseIdentityMode: 'identified',
			requiresParticipantCode: false,
			defaultLocale: 'en',
			consentDocument: {
				id: 'consent-document-id',
				locale: 'en',
				version: '1.0.0',
				title: 'Default participant disclosure',
				bodyMarkdown: 'Consent body',
				requiredGrants: ['data_processing'],
				optionalGrants: []
			},
			questions: [
				{
					id: 'question-id',
					ordinal: 1,
					code: 'q01',
					type: 'likert',
					textDefault: 'After work, I need time to recover mentally.',
					required: true
				}
			]
		};
	}

	if (path === '/respondent/open-links/open-link-token/sessions') {
		return {
			id: 'session-id',
			assignmentId: 'assignment-id',
			locale: 'en',
			startedAt: '2026-05-07T12:00:00Z',
			submittedAt: null,
			timeTakenMs: null
		};
	}

	if (path === '/respondent/identified-entries/identified-token/sessions') {
		return {
			id: 'session-id',
			assignmentId: 'assignment-id',
			locale: 'en',
			startedAt: '2026-05-07T12:00:00Z',
			submittedAt: null,
			timeTakenMs: null,
			publicHandle: 'public-session-handle'
		};
	}

	if (path === '/respondent/open-links/open-link-token/sessions/session-id/answers') {
		return { sessionId: 'session-id', savedAnswerCount: 1 };
	}

	if (path === '/respondent/open-links/open-link-token/sessions/session-id/submit') {
		return { id: 'session-id', submittedAt: '2026-05-07T12:05:00Z' };
	}

	if (path === '/export-artifacts/artifact-id') {
		return {
			id: 'artifact-id',
			campaignId: 'campaign-id',
			campaignSeriesId: 'series-id',
			artifactType: 'report_proof_csv_codebook',
			status: 'succeeded',
			format: 'csv_codebook',
			fileName: 'campaign-report-proof.csv',
			contentType: 'text/csv',
			rowCount: 1,
			byteSize: 36,
			checksumSha256: 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
			createdAt: '2026-05-08T12:00:00Z',
			completedAt: '2026-05-08T12:00:00Z',
			csvContent: 'campaign_id,dimension_code,disclosure\r\n',
			codebookJson: '{"artifactType":"report_proof_csv_codebook","columns":[]}'
		};
	}

	if (path === '/respondent/sessions') {
		return {
			id: 'session-id',
			assignmentId: 'assignment-id',
			locale: 'en',
			startedAt: '2026-05-07T12:00:00Z',
			submittedAt: null,
			timeTakenMs: null
		};
	}

	if (path === '/respondent/sessions/session-id/answers') {
		return { sessionId: 'session-id', savedAnswerCount: 1 };
	}

	if (path === '/respondent/sessions/session-id/submit') {
		return { id: 'session-id', submittedAt: '2026-05-07T12:05:00Z' };
	}

	if (path === '/respondent/sessions/session-id/scores') {
		return {
			scoreRunId: 'score-run-id',
			sessionId: 'session-id',
			scores: [{ dimensionCode: 'total', value: 4, n: 1 }]
		};
	}

	if (path === '/instruments/private-imports') {
		return {
			id: 'instrument-id',
			code: 'tenant-burnout-pulse',
			version: '1.0.0',
			fullName: 'Tenant burnout pulse',
			rightsStatus: 'attested_by_tenant',
			validityLabel: 'tenant_provided',
			canStartNewCampaign: true
		};
	}

	return { id: 'created-id' };
}
