import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { expect, test, type Page, type Request } from '@playwright/test';
import type {
	CampaignSeriesOperationsCampaignResponse,
	CampaignSeriesOperationsWorkspaceResponse,
	CampaignSeriesReportsCampaignResponse,
	CampaignSeriesReportsWorkspaceResponse,
	CampaignSeriesSetupCampaignResponse,
	CampaignSeriesSetupWorkspaceResponse,
	CampaignSeriesWavesWaveResponse,
	CampaignSeriesWavesWorkspaceResponse
} from '$lib/api/product';
import type {
	CampaignReportProofResponse,
	CampaignSeriesWaveComparisonProofResponse,
	OpenLinkEntryResponse
} from '$lib/api/setup';

const sampleSeriesId = '2f2f819f-f6eb-486a-9e0f-872ac30af3d4';
const reportCampaignId = 'b9514b8e-ecbc-4085-bc44-c6f377152f32';
const comparisonCampaignId = 'b46e7085-c4c3-472d-9386-d259e7ad5f27';
const openLinkToken =
	'opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ';

test.describe('BUILD-004 route-smoke dependency requests', () => {
	test('does not request dashboard or SurveyJS runtime chunks on respondent entry', async ({
		page
	}) => {
		const dependencyAssets = await readDependencyAssets();
		const getSessionChecks = await routeRespondentEntry(page);

		const assetRequests = await recordRouteAssets(page, `/r/${openLinkToken}`, async () => {
			await expect(page.getByRole('heading', { name: 'Wave 1' })).toBeVisible();
			await expect(
				page.getByRole('heading', { name: 'Default participant disclosure' })
			).toBeVisible();
			await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
		});

		expectNoDependencyRequests(assetRequests, dependencyAssets.echarts, 'ECharts');
		expectNoDependencyRequests(assetRequests, dependencyAssets.surveyRuntime, 'SurveyJS runtime');
		expectNoDependencyRequests(assetRequests, dependencyAssets.surveyCreator, 'Survey Creator');
		expectNoTextLeaks(assetRequests);
		expect(getSessionChecks()).toBe(0);
	});

	test('does not request dashboard chart chunks on the setup surface', async ({ page }) => {
		const dependencyAssets = await readDependencyAssets();
		await routeAuthenticatedSession(page);
		await routeProductReadModels(page);

		const setupRequests = await recordRouteAssets(
			page,
			`/app/campaign-series/${sampleSeriesId}/setup`,
			async () => {
				await expect(page.getByRole('heading', { name: 'Study protocol', exact: true })).toBeVisible();
				await expect(page.getByRole('region', { name: 'Protocol workspace' })).toBeVisible();
			}
		);

		expectNoDependencyRequests(setupRequests, dependencyAssets.echarts, 'ECharts');
		expectNoDependencyRequests(setupRequests, dependencyAssets.surveyCreator, 'Survey Creator');
		expectNoTextLeaks(setupRequests);
	});

	test('does not request dashboard chart chunks on the operations surface', async ({ page }) => {
		const dependencyAssets = await readDependencyAssets();
		await routeAuthenticatedSession(page);
		await routeProductReadModels(page);

		const operationsRequests = await recordRouteAssets(
			page,
			`/app/campaign-series/${sampleSeriesId}/operations`,
			async () => {
				await expect(page.getByRole('heading', { name: 'Field', exact: true })).toBeVisible();
				await expect(page.getByRole('region', { name: 'Field workspace' })).toBeVisible();
			}
		);

		expectNoDependencyRequests(operationsRequests, dependencyAssets.echarts, 'ECharts');
		expectNoDependencyRequests(
			operationsRequests,
			dependencyAssets.surveyCreator,
			'Survey Creator'
		);
		expectNoTextLeaks(operationsRequests);
	});

	test('does not request chart chunks on the reports surface (SVG dashboards)', async ({ page }) => {
		const dependencyAssets = await readDependencyAssets();
		await routeAuthenticatedSession(page);
		await routeProductReadModels(page);
		await routeReportProof(page);

		const reportRequests = await recordRouteAssets(
			page,
			`/app/campaign-series/${sampleSeriesId}/reports`,
			async () => {
				await expect(page.getByRole('heading', { name: 'Evidence', exact: true })).toBeVisible();
				await expect(page.getByRole('region', { name: 'Evidence workspace' })).toBeVisible();
			}
		);

		expectNoDependencyRequests(reportRequests, dependencyAssets.echarts, 'ECharts');
		expectNoDependencyRequests(reportRequests, dependencyAssets.surveyCreator, 'Survey Creator');
	});

	test('requests ECharts only when wave visual analytics mounts', async ({ page }) => {
		const dependencyAssets = await readDependencyAssets();
		await routeAuthenticatedSession(page);
		await routeProductReadModels(page);
		await routeWaveComparisonProof(page);

		const waveRequests = await recordRouteAssets(
			page,
			`/app/campaign-series/${sampleSeriesId}/waves`,
			async () => {
				await expect(page.getByRole('group', { name: 'Wave visual analytics' })).toBeVisible();
				await expect(page.getByTestId('wave-visual-analytics-chart')).toHaveAttribute(
					'data-chart-state',
					'ready'
				);
			}
		);

		expectSomeDependencyRequests(waveRequests, dependencyAssets.echarts, 'ECharts');
		expectNoDependencyRequests(waveRequests, dependencyAssets.surveyCreator, 'Survey Creator');
	});
});

type ManifestEntry = {
	file?: string;
	src?: string;
	name?: string;
};

type DependencyAssets = {
	echarts: string[];
	surveyRuntime: string[];
	surveyCreator: string[];
};

async function readDependencyAssets(): Promise<DependencyAssets> {
	const manifestPath = path.resolve('.svelte-kit/output/client/.vite/manifest.json');
	const manifest = JSON.parse(await readFile(manifestPath, 'utf8')) as Record<
		string,
		ManifestEntry
	>;
	const dependencies: DependencyAssets = {
		echarts: [],
		surveyRuntime: [],
		surveyCreator: []
	};

	for (const [key, entry] of Object.entries(manifest)) {
		if (!entry.file) {
			continue;
		}

		const sourceText = `${key} ${entry.src ?? ''} ${entry.name ?? ''}`;

		if (/node_modules\/(?:echarts|zrender)\//.test(sourceText)) {
			dependencies.echarts.push(entry.file);
		}

		if (/node_modules\/(?:survey-core|survey-js-ui)\//.test(sourceText)) {
			dependencies.surveyRuntime.push(entry.file);
		}

		if (/node_modules\/survey-creator/.test(sourceText)) {
			dependencies.surveyCreator.push(entry.file);
		}
	}

	return {
		echarts: [...new Set(dependencies.echarts)].sort(),
		surveyRuntime: [...new Set(dependencies.surveyRuntime)].sort(),
		surveyCreator: [...new Set(dependencies.surveyCreator)].sort()
	};
}

async function recordRouteAssets(
	page: Page,
	routePath: string,
	assertReady: () => Promise<void>
): Promise<string[]> {
	const requests = new Set<string>();
	const capture = (request: Request) => {
		const resourceType = request.resourceType();
		const url = request.url();

		if (
			resourceType !== 'script' &&
			resourceType !== 'stylesheet' &&
			!url.includes('/_app/immutable/')
		) {
			return;
		}

		const pathname = new URL(url).pathname.replace(/^\/+/, '');

		if (pathname.startsWith('_app/immutable/')) {
			requests.add(pathname);
		}
	};

	page.on('request', capture);
	await page.goto(routePath);
	await assertReady();
	await page.waitForLoadState('networkidle', { timeout: 5_000 }).catch(() => undefined);
	page.off('request', capture);

	return [...requests].sort();
}

function expectNoDependencyRequests(
	assetRequests: string[],
	dependencyFiles: string[],
	label: string
) {
	expect(requestedDependencyFiles(assetRequests, dependencyFiles), `${label} requests`).toEqual([]);
}

function expectSomeDependencyRequests(
	assetRequests: string[],
	dependencyFiles: string[],
	label: string
) {
	expect(requestedDependencyFiles(assetRequests, dependencyFiles), `${label} requests`).not.toEqual(
		[]
	);
}

function requestedDependencyFiles(assetRequests: string[], dependencyFiles: string[]) {
	const dependencySet = new Set(dependencyFiles);
	return assetRequests.filter((assetRequest) => dependencySet.has(assetRequest)).sort();
}

function expectNoTextLeaks(assetRequests: string[]) {
	expect(
		assetRequests.filter((assetRequest) =>
			/(echarts|zrender|survey-creator|VisualAnalyticsChart)/i.test(assetRequest)
		),
		'plain dependency-name request leaks'
	).toEqual([]);
}

async function routeRespondentEntry(page: Page) {
	let sessionChecks = 0;

	await page.route('**/auth/session', async (route) => {
		sessionChecks += 1;
		await route.fulfill({ status: 500, json: { title: 'Unexpected session check' } });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	return () => sessionChecks;
}

async function routeAuthenticatedSession(page: Page) {
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({
			json: {
				userId: '22222222-2222-4222-8222-222222222222',
				tenantId: '11111111-1111-4111-8111-111111111111',
				permissions: ['setup.manage']
			}
		});
	});
}

async function routeProductReadModels(page: Page) {
	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		await route.fulfill({ json: sampleSetupWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/operations-workspace`, async (route) => {
		await route.fulfill({ json: sampleOperationsWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		await route.fulfill({ json: sampleReportsWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/waves-workspace`, async (route) => {
		await route.fulfill({ json: sampleWavesWorkspace });
	});
}

async function routeReportProof(page: Page) {
	await page.route(`**/campaigns/${reportCampaignId}/report-proof`, async (route) => {
		await route.fulfill({ json: sampleCampaignReportProof });
	});
}

async function routeWaveComparisonProof(page: Page) {
	await page.route(`**/campaign-series/${sampleSeriesId}/wave-comparison-proof`, async (route) => {
		await route.fulfill({ json: sampleWaveComparisonProof });
	});
}

function assertNoSetupAuthHeaders(headers: Record<string, string>) {
	expect(headers['x-tenant-id']).toBeUndefined();
	expect(headers['x-dev-user-id']).toBeUndefined();
	expect(headers['x-dev-tenant-memberships']).toBeUndefined();
	expect(headers['x-dev-permissions']).toBeUndefined();
	expect(headers['x-test-user-id']).toBeUndefined();
}

const sampleOpenLinkEntry = {
	campaignId: reportCampaignId,
	assignmentId: 'a8f5d2e2-48b8-4651-9d61-163381d8e4a9',
	templateVersionId: 'f6d8cc42-b721-4789-a933-83895f5d064e',
	name: 'Wave 1',
	status: 'live',
	responseIdentityMode: 'anonymous',
	requiresParticipantCode: false,
	defaultLocale: 'en',
	consentDocument: {
		id: 'ff4e1864-f691-4e26-a198-c8fa72d29bd9',
		locale: 'en',
		version: '1.0.0',
		title: 'Default participant disclosure',
		bodyMarkdown:
			'By continuing, the participant confirms the tenant may collect anonymous survey responses.',
		requiredGrants: ['data_processing', 'research_participation'],
		optionalGrants: []
	},
	questions: [
		{
			id: '07d19d0a-8417-41d7-8b36-5147d96ad7f8',
			ordinal: 1,
			code: 'q01',
			type: 'likert',
			textDefault: 'After work, I need time to recover mentally.',
			required: true,
			scaleMinValue: 1,
			scaleMaxValue: 5
		}
	]
} satisfies OpenLinkEntryResponse;

const setupCampaign = {
	id: reportCampaignId,
	name: 'Pulse wave 1',
	status: 'draft',
	responseIdentityMode: 'anonymous_longitudinal',
	defaultLocale: 'en',
	templateVersionId: 'f6d8cc42-b721-4789-a933-83895f5d064e',
	latestLaunchAt: null
} satisfies CampaignSeriesSetupCampaignResponse;

const operationsCampaign = {
	id: reportCampaignId,
	name: 'Pulse wave 1',
	status: 'live',
	responseIdentityMode: 'anonymous_longitudinal',
	defaultLocale: 'en',
	latestLaunchSnapshotId: 'wave-1-launch-id',
	latestLaunchAt: '2026-05-08T12:00:00Z',
	startedResponseCount: 13,
	draftResponseCount: 1,
	submittedResponseCount: 12,
	latestResponseStartedAt: '2026-05-08T12:12:00Z',
	latestResponseSubmittedAt: '2026-05-08T12:11:00Z',
	collectionStatus: 'has_submissions',
	reportVisibilityStatus: 'ready_for_aggregate_report',
	collectionGuidance: 'Enough submitted responses exist for aggregate report visibility.',
	openLinkAssignmentCount: 1,
	queuedInvitationCount: 0,
	sentInvitationCount: 0,
	failedInvitationCount: 0,
	deliveryAttemptCount: 1,
	latestDeliveryAttemptAt: '2026-05-08T12:10:00Z'
} satisfies CampaignSeriesOperationsCampaignResponse;

const reportsCampaign = {
	id: reportCampaignId,
	name: 'Pulse wave 1',
	status: 'live',
	responseIdentityMode: 'anonymous_longitudinal',
	defaultLocale: 'en',
	latestLaunchSnapshotId: 'wave-1-launch-id',
	latestLaunchAt: '2026-05-08T12:00:00Z',
	scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
	consentDocumentId: 'ff4e1864-f691-4e26-a198-c8fa72d29bd9',
	retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
	disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
	submittedResponseCount: 12,
	scoreCount: 2,
	exportArtifactCount: 0,
	visibleScoreCount: 1,
	suppressedScoreCount: 1,
	disclosureState: 'mixed',
	disclosureKMin: 5,
	reportStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	latestExportArtifactId: null,
	latestExportArtifactFileName: null,
	latestExportArtifactStatus: null,
	latestExportArtifactCreatedAt: null,
	latestExportArtifactCompletedAt: null,
	latestExportArtifactStartedAt: null,
	latestExportArtifactFailedAt: null,
	latestExportArtifactExpiresAt: null,
	latestExportArtifactDeletedAt: null,
	latestExportArtifactFailureReasonCode: null,
	latestExportArtifactCanDownload: false
} satisfies CampaignSeriesReportsCampaignResponse;

const baselineWave = {
	id: reportCampaignId,
	name: 'Pulse wave 1',
	status: 'live',
	responseIdentityMode: 'anonymous_longitudinal',
	defaultLocale: 'en',
	latestLaunchSnapshotId: 'wave-1-launch-id',
	latestLaunchAt: '2026-05-08T12:00:00Z',
	scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
	scoringRuleKey: 'burnout.total',
	scoringRuleVersion: '1.0.0',
	disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
	disclosureKMin: 5,
	submittedResponseCount: 12,
	scoreCount: 2,
	linkedTrajectoryCount: 6,
	waveState: 'baseline'
} satisfies CampaignSeriesWavesWaveResponse;

const comparisonWave = {
	...baselineWave,
	id: comparisonCampaignId,
	name: 'Pulse wave 2',
	latestLaunchSnapshotId: 'wave-2-launch-id',
	latestLaunchAt: '2026-05-15T12:00:00Z',
	waveState: 'comparison'
} satisfies CampaignSeriesWavesWaveResponse;

const sampleSetupWorkspace = {
	series: {
		id: sampleSeriesId,
		name: 'Quarterly pulse',
		studyKind: 'own',
		isSample: false,
		sampleScenario: null,
		readOnlyReason: null,
		createdAt: '2026-05-08T08:00:00Z',
		updatedAt: '2026-05-08T08:00:00Z'
	},
	summary: {
		campaignCount: 1,
		liveCampaignCount: 0,
		missingPrerequisiteCount: 0
	},
	selectedCampaign: setupCampaign,
	template: {
		templateId: '4a2218c8-0036-46a5-8c54-4f6f17fb96f7',
		templateVersionId: setupCampaign.templateVersionId,
		templateName: 'Tenant burnout pulse',
		semver: '1.0.0',
		status: 'attested_by_tenant',
		defaultLocale: 'en',
		instrumentId: '67ad3dc0-78a2-4373-bb36-117158345c45',
		questionCount: 1
	},
	scoring: {
		id: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		templateVersionId: setupCampaign.templateVersionId,
		ruleKey: 'burnout.total',
		ruleVersion: '1.0.0',
		status: 'attested_by_tenant',
		source: 'tenant_defined'
	},
	policies: {
		consent: { id: sampleOpenLinkEntry.consentDocument.id, version: '1.0.0', status: 'active' },
		retention: { id: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872', version: '1.0.0', status: 'active' },
		disclosure: { id: 'a0910474-79b6-444d-9d21-8e89c82b6d72', version: '1.0.0', status: 'active' }
	},
	readiness: {
		campaignId: reportCampaignId,
		status: 'ready',
		ready: true
	},
	missingPrerequisites: [],
	campaigns: [setupCampaign]
} satisfies CampaignSeriesSetupWorkspaceResponse;

const sampleOperationsWorkspace = {
	series: sampleSetupWorkspace.series,
	summary: {
		campaignCount: 1,
		liveCampaignCount: 1,
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 0,
		sentInvitationCount: 0,
		failedInvitationCount: 0,
		deliveryAttemptCount: 1,
		startedResponseCount: 13,
		draftResponseCount: 1,
		submittedResponseCount: 12,
		latestResponseStartedAt: '2026-05-08T12:12:00Z',
		latestResponseSubmittedAt: '2026-05-08T12:11:00Z',
		collectionStatus: 'has_submissions',
		reportVisibilityStatus: 'ready_for_aggregate_report',
		collectionGuidance: 'Enough submitted responses exist for aggregate report visibility.',
		missingPrerequisiteCount: 0
	},
	selectedCampaign: operationsCampaign,
	missingPrerequisites: [],
	campaigns: [operationsCampaign]
} satisfies CampaignSeriesOperationsWorkspaceResponse;

const sampleReportsWorkspace = {
	series: sampleSetupWorkspace.series,
	summary: {
		campaignCount: 1,
		liveCampaignCount: 1,
		reportableCampaignCount: 1,
		submittedResponseCount: 12,
		scoreCount: 2,
		exportArtifactCount: 0,
		visibleScoreCount: 1,
		suppressedScoreCount: 1,
		missingPrerequisiteCount: 0
	},
	selectedCampaign: reportsCampaign,
	missingPrerequisites: [],
	exportArtifacts: [],
	campaigns: [reportsCampaign]
} satisfies CampaignSeriesReportsWorkspaceResponse;

const sampleWavesWorkspace = {
	series: sampleSetupWorkspace.series,
	summary: {
		campaignCount: 2,
		liveCampaignCount: 2,
		longitudinalWaveCount: 2,
		submittedWaveCount: 2,
		linkedTrajectoryCount: 6,
		completeTrajectoryCount: 6,
		comparableScoreCount: 2,
		visibleComparisonCount: 1,
		suppressedComparisonCount: 1,
		blockedComparisonCount: 0,
		missingPrerequisiteCount: 0
	},
	selectedBaselineWave: baselineWave,
	selectedComparisonWave: comparisonWave,
	comparison: {
		status: 'proof_only',
		disclosureState: 'mixed',
		compatibilityState: 'compatible',
		interpretationStatus: 'not_validated_interpretation',
		disclosureKMin: 5,
		linkedPairCount: 6,
		visibleScoreCount: 1,
		suppressedScoreCount: 1,
		blockedScoreCount: 0
	},
	missingPrerequisites: [],
	waves: [baselineWave, comparisonWave]
} satisfies CampaignSeriesWavesWorkspaceResponse;

const sampleCampaignReportProof = {
	campaignId: reportCampaignId,
	campaignSeriesId: sampleSeriesId,
	campaignName: 'Pulse wave 1',
	campaignStatus: 'live',
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	launchSnapshot: {
		id: 'wave-1-launch-id',
		templateVersionId: setupCampaign.templateVersionId,
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoringRuleDocumentHash: 'scoring-hash',
		consentDocumentId: reportsCampaign.consentDocumentId,
		retentionPolicyId: reportsCampaign.retentionPolicyId,
		disclosurePolicyId: reportsCampaign.disclosurePolicyId,
		responseIdentityMode: reportsCampaign.responseIdentityMode,
		launchedAt: '2026-05-08T12:00:00Z'
	},
	disclosurePolicy: {
		id: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'suppress_dimension'
	},
	scores: [
		{
			dimensionCode: 'total',
			disclosure: 'visible',
			submittedResponseCount: 12,
			scoreCount: 12,
			mean: 3.75,
			min: 2,
			max: 5,
			suppressionReason: null
		},
		{
			dimensionCode: 'exhaustion',
			disclosure: 'suppressed',
			submittedResponseCount: 4,
			scoreCount: null,
			mean: null,
			min: null,
			max: null,
			suppressionReason: 'cohort_lt_k_min'
		}
	]
} satisfies CampaignReportProofResponse;

const sampleWaveComparisonProof = {
	campaignSeriesId: sampleSeriesId,
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	baselineWave: {
		campaignId: baselineWave.id,
		name: baselineWave.name,
		status: baselineWave.status,
		responseIdentityMode: baselineWave.responseIdentityMode,
		launchedAt: '2026-05-08T12:00:00Z',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'baseline-scoring-hash',
		submittedResponseCount: baselineWave.submittedResponseCount
	},
	comparisonWave: {
		campaignId: comparisonWave.id,
		name: comparisonWave.name,
		status: comparisonWave.status,
		responseIdentityMode: comparisonWave.responseIdentityMode,
		launchedAt: '2026-05-15T12:00:00Z',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'comparison-scoring-hash',
		submittedResponseCount: comparisonWave.submittedResponseCount
	},
	disclosurePolicy: {
		id: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'suppress_dimension'
	},
	scores: [
		{
			dimensionCode: 'total',
			compatibilityStatus: 'compatible',
			disclosure: 'visible',
			baselineSubmittedResponseCount: 12,
			comparisonSubmittedResponseCount: 12,
			linkedPairCount: 6,
			baselineScoreCount: 12,
			comparisonScoreCount: 12,
			baselineMean: 3.75,
			comparisonMean: 3.45,
			aggregateDelta: -0.3,
			pairedDeltaMean: -0.25,
			suppressionReason: null,
			compatibilityReason: null
		},
		{
			dimensionCode: 'exhaustion',
			compatibilityStatus: 'compatible',
			disclosure: 'suppressed',
			baselineSubmittedResponseCount: 4,
			comparisonSubmittedResponseCount: 4,
			linkedPairCount: 4,
			baselineScoreCount: null,
			comparisonScoreCount: null,
			baselineMean: null,
			comparisonMean: null,
			aggregateDelta: null,
			pairedDeltaMean: null,
			suppressionReason: 'linked_pairs_lt_k_min',
			compatibilityReason: null
		}
	]
} satisfies CampaignSeriesWaveComparisonProofResponse;
