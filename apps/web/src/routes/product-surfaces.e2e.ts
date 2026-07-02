import { expect, test, type Locator, type Page } from '@playwright/test';
import type {
	CampaignSeriesHubResponse,
	CampaignSeriesDuplicateResponse,
	CampaignSeriesOperationsWorkspaceResponse,
	CampaignSeriesReportsWidgetManifestResponse,
	CampaignSeriesReportsWorkspaceResponse,
	CampaignSeriesSetupWorkspaceResponse,
	CampaignSeriesWavesWorkspaceResponse,
	CampaignSeriesListResponse,
	DirectoryConnectionStateResponse,
	DirectoryImportRuleListResponse,
	DirectoryImportRuleResponse,
	DirectoryImportRunHistoryResponse,
	ExportArtifactLibraryResponse,
	MicrosoftGraphConsentRequestResponse,
	RespondentRulePreviewResponse,
	SubjectDirectoryResponse,
	SubjectGroupListResponse,
	TenantSettingsWorkspaceResponse,
	TenantRoleListResponse,
	TenantMemberRosterResponse,
	WorkspaceOverviewResponse
} from '$lib/api/product';
import type {
	CampaignRespondentRuleListResponse,
	CampaignSeriesWaveComparisonProofResponse,
	InstrumentSummaryResponse
} from '$lib/api/setup';

const oldFoundationSeriesId = '985c65ad-f919-4c87-a40d-7445868dc587';
const sampleSeriesId = '2f2f819f-f6eb-486a-9e0f-872ac30af3d4';
const setupSampleSeriesId = '019ad5b6-7f00-7000-8a00-000000000101';
const collectionSampleSeriesId = '019ad5b6-7f00-7000-8a00-000000000102';
const longitudinalSampleSeriesId = '019ad5b6-7f00-7000-8a00-000000000103';
const sampleCampaignId = '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2';
const alternateSeriesId = '6a82f6e0-4712-4c3e-9d20-53715d5c96f3';
const retrySeriesId = 'b79f2bb3-f68f-4b71-9dc9-c344a3730a0e';
const sampleSessionUserId = '22222222-2222-4222-8222-222222222222';
const sampleSessionTenantId = '11111111-1111-4111-8111-111111111111';
const sampleSessionEmail = 'owner@example.test';
const selectedSeriesSurfaceLabels = ['Overview', 'Protocol', 'Field', 'Evidence'];
// M1.2 note: stale pre-product-spine product-surface contracts are temporarily skipped in this file.
// Keep the current M1 spine tests active; modernize skipped legacy contracts in focused slices.
const ownSeriesOwnership = {
	studyKind: 'own',
	isSample: false,
	sampleScenario: null,
	readOnlyReason: null
} as const;
const sampleStudyOwnership = {
	studyKind: 'sample',
	isSample: true,
	sampleScenario: 'mixed_lifecycle',
	readOnlyReason: 'sample_study'
} as const;
const setupSampleStudyOwnership = {
	...sampleStudyOwnership,
	sampleScenario: 'setup'
} as const;
const collectionSampleStudyOwnership = {
	...sampleStudyOwnership,
	sampleScenario: 'in_collection'
} as const;
const longitudinalSampleStudyOwnership = {
	...sampleStudyOwnership,
	sampleScenario: 'longitudinal'
} as const;

test.beforeEach(async ({ page }) => {
	await routeAuthenticatedSession(page);
	await routeCsrfToken(page);
	await routeProductReadModels(page);
});

test('renders the authenticated product shell and workspace overview', async ({ page }) => {
	await page.goto('/app');

	await expect(page.getByRole('heading', { name: 'Briefing', exact: true })).toBeVisible();
	await expect(
		page.getByText('UX02 product surfaces - read-model bridge', { exact: true })
	).toHaveCount(0);
	await expect(
		page.getByText('GF05 setup APIs - F41-ready workspace', { exact: true })
	).toHaveCount(0);
	await expect(page.getByText('Research workspace', { exact: true })).toBeVisible();
	await expect(page.getByText('Tenant command workspace', { exact: true })).toHaveCount(0);
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav).toBeVisible();
	const overview = page.getByRole('region', { name: 'Workspace home' });
	const commandCenter = overview.getByRole('region', { name: 'Next actions' });
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	const setupCommand = commandCenter.getByRole('link', {
		name: /Finish preparation for Quarterly pulse/i
	});

	await expect(setupCommand).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/setup`
	);
	await expect(commandCenter.getByText('setup.manage', { exact: true })).toHaveCount(0);
	await expect(commandCenter.getByText('Open Prepare', { exact: true })).toBeVisible();
	await expect(sampleStudies.getByText('Sample study', { exact: true })).toHaveCount(4);
	await expect(ownStudies.getByText('Your study', { exact: true })).toBeVisible();
	await overview.getByText('Workspace overview', { exact: true }).click();
	const totals = overview.getByRole('group', { name: 'Workspace totals' });
	await expect(totals.getByText('Studies', { exact: true })).toBeVisible();
	await expect(totals.getByText('3', { exact: true })).toBeVisible();
	await expect(totals.getByText('Measurements', { exact: true })).toBeVisible();
	await expect(totals.getByText('9', { exact: true })).toBeVisible();
	await expect(totals.getByText('Live measurements', { exact: true })).toBeVisible();
	await expect(totals.getByText('2', { exact: true })).toBeVisible();
	await expect(totals.getByText('Submitted responses', { exact: true })).toBeVisible();
	await expect(totals.getByText('128', { exact: true })).toBeVisible();
	await expect(totals.getByText('Export files', { exact: true })).toBeVisible();
	await expect(totals.getByText('5', { exact: true })).toBeVisible();
	await expect(page.locator(`[href*="${oldFoundationSeriesId}"]`)).toHaveCount(0);

	await expect(nav.getByRole('link', { name: /^Briefing/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: /^Studies\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: /^Team\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: 'Demo fixtures' })).toHaveCount(0);
});

test('ui03 product workspace uses quiet section styling instead of raised cards', async ({
	page
}) => {
	await page.goto('/app');

	await expect(page.getByRole('region', { name: 'Workspace home' })).toBeVisible();
	await expect(page.getByText('Workspace overview', { exact: true })).toBeVisible();
	await expect(page.getByLabel('Workspace posture')).toBeVisible();

	const visualOffenders = await page
		.locator('.product-panel, .record-row, .metric-card, .route-guidance, .setup-callout')
		.evaluateAll((elements) =>
			elements
				.map((element) => {
					const style = window.getComputedStyle(element);

					return {
						className: element.className,
						boxShadow: style.boxShadow,
						borderRadius: style.borderRadius
					};
				})
				.filter(
					(style) =>
						style.boxShadow !== 'none' || Number.parseFloat(style.borderRadius) > 8
				)
		);

	expect(visualOffenders).toEqual([]);
});

test('renders self-serve home cockpit for sample and own studies', async ({ page }) => {
	await page.goto('/app');

	await expect(page.getByRole('heading', { name: 'Briefing', exact: true })).toBeVisible();
	const overview = page.getByRole('region', { name: 'Workspace home' });
	const lifecycle = overview.locator('[aria-label="Study workflow"]');
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	const suggestedActions = overview.getByRole('region', { name: 'Next actions' });

	for (const label of ['Protocol', 'Field', 'Evidence', 'Export']) {
		await expect(lifecycle.getByText(label, { exact: true })).toBeVisible();
	}
	await expectElementBefore(sampleStudies, ownStudies);
	await expectElementBefore(ownStudies, overview.getByText('Workspace overview', { exact: true }));

	const sample = sampleStudies.getByRole('link', { name: /Completed sample/i });
	await expect(sample).toHaveAttribute('href', `/app/campaign-series/${sampleSeriesId}/reports`);
	await expect(sample.getByText('Sample study', { exact: true })).toBeVisible();
	await expect(sample.getByText('Review sample results', { exact: true })).toBeVisible();

	const own = ownStudies.getByRole('link', { name: /New team study/i });
	await expect(own).toHaveAttribute('href', `/app/campaign-series/${alternateSeriesId}/setup`);
	await expect(own.getByText('Your study', { exact: true })).toBeVisible();
	await expect(own.getByText('Continue preparation', { exact: true })).toBeVisible();

	await expect(suggestedActions.getByText('setup.manage', { exact: true })).toHaveCount(0);
	await expect(
		page.getByRole('navigation', { name: 'Product navigation' }).getByRole('link', {
			name: 'Demo fixtures'
		})
	).toHaveCount(0);
});

test('home leads with the action queue before secondary study context', async ({ page }) => {
	await page.goto('/app');

	const overview = page.getByRole('region', { name: 'Workspace home' });
	const startHere = overview.getByRole('region', { name: 'Start here' });
	const nextWork = overview.getByRole('region', { name: 'Next actions' });
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	const lifecycle = overview.locator('[aria-label="Study workflow"]');
	const totals = overview.getByText('Workspace overview', { exact: true });

	await expectElementBefore(lifecycle, nextWork);
	await expectElementBefore(nextWork, startHere);
	await expectElementBefore(startHere, sampleStudies);
	await expectElementBefore(sampleStudies, ownStudies);
	await expectElementBefore(ownStudies, totals);
});

test('home lifecycle and totals do not use metric cards', async ({ page }) => {
	await page.goto('/app');

	const overview = page.getByRole('region', { name: 'Workspace home' });
	const lifecycle = overview.locator('[aria-label="Study workflow"]');
	await overview.getByText('Workspace overview', { exact: true }).click();
	const totals = overview.getByRole('group', { name: 'Workspace totals' });

	await expect(lifecycle).toBeVisible();
	await expect(totals).toBeVisible();
	await expect(overview.locator('.metric-card')).toHaveCount(0);
});

test('self-serve walkthrough contract exposes starter states and duplicate-to-edit', async ({
	page
}) => {
	const expectedSampleStates = [
		{
			id: setupSampleSeriesId,
			name: 'Setup readiness sample',
			homeHref: `/app/campaign-series/${setupSampleSeriesId}/setup`,
			portfolioHref: `/app/campaign-series/${setupSampleSeriesId}/setup`,
			actionLabel: 'Inspect sample preparation',
			portfolioActionLabel: 'Inspect sample preparation',
			readOnlyMessage:
				'Preparation sample: read-only starter content showing study preparation before launch.'
		},
		{
			id: collectionSampleSeriesId,
			name: 'Collection in progress sample',
			homeHref: `/app/campaign-series/${collectionSampleSeriesId}/operations`,
			portfolioHref: `/app/campaign-series/${collectionSampleSeriesId}/operations`,
			actionLabel: 'Inspect sample collection',
			portfolioActionLabel: 'Inspect sample collection',
			readOnlyMessage:
				'Collection sample: read-only starter content showing live or partial response collection.'
		},
		{
			id: sampleSeriesId,
			name: 'Completed sample',
			homeHref: `/app/campaign-series/${sampleSeriesId}/reports`,
			portfolioHref: `/app/campaign-series/${sampleSeriesId}/reports`,
			actionLabel: 'Review sample results',
			portfolioActionLabel: 'Review sample results',
			readOnlyMessage:
				'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
		},
		{
			id: longitudinalSampleSeriesId,
			name: 'Longitudinal wave sample',
			homeHref: `/app/campaign-series/${longitudinalSampleSeriesId}/reports`,
			portfolioHref: `/app/campaign-series/${longitudinalSampleSeriesId}/reports`,
			actionLabel: 'Review sample results',
			portfolioActionLabel: 'Review sample results',
			readOnlyMessage:
				'Repeat-participation sample: read-only starter content showing repeated measurements and linked repeat-response review.'
		}
	];
	const createdSeriesId = '019ad5b6-7f00-7000-8a00-000000000201';
	const duplicateRequests: Array<{ name: string }> = [];

	await page.route(`**/campaign-series/${sampleSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				...sampleStudyOwnership,
				name: 'Completed sample'
			} satisfies CampaignSeriesHubResponse
		});
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleSetupWorkspace,
				series: {
					...sampleSetupWorkspace.series,
					...sampleStudyOwnership,
					name: 'Completed sample'
				}
			} satisfies CampaignSeriesSetupWorkspaceResponse
		});
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/duplicate`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/duplicate`)) {
			await route.fallback();
			return;
		}

		duplicateRequests.push(route.request().postDataJSON() as { name: string });
		await route.fulfill({
			status: 201,
			json: {
				id: createdSeriesId,
				name: 'Copy of Completed sample',
				studyKind: 'own',
				isSample: false,
				sourceCampaignSeriesId: sampleSeriesId
			} satisfies CampaignSeriesDuplicateResponse
		});
	});

	await page.route(`**/campaign-series/${createdSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${createdSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				...ownSeriesOwnership,
				id: createdSeriesId,
				name: 'Copy of Completed sample',
				campaigns: []
			} satisfies CampaignSeriesHubResponse
		});
	});

	await page.route(`**/campaign-series/${createdSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${createdSeriesId}/setup-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: createEmptySetupWorkspace(createdSeriesId, 'Copy of Completed sample')
		});
	});

	await page.goto('/app');

	const overview = page.getByRole('region', { name: 'Workspace home' });
	const lifecycle = overview.locator('[aria-label="Study workflow"]');
	const homeSamples = overview.getByRole('region', { name: 'Sample studies' });

	for (const label of ['Protocol', 'Field', 'Evidence', 'Export']) {
		await expect(lifecycle.getByText(label, { exact: true })).toBeVisible();
	}

	for (const sample of expectedSampleStates) {
		const card = homeSamples.getByRole('link', { name: new RegExp(sample.name) });
		await expect(card).toHaveAttribute('href', sample.homeHref);
		await expect(card.getByText('Sample study', { exact: true })).toBeVisible();
		await expect(card.getByText(sample.actionLabel, { exact: true })).toBeVisible();
	}

	await page.route('**/campaign-series**', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/campaign-series')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				items: [
					{
						...sampleCampaignSeriesList.items[0],
						...setupSampleStudyOwnership,
						id: setupSampleSeriesId,
						name: 'Setup readiness sample',
						campaignCount: 0,
						liveCampaignCount: 0,
						submittedResponseCount: 0,
						latestLaunchAt: null,
						latestSubmissionAt: null,
						readinessStatus: 'not_configured'
					},
					{
						...sampleCampaignSeriesList.items[0],
						...collectionSampleStudyOwnership,
						id: collectionSampleSeriesId,
						name: 'Collection in progress sample',
						campaignCount: 1,
						liveCampaignCount: 1,
						submittedResponseCount: 0,
						latestLaunchAt: '2026-05-05T10:15:00Z',
						latestSubmissionAt: null,
						readinessStatus: 'ready'
					},
					{
						...sampleCampaignSeriesList.items[0],
						...sampleStudyOwnership,
						id: sampleSeriesId,
						name: 'Completed sample',
						campaignCount: 3,
						liveCampaignCount: 0,
						submittedResponseCount: 64,
						latestLaunchAt: '2026-05-05T10:15:00Z',
						latestSubmissionAt: '2026-05-07T11:20:00Z',
						readinessStatus: 'proof_only'
					},
					{
						...sampleCampaignSeriesList.items[0],
						...longitudinalSampleStudyOwnership,
						id: longitudinalSampleSeriesId,
						name: 'Longitudinal wave sample',
						campaignCount: 2,
						liveCampaignCount: 2,
						submittedResponseCount: 12,
						latestLaunchAt: '2026-05-12T10:15:00Z',
						latestSubmissionAt: '2026-05-12T11:20:00Z',
						readinessStatus: 'proof_only'
					},
					{
						...sampleCampaignSeriesList.items[0],
						...ownSeriesOwnership,
						id: alternateSeriesId,
						name: 'New team study',
						campaignCount: 0,
						liveCampaignCount: 0,
						submittedResponseCount: 0,
						latestLaunchAt: null,
						latestSubmissionAt: null,
						readinessStatus: 'not_configured'
					}
				]
			} satisfies CampaignSeriesListResponse
		});
	});

	await page.goto('/app/campaign-series');

	const portfolio = page.getByRole('region', { name: 'Studies' }).first();
	for (const sample of expectedSampleStates) {
		const article = portfolio.getByRole('article', { name: sample.name });
		await expect(article.getByText('Sample study', { exact: true })).toBeVisible();
		await expect(article.getByText(sample.readOnlyMessage, { exact: true })).toBeVisible();
		await expect(
			article.getByRole('link', { name: new RegExp(sample.portfolioActionLabel) })
		).toHaveAttribute('href', sample.portfolioHref);
		await expect(article.getByRole('button', { name: `Duplicate as study ${sample.name}` })).toBeVisible();
		await expect(article.getByRole('button', { name: new RegExp(`Rename ${sample.name}`) })).toHaveCount(0);
		await expect(article.getByRole('button', { name: new RegExp(`Archive ${sample.name}`) })).toHaveCount(0);
	}

	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const hub = page.getByRole('region', { name: 'Selected study overview' });
	const readOnlyState = hub.getByRole('note', { name: 'Sample study read-only state' });
	await expect(readOnlyState.getByText('Sample study', { exact: true })).toBeVisible();
	await expect(
		readOnlyState.getByText(
			'Results sample: read-only starter content showing collected responses, scores, reports, and exports.',
			{ exact: true }
		)
	).toBeVisible();
	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);
	const setupWorkspace = page.getByRole('region', { name: 'Protocol workspace' });
	await expect(setupWorkspace.getByRole('region', { name: 'Sample study read-only state' })).toBeVisible();
	await expect(setupWorkspace.getByRole('group', { name: 'Protocol progress' })).toBeVisible();

	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page, []);
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);
	await expect(
		page.getByRole('button', { name: 'Duplicate as study Completed sample' })
	).toHaveCount(0);
	await expect(page.getByRole('note', { name: 'Sample study read-only state' })).toBeVisible();

	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page);
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);
	await page
		.getByRole('note', { name: 'Sample study read-only state' })
		.getByRole('button', { name: 'Duplicate as study Completed sample' })
		.click();

	await expect.poll(() => duplicateRequests).toEqual([{ name: 'Copy of Completed sample' }]);
	await expect(page).toHaveURL(`/app/campaign-series/${createdSeriesId}/setup`);
});

test.skip('renders route guidance before top-level route work panels', async ({ page }) => {
	const routes = [
		{
			path: '/app',
			title: 'Start with study context',
			surface: 'Self-serve study cockpit'
		},
		{
			path: '/app/campaign-series',
			title: 'Choose the right study',
			surface: 'Study portfolio'
		},
		{
			path: '/app/exports',
			title: 'Use generated files',
			surface: 'Export workspace'
		}
	];

	for (const route of routes) {
		await page.goto(route.path);

		const guidance = page.getByRole('region', { name: 'Route guidance' });
		const surface = page.getByRole('region', { name: route.surface });

		await expect(guidance).toBeVisible();
		await expect(guidance.getByRole('heading', { name: route.title, exact: true })).toBeVisible();
		await expect(guidance.getByText('Inspect first', { exact: true })).toBeVisible();
		await expect(guidance.getByText('When it matters', { exact: true })).toBeVisible();
		await expect(guidance.getByText('Next move', { exact: true })).toBeVisible();
		await expectElementBefore(guidance, surface);
	}
});

test.skip('first-session hierarchy keeps guidance compact and current work first', async ({ page }) => {
	await page.goto('/app');

	const homeGuidance = page.getByRole('region', { name: 'Route guidance' });
	const cockpit = page.getByRole('region', { name: 'Self-serve study cockpit' });
	const lifecycle = cockpit.getByRole('group', { name: 'Study lifecycle' });
	const sampleStudies = cockpit.getByRole('region', { name: 'Sample studies' });
	const totals = cockpit.getByRole('group', { name: 'Workspace totals' });
	const suggestedActions = cockpit.getByRole('region', { name: 'Suggested next actions' });

	await expect(homeGuidance).toBeVisible();
	await expect(page.locator('.route-guidance.product-panel')).toHaveCount(0);
	await expectElementBefore(suggestedActions, sampleStudies);
	await expectElementBefore(suggestedActions, lifecycle);
	await expectElementBefore(suggestedActions, totals);
	await expectElementBefore(sampleStudies, lifecycle);
	await expectElementBefore(sampleStudies, totals);

	await page.goto('/app/campaign-series');

	const studiesGuidance = page.getByRole('region', { name: 'Route guidance' });
	const portfolio = page.getByRole('region', { name: 'Study portfolio' });
	const firstPortfolioRow = portfolio.getByRole('article', { name: 'Quarterly pulse' });
	const filters = page.getByRole('group', { name: 'Study portfolio filters' });
	const createStudy = page.getByRole('region', { name: 'Create your study' });

	await expect(studiesGuidance).toBeVisible();
	await expect(page.locator('.route-guidance.product-panel')).toHaveCount(0);
	await expectElementBefore(createStudy, firstPortfolioRow);
	await expectElementBefore(createStudy, filters);
	await expectElementBefore(firstPortfolioRow, filters);

	await page.route(`**/campaign-series/${sampleSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				...sampleStudyOwnership
			} satisfies CampaignSeriesHubResponse
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const hub = page.getByRole('region', { name: 'Selected study overview' });
	const summary = hub.getByRole('region', { name: 'Selected study summary' });
	const hubGuidance = hub.getByRole('region', { name: 'Route guidance' });
	const readOnlyState = hub.getByRole('note', { name: 'Sample study read-only state' });
	const lifecycleMap = hub.getByRole('group', { name: 'Study lifecycle' });
	const reference = hub.getByRole('region', { name: 'Study reference' });

	await expect(hubGuidance).toBeVisible();
	await expect(page.locator('.route-guidance.product-panel')).toHaveCount(0);
	await expectElementBefore(summary, hubGuidance);
	await expectElementBefore(readOnlyState, hubGuidance);
	await expectElementBefore(lifecycleMap, reference);

	await page.setViewportSize({ width: 390, height: 800 });
	await page.reload();
	await expect(page.getByRole('region', { name: 'Route guidance' })).toBeVisible();
	await expect(
		page.getByRole('note', { name: 'Sample study read-only state' }).getByRole('button', {
			name: 'Duplicate as study Quarterly pulse'
		})
	).toBeVisible();
});

test('renders home empty states with sample and own study guidance', async ({ page }) => {
	await page.route('**/workspace-overview', async (route) => {
		if (!isProductApiPath(route.request().url(), '/workspace-overview')) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleWorkspaceOverview,
				studyCollections: {
					sampleStudies: [],
					ownStudies: []
				},
				commandCenter: {
					items: []
				},
				recentSeries: []
			} satisfies WorkspaceOverviewResponse
		});
	});

	await page.goto('/app');

	const overview = page.getByRole('region', { name: 'Workspace home' });
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	const suggestedActions = overview.getByRole('region', { name: 'Next actions' });

	await expect(
		sampleStudies.getByText(
			'Sample studies are read-only examples. They do not create or change real workspace studies.',
			{ exact: true }
		).first()
	).toBeVisible();
	await expect(
		ownStudies.getByText(
			'Your editable studies appear here after you create one.',
			{ exact: true }
		)
	).toBeVisible();
	await expect(
		suggestedActions.getByText('Open Studies to create or continue study work.', {
			exact: true
		})
	).toBeVisible();
});

test('renders grouped product navigation by intent on the home surface', async ({ page }) => {
	await page.goto('/app');


	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	const studies = nav.getByRole('group', { name: 'Workspace', exact: true });
	const people = nav.getByRole('group', { name: 'People and access' });
	const admin = nav.getByRole('group', { name: 'Workspace admin' });

	await expect(studies).toBeVisible();
	await expect(people).toBeVisible();
	await expect(admin).toBeVisible();

	for (const link of [
		{ label: /^Briefing/, href: '/app' },
		{ label: /^Studies\b/, href: '/app/campaign-series' }
	]) {
		await expect(studies.getByRole('link', { name: link.label })).toHaveAttribute(
			'href',
			link.href
		);
	}

	for (const link of [
		{ label: /^People\b/, href: '/app/directory' },
		{ label: /^Team\b/, href: '/app/team' }
	]) {
		await expect(people.getByRole('link', { name: link.label })).toHaveAttribute('href', link.href);
	}

	await expect(admin.getByRole('link', { name: /^Settings\b/ })).toHaveAttribute(
		'href',
		'/app/settings'
	);
	await expect(nav.getByRole('link', { name: /^Workspace\b/ })).toHaveCount(0);
	await expect(nav.getByRole('link', { name: /^Campaign series\b/ })).toHaveCount(0);
	await expect(nav.getByRole('link', { name: /^Instrument library\b/ })).toHaveCount(0);
	await expect(nav.getByRole('link', { name: /^Files\b/ })).toHaveCount(0);
	await expect(nav.getByRole('link', { name: /^Demo fixtures\b/ })).toHaveCount(0);
});

test('renders selected-study navigation separately from global studies links', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	const studies = nav.getByRole('group', { name: 'Workspace', exact: true });
	const selectedStudy = nav.getByRole('group', { name: 'Active study' });

	await expect(studies).toBeVisible();
	await expect(nav.getByRole('group', { name: 'People and access' })).toBeVisible();
	await expect(nav.getByRole('group', { name: 'Workspace admin' })).toBeVisible();
	await expect(selectedStudy).toBeVisible();

	await expect(studies.getByRole('link', { name: /^Studies\b/ })).toHaveAttribute(
		'href',
		'/app/campaign-series'
	);
	for (const link of [
		{ label: /^Overview\b/, href: `/app/campaign-series/${sampleSeriesId}` },
		{ label: /^Protocol/, href: `/app/campaign-series/${sampleSeriesId}/setup` },
		{ label: /^Field/, href: `/app/campaign-series/${sampleSeriesId}/operations` },
		{ label: /^Evidence/, href: `/app/campaign-series/${sampleSeriesId}/reports` }
	]) {
		await expect(selectedStudy.getByRole('link', { name: link.label })).toHaveAttribute(
			'href',
			link.href
		);
		await expect(studies.getByRole('link', { name: link.label })).toHaveCount(0);
	}
	await expect(selectedStudy.getByRole('link', { name: /^Evidence/ })).toHaveAttribute(
		'aria-current',
		'page'
	);
});

test('renders authenticated app shell session profile without exposing technical ids by default', async ({
	page
}) => {
	await page.goto('/app');

	const session = page.getByLabel('Signed-in workspace account');
	await expect(session.getByText(sampleSessionEmail, { exact: true }).first()).toBeVisible();
	await expect(
		session.getByText('Workspace administration access', { exact: true }).first()
	).toBeVisible();
	await expect(session.getByText(sampleSessionUserId, { exact: true })).toBeHidden();
	await expect(session.getByText(sampleSessionTenantId, { exact: true })).toBeHidden();

	await session.locator('summary').click();
	await expect(session.getByText('Signed in as', { exact: true })).toBeVisible();
	await expect(session.getByRole('link', { name: 'Sign out' })).toBeVisible();
	await expect(session.getByText(sampleSessionUserId, { exact: true })).toHaveCount(0);
	await expect(session.getByText(sampleSessionTenantId, { exact: true })).toHaveCount(0);
});

test('renders tenant settings profile, counts, management links, and mutable report branding', async ({ page }) => {
	await page.goto('/app/settings');

	await expect(page.getByRole('heading', { name: 'Workspace settings', exact: true })).toBeVisible();
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: 'Settings' })).toHaveAttribute('aria-current', 'page');

	const settings = page.getByRole('region', { name: 'Workspace settings' });
	await expect(settings.getByRole('heading', { name: 'What can you manage here?' })).toBeVisible();
	const links = settings.getByLabel('Workspace setting shortcuts');
	await expect(links.getByRole('link', { name: /Team access/i })).toHaveAttribute(
		'href',
		'/app/team'
	);
	await expect(links.getByRole('link', { name: /People/i })).toHaveAttribute(
		'href',
		'/app/directory'
	);
	await expect(links.getByRole('link', { name: /Study preparation/i })).toHaveAttribute(
		'href',
		'/app/campaign-series'
	);
	await expect(links.getByRole('link', { name: /Exports/i })).toHaveAttribute(
		'href',
		'/app/exports'
	);
	await expect(settings.locator('.metric-card')).toHaveCount(0);

	await settings.locator('summary').filter({ hasText: 'Workspace details' }).click();
	await expect(settings.getByRole('group', { name: 'Workspace profile details' })).toBeVisible();
	const reportBranding = settings.getByRole('group', { name: 'Report branding preview' });
	await expect(reportBranding.getByText('Report branding', { exact: true })).toBeVisible();
	await expect(
		reportBranding.getByRole('heading', { name: 'Occupational Health Lab', exact: true })
	).toBeVisible();
	await expect(reportBranding.getByText('Campaign series report', { exact: true })).toBeVisible();
	await expect(reportBranding.getByText('Tenant profile', { exact: true })).toBeVisible();
	await expect(
		reportBranding.getByText('Logo upload, Custom fonts, Product shell theming', { exact: true })
	).toBeVisible();

	const brandingForm = reportBranding.getByRole('form', { name: 'Report branding settings' });
	await brandingForm.getByLabel('Organization label').fill('Acme OSH Consulting');
	await brandingForm.getByLabel('Report title').fill('Monthly workplace risk report');
	await brandingForm.getByLabel('Accent color').fill('#0f766e');
	await brandingForm.getByLabel('Layout').selectOption('compact');
	await brandingForm.getByRole('button', { name: 'Save report branding' }).click();
	await expect(reportBranding.getByText('Report branding saved', { exact: true })).toBeVisible();
	await expect(
		reportBranding.getByRole('heading', { name: 'Acme OSH Consulting', exact: true })
	).toBeVisible();
	await expect(reportBranding.getByText('Monthly workplace risk report', { exact: true })).toBeVisible();
	await expect(reportBranding.getByText('Tenant settings', { exact: true })).toBeVisible();
	await expect(settings.getByRole('group', { name: 'Workspace counts' })).toBeVisible();
});

test('renders instrument library summary and visible instruments', async ({ page }) => {
	await page.goto('/app/instruments');

	await expect(page.getByRole('heading', { name: 'Instruments', exact: true })).toBeVisible();
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: /^Instrument library\b/ })).toHaveCount(0);

	const library = page.getByRole('region', { name: 'Instrument library' });
	await expect(library.locator('.metric-card')).toHaveCount(0);
	await expect(page.getByText('Instrument library unavailable')).toHaveCount(0);
});

test('renders export file library summary and latest artifacts', async ({ page }) => {
	await page.goto('/app/exports');

	await expect(page.getByRole('heading', { name: 'Download files', exact: true })).toBeVisible();
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: 'Exports' })).toHaveCount(0);

	const library = page.getByRole('region', { name: 'Download files' });
	const overview = library.getByRole('group', { name: 'Export overview' });
	const artifacts = library.getByLabel('Export files');

	await expect(overview).toBeVisible();
	await expectElementBefore(overview, artifacts);
	await expect(library.locator('.metric-card')).toHaveCount(0);

	const report = artifacts.getByRole('article', { name: 'baseline-report.csv' });
	await expect(report.getByText('baseline-report.csv', { exact: true })).toBeVisible();
	await expect(report.getByText('Succeeded', { exact: true })).toBeVisible();
	await expect(report.getByText('Download')).toBeVisible();
	await expect(report.getByText('Available', { exact: true })).toBeVisible();

	const response = artifacts.getByRole('article', { name: 'responses.csv' });
	await expect(response.getByText('Failed', { exact: true })).toBeVisible();
	await expect(response.getByText('export.failed', { exact: true })).toBeVisible();
	await expect(response.getByRole('link', { name: 'Reports' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/reports`
	);
});

test('renders tenant member roster from the product read model', async ({ page }) => {
	await page.goto('/app/team');

	await expect(page.getByRole('heading', { name: 'Team', exact: true })).toBeVisible();
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: 'Team' })).toHaveAttribute('aria-current', 'page');

	const roster = page.getByRole('region', { name: 'Team roster' });
	await expect(
		roster.getByRole('group', { name: 'Tenant member roster counts' }).getByText('2', {
			exact: true
		})
	).toBeVisible();
	const owner = roster.getByRole('article', { name: 'owner@example.test' });
	const analyst = roster.getByRole('article', { name: 'analyst@example.test' });
	await expect(owner.getByText('owner@example.test', { exact: true })).toBeVisible();
	await expect(owner.getByText('Current user', { exact: true })).toBeVisible();
	await expect(
		owner
			.getByRole('group', { name: 'Assigned roles for owner@example.test' })
			.getByText('Tenant Owner', { exact: true })
	).toBeVisible();
	await expect(
		owner.getByRole('group', { name: 'Capabilities for owner@example.test' })
	).toContainText('Study setup and launch');
	await expect(
		owner.getByRole('group', { name: 'Capabilities for owner@example.test' })
	).toContainText('Team access management');
	await expect(owner.getByText('Active', { exact: true })).toBeVisible();
	await expect(analyst.getByText('analyst@example.test', { exact: true })).toBeVisible();
	await expect(
		analyst
			.getByRole('group', { name: 'Assigned roles for analyst@example.test' })
			.getByText('Analyst', { exact: true })
	).toBeVisible();
	await expect(
		analyst.getByRole('group', { name: 'Capabilities for analyst@example.test' })
	).toContainText('Reports and exports');
	await expect(analyst.getByText('Invite pending', { exact: true })).toBeVisible();
	await expect(page.getByRole('region', { name: 'Prepare tenant member' })).toBeVisible();
	await expect(page.getByLabel('Member email')).toBeVisible();
	await expect(page.getByLabel('Member role')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Add member' })).toBeVisible();
	await expect(page.getByRole('combobox', { name: 'Role for analyst@example.test' })).toBeVisible();
});

test('team route explains access capabilities without raw permission codes', async ({ page }) => {
	await page.goto('/app/team');

	const overview = page.getByRole('region', { name: 'Team access overview' });
	const prepare = page.getByRole('region', { name: 'Prepare tenant member' });
	const roster = page.getByRole('region', { name: 'Team roster' });
	const owner = roster.getByRole('article', { name: 'owner@example.test' });

	await expectElementBefore(overview, prepare);
	await expectElementBefore(overview, roster);
	await expect(overview.getByRole('heading', { name: 'Team access management' })).toBeVisible();
	await expect(overview.locator('.metric-card')).toHaveCount(0);
	await expect(roster.locator('.metric-card')).toHaveCount(0);
	await expect(
		owner.getByRole('group', { name: 'Capabilities for owner@example.test' })
	).toContainText('Study setup and launch');
	await expect(
		owner.getByRole('group', { name: 'Capabilities for owner@example.test' })
	).toContainText('Team access management');
	await expect(roster.getByText('setup.manage', { exact: true })).toHaveCount(0);
	await expect(roster.getByText('team.manage', { exact: true })).toHaveCount(0);
});

test('renders tenant member roster read-only without team management permission', async ({
	page
}) => {
	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page, ['setup.manage']);

	await page.goto('/app/team');

	const readOnly = page.getByRole('region', { name: 'Read-only team access' });
	const roster = page.getByRole('region', { name: 'Team roster' });
	await expect(readOnly.getByText('team management access')).toBeVisible();
	await expectElementBefore(readOnly, roster);
	await expect(roster.getByText('owner@example.test', { exact: true })).toBeVisible();
	await expect(page.getByRole('region', { name: 'Prepare tenant member' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Add member' })).toHaveCount(0);
	await expect(page.getByRole('combobox', { name: 'Role for analyst@example.test' })).toHaveCount(
		0
	);
	await expect(
		page.getByRole('button', { name: 'Change role for analyst@example.test' })
	).toHaveCount(0);
});

test('creates tenant members from the team page and refreshes the roster', async ({ page }) => {
	const createdMember = {
		...sampleTenantMemberRoster.members[1],
		userId: '66666666-6666-4666-8666-666666666666',
		email: 'new.member@example.test',
		locale: 'hr',
		identityStatus: 'pending_provider_link' as const
	};
	let roster = sampleTenantMemberRoster;
	const createBodies: unknown[] = [];

	await page.route('**/tenant-members', async (route) => {
		if (!isProductApiPath(route.request().url(), '/tenant-members')) {
			await route.fallback();
			return;
		}

		if (route.request().method() === 'GET') {
			await route.fulfill({ json: roster });
			return;
		}

		if (route.request().method() === 'POST') {
			createBodies.push(route.request().postDataJSON());
			roster = {
				...sampleTenantMemberRoster,
				members: [...sampleTenantMemberRoster.members, createdMember]
			};
			await route.fulfill({ json: { member: createdMember } });
			return;
		}

		await route.fallback();
	});

	await page.goto('/app/team');
	await page.getByLabel('Member email').fill('new.member@example.test');
	await page.getByLabel('Member role').selectOption('analyst');
	await page.getByLabel('Member locale').fill('hr');
	await page.getByRole('button', { name: 'Add member' }).click();

	await expect(page.getByText('new.member@example.test', { exact: true })).toBeVisible();
	const created = page.getByRole('article', { name: 'new.member@example.test' });
	await expect(created.getByText('Invite pending', { exact: true })).toBeVisible();
	await expect(created.getByRole('link', { name: 'Open link' })).toHaveAttribute(
		'href',
		/login_hint=new\.member%40example\.test/
	);
	await expect(created.getByRole('button', { name: 'Copy link' })).toBeVisible();
	expect(createBodies).toEqual([
		{
			email: 'new.member@example.test',
			roleCode: 'analyst',
			locale: 'hr'
		}
	]);
});

test('changes another tenant member role from the team page and refreshes the roster', async ({
	page
}) => {
	const updatedAnalyst = {
		...sampleTenantMemberRoster.members[1],
		roles: [
			{
				roleId: '33333333-3333-4333-8333-333333333333',
				code: 'tenant_owner',
				name: 'Tenant Owner',
				scopeType: 'tenant',
				scopeId: null,
				grantedAt: '2026-05-12T10:00:00Z'
			}
		],
		permissions: ['setup.manage', 'team.manage', 'export.read']
	};
	let roster = sampleTenantMemberRoster;
	const changeBodies: unknown[] = [];

	await page.route('**/tenant-members', async (route) => {
		if (!isProductApiPath(route.request().url(), '/tenant-members')) {
			await route.fallback();
			return;
		}

		if (route.request().method() === 'GET') {
			await route.fulfill({ json: roster });
			return;
		}

		await route.fallback();
	});

	await page.route('**/tenant-members/*/tenant-role', async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/tenant-members/${sampleTenantMemberRoster.members[1].userId}/tenant-role`
			)
		) {
			await route.fallback();
			return;
		}

		changeBodies.push(route.request().postDataJSON());
		roster = {
			...sampleTenantMemberRoster,
			members: [sampleTenantMemberRoster.members[0], updatedAnalyst]
		};
		await route.fulfill({ json: { member: updatedAnalyst } });
	});

	await page.goto('/app/team');
	const analyst = page.getByRole('article', { name: 'analyst@example.test' });
	await analyst
		.getByRole('combobox', { name: 'Role for analyst@example.test' })
		.selectOption('tenant_owner');
	await analyst.getByRole('button', { name: 'Change role for analyst@example.test' }).click();

	await expect(
		analyst
			.getByRole('group', { name: 'Assigned roles for analyst@example.test' })
			.getByText('Tenant Owner', { exact: true })
	).toBeVisible();
	await expect(
		analyst.getByRole('group', { name: 'Capabilities for analyst@example.test' })
	).toContainText('Team access management');
	expect(changeBodies).toEqual([{ roleCode: 'tenant_owner' }]);
});

test('directory targeting overview explains hierarchy before setup actions', async ({ page }) => {
	await page.goto('/app/directory');

	await expect(page.getByRole('heading', { name: 'People and groups', level: 1 })).toBeVisible();
	const overview = page.getByRole('region', { name: 'People and groups' });
	const subjectDirectory = page.getByRole('region', { name: 'People directory' });
	const subjectGroups = page.getByRole('region', { name: 'Audience groups' });
	const createRecords = page.getByRole('region', { name: 'Create people records' });
	const relationships = page.getByRole('region', { name: 'People relationships' });

	await expect(overview).toBeVisible();
	await expect(overview.getByText('Build the audience list first', { exact: true })).toBeVisible();
	await expect(overview.getByText('How people data is used', { exact: true })).toBeVisible();
	const overviewCounts = overview.locator('[aria-label="People and targeting counts"]');
	await expect(overviewCounts.locator('div').filter({ hasText: 'People' })).toContainText('2');
	await expect(overviewCounts.locator('div').filter({ hasText: 'Groups' })).toContainText('1');
	await expect(overviewCounts.locator('div').filter({ hasText: 'Memberships' })).toContainText(
		'1'
	);
	await expect(overviewCounts.locator('div').filter({ hasText: 'Manager links' })).toContainText(
		'1'
	);

	const graphCounts = subjectDirectory.locator('dl').first();
	await expect(graphCounts.locator('div').filter({ hasText: 'Subjects' })).toContainText('2');
	await expect(graphCounts.locator('div').filter({ hasText: 'Groups' })).toContainText('1');
	await expect(graphCounts.locator('div').filter({ hasText: 'Manager links' })).toContainText('1');
	await expect(page.locator('.metric-card')).toHaveCount(0);

	await expectElementBefore(overview, createRecords);
	await expectElementBefore(overview, relationships);
	await expectElementBefore(subjectDirectory, createRecords);
	await expectElementBefore(subjectDirectory, relationships);
	await expectElementBefore(subjectGroups, createRecords);

	const visibleAttributeFields = page
		.locator('label.field:visible')
		.filter({ hasText: 'Attributes JSON' });
	await expect(page.getByText('Advanced attributes', { exact: true })).toHaveCount(3);
	await expect(visibleAttributeFields).toHaveCount(0);
	await createRecords.getByText('Advanced attributes', { exact: true }).first().click();
	await expect(
		createRecords.locator('label.field:visible').filter({ hasText: 'Attributes JSON' })
	).toHaveCount(1);
});

test('directory Microsoft Graph panel prepares consent without exposing secrets', async ({
	page
}) => {
	await page.goto('/app/directory');

	const graph = page.getByRole('region', { name: 'Microsoft people import' });
	await expect(graph.getByText('Not connected', { exact: true })).toBeVisible();
	const history = page.getByRole('region', { name: 'Microsoft people import runs' });
	await expect(history.getByText('Recent Microsoft people imports', { exact: true })).toBeVisible();
	await expect(history.getByText(/Runs linked to an active saved import rule/)).toBeVisible();
	await expect(history.getByRole('button', { name: 'Preview this rule again' })).toBeVisible();
	await expect(history.getByText('apply', { exact: true })).toBeVisible();
	await expect(history.getByText('3/3', { exact: true })).toBeVisible();
	const rules = page.getByRole('region', { name: 'Microsoft people import rules' });
	await expect(rules.getByText('Saved Microsoft import rules', { exact: true })).toBeVisible();
	await expect(rules.getByText('All employees', { exact: true })).toBeVisible();
	await expect(rules.getByText('Mark stale', { exact: true })).toBeVisible();
	await expect(rules.getByText('external_id, email, manager_external_id', { exact: true })).toBeVisible();
	await expect(rules.getByText(/still needs live connector input/)).toBeVisible();

	await graph.getByRole('button', { name: 'Prepare admin consent' }).click();

	await expect(graph.getByText('Consent pending', { exact: true })).toBeVisible();
	await expect(graph.getByText('Consent request prepared', { exact: true })).toBeVisible();
	await expect(graph.getByText('Request expires', { exact: true })).toBeVisible();
	await expect(graph.getByText('2026-06-12T12:20:00+00:00', { exact: true })).toBeVisible();
	await expect(graph.getByText('Callback path', { exact: true })).toBeVisible();
	await expect(
		graph.getByText('/app/directory', {
			exact: true
		})
	).toBeVisible();
	await expect(graph.getByRole('link', { name: 'Open Microsoft admin consent' })).toHaveAttribute(
		'href',
		/login\.microsoftonline\.com\/common\/adminconsent/
	);
	await expect(graph.getByText('state-value', { exact: true })).toHaveCount(0);
	await expect(graph.getByText('nonce-value', { exact: true })).toHaveCount(0);
});

test('directory Microsoft Graph panel completes admin consent redirect from query', async ({
	page
}) => {
	await page.goto('/app/directory?admin_consent=True&tenant=ms-tenant-001&state=state-value');

	const graph = page.getByRole('region', { name: 'Microsoft people import' });
	await expect(graph.getByText('Connected', { exact: true })).toBeVisible();
	await expect(graph.getByText('contoso.example', { exact: true })).toBeVisible();
	await expect(graph.getByText('state-value', { exact: true })).toHaveCount(0);
	await expect(graph.getByText('nonce-value', { exact: true })).toHaveCount(0);
	await expect(page).toHaveURL(/\/app\/directory$/);
});

test('directory Microsoft Graph panel reruns a saved-rule import from history', async ({ page }) => {
	await page.goto('/app/directory');

	const history = page.getByRole('region', { name: 'Microsoft people import runs' });
	await history.getByRole('button', { name: 'Preview this rule again' }).click();

	const rules = page.getByRole('region', { name: 'Microsoft people import rules' });
	const allEmployees = rules.getByRole('article', { name: 'All employees' });
	await expect(allEmployees.getByText('Microsoft import preview ready', { exact: true })).toBeVisible();
	await expect(allEmployees.getByText('live-preview-run-id', { exact: true })).toBeVisible();
});

test('directory Microsoft Graph panel saves and archives import rules', async ({ page }) => {
	await page.goto('/app/directory');

	const rules = page.getByRole('region', { name: 'Microsoft people import rules' });
	await rules.getByLabel('Rule name').fill('Weekly all employees');
	await rules.getByRole('button', { name: 'Save import rule' }).click();

	await expect(rules.getByText('Weekly all employees', { exact: true })).toBeVisible();
	await expect(rules.getByText('raw_payload', { exact: true })).toHaveCount(0);
	await expect(rules.getByText('state-value', { exact: true })).toHaveCount(0);
	await rules
		.getByRole('article', { name: 'Weekly all employees' })
		.getByRole('button', { name: 'Archive rule' })
		.click();
	await expect(rules.getByText('Weekly all employees', { exact: true })).toHaveCount(0);
});

test('directory Microsoft Graph panel previews and applies a live saved rule', async ({ page }) => {
	await page.goto('/app/directory');

	const rules = page.getByRole('region', { name: 'Microsoft people import rules' });
	const allEmployees = rules.getByRole('article', { name: 'All employees' });
	await allEmployees.getByRole('button', { name: 'Preview Microsoft import' }).click();

	await expect(allEmployees.getByText('Microsoft import preview ready', { exact: true })).toBeVisible();
	await expect(allEmployees.getByText('1/1', { exact: true })).toBeVisible();
	await expect(allEmployees.getByText('live-preview-run-id', { exact: true })).toBeVisible();
	await allEmployees.getByRole('button', { name: 'Apply Microsoft import' }).click();
	await expect(allEmployees.getByText('Microsoft import preview ready', { exact: true })).toHaveCount(0);
});

test('renders campaign-series list items from the product read model', async ({ page }) => {
	await page.goto('/app/campaign-series');

	const list = page.getByRole('region', { name: 'Studies' }).first();
	const seriesArticle = list.getByRole('article', { name: 'Quarterly pulse' });
	const seriesItem = seriesArticle.getByRole('link', { name: /Quarterly pulse/i });

	await expect(page.getByRole('heading', { name: 'Studies', exact: true })).toBeVisible();
	await expect(seriesItem).toHaveAttribute('href', `/app/campaign-series/${sampleSeriesId}`);
	await expect(seriesArticle.getByText('Pending', { exact: true })).toBeVisible();
	await expect(seriesArticle.getByText('Measurements', { exact: true })).toBeVisible();
	await expect(seriesArticle.getByText('7', { exact: true })).toBeVisible();
	await expect(seriesArticle.getByText('Submitted responses', { exact: true })).toBeVisible();
	await expect(seriesArticle.getByText('128', { exact: true })).toBeVisible();
	await expect(page.locator(`[href*="${oldFoundationSeriesId}"]`)).toHaveCount(0);

	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: /^Briefing/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: /^Studies\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: /^Team\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: 'Demo fixtures' })).toHaveCount(0);
});

test('renders campaign-series as a grouped study portfolio', async ({ page }) => {
	await page.route('**/campaign-series**', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/campaign-series')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				items: [
					{
						...sampleCampaignSeriesList.items[0],
						...sampleStudyOwnership,
						id: sampleSeriesId,
						name: 'Completed burnout sample',
						liveCampaignCount: 0,
						submittedResponseCount: 128
					},
					{
						...sampleCampaignSeriesList.items[0],
						...ownSeriesOwnership,
						id: alternateSeriesId,
						name: 'New work ergonomics study',
						campaignCount: 0,
						liveCampaignCount: 0,
						submittedResponseCount: 0,
						readinessStatus: 'not_configured'
					},
					{
						...sampleCampaignSeriesList.items[0],
						...ownSeriesOwnership,
						id: oldFoundationSeriesId,
						name: 'Archived pilot',
						archived: true,
						archivedAt: '2026-05-11T13:15:00Z',
						archivedByUserId: '22222222-2222-4222-8222-222222222222',
						archiveReason: 'Completed pilot'
					}
				]
			} satisfies CampaignSeriesListResponse
		});
	});

	await page.goto('/app/campaign-series?visibility=all');

	await expect(page.getByRole('heading', { name: 'Studies', exact: true })).toBeVisible();
	await expect(page.getByText('Workspace object', { exact: true })).toHaveCount(0);
	await expect(page.getByText('Tenant read model', { exact: true })).toHaveCount(0);
	await expect(page.getByText('Series entry point', { exact: true })).toHaveCount(0);

	const portfolio = page.getByRole('region', { name: 'Studies' }).first();
	await expect(portfolio.getByRole('heading', { name: 'Sample studies' })).toBeVisible();
	await expect(portfolio.getByRole('heading', { name: 'Your studies' })).toBeVisible();
	const lifecycleHeaders = portfolio.locator('.study-lifecycle-group__header');
	await expect(lifecycleHeaders.getByText('Results ready', { exact: true })).toBeVisible();
	await expect(lifecycleHeaders.getByText('Needs setup', { exact: true })).toBeVisible();
	await expect(lifecycleHeaders.getByText('Archived', { exact: true })).toBeVisible();

	const sampleStudy = portfolio.getByRole('article', { name: 'Completed burnout sample' });
	await expect(sampleStudy.getByText('Sample study', { exact: true })).toBeVisible();
	await expect(sampleStudy.getByRole('link', { name: /Review sample results/i })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/reports`
	);
	await expect(
		sampleStudy.getByRole('button', { name: /Rename Completed burnout sample/i })
	).toHaveCount(0);

	const ownStudy = portfolio.getByRole('article', { name: 'New work ergonomics study' });
	await expect(ownStudy.getByText('Your study', { exact: true })).toBeVisible();
	await expect(ownStudy.getByRole('link', { name: /Continue preparation/i })).toHaveAttribute(
		'href',
		`/app/campaign-series/${alternateSeriesId}/setup`
	);

	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: /^Studies\b/ })).toBeVisible();
	await expect(nav.getByText('People and access', { exact: true })).toBeVisible();
	await expect(nav.getByText('Workspace admin', { exact: true })).toBeVisible();
});

test('studies route surfaces create path before portfolio scanning controls', async ({
	page
}) => {
	await page.goto('/app/campaign-series');

	const portfolio = page.getByRole('region', { name: 'Studies' }).first();
	const createStudy = page.locator('.portfolio-create').first();
	const firstPortfolioRow = portfolio
		.getByRole('article', { name: /Quarterly pulse/i })
		.first();
	const filters = portfolio.getByRole('group', { name: 'Study list' });

	await expectElementBefore(createStudy, firstPortfolioRow);
	await expectElementBefore(createStudy, filters);
});

test('labels sample studies and hides portfolio mutations for read-only starter content', async ({
	page
}) => {
	const sampleStudyId = sampleSeriesId;
	const ownStudyId = alternateSeriesId;

	await page.route('**/campaign-series**', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/campaign-series')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				items: [
					{
						...sampleCampaignSeriesList.items[0],
						...sampleStudyOwnership,
						id: sampleStudyId,
						name: 'Starter burnout sample'
					},
					{
						...sampleCampaignSeriesList.items[0],
						...ownSeriesOwnership,
						id: ownStudyId,
						name: 'Owner follow-up study'
					}
				]
			} satisfies CampaignSeriesListResponse
		});
	});

	await page.goto('/app/campaign-series');

	const list = page.getByRole('region', { name: 'Studies' }).first();
	const sampleStudy = list.getByRole('article', { name: 'Starter burnout sample' });
	const ownStudy = list.getByRole('article', { name: 'Owner follow-up study' });

	await expect(sampleStudy.getByText('Sample study', { exact: true })).toBeVisible();
	await expect(
		sampleStudy.getByText(
			'Results sample: read-only starter content showing collected responses, scores, reports, and exports.',
			{ exact: true }
		)
	).toBeVisible();
	await expect(
		sampleStudy.getByRole('button', { name: /Rename Starter burnout sample/i })
	).toHaveCount(0);
	await expect(
		sampleStudy.getByRole('button', { name: /Archive Starter burnout sample/i })
	).toHaveCount(0);
	await expect(
		sampleStudy.getByRole('button', { name: 'Duplicate as study Starter burnout sample' })
	).toBeVisible();

	await expect(ownStudy.getByText('Your study', { exact: true })).toBeVisible();
	await expect(
		ownStudy.getByRole('button', { name: 'Rename Owner follow-up study' })
	).toBeVisible();
	await expect(
		ownStudy.getByRole('button', { name: 'Archive Owner follow-up study' })
	).toBeVisible();
	await expect(ownStudy.getByRole('button', { name: /Duplicate as study/i })).toHaveCount(0);
});

test('duplicates a sample study from the portfolio and routes to setup', async ({ page }) => {
	const createdSeriesId = '4e1a4f0a-2788-49ce-b1b7-4a4fae0ea911';
	const createdSeriesName = 'Copy of Starter burnout sample';
	const duplicateRequests: Array<{ name: string }> = [];

	await page.route('**/campaign-series**', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/campaign-series')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				items: [
					{
						...sampleCampaignSeriesList.items[0],
						...sampleStudyOwnership,
						id: sampleSeriesId,
						name: 'Starter burnout sample'
					}
				]
			} satisfies CampaignSeriesListResponse
		});
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/duplicate`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/duplicate`)) {
			await route.fallback();
			return;
		}

		duplicateRequests.push(route.request().postDataJSON() as { name: string });
		await route.fulfill({
			status: 201,
			json: {
				id: createdSeriesId,
				name: createdSeriesName,
				studyKind: 'own',
				isSample: false,
				sourceCampaignSeriesId: sampleSeriesId
			} satisfies CampaignSeriesDuplicateResponse
		});
	});

	await page.route(`**/campaign-series/${createdSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${createdSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				...ownSeriesOwnership,
				id: createdSeriesId,
				name: createdSeriesName,
				campaigns: []
			} satisfies CampaignSeriesHubResponse
		});
	});

	await page.route(`**/campaign-series/${createdSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${createdSeriesId}/setup-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: createEmptySetupWorkspace(createdSeriesId, createdSeriesName)
		});
	});

	await page.goto('/app/campaign-series');

	await page
		.getByRole('article', { name: 'Starter burnout sample' })
		.getByRole('button', { name: 'Duplicate as study Starter burnout sample' })
		.click();

	await expect.poll(() => duplicateRequests).toEqual([{ name: createdSeriesName }]);
	await expect(page).toHaveURL(`/app/campaign-series/${createdSeriesId}/setup`);
	await expect(page.getByRole('region', { name: 'Protocol workspace' })).toBeVisible();
	await expect(page.getByRole('group', { name: 'Protocol progress' })).toBeVisible();
});

test('renders product surfaces read-only without setup management permission', async ({ page }) => {
	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page, []);

	await page.goto('/app/campaign-series');

	const studies = page.getByRole('region', { name: 'Studies', exact: true });
	await expect(studies).toBeVisible();
	await expect(studies.getByRole('region', { name: 'Read-only access' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create study' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: /^Rename\b/i })).toHaveCount(0);
	await expect(page.getByRole('button', { name: /^Archive\b/i })).toHaveCount(0);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);
	await expect(page.getByRole('region', { name: 'Protocol workspace' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create instrument import' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Generate new sample values' })).toHaveCount(0);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);
	const collection = page.getByRole('region', { name: 'Field workspace' });
	await expect(collection).toBeVisible();
	await expect(
		collection.getByText('Collection actions require workspace management access.', { exact: true })
	).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create respondent link' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Process local delivery' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Remediate missing scores' })).toHaveCount(0);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);
	await expect(page.getByRole('region', { name: 'Evidence workspace' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create results matrix export' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Create response export' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Review export file' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Download response dataset CSV' })).toHaveCount(0);

	await page.goto('/app/team');
	const roster = page.getByRole('region', { name: 'Team roster' });
	await expect(roster).toBeVisible();
	await expect(roster.getByText('owner@example.test', { exact: true })).toBeVisible();
});

test('loads campaign-series portfolio from query parameters', async ({ page }) => {
	const requestedPaths: string[] = [];

	await page.route('**/campaign-series**', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/campaign-series')
		) {
			await route.fallback();
			return;
		}

		const url = new URL(route.request().url());
		requestedPaths.push(`${url.pathname}${url.search}`);
		await route.fulfill({
			json: {
				items: [
					{
						...sampleCampaignSeriesList.items[0],
						name: 'Gamma proof',
						readinessStatus: 'proof_only',
						archived: true,
						archivedAt: '2026-05-11T13:15:00Z',
						archivedByUserId: '22222222-2222-4222-8222-222222222222',
						archiveReason: 'Completed pilot'
					}
				]
			} satisfies CampaignSeriesListResponse
		});
	});

	await page.goto(
		'/app/campaign-series?search=gamma&status=proof_only&sort=name_asc&visibility=archived'
	);

	await expect(page.getByLabel('Search studies')).toHaveValue('gamma');
	await expect(page.getByLabel('Readiness')).toHaveValue('proof_only');
	await expect(page.getByLabel('Sort')).toHaveValue('name_asc');
	await expect(page.getByLabel('Visibility')).toHaveValue('archived');
	await expect(page.getByRole('link', { name: /Gamma proof/i })).toBeVisible();
	expect(requestedPaths).toContain(
		'/campaign-series?search=gamma&status=proof_only&sort=name_asc&visibility=archived'
	);
});

test('updates campaign-series portfolio URL from controls', async ({ page }) => {
	const requestedPaths: string[] = [];

	await page.route('**/campaign-series**', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/campaign-series')
		) {
			await route.fallback();
			return;
		}

		const url = new URL(route.request().url());
		requestedPaths.push(`${url.pathname}${url.search}`);
		await route.fulfill({ json: sampleCampaignSeriesList });
	});

	await page.goto('/app/campaign-series');
	await page.getByLabel('Search studies').fill('retention');
	await page.getByLabel('Readiness').selectOption('proof_only');
	await page.getByLabel('Sort').selectOption('name_asc');
	await page.getByLabel('Visibility').selectOption('archived');

	await expect(page).toHaveURL(
		/\/app\/campaign-series\?search=retention&status=proof_only&sort=name_asc&visibility=archived$/
	);
	await expect
		.poll(() => requestedPaths)
		.toContain(
			'/campaign-series?search=retention&status=proof_only&sort=name_asc&visibility=archived'
		);
});

test('shows filtered empty campaign-series portfolio state', async ({ page }) => {
	await page.route('**/campaign-series**', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/campaign-series')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: { items: [] } satisfies CampaignSeriesListResponse });
	});

	await page.goto('/app/campaign-series?search=gamma&status=proof_only');

	await expect(page.getByText('No matching studies', { exact: true })).toBeVisible();
	await expect(
		page.getByText('Adjust search, readiness, or visibility filters to show sample or own studies.')
	).toBeVisible();
});

test('renames campaign-series portfolio row and reloads current query', async ({ page }) => {
	const requestedPaths: string[] = [];
	const renameRequests: Array<{ name: string }> = [];
	let renamed = false;

	await page.route('**/campaign-series**', async (route) => {
		const url = new URL(route.request().url());

		if (
			route.request().method() === 'PATCH' &&
			url.pathname === `/campaign-series/${sampleSeriesId}`
		) {
			renameRequests.push(route.request().postDataJSON() as { name: string });
			renamed = true;
			await route.fulfill({
				json: {
					id: sampleSeriesId,
					name: 'Renamed pulse',
					updatedAt: '2026-05-09T12:30:00Z'
				}
			});
			return;
		}

		if (route.request().method() !== 'GET' || url.pathname !== '/campaign-series') {
			await route.fallback();
			return;
		}

		requestedPaths.push(`${url.pathname}${url.search}`);
		await route.fulfill({
			json: {
				items: [
					{
						...sampleCampaignSeriesList.items[0],
						name: renamed ? 'Renamed pulse' : sampleCampaignSeriesList.items[0].name
					}
				]
			} satisfies CampaignSeriesListResponse
		});
	});

	await page.goto('/app/campaign-series?search=quarterly&status=pending&sort=name_asc');
	await page.getByRole('button', { name: 'Rename Quarterly pulse' }).click();
	await page.getByLabel('Rename study name').fill('Renamed pulse');
	await page.getByRole('button', { name: 'Save name' }).click();

	await expect.poll(() => renameRequests).toEqual([{ name: 'Renamed pulse' }]);
	await expect(page.getByRole('link', { name: /Renamed pulse/i })).toBeVisible();
	await expect
		.poll(() => requestedPaths)
		.toContain('/campaign-series?search=quarterly&status=pending&sort=name_asc');
});

test('archives campaign-series portfolio row and reloads active visibility', async ({ page }) => {
	const archiveRequests: unknown[] = [];
	let archived = false;

	await page.route('**/campaign-series**', async (route) => {
		const url = new URL(route.request().url());

		if (
			route.request().method() === 'POST' &&
			url.pathname === `/campaign-series/${sampleSeriesId}/archive`
		) {
			archiveRequests.push(route.request().postDataJSON());
			archived = true;
			await route.fulfill({
				json: {
					id: sampleSeriesId,
					archived: true,
					updatedAt: '2026-05-11T13:15:00Z',
					archivedAt: '2026-05-11T13:15:00Z',
					archivedByUserId: '22222222-2222-4222-8222-222222222222',
					archiveReason: null
				}
			});
			return;
		}

		if (route.request().method() !== 'GET' || url.pathname !== '/campaign-series') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				items: archived ? [] : sampleCampaignSeriesList.items
			} satisfies CampaignSeriesListResponse
		});
	});

	await page.goto('/app/campaign-series');
	await page.getByRole('button', { name: 'Archive Quarterly pulse' }).click();

	await expect.poll(() => archiveRequests).toEqual([{}]);
	await expect(page.getByRole('link', { name: /Quarterly pulse/i })).toHaveCount(0);
});

test('restores archived campaign-series portfolio row and reloads archived visibility', async ({
	page
}) => {
	const restoreRequests: unknown[] = [];
	let restored = false;

	await page.route('**/campaign-series**', async (route) => {
		const url = new URL(route.request().url());

		if (
			route.request().method() === 'POST' &&
			url.pathname === `/campaign-series/${sampleSeriesId}/restore`
		) {
			restoreRequests.push(route.request().postDataJSON());
			restored = true;
			await route.fulfill({
				json: {
					id: sampleSeriesId,
					archived: false,
					updatedAt: '2026-05-11T13:30:00Z',
					archivedAt: null,
					archivedByUserId: null,
					archiveReason: null
				}
			});
			return;
		}

		if (route.request().method() !== 'GET' || url.pathname !== '/campaign-series') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				items: restored
					? []
					: [
							{
								...sampleCampaignSeriesList.items[0],
								archived: true,
								archivedAt: '2026-05-11T13:15:00Z',
								archivedByUserId: '22222222-2222-4222-8222-222222222222',
								archiveReason: 'Completed pilot'
							}
						]
			} satisfies CampaignSeriesListResponse
		});
	});

	await page.goto('/app/campaign-series?visibility=archived');
	await page.getByRole('button', { name: 'Restore Quarterly pulse' }).click();

	await expect.poll(() => restoreRequests).toEqual([{}]);
	await expect(page.getByRole('link', { name: /Quarterly pulse/i })).toHaveCount(0);
});

test('creates a campaign series from the list and routes to setup', async ({ page }) => {
	const createdSeriesId = '3505be02-11b0-4b43-8a73-1f9f3f2c7d15';
	const createRequests: Array<{ name: string }> = [];

	await page.route('**/campaign-series', async (route) => {
		if (!isProductApiPath(route.request().url(), '/campaign-series')) {
			await route.fallback();
			return;
		}

		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		createRequests.push(route.request().postDataJSON() as { name: string });
		await route.fulfill({ status: 201, json: { id: createdSeriesId } });
	});

	await page.route(`**/campaign-series/${createdSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${createdSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				id: createdSeriesId,
				name: 'New routed pulse',
				totals: {
					campaignCount: 0,
					liveCampaignCount: 0,
					submittedResponseCount: 0,
					scoreCount: 0,
					exportArtifactCount: 0
				},
				campaigns: []
			}
		});
	});

	await page.route(`**/campaign-series/${createdSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${createdSeriesId}/setup-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: createEmptySetupWorkspace(createdSeriesId, 'New routed pulse')
		});
	});

	await page.goto('/app/campaign-series');
	await page.getByLabel('Study name').fill('New routed pulse');
	await page.getByRole('button', { name: 'Create study' }).click();

	await expect(page).toHaveURL(`/app/campaign-series/${createdSeriesId}/setup`);
	expect(createRequests).toEqual([
		expect.objectContaining({
			name: 'New routed pulse',
			studyBrief: expect.objectContaining({
				designType: 'single_wave',
				intendedUse: 'research_analysis'
			})
		})
	]);
	await expect(page.getByRole('region', { name: 'Protocol workspace' })).toBeVisible();
	await expect(page.getByRole('group', { name: 'Protocol progress' })).toBeVisible();
});

test('renders selected study overview from the product read model', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const hub = page.getByRole('region', { name: 'Selected study overview' });
	const summary = hub.getByRole('region', { name: 'Selected study summary' });
	const command = summary.getByLabel('Collecting responses');
	const metrics = summary.getByRole('group', { name: 'Status and records' });

	await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toBeVisible();
	await expect(summary.getByRole('heading', { name: 'Quarterly pulse', exact: true })).toBeVisible();
	await expect(summary.getByText('Your study', { exact: true })).toBeVisible();
	await expect(summary.getByText('Active', { exact: true })).toBeVisible();
	await expect(command.getByRole('heading', { name: 'Collecting responses' })).toBeVisible();
	await expect(command.getByRole('link', { name: 'Open Collect' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/operations`
	);

	await expect(metrics.locator('div').filter({ hasText: 'Measurements' })).toContainText(
		'7 measurements'
	);
	await expect(metrics.locator('div').filter({ hasText: 'Live' })).toContainText('2');
	await expect(metrics.locator('div').filter({ hasText: 'Responses' })).toContainText(
		'128 responses'
	);
	await expect(metrics.locator('div').filter({ hasText: 'Scores' })).toContainText('120 scores');
	await expect(metrics.locator('div').filter({ hasText: 'Exports' })).toContainText(
		'5 export files'
	);
	await expect(hub.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);

	const campaign = summary.getByRole('article', { name: 'Pulse wave 1' });
	await expect(campaign.getByRole('heading', { name: 'Pulse wave 1', exact: true })).toBeVisible();
	await expect(campaign.getByText('Live', { exact: true })).toBeVisible();
	await expect(campaign.getByText('Identity mode', { exact: true })).toBeVisible();
	await expect(campaign.getByText('anonymous', { exact: true })).toBeVisible();
	await expect(campaign.getByText('Locale', { exact: true })).toBeVisible();
	await expect(campaign.getByText('en', { exact: true })).toBeVisible();
	await expect(campaign.getByText('Submitted responses', { exact: true })).toBeVisible();
	await expect(campaign.getByText('128', { exact: true })).toBeVisible();
	await expect(campaign.getByText('Scores', { exact: true })).toBeVisible();
	await expect(campaign.getByText('120', { exact: true })).toBeVisible();
	await expect(campaign.getByText('Export files', { exact: true })).toBeVisible();
	await expect(campaign.getByText('5', { exact: true })).toBeVisible();
});

test('renders selected study overview as command summary before details', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const overview = page.getByRole('region', { name: 'Selected study overview' });
	const summary = overview.getByRole('region', { name: 'Selected study summary' });
	const command = summary.getByLabel('Collecting responses');
	const metrics = summary.getByRole('group', { name: 'Status and records' });
	const waves = summary.getByText('Waves (1)', { exact: true });
	const dates = summary.getByText('Dates', { exact: true });

	await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toBeVisible();
	await expect(
		summary.getByRole('heading', { name: 'Quarterly pulse', exact: true })
	).toBeVisible();
	await expect(summary.getByText('Your study', { exact: true })).toBeVisible();
	await expect(summary.getByText('Active', { exact: true })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Campaign series', exact: true })).toHaveCount(0);
	await expect(page.getByRole('region', { name: 'Campaign series hub' })).toHaveCount(0);
	await expect(overview.getByText('Tenant read model', { exact: true })).toHaveCount(0);
	await expect(overview.getByText('Campaign series details', { exact: true })).toHaveCount(0);

	await expect(command).toBeVisible();
	await expect(command.getByRole('link', { name: 'Open Collect' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/operations`
	);
	await expect(metrics).toBeVisible();
	await expect(waves).toBeVisible();
	await expect(dates).toBeVisible();
	await expectElementBefore(command, metrics);
	await expectElementBefore(metrics, waves);
	await expectElementBefore(waves, dates);
});

test('selected study overview uses summary panels instead of legacy lifecycle cards', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const overview = page.getByRole('region', { name: 'Selected study overview' });
	const summary = overview.getByRole('region', { name: 'Selected study summary' });
	const command = summary.getByLabel('Collecting responses');
	const metrics = summary.getByRole('group', { name: 'Status and records' });

	await expectElementBefore(command, metrics);
	await expect(overview.locator('.selected-lifecycle-row')).toHaveCount(0);
	await expect(overview.getByRole('group', { name: 'Study lifecycle' })).toHaveCount(0);
	await expect(overview.getByRole('region', { name: 'Study reference' })).toHaveCount(0);
});

test('selected study status records do not expose raw technical ids by default', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const overview = page.getByRole('region', { name: 'Selected study overview' });
	const summary = overview.getByRole('region', { name: 'Selected study summary' });
	const totals = summary.getByRole('group', { name: 'Status and records' });

	await expect(totals).toBeVisible();
	await expect(totals.getByText('Measurements', { exact: true })).toBeVisible();
	await expect(totals.getByText('Responses', { exact: true })).toBeVisible();
	await expect(overview.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);
});

test('labels sample selected-series hubs as read-only starter content', async ({ page }) => {
	await page.route(`**/campaign-series/${sampleSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				...sampleStudyOwnership
			} satisfies CampaignSeriesHubResponse
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const hub = page.getByRole('region', { name: 'Selected study overview' });
	const readOnlyState = hub.getByRole('note', { name: 'Sample study read-only state' });

	await expect(readOnlyState).toBeVisible();
	await expect(readOnlyState.getByText('Sample study', { exact: true })).toBeVisible();
	await expect(
		readOnlyState.getByText(
			'Results sample: read-only starter content showing collected responses, scores, reports, and exports.',
			{ exact: true }
		)
	).toBeVisible();
});

test('duplicates a sample study from the selected overview and routes to setup', async ({
	page
}) => {
	const createdSeriesId = '67dfd072-5897-40b3-8998-5fbf8cb4d0f6';
	const createdSeriesName = 'Copy of Quarterly pulse';
	const duplicateRequests: Array<{ name: string }> = [];

	await page.route(`**/campaign-series/${sampleSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				...sampleStudyOwnership
			} satisfies CampaignSeriesHubResponse
		});
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/duplicate`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/duplicate`)) {
			await route.fallback();
			return;
		}

		duplicateRequests.push(route.request().postDataJSON() as { name: string });
		await route.fulfill({
			status: 201,
			json: {
				id: createdSeriesId,
				name: createdSeriesName,
				studyKind: 'own',
				isSample: false,
				sourceCampaignSeriesId: sampleSeriesId
			} satisfies CampaignSeriesDuplicateResponse
		});
	});

	await page.route(`**/campaign-series/${createdSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${createdSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				...ownSeriesOwnership,
				id: createdSeriesId,
				name: createdSeriesName,
				campaigns: []
			} satisfies CampaignSeriesHubResponse
		});
	});

	await page.route(`**/campaign-series/${createdSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${createdSeriesId}/setup-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: createEmptySetupWorkspace(createdSeriesId, createdSeriesName)
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}`);
	await page
		.getByRole('note', { name: 'Sample study read-only state' })
		.getByRole('button', { name: 'Duplicate as study Quarterly pulse' })
		.click();

	await expect.poll(() => duplicateRequests).toEqual([{ name: createdSeriesName }]);
	await expect(page).toHaveURL(`/app/campaign-series/${createdSeriesId}/setup`);
	await expect(page.getByRole('region', { name: 'Protocol workspace' })).toBeVisible();
	await expect(page.getByRole('group', { name: 'Protocol progress' })).toBeVisible();
});

test('shows archived selected campaign-series state and restores from hub', async ({ page }) => {
	const restoreRequests: unknown[] = [];
	let restored = false;

	await page.route(`**/campaign-series/${sampleSeriesId}**`, async (route) => {
		const url = new URL(route.request().url());
		const isHubApi = isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}`);
		const isRestoreApi = isProductApiPath(
			route.request().url(),
			`/campaign-series/${sampleSeriesId}/restore`
		);

		if (!isHubApi && !isRestoreApi) {
			await route.fallback();
			return;
		}

		if (
			route.request().method() === 'POST' &&
			url.pathname === `/campaign-series/${sampleSeriesId}/restore`
		) {
			restoreRequests.push(route.request().postDataJSON());
			restored = true;
			await route.fulfill({
				json: {
					id: sampleSeriesId,
					archived: false,
					updatedAt: '2026-05-11T13:30:00Z',
					archivedAt: null,
					archivedByUserId: null,
					archiveReason: null
				}
			});
			return;
		}

		if (route.request().method() !== 'GET') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleCampaignSeriesHub,
				archived: !restored,
				archivedAt: restored ? null : '2026-05-11T13:15:00Z',
				archivedByUserId: restored ? null : '22222222-2222-4222-8222-222222222222',
				archiveReason: restored ? null : 'Completed pilot'
			} satisfies CampaignSeriesHubResponse
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}`);
	const hub = page.getByRole('region', { name: 'Selected study overview' });

	await expect(hub.getByText('Archived', { exact: true })).toHaveCount(3);
	await hub.getByText('Dates', { exact: true }).click();
	await expect(hub.getByText('Completed pilot', { exact: true })).toBeVisible();
	await page.getByRole('button', { name: 'Restore' }).click();

	await expect.poll(() => restoreRequests).toEqual([{}]);
	await expect(hub.getByText('Active', { exact: true }).first()).toBeVisible();
	await expect(hub.getByRole('button', { name: 'Restore' })).toHaveCount(0);
});

test('refreshes selected campaign-series hub on client-side series id changes', async ({
	page
}) => {
	let releaseOriginalSeries: () => void = () => {};
	const originalSeriesCanRespond = new Promise<void>((resolve) => {
		releaseOriginalSeries = resolve;
	});

	await page.route(`**/campaign-series/${oldFoundationSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${oldFoundationSeriesId}`)) {
			await route.fallback();
			return;
		}

		await originalSeriesCanRespond;
		await route.fulfill({ json: staleCampaignSeriesHub });
	});

	await page.route(`**/campaign-series/${alternateSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${alternateSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: alternateCampaignSeriesHub });
	});

	await page.goto(`/app/campaign-series/${oldFoundationSeriesId}`);
	await expect(page.getByRole('status')).toContainText('Loading study overview');

	await page.evaluate((href) => {
		(window as Window & { __seriesHubNavigationMarker?: string }).__seriesHubNavigationMarker =
			'client';
		const link = document.createElement('a');
		link.href = href;
		link.textContent = 'Open alternate series';
		link.dataset.testid = 'open-alternate-series';
		document.body.append(link);
	}, `/app/campaign-series/${alternateSeriesId}`);
	await page.getByTestId('open-alternate-series').click();

	await expect(page).toHaveURL(`/app/campaign-series/${alternateSeriesId}`);
	await expect
		.poll(() =>
			page.evaluate(
				() =>
					(window as Window & { __seriesHubNavigationMarker?: string }).__seriesHubNavigationMarker
			)
		)
		.toBe('client');

	const hub = page.getByRole('region', { name: 'Selected study overview' });
	await expect(hub.getByRole('heading', { name: 'Retention pulse', exact: true })).toBeVisible();
	await expect(hub.getByText(alternateSeriesId, { exact: true })).toHaveCount(0);
	await expect(hub.getByRole('link', { name: 'Open Collect', exact: true })).toHaveAttribute(
		'href',
		`/app/campaign-series/${alternateSeriesId}/operations`
	);

	releaseOriginalSeries();
	await expect(
		hub.getByRole('heading', { name: 'Legacy foundation pulse', exact: true })
	).toHaveCount(0);
	await expect(hub.getByRole('heading', { name: 'Retention pulse', exact: true })).toBeVisible();
});

test('renders selected campaign-series not-found state', async ({ page }) => {
	const missingSeriesId = '4c17646f-0b95-47bf-95d0-c0e65dc344e7';
	await page.route(`**/campaign-series/${missingSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${missingSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			status: 404,
			json: {
				title: 'campaign_series.not_found',
				detail: 'Campaign series was not found.'
			}
		});
	});

	await page.goto(`/app/campaign-series/${missingSeriesId}`);

	await expect(page.getByRole('alert')).toContainText('Campaign series was not found');
});

test('retries selected campaign-series hub with the route series id', async ({ page }) => {
	const requestedProductPaths: string[] = [];
	let retrySeriesRequestCount = 0;

	await page.route('**/campaign-series/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.startsWith('/campaign-series/')) {
			await route.fallback();
			return;
		}

		requestedProductPaths.push(pathname);

		if (pathname !== `/campaign-series/${retrySeriesId}`) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_path',
					detail: `Unexpected campaign series path: ${pathname}`
				}
			});
			return;
		}

		retrySeriesRequestCount += 1;
		if (retrySeriesRequestCount === 1) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'campaign_series.retry',
					detail: 'Temporary campaign series hub failure.'
				}
			});
			return;
		}

		await route.fulfill({
			json: {
				...alternateCampaignSeriesHub,
				id: retrySeriesId,
				name: 'Retryable retention pulse'
			}
		});
	});

	await page.goto(`/app/campaign-series/${retrySeriesId}`);
	await expect(page.getByRole('alert')).toContainText('Temporary campaign series hub failure');

	await page.getByRole('button', { name: 'Retry overview' }).click();

	const hub = page.getByRole('region', { name: 'Selected study overview' });
	await expect(hub.getByRole('heading', { name: 'Retryable retention pulse' })).toBeVisible();
	await expect(hub.getByText(retrySeriesId, { exact: true })).toHaveCount(0);
	await expect(hub.getByRole('link', { name: 'Open Collect', exact: true })).toHaveAttribute(
		'href',
		`/app/campaign-series/${retrySeriesId}/operations`
	);
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([`/campaign-series/${retrySeriesId}`, `/campaign-series/${retrySeriesId}`]);
});

test('renders the campaign-series product route map', async ({ page }) => {
	const routes = [
		{ path: '/app/campaign-series', heading: 'Studies', region: 'Studies' },
		{
			path: `/app/campaign-series/${sampleSeriesId}`,
			heading: 'Overview',
			region: 'Selected study overview'
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			heading: 'Study protocol',
			region: 'Protocol workspace'
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			heading: 'Field',
			region: 'Field workspace'
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			heading: 'Evidence',
			region: 'Evidence workspace'
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			heading: 'Compare rounds',
			region: 'Rounds and linked repeat responses'
		}
	];

	for (const route of routes) {
		await page.goto(route.path);
		await expect(page.getByRole('heading', { name: route.heading, exact: true })).toBeVisible();
		const routeRegion = page.getByRole('region', { name: route.region });
		await expect(route.path === '/app/campaign-series' ? routeRegion.first() : routeRegion).toBeVisible();

		const nav = page.getByRole('navigation', { name: 'Product navigation' });
		const expectedCurrentNav =
			route.path === '/app/campaign-series'
				? /^Studies\b/
				: route.path.endsWith(`/${sampleSeriesId}`)
					? /^Overview\b/
					: route.path.endsWith('/setup')
						? /^Protocol/
						: route.path.endsWith('/operations')
							? /^Field/
							: route.path.endsWith('/reports')
								? /^Evidence/
								: null;
		await expect(nav.locator('[aria-current="page"]')).toHaveCount(expectedCurrentNav ? 1 : 0);
		if (expectedCurrentNav) {
			await expect(nav.getByRole('link', { name: expectedCurrentNav })).toHaveAttribute(
				'aria-current',
				'page'
			);
		}

		if (route.path.includes(sampleSeriesId)) {
			const selectedStudy = nav.getByRole('group', { name: 'Active study' });
			await expect(selectedStudy).toBeVisible();
			await expect(selectedStudy.getByRole('link', { name: /^Overview\b/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}`
			);
			await expect(selectedStudy.getByRole('link', { name: /^Protocol/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}/setup`
			);
			await expect(selectedStudy.getByRole('link', { name: /^Field/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}/operations`
			);
			await expect(selectedStudy.getByRole('link', { name: /^Evidence/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}/reports`
			);
			await expect(selectedStudy.getByRole('link', { name: /^Rounds\b/ })).toHaveCount(0);
		}
	}
});

test('renders selected-series context on child product routes', async ({ page }) => {
	let childHubRequestCount = 0;
	let setupWorkspaceRequestCount = 0;
	let operationsWorkspaceRequestCount = 0;
	let reportsWorkspaceRequestCount = 0;
	let wavesWorkspaceRequestCount = 0;
	await page.route(`**/campaign-series/${sampleSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}`)) {
			await route.fallback();
			return;
		}

		childHubRequestCount += 1;
		await route.fulfill({ json: sampleCampaignSeriesHub });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		setupWorkspaceRequestCount += 1;
		await route.fulfill({ json: sampleSetupWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/operations-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/operations-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		operationsWorkspaceRequestCount += 1;
		await route.fulfill({ json: sampleOperationsWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/reports-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		reportsWorkspaceRequestCount += 1;
		await route.fulfill({ json: sampleReportsWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/waves-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/waves-workspace`)
		) {
			await route.fallback();
			return;
		}

		wavesWorkspaceRequestCount += 1;
		await route.fulfill({ json: sampleWavesWorkspace });
	});

	const routes = [
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			region: 'Protocol workspace',
			expected: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			region: 'Field workspace',
			expected: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			region: 'Evidence workspace',
			expected: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			region: 'Rounds and linked repeat responses',
			expected: []
		}
	];

	for (const route of routes) {
		await page.goto(route.path);
		const region = page.getByRole('region', { name: route.region });
		await expect(region).toBeVisible();

		for (const expected of route.expected) {
			await expect(region.getByText(expected, { exact: true }).first()).toBeVisible();
		}
	}

	expect(childHubRequestCount).toBe(0);
	expect(setupWorkspaceRequestCount).toBe(1);
	expect(operationsWorkspaceRequestCount).toBe(1);
	expect(reportsWorkspaceRequestCount).toBe(1);
	expect(wavesWorkspaceRequestCount).toBe(1);
});

test('M3 OSH exit path surfaces prepare collect results and PDF evidence', async ({ page }) => {
	const oshSeriesName = 'Factory ergonomics review';
	const oshCampaignName = 'Factory ergonomics wave 1';
	const signedDownloadArtifactIds: string[] = [];
	let setupWorkspaceRequestCount = 0;
	let operationsWorkspaceRequestCount = 0;
	let reportsWorkspaceRequestCount = 0;
	const oshSetupWorkspace: CampaignSeriesSetupWorkspaceResponse = {
		...sampleSetupWorkspace,
		series: {
			...sampleSetupWorkspace.series,
			name: oshSeriesName
		},
		summary: {
			...sampleSetupWorkspace.summary,
			missingPrerequisiteCount: 0
		},
		selectedCampaign: {
			...sampleSetupWorkspace.selectedCampaign!,
			name: oshCampaignName
		},
		template: {
			...sampleSetupWorkspace.template!,
			templateName: 'Tenant-attested OSH review template',
			questionCount: 5
		},
		readiness: {
			campaignId: sampleSetupWorkspace.selectedCampaign!.id,
			status: 'ready',
			ready: true
		},
		missingPrerequisites: [],
		campaigns: [
			{
				...sampleSetupWorkspace.campaigns[0],
				name: oshCampaignName
			}
		]
	};
	const oshOperationsWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
		...sampleOperationsWorkspace,
		series: {
			...sampleOperationsWorkspace.series,
			name: oshSeriesName
		},
		selectedCampaign: {
			...sampleOperationsWorkspace.selectedCampaign!,
			name: oshCampaignName
		},
		campaigns: [
			{
				...sampleOperationsWorkspace.campaigns[0],
				name: oshCampaignName
			}
		]
	};
	const oshReportPdfArtifact = {
		...sampleReportPdfArtifact,
		targetLabel: oshSeriesName,
		campaignName: oshCampaignName
	};
	const oshReportsWorkspace: CampaignSeriesReportsWorkspaceResponse = {
		...sampleReportsWorkspace,
		series: {
			...sampleReportsWorkspace.series,
			name: oshSeriesName
		},
		summary: {
			...sampleReportsWorkspace.summary,
			exportArtifactCount: 1
		},
		selectedCampaign: {
			...sampleReportsWorkspace.selectedCampaign!,
			name: oshCampaignName,
			exportArtifactCount: 1
		},
		exportArtifacts: [oshReportPdfArtifact],
		campaigns: [
			{
				...sampleReportsWorkspace.campaigns[0],
				name: oshCampaignName,
				exportArtifactCount: 1
			},
			sampleReportsWorkspace.campaigns[1]
		]
	};

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		setupWorkspaceRequestCount += 1;
		await route.fulfill({ json: oshSetupWorkspace });
	});
	await page.route(`**/campaign-series/${sampleSeriesId}/operations-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/operations-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		operationsWorkspaceRequestCount += 1;
		await route.fulfill({ json: oshOperationsWorkspace });
	});
	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/reports-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		reportsWorkspaceRequestCount += 1;
		await route.fulfill({ json: oshReportsWorkspace });
	});
	await page.route('**/campaigns/*/report-proof', async (route) => {
		await route.fulfill({
			json: {
				...sampleCampaignReportProof,
				campaignName: oshCampaignName
			}
		});
	});
	await page.route('**/export-artifacts/*/signed-download-url', async (route) => {
		signedDownloadArtifactIds.push(new URL(route.request().url()).pathname.split('/').at(-2) ?? '');
		await route.fulfill({ json: sampleReportPdfSignedDownloadUrl });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);
	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const preparation = setup.getByRole('group', { name: 'Protocol progress' });
	await expect(preparation).toBeVisible();
	await expect(preparation).toContainText('Build questionnaire');
	await expect(preparation).toContainText('Review results setup');
	await expect(preparation).toContainText('Ready to collect');

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);
	const operations = page.getByRole('region', { name: 'Field workspace' });
	const collection = operations.getByRole('group', { name: 'Study collection flow' });
	await expect(collection).toBeVisible();
	await expect(collection.getByRole('heading', { name: 'Collection flow' })).toBeVisible();
	await expect(collection.getByText('3/4 steps complete', { exact: true })).toBeVisible();
	await expect(collection.getByRole('region', { name: 'Collection step' })).toContainText(
		'Close collection'
	);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);
	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	await expect(workflow).toBeVisible();
	const useReview = workflow.getByRole('region', { name: 'Results use review' });
	await useReview.getByRole('button', { name: 'Review results' }).click();
	const reportPreview = workflow.getByRole('region', { name: 'Report preview' });
	await expect(reportPreview.getByText('Higher tenant band', { exact: true })).toBeVisible();
	await expect(
		reportPreview.getByText('tenant attested / tenant defined / not reviewed / not official', {
			exact: true
		})
	).toBeVisible();

	const reportPdfResult = workflow.getByRole('region', { name: 'Report PDF result' });
	await expect(reportPdfResult).toBeVisible();
	await expect(reportPdfResult.getByText('campaign-series-report.pdf', { exact: true })).toBeVisible();
	await expect(reportPdfResult.getByText('Downloadable', { exact: true }).first()).toBeVisible();
	await reportPdfResult.getByRole('button', { name: 'Get secure PDF link' }).click();
	await expect(reportPdfResult.getByText('Secure PDF link ready', { exact: true })).toBeVisible();
	await expect(reportPdfResult.getByRole('link', { name: 'Open secure PDF link' })).toHaveAttribute(
		'href',
		sampleReportPdfSignedDownloadUrl.url
	);

	expect(setupWorkspaceRequestCount).toBe(1);
	expect(operationsWorkspaceRequestCount).toBe(1);
	expect(reportsWorkspaceRequestCount).toBe(1);
	expect(signedDownloadArtifactIds).toEqual([sampleReportPdfArtifact.id]);
});

test('labels sample selected-series child routes as read-only starter content', async ({
	page
}) => {
	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleSetupWorkspace,
				series: {
					...sampleSetupWorkspace.series,
					...sampleStudyOwnership
				}
			} satisfies CampaignSeriesSetupWorkspaceResponse
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setupWorkspace = page.getByRole('region', { name: 'Protocol workspace' });
	const readOnlyState = setupWorkspace.getByRole('region', {
		name: 'Sample study read-only state'
	});
	const studyContext = setupWorkspace.getByRole('region', { name: 'Study context' });
	const preparation = setupWorkspace.getByRole('group', { name: 'Protocol progress' });

	await expect(readOnlyState).toBeVisible();
	await expect(readOnlyState.getByText('Sample study', { exact: true })).toBeVisible();
	await expect(
		readOnlyState.getByText(
			'Results sample: read-only starter content showing collected responses, scores, reports, and exports.',
			{ exact: true }
		)
	).toBeVisible();
	await expect(studyContext).toBeVisible();
	await expect(preparation).toBeVisible();
	await expect(preparation.getByRole('heading', { name: 'Protocol progress' })).toBeVisible();
	await expectElementBefore(readOnlyState, studyContext);
	await expectElementBefore(studyContext, preparation);
});

test.skip('selected-series destinations put primary work before reference context', async ({ page }) => {
	const destinations = [
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			shell: 'Protocol workspace',
			primary: { role: 'group' as const, name: 'Preparation actions' },
			reference: { role: 'region' as const, name: 'Setup reference' }
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			shell: 'Field workspace',
			primary: { role: 'group' as const, name: 'Collection progress' },
			reference: { role: 'region' as const, name: 'Collection reference' }
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			shell: 'Evidence workspace',
			primary: { role: 'group' as const, name: 'Results overview' },
			reference: { role: 'region' as const, name: 'Results reference' }
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			shell: 'Rounds and linked repeat responses',
			primary: { role: 'group' as const, name: 'Measurement comparison workflow' },
			reference: { role: 'region' as const, name: 'Waves selected-series context' }
		}
	];

	for (const destination of destinations) {
		await page.goto(destination.path);
		const shell = page.getByRole('region', { name: destination.shell });
		const guidance = shell.getByRole('region', { name: 'Route guidance' });
		const primary = shell.getByRole(destination.primary.role, { name: destination.primary.name });
		const reference = shell.getByRole(destination.reference.role, {
			name: destination.reference.name
		});

		await expect(guidance).toBeVisible();
		await expect(primary).toBeVisible();
		await expect(reference).toBeVisible();
		await expectElementBefore(guidance, primary);
		await expectElementBefore(primary, reference);
	}
});

test('product wayfinding keeps selected-series workspace context and current route visible', async ({
	page
}) => {
	const workspaceLinks = [
		{ label: /^Overview\b/, href: `/app/campaign-series/${sampleSeriesId}` },
		{ label: /^Protocol/, href: `/app/campaign-series/${sampleSeriesId}/setup` },
		{ label: /^Field/, href: `/app/campaign-series/${sampleSeriesId}/operations` },
		{ label: /^Evidence/, href: `/app/campaign-series/${sampleSeriesId}/reports` }
	];
	const destinations = [
		{
			path: `/app/campaign-series/${sampleSeriesId}`,
			shell: 'Selected study overview',
			current: /^Overview\b/,
			primary: { role: 'group' as const, name: 'Study lifecycle' },
			reference: { role: 'region' as const, name: 'Study reference' },
			safetyLabels: ['Pending']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			shell: 'Protocol workspace',
			current: 'Protocol',
			primary: { role: 'group' as const, name: 'Preparation actions' },
			reference: { role: 'region' as const, name: 'Setup reference' },
			safetyLabels: ['Consent', 'Retention']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			shell: 'Field workspace',
			current: 'Field',
			primary: { role: 'group' as const, name: 'Collection progress' },
			reference: { role: 'region' as const, name: 'Collection reference' },
			safetyLabels: ['Preview ready']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			shell: 'Evidence workspace',
			current: 'Evidence',
			primary: { role: 'group' as const, name: 'Results overview' },
			reference: { role: 'region' as const, name: 'Results reference' },
			safetyLabels: ['Preview ready', 'Finality and provenance', 'Suppressed scores']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			shell: 'Rounds and linked repeat responses',
			current: null,
			primary: { role: 'group' as const, name: 'Repeated-round comparison snapshot' },
			reference: { role: 'region' as const, name: 'Waves selected-series context' },
			safetyLabels: ['Preview ready']
		}
	];

	for (const destination of destinations) {
		await page.goto(destination.path);
		const shell = page.getByRole('region', { name: destination.shell });
		const nav = page.getByRole('navigation', { name: 'Product navigation' });
		const workspace = nav.getByRole('group', { name: 'Active study' });

		await expect(shell).toBeVisible();
		await expect(workspace).toBeVisible();

		for (const link of workspaceLinks) {
			await expect(workspace.getByRole('link', { name: link.label })).toHaveAttribute('href', link.href);
		}

		await expect(workspace.locator('[aria-current="page"]')).toHaveCount(destination.current ? 1 : 0);
		if (destination.current) {
			await expect(
				workspace.getByRole('link', { name: destination.current })
			).toHaveAttribute('aria-current', 'page');
		}
	}
});

test('product vocabulary avoids proof-harness phrases in normal app workflows', async ({
	page
}) => {
	const routes = [
		{ path: '/app', surface: 'Workspace home', allowedLabels: [] },
		{ path: '/app/campaign-series', surface: 'Studies', allowedLabels: [] },
		{
			path: `/app/campaign-series/${sampleSeriesId}`,
			surface: 'Selected study overview',
			allowedLabels: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			surface: 'Protocol workspace',
			allowedLabels: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			surface: 'Field workspace',
			allowedLabels: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			surface: 'Evidence workspace',
			allowedLabels: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			surface: 'Rounds and linked repeat responses',
			allowedLabels: []
		}
	];
	const bannedWorkflowPhrases = [
		/\bProof workflow\b/,
		/\bproof workflow\b/,
		/\bProof action workbench\b/,
		/\bproof actions\b/i,
		/\bproof entry\b/i,
		/\bpublic proof entry\b/i,
		/\bLocal delivery proof\b/,
		/\bReport proof actions\b/,
		/\bTwo-wave proof actions\b/,
		/\bView report proof\b/i,
		/\breviewing report proof\b/i,
		/\bRefresh two-wave proof\b/i,
		/\bTwo-wave proof\b/,
		/\bView wave comparison proof\b/i,
		/\bRepeated-round comparison proof\b/
	];

	for (const route of routes) {
		await page.goto(route.path);
		await expect(page.getByRole('region', { name: route.surface, exact: true }).first()).toBeVisible();
		const workspace = page.getByRole('main', { name: 'Product workspace' });
		await expect(workspace).toBeVisible();
		const visibleText = await workspace.innerText();

		for (const bannedPhrase of bannedWorkflowPhrases) {
			expect(visibleText, `${route.path} should not expose ${bannedPhrase}`).not.toMatch(
				bannedPhrase
			);
		}

		for (const allowedLabel of route.allowedLabels) {
			expect(visibleText).toContain(allowedLabel);
		}
	}
});

test('product UI foundation exposes stable landmarks and route hierarchy', async ({ page }) => {
	const highVisibilityRoutes = [
		'/app',
		'/app/campaign-series',
		`/app/campaign-series/${sampleSeriesId}`,
		`/app/campaign-series/${sampleSeriesId}/reports`
	];

	for (const path of highVisibilityRoutes) {
		await page.goto(path);
		await expect(page.getByRole('main', { name: 'Product workspace' })).toBeVisible();
		await expect(page.getByRole('navigation', { name: 'Product navigation' })).toBeVisible();
		await expect(page.locator('.product-panel .product-panel')).toHaveCount(0);
	}

	await page.goto('/app');
	const overview = page.getByRole('region', { name: 'Workspace home' });
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	await overview.getByText('Workspace overview', { exact: true }).click();
	const totals = overview.getByRole('group', { name: 'Workspace totals' });
	await expectElementBefore(sampleStudies, ownStudies);
	await expectElementBefore(ownStudies, totals);

	await page.goto('/app/campaign-series');
	const portfolio = page.getByRole('region', { name: 'Studies' }).first();
	await expect(portfolio.getByRole('group', { name: 'Study list' })).toBeVisible();
	await expect(portfolio.getByRole('article', { name: /Quarterly pulse/i })).toBeVisible();

	await page.goto(`/app/campaign-series/${sampleSeriesId}`);
	const hub = page.getByRole('region', { name: 'Selected study overview' });
	const summary = hub.getByRole('region', { name: 'Selected study summary' });
	await expect(summary).toBeVisible();
	await expect(summary.getByRole('group', { name: 'Status and records' })).toBeVisible();
	await expect(hub.getByRole('group', { name: 'Study lifecycle' })).toHaveCount(0);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);
	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const resultsContext = reports.getByRole('region', { name: 'Study context' });
	const resultsActions = reports.getByRole('group', { name: 'Review and export actions' });
	const resultsDetails = reports.getByRole('group', { name: 'Results details' });
	await expect(resultsContext).toBeVisible();
	await expect(resultsActions).toBeVisible();
	await expect(resultsActions.getByRole('region', { name: 'Results use review' })).toBeVisible();
	await expect(resultsActions.getByRole('region', { name: 'Export preview' })).toBeVisible();
	await expect(resultsDetails).toBeVisible();
	await expectElementBefore(resultsContext, resultsActions);
	await expectElementBefore(resultsActions, resultsDetails);
});

test('waves workflow renders primary actions instead of the proof workbench', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Rounds and linked repeat responses' });
	const workflow = waves.getByRole('group', { name: 'Measurement comparison workflow' });

	await expect(workflow).toBeVisible();
	await expect(
		workflow.getByRole('heading', { name: 'Compare repeated measurements' })
	).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Measurement timeline' })).toBeVisible();
	await expect(
		workflow.getByRole('region', { name: 'Measurement comparison preview' })
	).toBeVisible();
	await expect(waves.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);
});

test('waves longitudinal overview leads before mechanics and current task leads workflow', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Rounds and linked repeat responses' });
	const workflow = waves.getByRole('group', { name: 'Measurement comparison workflow' });
	const timeline = workflow.getByRole('region', { name: 'Measurement timeline' });
	const preview = workflow.getByRole('region', { name: 'Measurement comparison preview' });
	const details = waves.getByRole('group', { name: 'Waves details' });

	await expect(workflow).toBeVisible();
	await expect(
		workflow.getByRole('heading', { name: 'Compare repeated measurements' })
	).toBeVisible();
	await expect(timeline).toBeVisible();
	await expect(preview).toBeVisible();
	await expect(details).toBeVisible();
	await expectElementBefore(workflow, details);
	await expectElementBefore(timeline, preview);
});

test('operations workspace loads the dedicated read model and renders operations state', async ({
	page
}) => {
	const requestedProductPaths: string[] = [];

	await page.route('**/campaign-series/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.startsWith('/campaign-series/')) {
			await route.fallback();
			return;
		}

		requestedProductPaths.push(pathname);

		if (pathname !== `/campaign-series/${sampleSeriesId}/operations-workspace`) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_path',
					detail: `Unexpected operations path: ${pathname}`
				}
			});
			return;
		}

		await route.fulfill({ json: sampleOperationsWorkspace });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);

	const operations = page.getByRole('region', { name: 'Field workspace' });
	const workflow = operations.getByRole('group', { name: 'Study collection flow' });
	const details = operations.locator('details[aria-label="Collection details"]');

	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('heading', { name: 'Collection flow' })).toBeVisible();
	await expect(workflow.getByRole('list', { name: 'Collection path' })).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Collection step' })).toContainText(
		'Close collection'
	);
	await expect(operations.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);

	await expect(details).toBeVisible();
	await details.locator('summary').click();
	await expect(details.getByRole('heading', { name: 'Operational details' })).toBeVisible();
	await expect(details.getByText('Measurements', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Live measurements', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Respondent links', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Queued emails', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Sent emails', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Failed emails', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Started responses', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Submitted responses', { exact: true }).first()).toBeVisible();

	const collectionMonitor = details.getByRole('group', { name: 'Collection monitor' });
	await expect(collectionMonitor).toBeVisible();
	await expect(collectionMonitor.getByText('Started responses', { exact: true })).toBeVisible();
	await expect(collectionMonitor.getByText('Draft responses', { exact: true })).toBeVisible();
	await expect(
		collectionMonitor.getByText('Enough submitted responses exist for aggregate report visibility.')
	).toBeVisible();
	await expect(collectionMonitor.getByText('sensitive.recipient@example.test')).toHaveCount(0);
	await expect(collectionMonitor.getByText('sensitive-answer')).toHaveCount(0);
	await expect(collectionMonitor.getByText('opn_')).toHaveCount(0);
	await expect(collectionMonitor.getByText('inv_')).toHaveCount(0);

	const scoreCoverage = details.getByRole('group', { name: 'Score coverage' });
	await expect(scoreCoverage).toBeVisible();
	await expect(
		scoreCoverage.getByText('All submitted responses have successful scoring activity.')
	).toBeVisible();
	await expect(scoreCoverage.getByText('sensitive.recipient@example.test')).toHaveCount(0);
	await expect(scoreCoverage.getByText('sensitive-answer')).toHaveCount(0);
	await expect(scoreCoverage.getByText('opn_')).toHaveCount(0);
	await expect(scoreCoverage.getByText('inv_')).toHaveCount(0);

	await expect
		.poll(() => requestedProductPaths)
		.toEqual([`/campaign-series/${sampleSeriesId}/operations-workspace`]);
});

test('operations route leads with collection progress before reference detail', async ({
	page
}) => {
	await page.route(`**/campaign-series/${sampleSeriesId}/operations-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/operations-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleOperationsWorkspace });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);

	await expect(page.getByRole('heading', { name: 'Field', exact: true })).toBeVisible();

	const collection = page.getByRole('region', { name: 'Field workspace' });
	const progress = collection.getByRole('group', { name: 'Study collection flow' });
	const reference = collection.locator('details[aria-label="Collection details"]');

	await expect(progress).toBeVisible();
	await expect(reference).toBeVisible();
	await expectElementBefore(progress, reference);

	await expect(progress.getByText('3/4 steps complete', { exact: true })).toBeVisible();
	await expect(progress.getByRole('list', { name: 'Collection path' })).toBeVisible();
	await expect(progress.getByRole('region', { name: 'Collection step' })).toContainText(
		'Close collection'
	);
	await expect(progress.getByText('launch-snapshot-id', { exact: true })).toHaveCount(0);
	await expect(progress.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);

	await reference.locator('summary').click();
	await expect(reference.getByRole('heading', { name: 'Operational details' })).toBeVisible();
	await expect(reference.getByRole('group', { name: 'Collection monitor' })).toBeVisible();
	await expect(reference.getByRole('group', { name: 'Score coverage' })).toBeVisible();
	await expect(reference.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);
	await expect(reference.getByText('launch-snapshot-id', { exact: true })).toHaveCount(0);
});

test.skip('operations launch snapshot review shows frozen campaign configuration', async ({ page }) => {
	await page.route(`**/campaign-series/${sampleSeriesId}/operations-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/operations-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleOperationsWorkspace });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);

	const launchReview = page.getByRole('group', { name: 'Launch snapshot review' });
	await expect(launchReview).toBeVisible();
	await expect(
		launchReview.getByRole('heading', { name: 'Frozen launch configuration' })
	).toBeVisible();
	await expect(launchReview.getByText('anonymous', { exact: true })).toBeVisible();
	await expect(launchReview.getByText('en', { exact: true })).toBeVisible();
	await expect(launchReview.getByText('8', { exact: true })).toBeVisible();
	await expect(launchReview.getByText('05. 05. 2026. 12:15', { exact: true })).toBeVisible();
	await expect(launchReview.getByText('launch-snapshot-id')).toHaveCount(0);
	await expect(launchReview.getByText('template-version-id')).toHaveCount(0);
	await expect(launchReview.getByText('owner-user-id')).toHaveCount(0);
	await expect(launchReview.getByText('sensitive-answer')).toHaveCount(0);
	await expect(launchReview.getByText('opn_')).toHaveCount(0);
	await expect(launchReview.getByText('inv_')).toHaveCount(0);
});

test('operations workflow exposes one current operations task for a draft campaign', async ({
	page
}) => {
	const readinessCampaignIds: string[] = [];
	const draftOperationsWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
		...sampleOperationsWorkspace,
		summary: {
			...sampleOperationsWorkspace.summary,
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
			collectionGuidance: 'Share the public link or send invitations.'
		},
		selectedCampaign: {
			...sampleOperationsWorkspace.selectedCampaign!,
			name: 'Draft operations wave',
			status: 'draft',
			latestLaunchSnapshotId: null,
			latestLaunchAt: null,
			launchSnapshot: null,
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
			collectionStatus: 'closed_or_inactive',
			reportVisibilityStatus: 'unknown_policy',
			collectionGuidance:
				'Report visibility readiness is unknown because disclosure policy is missing.',
			latestDeliveryAttemptAt: null
		},
		campaigns: [sampleOperationsWorkspace.campaigns[1]]
	};

	await page.route(`**/campaign-series/${sampleSeriesId}/operations-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/operations-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: draftOperationsWorkspace });
	});
	await page.route('**/campaigns/*/launch-readiness', async (route) => {
		readinessCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				ready: true,
				issues: []
			}
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);

	const operations = page.getByRole('region', { name: 'Field workspace' });
	const workflow = operations.getByRole('group', { name: 'Study collection flow' });
	const currentStep = workflow.getByRole('region', { name: 'Collection step' });
	await expect(workflow.getByRole('heading', { name: 'Collection flow' })).toBeVisible();
	await expect.poll(() => readinessCampaignIds).toEqual([draftOperationsWorkspace.selectedCampaign!.id]);
	await expect(currentStep).toContainText('Start collection');
	await expect(workflow.getByRole('button', { name: 'Start collection', exact: true })).toBeEnabled();
	await expect(workflow.getByRole('button', { name: 'Create respondent link' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Create ad hoc invitations' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Send next email batch' })).toHaveCount(0);
});

test('retries operations workspace with the route series id', async ({ page }) => {
	const requestedProductPaths: string[] = [];
	let requestCount = 0;

	await page.route('**/campaign-series/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.startsWith('/campaign-series/')) {
			await route.fallback();
			return;
		}

		requestedProductPaths.push(pathname);

		if (pathname !== `/campaign-series/${retrySeriesId}/operations-workspace`) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_path',
					detail: `Unexpected operations path: ${pathname}`
				}
			});
			return;
		}

		requestCount += 1;
		if (requestCount === 1) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'operations.retry',
					detail: 'Temporary operations workspace failure.'
				}
			});
			return;
		}

		await route.fulfill({
			json: {
				...sampleOperationsWorkspace,
				series: {
					...sampleOperationsWorkspace.series,
					id: retrySeriesId,
					name: 'Retryable operations pulse'
				}
			}
		});
	});

	await page.goto(`/app/campaign-series/${retrySeriesId}/operations`);
	await expect(page.getByRole('alert')).toContainText('Temporary operations workspace failure');

	await page.getByRole('button', { name: 'Retry surface' }).click();

	const operations = page.getByRole('region', { name: 'Field workspace' });
	const workflow = operations.getByRole('group', { name: 'Study collection flow' });
	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('heading', { name: 'Collection flow' })).toBeVisible();
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([
			`/campaign-series/${retrySeriesId}/operations-workspace`,
			`/campaign-series/${retrySeriesId}/operations-workspace`
		]);
});

test('operations workflow runs primary actions against the selected campaign', async ({ page }) => {
	const selectedCampaignId = '6d3271db-494f-401d-af8b-a5c86c9293a8';
	const readinessCampaignIds: string[] = [];
	const launchCampaignIds: string[] = [];
	const openLinkCampaignIds: string[] = [];
	const invitationCampaignIds: string[] = [];
	const deliveryCampaignIds: string[] = [];
	let operationsWorkspaceRequestCount = 0;
	const draftOperationsWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
		...sampleOperationsWorkspace,
		summary: {
			...sampleOperationsWorkspace.summary,
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
			collectionGuidance: 'Share the public link or send invitations.'
		},
		selectedCampaign: {
			...sampleOperationsWorkspace.selectedCampaign!,
			id: selectedCampaignId,
			name: 'Draft operations wave',
			status: 'draft',
			latestLaunchSnapshotId: null,
			latestLaunchAt: null,
			launchSnapshot: null,
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
			collectionStatus: 'closed_or_inactive',
			reportVisibilityStatus: 'unknown_policy',
			collectionGuidance:
				'Report visibility readiness is unknown because disclosure policy is missing.',
			latestDeliveryAttemptAt: null
		},
		campaigns: [
			{
				...sampleOperationsWorkspace.campaigns[1],
				id: selectedCampaignId,
				name: 'Draft operations wave'
			}
		]
	};

	await page.route(`**/campaign-series/${sampleSeriesId}/operations-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/operations-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		operationsWorkspaceRequestCount += 1;
		await route.fulfill({
			json:
				operationsWorkspaceRequestCount === 1
					? draftOperationsWorkspace
					: {
							...draftOperationsWorkspace,
							summary: {
								...draftOperationsWorkspace.summary,
								campaignCount: draftOperationsWorkspace.summary.campaignCount + 1
							}
						}
		});
	});
	await page.route('**/campaigns/*/launch-readiness', async (route) => {
		readinessCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({
			json: { campaignId: campaignIdFromPath(route.request().url()), ready: true, issues: [] }
		});
	});
	await page.route('**/campaigns/*/launch', async (route) => {
		launchCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				status: 'live',
				launchSnapshotId: '7a7d76b5-7da2-4c1d-af6a-5d16d52bd672',
				templateVersionId: 'template-version-id',
				scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
				retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
				disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
				responseIdentityMode: 'anonymous',
				defaultLocale: 'en',
				launchedAt: '2026-05-08T12:00:00Z'
			}
		});
	});
	await page.route('**/campaigns/*/open-link', async (route) => {
		openLinkCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({
			status: 201,
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				assignmentId: '98d34e66-5c5b-424a-939d-3075340f880a',
				token: 'opn_test',
				respondentPath: '/r/opn_test'
			}
		});
	});
	await page.route('**/campaigns/*/invitation-batches', async (route) => {
		invitationCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({
			status: 201,
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				requestedRecipientCount: 2,
				createdInvitationCount: 2,
				invitations: [
					{
						assignmentId: '9b319c50-65ba-49d0-9b55-806a1fdd4f5a',
						invitationTokenId: '29d48a13-067c-4e72-8b56-6eef67569558',
						notificationId: '917c7dc2-1506-4cc1-9d64-540c20183348',
						recipient: 'ada.ops@example.com',
						token: 'inv_ops_1',
						respondentPath: '/r/inv_ops_1',
						status: 'queued'
					},
					{
						assignmentId: 'db2ebe3d-c461-479d-b33b-59177d17b36b',
						invitationTokenId: '1087618e-30fc-4595-8a1e-d0ea4c9c9896',
						notificationId: '80d8db79-fe1e-4d7a-b2a9-b199173b1b2f',
						recipient: 'bo.ops@example.com',
						token: 'inv_ops_2',
						respondentPath: '/r/inv_ops_2',
						status: 'queued'
					}
				]
			}
		});
	});
	await page.route('**/campaigns/*/notification-deliveries/process', async (route) => {
		deliveryCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				requestedBatchSize: 25,
				processedCount: 2,
				sentCount: 2,
				failedCount: 0,
				deliveries: [
					{
						notificationId: '917c7dc2-1506-4cc1-9d64-540c20183348',
						recipient: 'ada.ops@example.com',
						status: 'sent',
						provider: 'local',
						respondentPath: '/r/inv_ops_1',
						providerMessageId: null,
						error: null
					}
				]
			}
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);

	const operations = page.getByRole('region', { name: 'Field workspace' });
	const workflow = operations.getByRole('group', { name: 'Study collection flow' });
	const currentTask = workflow.getByRole('region', { name: 'Collection step' });
	await expect(workflow).toBeVisible();
	await expect.poll(() => readinessCampaignIds).toEqual([selectedCampaignId]);
	await expect(currentTask).toContainText('Start collection');
	await expect(workflow.getByRole('button', { name: 'Start collection', exact: true })).toBeEnabled();
	await workflow.getByRole('button', { name: 'Start collection', exact: true }).click();
	await expect(currentTask).toContainText('Share access');
	const openLinkButton = workflow.getByRole('button', { name: 'Create respondent link' });
	await expect(openLinkButton).toBeEnabled();
	await openLinkButton.click();
	await expect.poll(() => openLinkCampaignIds).toEqual([selectedCampaignId]);
	await expect(currentTask).toContainText('Monitor responses');

	expect(readinessCampaignIds).toEqual([selectedCampaignId]);
	expect(launchCampaignIds).toEqual([selectedCampaignId]);
	expect(openLinkCampaignIds).toEqual([selectedCampaignId]);
	await expect.poll(() => operationsWorkspaceRequestCount).toBeGreaterThanOrEqual(3);
	await expect(operations.getByRole('group', { name: 'Study collection flow' })).toBeVisible();
});

test('reports workspace loads the dedicated read model and renders report state', async ({
	page
}) => {
	const requestedProductPaths: string[] = [];

	await page.route('**/campaign-series/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.startsWith('/campaign-series/')) {
			await route.fallback();
			return;
		}

		requestedProductPaths.push(pathname);

		if (pathname === `/campaign-series/${sampleSeriesId}/reports-widget-manifest`) {
			await route.fulfill({ json: sampleReportsWidgetManifest });
			return;
		}

		if (pathname !== `/campaign-series/${sampleSeriesId}/reports-workspace`) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_path',
					detail: `Unexpected reports path: ${pathname}`
				}
			});
			return;
		}

		await route.fulfill({ json: sampleReportsWorkspace });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	const details = reports.locator('details[aria-label="Results details"]');

	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('heading', { name: 'Review and export results' })).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Results use review' })).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Export preview' })).toBeVisible();
	await expect(workflow.getByRole('article', { name: 'Visual analytics' })).toBeVisible();
	await expect(reports.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);

	await expect(details).toBeVisible();
	await details.locator('summary').click();
	await expect(details.getByRole('heading', { name: 'Audit and troubleshooting' })).toBeVisible();
	await expect(details.getByText('Reportable campaigns', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Visible scores', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Suppressed scores', { exact: true }).first()).toBeVisible();
	await expect(details.getByRole('group', { name: 'Results readiness' })).toBeVisible();
	await expect(details.getByRole('group', { name: 'Results selected campaign' })).toBeVisible();
	await expect(details.getByText('report-proof.csv', { exact: true }).first()).toBeVisible();
	await expect(details.getByRole('article', { name: 'Pulse wave 1' })).toBeVisible();
	await expect(details.getByRole('article', { name: 'Draft wave' })).toBeVisible();
	await expect(details.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);
	await expect(details.getByText('launch-snapshot-id', { exact: true })).toHaveCount(0);

	const scoreCoverage = details.getByRole('group', { name: 'Score coverage' });
	await expect(scoreCoverage).toBeVisible();
	await expect(
		scoreCoverage.getByText(
			'Some submitted responses still need scoring activity before score-dependent reports are complete.'
		)
	).toBeVisible();
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([
			`/campaign-series/${sampleSeriesId}/reports-workspace`,
			`/campaign-series/${sampleSeriesId}/reports-widget-manifest`
		]);
});

test('reports route leads with result availability, limits, and export next use', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	await expect(page.getByRole('heading', { name: 'Evidence', exact: true })).toBeVisible();

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	const useReview = workflow.getByRole('region', { name: 'Results use review' });
	const exportPreview = workflow.getByRole('region', { name: 'Export preview' });
	const reference = reports.locator('details[aria-label="Results details"]');

	await expect(workflow).toBeVisible();
	await expect(
		workflow.getByRole('heading', { name: 'Review and export results' })
	).toBeVisible();
	await expect(workflow.getByRole('article', { name: 'Visual analytics' })).toBeVisible();
	await expect(useReview).toBeVisible();
	await expect(useReview).toContainText('Can these results be used?');
	await expect(useReview.getByRole('button', { name: 'Review results' })).toBeVisible();
	await expect(
		exportPreview.getByText('Results matrix CSV, not row-level response data', { exact: true })
	).toBeVisible();
	await expect(reference).toBeVisible();
	await expectElementBefore(workflow, reference);

	await reference.locator('summary').click();
	await expect(reference.getByRole('heading', { name: 'Audit and troubleshooting' })).toBeVisible();
	await expect(reference.getByText('Export file', { exact: true })).toBeVisible();
	await expect(reference.getByRole('article', { name: 'Pulse wave 1' })).toBeVisible();
	await expect(reference.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);
	await expect(reference.getByText('launch-snapshot-id', { exact: true })).toHaveCount(0);
});

test.skip('Results summary render known and unsupported manifest widgets before workflow', async ({
	page
}) => {
	await page.route(
		`**/campaign-series/${sampleSeriesId}/reports-widget-manifest`,
		async (route) => {
			if (
				!isProductApiPath(
					route.request().url(),
					`/campaign-series/${sampleSeriesId}/reports-widget-manifest`
				)
			) {
				await route.fallback();
				return;
			}

			await route.fulfill({
				json: {
					...sampleReportsWidgetManifest,
					widgets: [
						...sampleReportsWidgetManifest.widgets,
						{
							id: 'unsupported-local-proof',
							kind: 'custom-report-widget/v1',
							title: 'Custom unsupported widget',
							size: 'half',
							state: 'ready',
							message: null,
							data: { label: 'not rendered as raw JSON' },
							dataSource: null,
							actions: []
						}
					]
				} satisfies CampaignSeriesReportsWidgetManifestResponse
			});
		}
	);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const widgets = reports.getByRole('region', { name: 'Results summary' });
	await expect(widgets).toBeVisible();
	await expect(widgets.getByRole('article', { name: 'Score coverage' })).toBeVisible();
	await expect(widgets.getByText('Scored', { exact: true })).toBeVisible();
	const unsupported = widgets.getByRole('article', { name: 'Custom unsupported widget' });
	await expect(unsupported).toBeVisible();
	await expect(unsupported.getByText('Unsupported', { exact: true })).toBeVisible();
	await expect(unsupported.getByText('custom-report-widget/v1', { exact: true })).toBeVisible();
	await expect(reports.getByRole('group', { name: 'Review and export actions' })).toBeVisible();
});

test('Results summary render the current backend-known manifest without unsupported fallbacks', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('article', { name: 'Visual analytics' })).toBeVisible();

	const details = workflow.locator('details').filter({
		hasText: 'Details: method, readiness, coverage, and exports'
	});
	await details.locator('summary').click();
	for (const title of [
		'Report readiness',
		'Score coverage',
		'Selected campaign report state',
		'Export files',
		'Finality and provenance'
	]) {
		await expect(details.getByRole('article', { name: title })).toBeVisible();
	}

	await expect(workflow.getByText('Unsupported', { exact: true })).toHaveCount(0);
});

test('Results summary manifest delay does not block reports workspace', async ({ page }) => {
	let releaseManifest = () => {};
	const manifestGate = new Promise<void>((resolve) => {
		releaseManifest = resolve;
	});

	await page.route(
		`**/campaign-series/${sampleSeriesId}/reports-widget-manifest`,
		async (route) => {
			if (
				!isProductApiPath(
					route.request().url(),
					`/campaign-series/${sampleSeriesId}/reports-widget-manifest`
				)
			) {
				await route.fallback();
				return;
			}

			await manifestGate;
			await route.fulfill({ json: sampleReportsWidgetManifest });
		}
	);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Results use review' })).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Export preview' })).toBeVisible();

	releaseManifest();
	await expect(workflow.getByRole('article', { name: 'Visual analytics' })).toBeVisible();
});

test.skip('report snapshot renders selected campaign aggregate proof as route content', async ({
	page
}) => {
	const reportProofCampaignIds: string[] = [];

	await page.route('**/campaigns/*/report-proof', async (route) => {
		reportProofCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({ json: sampleCampaignReportProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const snapshot = reports.getByRole('group', { name: 'Report snapshot' });

	await expect(snapshot).toBeVisible();
	await expect(
		snapshot.getByRole('heading', { name: 'Selected-series report snapshot' })
	).toBeVisible();
	await expect(snapshot.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(
		snapshot.getByText('not_validated_interpretation', { exact: true }).first()
	).toBeVisible();
	const aggregateSnapshot = snapshot.getByRole('region', { name: 'Aggregate report snapshot' });
	await expect(aggregateSnapshot).toBeVisible();
	const reportSummary = aggregateSnapshot.getByRole('region', {
		name: 'Preliminary report summary'
	});
	await expect(reportSummary).toBeVisible();
	await expect(
		reportSummary.getByText('1 visible score row across up to 128 submitted responses.', {
			exact: true
		})
	).toBeVisible();
	await expect(
		reportSummary.getByText('1 score row is suppressed by disclosure guardrails.', {
			exact: false
		})
	).toBeVisible();
	await expect(
		reportSummary.getByText('not validated interpretation', { exact: false })
	).toBeVisible();
	await expect(reportSummary.getByText('Aggregate only', { exact: true })).toBeVisible();
	await expect(
		reportSummary.getByText('Disclosure guardrails still apply', { exact: true })
	).toBeVisible();
	await expect(aggregateSnapshot.getByText('mean 3.75', { exact: true })).toBeVisible();
	await expect(aggregateSnapshot.getByText('scores 120', { exact: true })).toBeVisible();
	await expect(aggregateSnapshot.getByText('Suppressed', { exact: true }).first()).toBeVisible();
	await expect(aggregateSnapshot.getByText('cohort_lt_k_min', { exact: true })).toBeVisible();
	await expect(reports.getByRole('group', { name: 'Review and export actions' })).toBeVisible();
	await expect
		.poll(() => reportProofCampaignIds)
		.toEqual([sampleReportsWorkspace.selectedCampaign?.id]);

	await snapshot.getByRole('button', { name: 'Refresh report snapshot' }).click();
	await expect
		.poll(() => reportProofCampaignIds)
		.toEqual([
			sampleReportsWorkspace.selectedCampaign?.id,
			sampleReportsWorkspace.selectedCampaign?.id
		]);
});

test.skip('report dashboard renders selected campaign decision surface semantics', async ({ page }) => {
	const reportProofCampaignIds: string[] = [];

	await page.route('**/campaigns/*/report-proof', async (route) => {
		reportProofCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({ json: sampleCampaignReportProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const snapshot = reports.getByRole('group', { name: 'Report snapshot' });
	const dashboard = snapshot.getByRole('group', { name: 'Report dashboard' });

	await expect(
		dashboard.getByRole('heading', { name: 'Selected-series report dashboard' })
	).toBeVisible();

	const readiness = dashboard.getByRole('group', { name: 'Report readiness' });
	await expect(readiness.getByText('Selected campaign', { exact: true })).toBeVisible();
	await expect(readiness.getByText('Pulse wave 1', { exact: true })).toBeVisible();
	await expect(readiness.getByText('Report status', { exact: true })).toBeVisible();
	await expect(readiness.getByText('preview', { exact: true })).toBeVisible();
	await expect(readiness.getByText('not validated interpretation', { exact: true })).toBeVisible();

	const disclosure = dashboard.getByRole('group', { name: 'Disclosure guardrails' });
	await expect(disclosure.getByText('Disclosure k', { exact: true })).toBeVisible();
	await expect(disclosure.getByText('Visible scores', { exact: true })).toBeVisible();
	await expect(disclosure.getByText('Suppressed scores', { exact: true })).toBeVisible();

	const provenance = dashboard.getByRole('group', { name: 'Report provenance' });
	await expect(provenance.getByText('Launch snapshot', { exact: true })).toBeVisible();
	await expect(provenance.getByText('launch-snapshot-id', { exact: true })).toBeVisible();
	await expect(provenance.getByText('Scoring rule', { exact: true })).toBeVisible();

	const exportReadiness = dashboard.getByRole('group', { name: 'Export readiness' });
	await expect(exportReadiness.getByText('Latest export file', { exact: true })).toBeVisible();
	await expect(exportReadiness.getByText('report-proof.csv', { exact: true })).toBeVisible();
	await expect(exportReadiness.getByText('succeeded', { exact: true })).toBeVisible();

	const registry = dashboard.getByRole('group', { name: 'Export files' });
	await expect(registry.getByText('report-proof.csv', { exact: true })).toBeVisible();
	await expect(registry.getByText('report summary CSV and codebook', { exact: true })).toBeVisible();
	await expect(registry.getByText('csv codebook', { exact: true })).toBeVisible();
	await expect(registry.getByText('120 rows', { exact: true })).toBeVisible();
	await expect(registry.getByText('2,048 bytes', { exact: true })).toBeVisible();
	await expect(
		registry.getByText('8e592f74-d0ca-4204-aead-fb00e9e5085a', { exact: true })
	).toBeVisible();
	await expect(registry.getByText('checksum-sha256', { exact: true })).toBeVisible();

	const visualAnalytics = dashboard.getByRole('group', { name: 'Report visual analytics' });
	await expect(
		visualAnalytics.getByText('Preview / not validated', { exact: true })
	).toBeVisible();
	await expect(visualAnalytics.getByTestId('report-visual-analytics-chart')).toBeVisible();

	const chartValues = visualAnalytics.getByRole('list', {
		name: 'Report visual analytics chart values'
	});
	await expect(chartValues.getByText('total', { exact: true })).toBeVisible();
	await expect(chartValues.getByText('Mean 3.75', { exact: true })).toBeVisible();
	await expect(chartValues.getByText('scores 120', { exact: true })).toBeVisible();
	await expect(chartValues.getByText('exhaustion', { exact: true })).toHaveCount(0);

	const excludedRows = visualAnalytics.getByRole('list', {
		name: 'Report visual analytics excluded rows'
	});
	await expect(excludedRows.getByText('exhaustion', { exact: true })).toBeVisible();
	await expect(excludedRows.getByText('cohort_lt_k_min', { exact: true })).toBeVisible();

	const scoreRows = dashboard.getByRole('region', { name: 'Report score rows' });
	await expect(scoreRows.getByRole('article', { name: 'Report score total' })).toBeVisible();
	await expect(scoreRows.getByText('mean 3.75', { exact: true })).toBeVisible();
	await expect(scoreRows.getByRole('article', { name: 'Report score exhaustion' })).toBeVisible();
	await expect(scoreRows.getByText('cohort_lt_k_min', { exact: true })).toBeVisible();
	await expect(scoreRows.getByText('Suppressed', { exact: true }).first()).toBeVisible();

	await expect(snapshot.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(
		snapshot.getByText('not_validated_interpretation', { exact: true }).first()
	).toBeVisible();
	await expect(reports.getByRole('group', { name: 'Review and export actions' })).toBeVisible();
	await expect
		.poll(() => reportProofCampaignIds)
		.toEqual([sampleReportsWorkspace.selectedCampaign?.id]);
});

test.skip('report snapshot blocks without calling report proof when no reportable campaign is selected', async ({
	page
}) => {
	const reportProofCampaignIds: string[] = [];
	const blockedReportsWorkspace: CampaignSeriesReportsWorkspaceResponse = {
		...sampleReportsWorkspace,
		summary: {
			...sampleReportsWorkspace.summary,
			reportableCampaignCount: 0,
			submittedResponseCount: 0,
			scoreCount: 0,
			visibleScoreCount: 0,
			suppressedScoreCount: 0,
			missingPrerequisiteCount: 1
		},
		selectedCampaign: null,
		missingPrerequisites: [
			{
				code: 'campaign.missing',
				label: 'Campaign',
				message: 'Create or select a campaign before reporting.',
				severity: 'blocking'
			}
		],
		campaigns: []
	};

	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		await route.fulfill({ json: blockedReportsWorkspace });
	});
	await page.route('**/campaigns/*/report-proof', async (route) => {
		reportProofCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({ json: sampleCampaignReportProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const snapshot = reports.getByRole('group', { name: 'Report snapshot' });

	await expect(snapshot.getByText('Not available', { exact: true })).toBeVisible();
	await expect(
		snapshot.getByText('Create or select a campaign before loading the report snapshot.')
	).toBeVisible();
	await expect(snapshot.getByRole('button', { name: 'Refresh report snapshot' })).toBeDisabled();
	await expect(reports.getByRole('group', { name: 'Review and export actions' })).toBeVisible();
	await expect.poll(() => reportProofCampaignIds).toEqual([]);
});

test.skip('report snapshot keeps endpoint failures local and recovers on retry', async ({ page }) => {
	const reportProofCampaignIds: string[] = [];
	let reportProofRequestCount = 0;

	await page.route('**/campaigns/*/report-proof', async (route) => {
		reportProofRequestCount += 1;
		reportProofCampaignIds.push(campaignIdFromPath(route.request().url()));

		if (reportProofRequestCount === 1) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'report.snapshot.retry',
					detail: 'Temporary report snapshot failure.'
				}
			});
			return;
		}

		await route.fulfill({ json: sampleCampaignReportProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const snapshot = reports.getByRole('group', { name: 'Report snapshot' });

	await expect(snapshot.getByText('Temporary report snapshot failure.')).toBeVisible();
	await expect(snapshot.locator('.step-pill[data-state="failed"]')).toContainText('Failed');
	await expect(snapshot.getByRole('region', { name: 'Aggregate report snapshot' })).toHaveCount(0);
	await expect(reports.getByRole('group', { name: 'Review and export actions' })).toBeVisible();

	await snapshot.getByRole('button', { name: 'Refresh report snapshot' }).click();

	const aggregateSnapshot = snapshot.getByRole('region', { name: 'Aggregate report snapshot' });
	await expect(aggregateSnapshot).toBeVisible();
	await expect(aggregateSnapshot.getByText('mean 3.75', { exact: true })).toBeVisible();
	await expect(aggregateSnapshot.getByText('cohort_lt_k_min', { exact: true })).toBeVisible();
	await expect
		.poll(() => reportProofCampaignIds)
		.toEqual([
			sampleReportsWorkspace.selectedCampaign?.id,
			sampleReportsWorkspace.selectedCampaign?.id
		]);
});

test.skip('reports workflow exposes one current reports task for a reportable campaign', async ({
	page
}) => {
	const reportableReportsWorkspace = createReportableReportsWorkspaceWithoutExports();

	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/reports-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: reportableReportsWorkspace });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	const currentTask = workflow.getByRole('region', { name: 'Current review task' });

	await expect(workflow.getByRole('heading', { name: 'Current review task' })).toBeVisible();
	await expect(currentTask).toContainText('Report preview');
	await expect(workflow.getByRole('button', { name: 'View report preview' })).toBeVisible();
	await expect(workflow.getByRole('button', { name: 'Create client export' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Create response export' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Review export file' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Download CSV' })).toHaveCount(0);
});

test('retries reports workspace with the route series id', async ({ page }) => {
	const requestedProductPaths: string[] = [];
	let requestCount = 0;

	await page.route('**/campaign-series/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.startsWith('/campaign-series/')) {
			await route.fallback();
			return;
		}

		requestedProductPaths.push(pathname);

		if (pathname === `/campaign-series/${retrySeriesId}/reports-widget-manifest`) {
			await route.fulfill({
				json: {
					...sampleReportsWidgetManifest,
					campaignSeriesId: retrySeriesId
				}
			});
			return;
		}

		if (pathname !== `/campaign-series/${retrySeriesId}/reports-workspace`) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_path',
					detail: `Unexpected reports path: ${pathname}`
				}
			});
			return;
		}

		requestCount += 1;
		if (requestCount === 1) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'reports.retry',
					detail: 'Temporary reports workspace failure.'
				}
			});
			return;
		}

		await route.fulfill({
			json: {
				...sampleReportsWorkspace,
				series: {
					...sampleReportsWorkspace.series,
					id: retrySeriesId,
					name: 'Retryable reports pulse'
				}
			}
		});
	});

	await page.goto(`/app/campaign-series/${retrySeriesId}/reports`);
	await expect(page.getByRole('alert')).toContainText('Temporary reports workspace failure');

	await page.getByRole('button', { name: 'Retry surface' }).click();

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('heading', { name: 'Review and export results' })).toBeVisible();
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([
			`/campaign-series/${retrySeriesId}/reports-workspace`,
			`/campaign-series/${retrySeriesId}/reports-workspace`,
			`/campaign-series/${retrySeriesId}/reports-widget-manifest`
		]);
});

test('reports workflow runs primary report and export actions against the selected campaign', async ({
	page
}) => {
	const reportableReportsWorkspace = createReportableReportsWorkspaceWithoutExports();
	const reportProofCampaignIds: string[] = [];
	const resultsMatrixExportSeriesIds: string[] = [];
	const responseExportSeriesIds: string[] = [];
	const reportPdfSeriesIds: string[] = [];
	const signedDownloadArtifactIds: string[] = [];
	const fetchedArtifactIds: string[] = [];
	const downloadedArtifactIds: string[] = [];
	let reportsWorkspaceRequestCount = 0;

	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/reports-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		reportsWorkspaceRequestCount += 1;
		await route.fulfill({
			json:
				reportsWorkspaceRequestCount === 1
					? reportableReportsWorkspace
					: {
							...reportableReportsWorkspace,
							summary: {
								...reportableReportsWorkspace.summary,
								exportArtifactCount: 1
							},
							selectedCampaign: reportableReportsWorkspace.selectedCampaign
								? {
										...reportableReportsWorkspace.selectedCampaign,
										exportArtifactCount: 1,
										latestExportArtifactId: sampleReportProofExportArtifact.id,
										latestExportArtifactFileName: sampleReportProofExportArtifact.fileName,
										latestExportArtifactStatus: 'succeeded',
										latestExportArtifactCreatedAt: sampleReportProofExportArtifact.createdAt,
										latestExportArtifactCompletedAt: sampleReportProofExportArtifact.completedAt,
										latestExportArtifactCanDownload: true
									}
								: null,
							exportArtifacts: [
								{
									id: sampleReportProofExportArtifact.id,
									targetKind: 'campaign',
									targetId: sampleReportsWorkspace.selectedCampaign?.id ?? '',
									targetLabel: sampleReportsWorkspace.selectedCampaign?.name ?? 'Campaign',
									campaignId: sampleReportsWorkspace.selectedCampaign?.id ?? '',
									campaignName: sampleReportsWorkspace.selectedCampaign?.name ?? 'Campaign',
									artifactType: 'report_proof_csv_codebook',
									status: 'succeeded',
									format: 'csv_codebook',
									fileName: sampleReportProofExportArtifact.fileName,
									rowCount: sampleReportProofExportArtifact.rowCount,
									byteSize: sampleReportProofExportArtifact.byteSize,
									checksumSha256: sampleReportProofExportArtifact.checksumSha256,
									createdAt: sampleReportProofExportArtifact.createdAt,
									completedAt: sampleReportProofExportArtifact.completedAt,
									startedAt: null,
									failedAt: null,
									expiresAt: null,
									deletedAt: null,
									failureReasonCode: null,
									canDownload: true
								}
							]
						}
		});
	});
	await page.route('**/campaigns/*/report-proof', async (route) => {
		reportProofCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({ json: sampleCampaignReportProof });
	});
	await page.route('**/campaign-series/*/results-matrix-exports', async (route) => {
		resultsMatrixExportSeriesIds.push(seriesIdFromPath(route.request().url()));
		await route.fulfill({ status: 201, json: sampleReportProofExportArtifact });
	});
	await page.route('**/campaign-series/*/response-exports', async (route) => {
		responseExportSeriesIds.push(seriesIdFromPath(route.request().url()));
		await route.fulfill({ status: 201, json: sampleResponseExportArtifact });
	});
	await page.route('**/campaign-series/*/report-pdf-artifacts', async (route) => {
		reportPdfSeriesIds.push(seriesIdFromPath(route.request().url()));
		await route.fulfill({ status: 201, json: sampleReportPdfArtifact });
	});
	await page.route('**/export-artifacts/*/signed-download-url', async (route) => {
		signedDownloadArtifactIds.push(new URL(route.request().url()).pathname.split('/').at(-2) ?? '');
		await route.fulfill({ json: sampleReportPdfSignedDownloadUrl });
	});
	await page.route('**/export-artifacts/*/download', async (route) => {
		const artifactId = artifactIdFromPath(route.request().url());
		downloadedArtifactIds.push(artifactId);
		await route.fulfill({
			status: 200,
			headers: {
				'content-type': 'text/csv',
				'content-disposition':
					artifactId === sampleResponseExportArtifact.id
						? 'attachment; filename="campaign-series-responses.csv"'
						: 'attachment; filename="updated-report-proof.csv"'
			},
			body:
				artifactId === sampleResponseExportArtifact.id
					? 'response_row_id,trajectory_id\n'
					: 'dimension_code,mean\n'
		});
	});
	await page.route('**/export-artifacts/*', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (pathname.endsWith('/download') || pathname.endsWith('/signed-download-url')) {
			await route.fallback();
			return;
		}

		const artifactId = artifactIdFromPath(route.request().url());
		fetchedArtifactIds.push(artifactId);
		await route.fulfill({
			json:
				artifactId === sampleResponseExportArtifact.id
					? sampleResponseExportArtifact
					: sampleReportProofExportArtifact
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	await expect(workflow).toBeVisible();
	const useReview = workflow.getByRole('region', { name: 'Results use review' });
	const exportPreview = workflow.getByRole('region', { name: 'Export preview' });
	await expect(useReview).toContainText('Can these results be used?');
	const reportProofButton = useReview.getByRole('button', { name: 'Review results' });
	await expect(reportProofButton).toBeEnabled();
	await reportProofButton.click();
	const reportPreview = workflow.getByRole('region', { name: 'Report preview' });
	await expect(reportPreview).toBeVisible();
	await expect(reportPreview.getByText('Higher tenant band', { exact: true })).toBeVisible();
	await expect(
		reportPreview.getByText('tenant attested / tenant defined / not reviewed / not official', {
			exact: true
		})
	).toBeVisible();

	const exportButton = exportPreview.getByRole('button', { name: 'Create results matrix export' });
	await expect(exportButton).toBeEnabled();
	await exportButton.click();
	await expect(workflow.getByRole('region', { name: 'Results matrix export result' })).toBeVisible();

	const responseExportButton = exportPreview.getByRole('button', { name: 'Create response export' });
	await expect(responseExportButton).toBeEnabled();
	await responseExportButton.click();
	await expect(workflow.getByRole('region', { name: 'Response export result' })).toBeVisible();

	const reportPdfButton = exportPreview.getByRole('button', { name: 'Create report PDF' });
	await expect(reportPdfButton).toBeEnabled();
	await reportPdfButton.click();
	const reportPdfResult = workflow.getByRole('region', { name: 'Report PDF result' });
	await expect(reportPdfResult).toBeVisible();
	await expect(reportPdfResult.getByText('campaign-series-report.pdf', { exact: true })).toBeVisible();
	await expect(reportPdfResult.getByText('Downloadable', { exact: true }).first()).toBeVisible();
	await reportPdfResult.getByRole('button', { name: 'Get secure PDF link' }).click();
	await expect(reportPdfResult.getByText('Secure PDF link ready', { exact: true })).toBeVisible();
	await expect(reportPdfResult.getByRole('link', { name: 'Open secure PDF link' })).toHaveAttribute(
		'href',
		sampleReportPdfSignedDownloadUrl.url
	);
	await expect(reportPdfResult.getByText(sampleReportPdfSignedDownloadUrl.expiresAt)).toBeVisible();

	await exportPreview.getByRole('button', { name: 'Review export file' }).click();
	await expect(exportPreview.getByText('campaign-series-responses.csv', { exact: true }).first()).toBeVisible();
	await workflow.getByText('Details: method, readiness, coverage, and exports').click();
	const variablesAndValues = workflow.getByRole('region', { name: 'Variables and values' });
	await expect(variablesAndValues.getByText('1 conditional display rule')).toBeVisible();
	await expect(variablesAndValues.getByText('1 option-score map')).toBeVisible();
	await expect(
		variablesAndValues.getByText('q02 shown when q01 includes o02; hidden answers __skipped')
	).toBeVisible();
	await expect(
		variablesAndValues.getByText('q01 carries tenant-defined option scores')
	).toBeVisible();
	await exportPreview.getByRole('button', { name: 'Download response dataset CSV' }).click();
	await expect(exportPreview.getByText('Downloaded CSV', { exact: true })).toBeVisible();
	await expect(
		exportPreview.getByText('response_row_id,trajectory_id', { exact: true }).first()
	).toBeVisible();

	expect(reportProofCampaignIds).toEqual([sampleReportsWorkspace.selectedCampaign?.id]);
	expect(resultsMatrixExportSeriesIds).toEqual([sampleSeriesId]);
	expect(responseExportSeriesIds).toEqual([sampleSeriesId]);
	expect(reportPdfSeriesIds).toEqual([sampleSeriesId]);
	expect(signedDownloadArtifactIds).toEqual([sampleReportPdfArtifact.id]);
	expect(fetchedArtifactIds).toEqual([sampleResponseExportArtifact.id]);
	expect(downloadedArtifactIds).toEqual([sampleResponseExportArtifact.id]);
	await expect.poll(() => reportsWorkspaceRequestCount).toBeGreaterThanOrEqual(3);
	await expect(
		workflow.getByText('updated-report-proof.csv', { exact: true }).first()
	).toBeVisible();
	await expect(
		exportPreview.getByText('campaign-series-responses.csv', { exact: true }).first()
	).toBeVisible();
});

test('reports workflow retries a failed report PDF artifact', async ({ page }) => {
	const reportPdfSeriesIds: string[] = [];
	const retriedArtifactIds: string[] = [];
	let reportsWorkspaceRequestCount = 0;

	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/reports-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		reportsWorkspaceRequestCount += 1;
		await route.fulfill({ json: createReportableReportsWorkspaceWithoutExports() });
	});
	await page.route('**/campaign-series/*/report-pdf-artifacts', async (route) => {
		reportPdfSeriesIds.push(seriesIdFromPath(route.request().url()));
		await route.fulfill({ status: 201, json: sampleFailedReportPdfArtifact });
	});
	await page.route('**/export-artifacts/*/retry', async (route) => {
		retriedArtifactIds.push(new URL(route.request().url()).pathname.split('/').at(-2) ?? '');
		await route.fulfill({ json: sampleRetryReportPdfArtifact });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	const exportPreview = workflow.getByRole('region', { name: 'Export preview' });
	await exportPreview.getByRole('button', { name: 'Create report PDF' }).click();

	const reportPdfResult = workflow.getByRole('region', { name: 'Report PDF result' });
	await expect(reportPdfResult).toBeVisible();
	await expect(reportPdfResult.getByText('campaign-series-report-failed.pdf')).toBeVisible();
	await expect(reportPdfResult.getByText('report_pdf.rendering_timeout')).toBeVisible();
	await expect(reportPdfResult.getByRole('button', { name: 'Get secure PDF link' })).toBeDisabled();
	await expect(reportPdfResult.getByRole('button', { name: 'Retry report PDF' })).toBeEnabled();

	await reportPdfResult.getByRole('button', { name: 'Retry report PDF' }).click();
	await expect(reportPdfResult.getByText('campaign-series-report-retry.pdf')).toBeVisible();
	await expect(reportPdfResult.getByText('queued', { exact: true }).first()).toBeVisible();

	expect(reportPdfSeriesIds).toEqual([sampleSeriesId]);
	expect(retriedArtifactIds).toEqual([sampleFailedReportPdfArtifact.id]);
	await expect.poll(() => reportsWorkspaceRequestCount).toBeGreaterThanOrEqual(2);
});

test('reports workflow restores existing report PDF delivery controls from workspace artifacts', async ({
	page
}) => {
	const signedDownloadArtifactIds: string[] = [];
	const workspaceWithReportPdf = {
		...createReportableReportsWorkspaceWithoutExports(),
		summary: {
			...createReportableReportsWorkspaceWithoutExports().summary,
			exportArtifactCount: 1
		},
		exportArtifacts: [sampleReportPdfArtifact]
	};

	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/reports-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: workspaceWithReportPdf });
	});
	await page.route('**/export-artifacts/*/signed-download-url', async (route) => {
		signedDownloadArtifactIds.push(new URL(route.request().url()).pathname.split('/').at(-2) ?? '');
		await route.fulfill({ json: sampleReportPdfSignedDownloadUrl });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const reportPdfResult = reports
		.getByRole('group', { name: 'Review and export actions' })
		.getByRole('region', { name: 'Report PDF result' });
	await expect(reportPdfResult).toBeVisible();
	await expect(reportPdfResult.getByText('campaign-series-report.pdf', { exact: true })).toBeVisible();
	await reportPdfResult.getByRole('button', { name: 'Get secure PDF link' }).click();
	await expect(reportPdfResult.getByText('Secure PDF link ready', { exact: true })).toBeVisible();
	await expect(reportPdfResult.getByRole('link', { name: 'Open secure PDF link' })).toHaveAttribute(
		'href',
		sampleReportPdfSignedDownloadUrl.url
	);

	expect(signedDownloadArtifactIds).toEqual([sampleReportPdfArtifact.id]);
});

test('waves workspace loads the dedicated read model and renders wave state', async ({ page }) => {
	const requestedProductPaths: string[] = [];

	await page.route('**/campaign-series/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.startsWith('/campaign-series/')) {
			await route.fallback();
			return;
		}

		if (pathname === `/campaign-series/${sampleSeriesId}/wave-comparison-proof`) {
			await route.fulfill({ json: sampleWaveComparisonProof });
			return;
		}

		requestedProductPaths.push(pathname);

		if (pathname !== `/campaign-series/${sampleSeriesId}/waves-workspace`) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_path',
					detail: `Unexpected waves path: ${pathname}`
				}
			});
			return;
		}

		await route.fulfill({ json: sampleWavesWorkspace });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Rounds and linked repeat responses' });
	const workflow = waves.getByRole('group', { name: 'Measurement comparison workflow' });
	const details = waves.locator('details[aria-label="Waves details"]');

	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('heading', { name: 'Compare repeated measurements' })).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Measurement timeline' })).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Measurement comparison preview' }).first()).toBeVisible();
	await expect(waves.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);

	await expect(details).toBeVisible();
	await details.locator('summary').click();
	await expect(details.getByRole('heading', { name: 'Comparison details' })).toBeVisible();
	await expect(details.getByText('Repeat-participation waves', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Linked repeat responses', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Complete repeat-response pairs', { exact: true }).first()).toBeVisible();
	await expect(details.getByText('Visible comparisons', { exact: true }).first()).toBeVisible();
	await expect(details.getByRole('group', { name: 'Compared waves' })).toBeVisible();
	await expect(details.getByRole('group', { name: 'Wave source context' })).toBeVisible();
	await expect(details.getByRole('article', { name: 'Pulse wave 1' })).toBeVisible();
	await expect(details.getByRole('article', { name: 'Pulse wave 2' })).toBeVisible();
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([`/campaign-series/${sampleSeriesId}/waves-workspace`]);
});

test.skip('wave comparison snapshot renders selected wave proof as route content', async ({ page }) => {
	const comparisonSeriesIds: string[] = [];

	await page.route(`**/campaign-series/${sampleSeriesId}/wave-comparison-proof`, async (route) => {
		comparisonSeriesIds.push(new URL(route.request().url()).pathname.split('/')[2]);
		await route.fulfill({ json: sampleWaveComparisonProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const snapshot = waves.getByRole('group', { name: 'Repeated-round comparison snapshot' });

	await expect(snapshot).toBeVisible();
	await expect(
		snapshot.getByRole('heading', { name: 'Selected-series wave comparison snapshot' })
	).toBeVisible();
	await expect(snapshot.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(
		snapshot.getByText('not_validated_interpretation', { exact: true }).first()
	).toBeVisible();
	const aggregateSnapshot = snapshot.getByRole('region', {
		name: 'Aggregate wave comparison snapshot'
	});
	await expect(aggregateSnapshot).toBeVisible();
	await expect(snapshot.getByText('linked pairs 6', { exact: true }).first()).toBeVisible();
	await expect(snapshot.getByText('paired delta -0.25', { exact: true })).toBeVisible();
	await expect(snapshot.getByText('Suppressed', { exact: true }).first()).toBeVisible();
	await expect(aggregateSnapshot.getByText('linked_pairs_lt_k_min', { exact: true })).toBeVisible();
	await expect(waves.getByRole('group', { name: 'Waves action workflow' })).toBeVisible();
	await expect.poll(() => comparisonSeriesIds).toEqual([sampleSeriesId]);

	await snapshot.getByRole('button', { name: 'Refresh wave comparison snapshot' }).click();
	await expect.poll(() => comparisonSeriesIds).toEqual([sampleSeriesId, sampleSeriesId]);
});

test.skip('wave dashboard renders selected comparison decision surface semantics', async ({ page }) => {
	const comparisonSeriesIds: string[] = [];

	await page.route(`**/campaign-series/${sampleSeriesId}/wave-comparison-proof`, async (route) => {
		comparisonSeriesIds.push(new URL(route.request().url()).pathname.split('/')[2]);
		await route.fulfill({ json: sampleWaveComparisonProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const snapshot = waves.getByRole('group', { name: 'Repeated-round comparison snapshot' });
	const dashboard = snapshot.getByRole('group', { name: 'Wave dashboard' });

	await expect(
		dashboard.getByRole('heading', { name: 'Selected-series wave dashboard' })
	).toBeVisible();

	const readiness = dashboard.getByRole('group', { name: 'Wave readiness' });
	await expect(readiness.getByText('Baseline wave', { exact: true })).toBeVisible();
	await expect(readiness.getByText('Pulse wave 1', { exact: true })).toBeVisible();
	await expect(readiness.getByText('Comparison wave', { exact: true })).toBeVisible();
	await expect(readiness.getByText('Pulse wave 2', { exact: true })).toBeVisible();
	await expect(readiness.getByText('Complete trajectories', { exact: true })).toBeVisible();

	const comparisonStatus = dashboard.getByRole('group', { name: 'Comparison status' });
	await expect(comparisonStatus.getByText('Proof status', { exact: true })).toBeVisible();
	await expect(comparisonStatus.getByText('preview', { exact: true })).toBeVisible();
	await expect(
		comparisonStatus.getByText('not validated interpretation', { exact: true })
	).toBeVisible();

	const guardrails = dashboard.getByRole('group', { name: 'Disclosure and compatibility' });
	await expect(guardrails.getByText('Disclosure k', { exact: true })).toBeVisible();
	await expect(guardrails.getByText('Compatibility', { exact: true })).toBeVisible();
	await expect(guardrails.getByText('compatible', { exact: true })).toBeVisible();
	await expect(guardrails.getByText('Visible scores', { exact: true })).toBeVisible();
	await expect(guardrails.getByText('Suppressed scores', { exact: true })).toBeVisible();
	await expect(guardrails.getByText('Blocked scores', { exact: true })).toBeVisible();

	const provenance = dashboard.getByRole('group', { name: 'Wave provenance' });
	await expect(provenance.getByText('Baseline launch snapshot', { exact: true })).toBeVisible();
	await expect(provenance.getByText('wave-1-launch-id', { exact: true })).toBeVisible();
	await expect(provenance.getByText('Comparison launch snapshot', { exact: true })).toBeVisible();
	await expect(provenance.getByText('wave-2-launch-id', { exact: true })).toBeVisible();
	await expect(provenance.getByText('burnout.total', { exact: true }).first()).toBeVisible();

	const visualAnalytics = dashboard.getByRole('group', { name: 'Wave visual analytics' });
	await expect(
		visualAnalytics.getByText('Preview / not validated', { exact: true })
	).toBeVisible();
	await expect(visualAnalytics.getByTestId('wave-visual-analytics-chart')).toBeVisible();

	const chartValues = visualAnalytics.getByRole('list', {
		name: 'Wave visual analytics chart values'
	});
	await expect(chartValues.getByText('total', { exact: true })).toBeVisible();
	await expect(chartValues.getByText('Aggregate delta -0.30', { exact: true })).toBeVisible();
	await expect(chartValues.getByText('Paired delta -0.25', { exact: true })).toBeVisible();
	await expect(chartValues.getByText('linked pairs 6', { exact: true })).toBeVisible();
	await expect(chartValues.getByText('exhaustion', { exact: true })).toHaveCount(0);

	const excludedRows = visualAnalytics.getByRole('list', {
		name: 'Wave visual analytics excluded rows'
	});
	await expect(excludedRows.getByText('exhaustion', { exact: true })).toBeVisible();
	await expect(excludedRows.getByText('linked_pairs_lt_k_min', { exact: true })).toBeVisible();

	const scoreRows = dashboard.getByRole('region', { name: 'Repeated-round comparison rows' });
	await expect(scoreRows.getByRole('article', { name: 'Repeated-round comparison total' })).toBeVisible();
	await expect(scoreRows.getByText('aggregate delta -0.30', { exact: true })).toBeVisible();
	await expect(scoreRows.getByText('paired delta -0.25', { exact: true })).toBeVisible();
	await expect(
		scoreRows.getByRole('article', { name: 'Repeated-round comparison exhaustion' })
	).toBeVisible();
	await expect(scoreRows.getByText('linked_pairs_lt_k_min', { exact: true })).toBeVisible();
	await expect(scoreRows.getByText('Suppressed', { exact: true }).first()).toBeVisible();

	await expect(snapshot.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(
		snapshot.getByText('not_validated_interpretation', { exact: true }).first()
	).toBeVisible();
	await expect(waves.getByRole('group', { name: 'Waves action workflow' })).toBeVisible();
	await expect.poll(() => comparisonSeriesIds).toEqual([sampleSeriesId]);
});

test.skip('wave comparison snapshot blocks without calling proof when comparison is not ready', async ({
	page
}) => {
	const comparisonSeriesIds: string[] = [];
	const blockedWavesWorkspace: CampaignSeriesWavesWorkspaceResponse = {
		...sampleWavesWorkspace,
		summary: {
			...sampleWavesWorkspace.summary,
			comparableScoreCount: 0,
			visibleComparisonCount: 0,
			suppressedComparisonCount: 0,
			blockedComparisonCount: 0,
			missingPrerequisiteCount: 1
		},
		selectedComparisonWave: null,
		comparison: {
			status: 'not_available',
			disclosureState: 'not_available',
			compatibilityState: 'not_available',
			interpretationStatus: 'not_available',
			disclosureKMin: null,
			linkedPairCount: 0,
			visibleScoreCount: 0,
			suppressedScoreCount: 0,
			blockedScoreCount: 0
		},
		missingPrerequisites: [
			{
				code: 'comparison.missing',
				label: 'Comparison wave',
				message: 'Select two comparable waves before reporting.',
				severity: 'blocking'
			}
		],
		waves: sampleWavesWorkspace.selectedBaselineWave
			? [sampleWavesWorkspace.selectedBaselineWave]
			: []
	};

	await page.route(`**/campaign-series/${sampleSeriesId}/waves-workspace`, async (route) => {
		await route.fulfill({ json: blockedWavesWorkspace });
	});
	await page.route(`**/campaign-series/${sampleSeriesId}/wave-comparison-proof`, async (route) => {
		comparisonSeriesIds.push(new URL(route.request().url()).pathname.split('/')[2]);
		await route.fulfill({ json: sampleWaveComparisonProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const snapshot = waves.getByRole('group', { name: 'Repeated-round comparison snapshot' });

	await expect(snapshot.getByText('Not available', { exact: true })).toBeVisible();
	await expect(
		snapshot.getByText('Select two comparable waves before loading the wave comparison snapshot.')
	).toBeVisible();
	await expect(
		snapshot.getByRole('button', { name: 'Refresh wave comparison snapshot' })
	).toBeDisabled();
	await expect(waves.getByRole('group', { name: 'Waves action workflow' })).toBeVisible();
	await expect.poll(() => comparisonSeriesIds).toEqual([]);
});

test.skip('wave comparison snapshot keeps endpoint failures local and recovers on retry', async ({
	page
}) => {
	const comparisonSeriesIds: string[] = [];
	let comparisonRequestCount = 0;

	await page.route(`**/campaign-series/${sampleSeriesId}/wave-comparison-proof`, async (route) => {
		comparisonRequestCount += 1;
		comparisonSeriesIds.push(new URL(route.request().url()).pathname.split('/')[2]);

		if (comparisonRequestCount === 1) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'wave.snapshot.retry',
					detail: 'Temporary wave comparison snapshot failure.'
				}
			});
			return;
		}

		await route.fulfill({ json: sampleWaveComparisonProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const snapshot = waves.getByRole('group', { name: 'Repeated-round comparison snapshot' });

	await expect(snapshot.getByText('Temporary wave comparison snapshot failure.')).toBeVisible();
	await expect(snapshot.locator('.step-pill[data-state="failed"]')).toContainText('Failed');
	await expect(
		snapshot.getByRole('region', { name: 'Aggregate wave comparison snapshot' })
	).toHaveCount(0);
	await expect(waves.getByRole('group', { name: 'Waves action workflow' })).toBeVisible();

	await snapshot.getByRole('button', { name: 'Refresh wave comparison snapshot' }).click();

	const aggregateSnapshot = snapshot.getByRole('region', {
		name: 'Aggregate wave comparison snapshot'
	});
	await expect(aggregateSnapshot).toBeVisible();
	await expect(snapshot.getByText('paired delta -0.25', { exact: true })).toBeVisible();
	await expect(aggregateSnapshot.getByText('linked_pairs_lt_k_min', { exact: true })).toBeVisible();
	await expect.poll(() => comparisonSeriesIds).toEqual([sampleSeriesId, sampleSeriesId]);
});

test('retries waves workspace with the route series id', async ({ page }) => {
	const requestedProductPaths: string[] = [];
	let requestCount = 0;

	await page.route('**/campaign-series/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.startsWith('/campaign-series/')) {
			await route.fallback();
			return;
		}

		if (pathname === `/campaign-series/${retrySeriesId}/wave-comparison-proof`) {
			await route.fulfill({
				json: {
					...sampleWaveComparisonProof,
					campaignSeriesId: retrySeriesId
				}
			});
			return;
		}

		requestedProductPaths.push(pathname);

		if (pathname !== `/campaign-series/${retrySeriesId}/waves-workspace`) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_path',
					detail: `Unexpected waves path: ${pathname}`
				}
			});
			return;
		}

		requestCount += 1;
		if (requestCount === 1) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'waves.retry',
					detail: 'Temporary waves workspace failure.'
				}
			});
			return;
		}

		await route.fulfill({
			json: {
				...sampleWavesWorkspace,
				series: {
					...sampleWavesWorkspace.series,
					id: retrySeriesId,
					name: 'Retryable waves pulse'
				}
			}
		});
	});

	await page.goto(`/app/campaign-series/${retrySeriesId}/waves`);
	await expect(page.getByRole('alert')).toContainText('Temporary waves workspace failure');

	await page.getByRole('button', { name: 'Retry surface' }).click();

	const waves = page.getByRole('region', { name: 'Rounds and linked repeat responses' });
	const workflow = waves.getByRole('group', { name: 'Measurement comparison workflow' });
	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('heading', { name: 'Compare repeated measurements' })).toBeVisible();
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([
			`/campaign-series/${retrySeriesId}/waves-workspace`,
			`/campaign-series/${retrySeriesId}/waves-workspace`
		]);
});

test('waves workflow runs primary comparison actions against the selected route series', async ({
	page
}) => {
	let wavesWorkspaceRequestCount = 0;
	const twoWaveSeriesIds: string[] = [];
	const comparisonSeriesIds: string[] = [];

	await page.route(`**/campaign-series/${sampleSeriesId}/waves-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/waves-workspace`)
		) {
			await route.fallback();
			return;
		}

		wavesWorkspaceRequestCount += 1;
		await route.fulfill({
			json:
				wavesWorkspaceRequestCount === 1
					? sampleWavesWorkspace
					: {
							...sampleWavesWorkspace,
							summary: {
								...sampleWavesWorkspace.summary,
								visibleComparisonCount: sampleWavesWorkspace.summary.visibleComparisonCount + 1
							},
							comparison: {
								...sampleWavesWorkspace.comparison,
								visibleScoreCount: sampleWavesWorkspace.comparison.visibleScoreCount + 1
							}
						}
		});
	});
	await page.route(`**/campaign-series/${sampleSeriesId}/two-wave-proof`, async (route) => {
		twoWaveSeriesIds.push(new URL(route.request().url()).pathname.split('/')[2]);
		await route.fulfill({ json: sampleTwoWaveProof });
	});
	await page.route(`**/campaign-series/${sampleSeriesId}/wave-comparison-proof`, async (route) => {
		comparisonSeriesIds.push(new URL(route.request().url()).pathname.split('/')[2]);
		await route.fulfill({ json: sampleWaveComparisonProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Rounds and linked repeat responses' });
	const workflow = waves.getByRole('group', { name: 'Measurement comparison workflow' });
	const comparisonPreview = workflow.getByRole('region', { name: 'Measurement comparison preview' }).first();

	await expect(workflow.getByRole('heading', { name: 'Compare repeated measurements' })).toBeVisible();
	await expect(comparisonPreview).toContainText('Comparison plan');
	const twoWaveButton = comparisonPreview.getByRole('button', {
		name: 'Check linked repeat responses'
	});
	await expect(twoWaveButton).toBeEnabled();
	await twoWaveButton.click();
	await expect(workflow.getByRole('region', { name: 'Linked repeat response check' })).toBeVisible();

	const comparisonButton = comparisonPreview.getByRole('button', {
		name: 'Review comparison'
	});
	await expect(comparisonButton).toBeEnabled();
	await comparisonButton.click();
	await expect(workflow.getByRole('region', { name: 'Aggregate wave comparison snapshot' })).toBeVisible();

	expect(twoWaveSeriesIds).toEqual([sampleSeriesId]);
	await expect.poll(() => comparisonSeriesIds.length).toBeGreaterThanOrEqual(2);
	await expect.poll(() => wavesWorkspaceRequestCount).toBeGreaterThanOrEqual(3);
	await expect(workflow.getByText('6 complete repeat-response pairs', { exact: true })).toBeVisible();
	await expect(comparisonPreview.getByText('paired delta -0.25', { exact: true })).toBeVisible();
});

test('setup workflow renders primary setup actions instead of the proof workbench', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	await expect(setup.getByRole('group', { name: 'Protocol progress' })).toBeVisible();
	await expect(
		setup.getByRole('heading', { name: 'Protocol progress', exact: true })
	).toBeVisible();
	await expect(setup.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);
});

test('setup route leads with a study preparation path before the current task', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	await expect(page.getByRole('heading', { name: 'Study protocol', exact: true })).toBeVisible();

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	const setupPath = workflow.locator('.setup-path');

	await expect(setupPath).toBeVisible();
	await expect(workflow.getByText(/required steps complete/)).toBeVisible();
	await expect(setup.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);
});

test('setup action hierarchy starts editable studies on the current setup task', async ({
	page
}) => {
	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: emptySetupWorkspace });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	const setupPath = workflow.locator('.setup-path');
	const currentTask = workflow.getByRole('region', {
		name: /Build questionnaire|Review results setup|Measurement and recipients|Ready to collect/
	});

	await expect(workflow).toBeVisible();
	await expect(currentTask).toBeVisible();
	await expect(setupPath).toBeVisible();
	await expectElementBefore(setupPath, currentTask);
});

test.skip('setup policy review details are visible in the setup workspace', async ({ page }) => {
	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleSetupWorkspace,
				summary: {
					...sampleSetupWorkspace.summary,
					missingPrerequisiteCount: 0
				},
				policies: {
					consent: {
						id: '7a8b40d5-5083-4b90-a521-6de19f88d108',
						version: '1.0.0',
						status: 'configured',
						details: [
							{ label: 'Title', value: 'Default participant disclosure' },
							{ label: 'Locale', value: 'en' },
							{ label: 'Required grants', value: '2' },
							{ label: 'Optional grants', value: '0' },
							{ label: 'Published', value: '2026-05-06' }
						]
					},
					retention: {
						id: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
						version: '1.0.0',
						status: 'configured',
						details: [
							{ label: 'Retain for', value: '1 year' },
							{ label: 'Starts from', value: 'response submitted at' },
							{ label: 'Action after retention', value: 'anonymize' },
							{ label: 'Next review', value: '2027-05-06' }
						]
					},
					disclosure: {
						id: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
						version: '1.0.0',
						status: 'configured',
						details: [
							{ label: 'Minimum group size', value: '5' },
							{ label: 'Suppression', value: 'hide cell' },
							{ label: 'Applies to', value: 'score, subscale, demographic, wave comparison' }
						]
					}
				},
				missingPrerequisites: []
			}
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const policies = page.getByRole('group', { name: 'Setup policies' });
	await expect(policies.getByText('Default participant disclosure', { exact: true })).toBeVisible();
	await expect(policies.getByText('Required grants', { exact: true })).toBeVisible();
	await expect(policies.getByText('response submitted at', { exact: true })).toBeVisible();
	await expect(policies.getByText('Minimum group size', { exact: true })).toBeVisible();
	await expect(
		policies.getByText('score, subscale, demographic, wave comparison', { exact: true })
	).toBeVisible();
});

test('setup workflow exposes one current setup task for an empty series', async ({ page }) => {
	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: emptySetupWorkspace });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	await expect(workflow.getByRole('region', { name: 'Build questionnaire' })).toContainText(
		'Questionnaire'
	);
	await expect(workflow.getByRole('button', { name: 'Create instrument import' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Save questionnaire' })).toBeVisible();
	await expect(workflow.getByRole('button', { name: 'Save result outputs' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Create wave draft' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Check launch readiness' })).toHaveCount(0);
});

test('setup template authoring edits question rows and generated scoring defaults', async ({
	page
}) => {
	const templateBodies: Array<{
		instrumentId: string | null;
		sections: Array<{
			ordinal: number;
			code: string;
			titleDefault: string;
		}>;
		questions: Array<{
			ordinal: number;
			code: string;
			textDefault: string;
			type: string;
			sectionCode?: string | null;
			required: boolean;
			reverseCoded: boolean;
			payload: string;
		}>;
	}> = [];
	const scoringBodies: Array<{ document: string; produces: string }> = [];
	const createdInstrumentId = '7e4d44f0-4b2a-472a-9e0c-57218f49bcb9';

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: emptySetupWorkspace });
	});

	await page.route('**/instruments/private-imports', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			status: 201,
			json: {
				id: createdInstrumentId,
				code: 'tenant-burnout-pulse',
				version: '1.0.0',
				fullName: 'Tenant burnout pulse',
				validityStatus: 'private_import',
				rightsStatus: 'attested_by_tenant'
			}
		});
	});

	await page.route('**/template-versions', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		templateBodies.push(route.request().postDataJSON());
		await route.fulfill({
			status: 201,
			json: {
				...sampleTemplateVersion,
				instrumentId: createdInstrumentId,
				questions: templateBodies.at(-1)?.questions ?? []
			}
		});
	});

	await page.route('**/scoring-rules', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		scoringBodies.push(route.request().postDataJSON());
		await route.fulfill({
			status: 201,
			json: { id: '716b2246-70f7-4728-9f44-150bd3b8da7a' }
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	await expect(workflow).toContainText('Questionnaire');

	const questionRows = workflow.locator('.question-row');
	await expect(questionRows).toHaveCount(3);
	await workflow.getByRole('button', { name: 'Add question' }).click();
	await expect(questionRows).toHaveCount(4);

	await questionRows.nth(3).getByRole('button').first().click();
	await questionRows
		.nth(3)
		.getByLabel('Question text')
		.fill('I can recover focus after a difficult interruption.');
	await questionRows.nth(3).getByLabel('Section / page').fill('Recovery page');
	await questionRows.nth(3).getByLabel('Dimension / construct').fill('Recovery');
	await questionRows.nth(3).getByLabel('Reverse score this question', { exact: true }).check();
	await questionRows.nth(1).getByRole('button').first().click();
	await questionRows.nth(1).getByRole('button', { name: 'Remove' }).click();

	await workflow.getByRole('button', { name: 'Save questionnaire' }).click();
	await expect.poll(() => templateBodies).toHaveLength(1);
	expect(templateBodies[0].sections).toEqual([
		{ ordinal: 1, code: 'page_1', titleDefault: 'Page 1' },
		{ ordinal: 2, code: 'recovery_page', titleDefault: 'Recovery page' }
	]);
	expect(templateBodies[0]).toMatchObject({
		instrumentId: createdInstrumentId,
		questions: [
			{
				ordinal: 1,
				code: 'q01',
				type: 'likert',
				sectionCode: 'page_1',
				reverseCoded: false
			},
			{
				ordinal: 2,
				code: 'q03',
				type: 'likert',
				sectionCode: 'page_1',
				reverseCoded: true
			},
			{
				ordinal: 3,
				code: 'q04',
				textDefault: 'I can recover focus after a difficult interruption.',
				type: 'likert',
				sectionCode: 'recovery_page',
				required: true,
				reverseCoded: true
			}
		]
	});
	expect(JSON.parse(templateBodies[0].questions[2].payload)).toMatchObject({
		authoring: { dimensionLabel: 'Recovery' }
	});
	expect(templateBodies[0].questions.map((question) => question.code)).not.toContain('q02');

	await expect(workflow).toContainText('Review results setup');
	await expect(workflow.getByRole('heading', { name: 'Total score' })).toBeVisible();
	await workflow.locator('summary').filter({ hasText: 'Result outputs plan' }).click();
	await workflow.getByLabel('Result name').fill('Recovery');
	await workflow.getByLabel('Result code').fill('recovery');
	await workflow.getByLabel('Calculation').selectOption('sum');
	await workflow.getByLabel('Missing answers').selectOption('min_valid_count');
	await workflow.getByLabel('Minimum answered').fill('1');
	await workflow.getByLabel('Write the first question for this study.').uncheck();
	await workflow.getByLabel('Write the third question for this study.').uncheck();
	await workflow.getByLabel('I can recover focus after a difficult interruption.').check();

	await workflow.getByRole('button', { name: 'Save results setup' }).click();
	await expect.poll(() => scoringBodies).toHaveLength(1);
	const submittedScoringDocument = JSON.parse(scoringBodies[0].document) as {
		inputs: Array<{ id: string; items: string[] }>;
		nodes: Array<{ id: string; op: string; explicit_reverse_items?: string[]; missing_data?: unknown }>;
		outputs: Array<{ code: string; node: string }>;
	};
	expect(submittedScoringDocument.inputs.find((input) => input.id === 'recovery_items')?.items).toEqual([
		'q04'
	]);
	expect(
		submittedScoringDocument.nodes.find((node) => node.id === 'recovery_scored_answers')
			?.explicit_reverse_items
	).toEqual(['q04']);
	expect(submittedScoringDocument.nodes.find((node) => node.id === 'recovery_score')).toMatchObject({
		op: 'sum',
		missing_data: { strategy: 'min_valid_count', min_valid_count: 1 }
	});
	expect(submittedScoringDocument.outputs).toEqual([
		{ code: 'recovery', node: 'recovery_score' }
	]);
	expect(submittedScoringDocument.outputs.map((output) => output.code)).toEqual(['recovery']);
	expect(JSON.parse(scoringBodies[0].produces)).toEqual({ scores: ['recovery'] });
});

test('setup template authoring saves M3 required answer formats together', async ({ page }) => {
	const templateBodies: Array<{
		instrumentId: string | null;
		questions: Array<{
			ordinal: number;
			code: string;
			textDefault: string;
			type: string;
			payload: string;
		}>;
	}> = [];
	const createdInstrumentId = '7e4d44f0-4b2a-472a-9e0c-57218f49bcb9';

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: emptySetupWorkspace });
	});

	await page.route('**/instruments/private-imports', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			status: 201,
			json: {
				id: createdInstrumentId,
				code: 'tenant-m3-question-types',
				version: '1.0.0',
				fullName: 'Tenant M3 question type proof',
				validityStatus: 'private_import',
				rightsStatus: 'attested_by_tenant'
			}
		});
	});

	await page.route('**/template-versions', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		templateBodies.push(route.request().postDataJSON());
		await route.fulfill({
			status: 201,
			json: {
				...sampleTemplateVersion,
				instrumentId: createdInstrumentId,
				questions: templateBodies.at(-1)?.questions ?? []
			}
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	const questionRows = workflow.locator('.question-row');
	await expect(questionRows).toHaveCount(3);
	await workflow.getByRole('button', { name: 'Add question' }).click();
	await workflow.getByRole('button', { name: 'Add question' }).click();
	await expect(questionRows).toHaveCount(5);

	await questionRows.nth(0).getByLabel('Question text').fill('Which strain area matters most?');
	await questionRows.nth(0).getByLabel('Answer format').selectOption('single');
	await questionRows.nth(0).locator('summary').filter({ hasText: 'Answer options' }).click();
	await questionRows.nth(0).getByRole('textbox', { name: /Options/ }).fill('Workload\nPosture');

	await questionRows.nth(1).getByRole('button').first().click();
	await questionRows.nth(1).getByLabel('Question text').fill('Which support gaps apply?');
	await questionRows.nth(1).getByLabel('Answer format').selectOption('multi');
	await questionRows.nth(1).locator('summary').filter({ hasText: 'Answer options' }).click();
	await questionRows.nth(1).getByRole('textbox', { name: /Options/ }).fill('Breaks\nEquipment');

	await questionRows.nth(2).getByRole('button').first().click();
	await questionRows.nth(2).getByLabel('Question text').fill('What context should the consultant know?');
	await questionRows.nth(2).getByLabel('Answer format').selectOption('text');
	await questionRows.nth(2).locator('summary').filter({ hasText: 'Text response rules' }).click();
	await questionRows.nth(2).getByLabel('Long text answer').check();
	await questionRows.nth(2).getByLabel('Max characters').fill('500');

	await questionRows.nth(3).getByRole('button').first().click();
	await questionRows.nth(3).getByLabel('Question text').fill('How many strain hours happened this week?');
	await questionRows.nth(3).getByLabel('Answer format').selectOption('number');
	await questionRows.nth(3).locator('summary').filter({ hasText: 'Number rules' }).click();
	await questionRows.nth(3).getByLabel('Minimum').fill('0');
	await questionRows.nth(3).getByLabel('Maximum').fill('80');
	await questionRows.nth(3).getByLabel('Unit label').fill('hours/week');
	await questionRows.nth(3).getByLabel('Whole numbers only').check();

	await questionRows.nth(4).getByRole('button').first().click();
	await questionRows.nth(4).getByLabel('Question text').fill('When did the issue start?');
	await questionRows.nth(4).getByLabel('Answer format').selectOption('date');
	await questionRows.nth(4).locator('summary').filter({ hasText: 'Date rules' }).click();
	await questionRows.nth(4).getByLabel('Earliest date').fill('2026-01-01');
	await questionRows.nth(4).getByLabel('Latest date').fill('2026-12-31');

	await workflow.getByRole('button', { name: 'Save questionnaire' }).click();
	await expect.poll(() => templateBodies).toHaveLength(1);
	expect(templateBodies[0].instrumentId).toBe(createdInstrumentId);
	expect(templateBodies[0].questions.map((question) => [question.code, question.type])).toEqual([
		['q01', 'single'],
		['q02', 'multi'],
		['q03', 'text'],
		['q04', 'number'],
		['q05', 'date']
	]);

	const payloadByCode = Object.fromEntries(
		templateBodies[0].questions.map((question) => [question.code, JSON.parse(question.payload)])
	);
	expect(payloadByCode.q01.options).toEqual([
		{ code: 'o01', label: 'Workload' },
		{ code: 'o02', label: 'Posture' }
	]);
	expect(payloadByCode.q02.options).toEqual([
		{ code: 'o01', label: 'Breaks' },
		{ code: 'o02', label: 'Equipment' }
	]);
	expect(payloadByCode.q03.text).toEqual({ multiline: true, maxLength: 500 });
	expect(payloadByCode.q04).toMatchObject({
		validation: { min: 0, max: 80, integerOnly: true },
		display: { unit: 'hours/week' }
	});
	expect(payloadByCode.q05).toMatchObject({
		validation: { minDate: '2026-01-01', maxDate: '2026-12-31' }
	});
});

test('setup template authoring saves single-choice option scoring', async ({ page }) => {
	const templateBodies: Array<{
		questions: Array<{
			ordinal: number;
			code: string;
			type: string;
			payload: string;
		}>;
	}> = [];
	const scoringBodies: Array<{ document: string; produces: string }> = [];
	const createdInstrumentId = '7e4d44f0-4b2a-472a-9e0c-57218f49bcb9';

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: emptySetupWorkspace });
	});

	await page.route('**/instruments/private-imports', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			status: 201,
			json: {
				id: createdInstrumentId,
				code: 'tenant-choice-pulse',
				version: '1.0.0',
				fullName: 'Tenant choice pulse',
				validityStatus: 'private_import',
				rightsStatus: 'attested_by_tenant'
			}
		});
	});

	await page.route('**/template-versions', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		templateBodies.push(route.request().postDataJSON());
		await route.fulfill({
			status: 201,
			json: {
				...sampleTemplateVersion,
				instrumentId: createdInstrumentId,
				questions: templateBodies.at(-1)?.questions ?? []
			}
		});
	});

	await page.route('**/scoring-rules', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		scoringBodies.push(route.request().postDataJSON());
		await route.fulfill({
			status: 201,
			json: { id: '716b2246-70f7-4728-9f44-150bd3b8da7a' }
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	const questionRows = workflow.locator('.question-row');
	await expect(questionRows).toHaveCount(3);

	await questionRows.nth(0).getByLabel('Answer format').selectOption('single');
	await questionRows.nth(0).locator('summary').filter({ hasText: 'Answer options' }).click();
	await questionRows.nth(0).getByRole('textbox', { name: /Options/ }).fill('Low\nSome\nHigh');
	await questionRows.nth(0).getByLabel('Score this single-choice question').check();
	await questionRows.nth(0).getByRole('spinbutton', { name: /Low o01/ }).fill('0');
	await questionRows.nth(0).getByRole('spinbutton', { name: /Some o02/ }).fill('2');
	await questionRows.nth(0).getByRole('spinbutton', { name: /High o03/ }).fill('4');

	await workflow.getByRole('button', { name: 'Save questionnaire' }).click();
	await expect.poll(() => templateBodies).toHaveLength(1);
	expect(templateBodies[0].questions[0]).toMatchObject({
		ordinal: 1,
		code: 'q01',
		type: 'single'
	});
	expect(JSON.parse(templateBodies[0].questions[0].payload)).toMatchObject({
		choiceScoring: {
			enabled: true,
			optionScores: [
				{ code: 'o01', score: 0 },
				{ code: 'o02', score: 2 },
				{ code: 'o03', score: 4 }
			]
		}
	});

	await expect(workflow).toContainText('Review results setup');
	await expect(workflow).toContainText('Score-mapped single choice: Low, Some, High');
	await workflow.getByLabel('Write the second question for this study.').uncheck();
	await workflow.getByLabel('Write the third question for this study.').uncheck();
	await workflow.getByRole('button', { name: 'Save results setup' }).click();
	await expect.poll(() => scoringBodies).toHaveLength(1);
	const submittedScoringDocument = JSON.parse(scoringBodies[0].document) as {
		nodes: Array<{
			id: string;
			op: string;
			option_scores?: Record<string, Record<string, number>>;
		}>;
	};
	expect(submittedScoringDocument.nodes.find((node) => node.id === 'total_answers')).toMatchObject({
		op: 'map_choice_scores',
		option_scores: {
			q01: { o01: 0, o02: 2, o03: 4 }
		}
	});
	expect(JSON.parse(scoringBodies[0].produces)).toEqual({ scores: ['total'] });
});

test('setup template authoring saves conditional display logic and scoring warnings', async ({
	page
}) => {
	const templateBodies: Array<{
		questions: Array<{
			ordinal: number;
			code: string;
			type: string;
			payload: string;
		}>;
	}> = [];
	const createdInstrumentId = '7e4d44f0-4b2a-472a-9e0c-57218f49bcb9';

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: emptySetupWorkspace });
	});

	await page.route('**/instruments/private-imports', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			status: 201,
			json: {
				id: createdInstrumentId,
				code: 'tenant-burnout-pulse',
				version: '1.0.0',
				fullName: 'Tenant burnout pulse',
				validityStatus: 'private_import',
				rightsStatus: 'attested_by_tenant'
			}
		});
	});

	await page.route('**/template-versions', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		templateBodies.push(route.request().postDataJSON());
		await route.fulfill({
			status: 201,
			json: {
				...sampleTemplateVersion,
				instrumentId: createdInstrumentId,
				questions: templateBodies.at(-1)?.questions ?? []
			}
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	const questionRows = workflow.locator('.question-row');
	await expect(questionRows).toHaveCount(3);

	await questionRows.nth(0).getByLabel('Answer format').selectOption('multi');
	await questionRows.nth(1).getByRole('button').first().click();
	await questionRows.nth(1).locator('summary').filter({ hasText: 'Display rule' }).click();
	await questionRows
		.nth(1)
		.getByLabel('Show this question only after a specific answer')
		.check();
	await expect(questionRows.nth(1).getByLabel('Source question')).toHaveValue('q01');
	await expect(questionRows.nth(1).getByLabel('Condition')).toHaveValue('contains');
	await questionRows.nth(1).getByLabel('Source answer').selectOption('o02');
	await expect(questionRows.nth(1)).toContainText(
		'Hidden follow-up questions are saved as skipped answers and are required only when visible.'
	);

	await workflow.getByRole('button', { name: 'Save questionnaire' }).click();
	await expect.poll(() => templateBodies).toHaveLength(1);
	expect(templateBodies[0].questions[1]).toMatchObject({
		ordinal: 2,
		code: 'q02',
		type: 'likert'
	});
	expect(JSON.parse(templateBodies[0].questions[1].payload)).toMatchObject({
		displayLogic: {
			mode: 'show_when',
			sourceQuestionCode: 'q01',
			operator: 'contains',
			value: 'o02',
			requiredWhenVisible: true
		}
	});

	await expect(workflow).toContainText('Review results setup');
	const scoringPlan = workflow
		.getByRole('region', { name: 'Review results setup' })
		.locator('.record-row')
		.filter({ hasText: 'Scoring plan preview' })
		.last();
	await expect(scoringPlan).toContainText(
		'Requires every selected question; includes 1 conditional question that may be hidden and saved as skipped'
	);
	await expect(workflow).toContainText(
		'1 selected scored question is conditional. Hidden conditional answers are saved as skipped; use a minimum-answered rule unless strict missingness is intended.'
	);
});

test('setup result authoring saves tenant-attested interpretation bands', async ({ page }) => {
	const templateBodies: Array<{
		questions: Array<{
			ordinal: number;
			code: string;
			payload: string;
		}>;
	}> = [];
	const scoringBodies: Array<{ document: string; produces: string }> = [];
	const createdInstrumentId = '7e4d44f0-4b2a-472a-9e0c-57218f49bcb9';

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: emptySetupWorkspace });
	});

	await page.route('**/instruments/private-imports', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			status: 201,
			json: {
				id: createdInstrumentId,
				code: 'tenant-burnout-pulse',
				version: '1.0.0',
				fullName: 'Tenant burnout pulse',
				validityStatus: 'private_import',
				rightsStatus: 'attested_by_tenant'
			}
		});
	});

	await page.route('**/template-versions', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		templateBodies.push(route.request().postDataJSON());
		await route.fulfill({
			status: 201,
			json: {
				...sampleTemplateVersion,
				instrumentId: createdInstrumentId,
				questions: templateBodies.at(-1)?.questions ?? []
			}
		});
	});

	await page.route('**/scoring-rules', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		scoringBodies.push(route.request().postDataJSON());
		await route.fulfill({
			status: 201,
			json: { id: '716b2246-70f7-4728-9f44-150bd3b8da7a' }
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	await workflow.getByRole('button', { name: 'Save questionnaire' }).click();
	await expect.poll(() => templateBodies).toHaveLength(1);

	await expect(workflow).toContainText('Review results setup');
	await workflow.locator('summary').filter({ hasText: 'Result outputs plan' }).click();
	await workflow.getByLabel('Add tenant-defined interpretation bands').check();
	await expect(workflow).toContainText(
		'Optional tenant-defined labels for score ranges. These are not official norms, benchmarks, or validated thresholds.'
	);
	await expect(workflow.getByLabel('Interpretation provenance')).toHaveValue(
		'Tenant-defined internal interpretation bands; not validated and not official.'
	);

	await workflow.getByRole('button', { name: 'Save results setup' }).click();
	await expect.poll(() => scoringBodies).toHaveLength(1);
	const submittedProduces = JSON.parse(scoringBodies[0].produces) as {
		scores: string[];
		interpretation?: {
			status: string;
			source: string;
			provenance: string;
			scores: Record<string, Array<{ code: string; label: string; min: number; max: number }>>;
		};
	};

	expect(submittedProduces).toMatchObject({
		scores: ['total'],
		interpretation: {
			status: 'tenant_attested',
			source: 'tenant_defined',
			provenance: 'Tenant-defined internal interpretation bands; not validated and not official.',
			scores: {
				total: [
					{ code: 'lower', label: 'Lower tenant band', min: 1, max: 2.49 },
					{ code: 'middle', label: 'Middle tenant band', min: 2.5, max: 3.49 },
					{ code: 'higher', label: 'Higher tenant band', min: 3.5, max: 5 }
				]
			}
		}
	});
});

test('setup workflow creates an editable draft from a published questionnaire version', async ({
	page
}) => {
	const draftCreates: Array<{ semver: string }> = [];
	const draftScoringRetires: string[] = [];
	const draftPublishes: string[] = [];
	const setupTemplateSelections: string[] = [];
	let setupWorkspaceRequestCount = 0;
	let setupWorkspace = sampleSetupWorkspace;
	const sectionId = sampleTemplateVersion.sections[0].id;
	const scaleId = sampleTemplateVersion.scales[0].id;
		const publishedTemplateDetail = {
			...sampleTemplateVersion,
			questions: [
			{
				id: '1e4b9cad-6d51-42d9-b742-0e5ab56d3520',
				sectionId,
				ordinal: 1,
				code: 'workload_frequency',
				type: 'likert',
				scaleId,
				textDefault: 'How often was workload too high?',
				descriptionDefault: null,
				required: true,
				reverseCoded: false,
				measurementLevel: 'ordinal',
				weight: 1,
				variableLabel: 'Workload frequency',
				payload: JSON.stringify({
					scale: { min: 1, max: 5, lowLabel: 'Never', highLabel: 'Always' }
				}),
				missingCodes: '[]'
				}
			]
		};
		const recoveryPublishedTemplateVersionId = '6ef1ed57-4e87-4d78-a861-a1e39ed0e785';
		const recoveryPublishedTemplateDetail = {
			...publishedTemplateDetail,
			templateVersionId: recoveryPublishedTemplateVersionId,
			semver: '1.0.1'
		};
		const historyAnchorTemplateVersionIds = [
			publishedTemplateDetail.templateVersionId,
			recoveryPublishedTemplateVersionId
		];

		await page.route('**/template-versions/**', async (route) => {
			const url = route.request().url();
			if (
				historyAnchorTemplateVersionIds.some((templateVersionId) =>
					isProductApiPath(url, `/template-versions/${templateVersionId}/versions`)
				)
			) {
				await route.fulfill({
					json: {
						templateId: publishedTemplateDetail.templateId,
					anchorTemplateVersionId: publishedTemplateDetail.templateVersionId,
					versions: [
						{
							templateVersionId: publishedTemplateDetail.templateVersionId,
							semver: '1.0.0',
							status: 'published',
							isLocked: true,
							isGlobal: false,
								createdAt: '2026-06-12T10:00:00Z',
								publishedAt: '2026-06-12T10:10:00Z',
								publishedBy: sampleSessionUserId
							},
							{
								templateVersionId: recoveryPublishedTemplateVersionId,
								semver: '1.0.1',
								status: 'published',
								isLocked: true,
								isGlobal: false,
								createdAt: '2026-06-12T10:15:00Z',
								publishedAt: '2026-06-12T10:16:00Z',
								publishedBy: sampleSessionUserId
							},
							{
								templateVersionId: '9f13e337-3bf9-405c-b3ec-2c9df1e92d5d',
								semver: '1.1.0',
							status: 'draft',
							isLocked: false,
							isGlobal: false,
							createdAt: '2026-06-12T10:20:00Z',
							publishedAt: null,
							publishedBy: null
						}
					]
				}
			});
			return;
		}

			if (
				route.request().method() === 'POST' &&
				historyAnchorTemplateVersionIds.some((templateVersionId) =>
					isProductApiPath(url, `/template-versions/${templateVersionId}/drafts`)
				)
			) {
				draftCreates.push(route.request().postDataJSON());
				await route.fulfill({
				status: 201,
				json: {
					...publishedTemplateDetail,
					templateVersionId: 'd9f8c7ef-71a7-4664-8db2-d0a4f0a00e18',
					semver: draftCreates.at(-1)?.semver ?? '1.2.0',
					status: 'draft'
				}
			});
			return;
		}

		if (
			route.request().method() === 'POST' &&
			isProductApiPath(
				url,
				'/template-versions/d9f8c7ef-71a7-4664-8db2-d0a4f0a00e18/draft-scoring/retire'
			)
		) {
			draftScoringRetires.push('d9f8c7ef-71a7-4664-8db2-d0a4f0a00e18');
			await route.fulfill({
				json: {
					templateVersionId: 'd9f8c7ef-71a7-4664-8db2-d0a4f0a00e18',
					retiredScoringRuleCount: 1
				}
			});
			return;
		}

		if (
			route.request().method() === 'POST' &&
			isProductApiPath(
				url,
				'/template-versions/d9f8c7ef-71a7-4664-8db2-d0a4f0a00e18/publish'
			)
		) {
			draftPublishes.push('d9f8c7ef-71a7-4664-8db2-d0a4f0a00e18');
			await route.fulfill({
				json: {
					...publishedTemplateDetail,
					templateVersionId: 'd9f8c7ef-71a7-4664-8db2-d0a4f0a00e18',
					semver: '1.2.0',
					status: 'published'
				}
			});
				return;
			}

			if (
				route.request().method() === 'GET' &&
				isProductApiPath(url, `/template-versions/${publishedTemplateDetail.templateVersionId}`)
			) {
				await route.fulfill({ json: publishedTemplateDetail });
				return;
			}

			if (
				route.request().method() === 'GET' &&
				isProductApiPath(url, `/template-versions/${recoveryPublishedTemplateVersionId}`)
			) {
				await route.fulfill({ json: recoveryPublishedTemplateDetail });
				return;
			}

			await route.fallback();
		});

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		setupWorkspaceRequestCount += 1;
		await route.fulfill({ json: setupWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-template`, async (route) => {
		if (
			route.request().method() === 'PUT' &&
			isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-template`)
		) {
			const body = route.request().postDataJSON();
			const selectedVersion =
				body.templateVersionId === recoveryPublishedTemplateVersionId
					? recoveryPublishedTemplateDetail
					: {
							...publishedTemplateDetail,
							templateVersionId: body.templateVersionId,
							semver: body.templateVersionId === 'd9f8c7ef-71a7-4664-8db2-d0a4f0a00e18' ? '1.2.0' : '1.0.0',
							status: 'published'
						};
			setupTemplateSelections.push(body.templateVersionId);
			setupWorkspace = {
				...sampleSetupWorkspace,
				selectedCampaign: sampleSetupWorkspace.selectedCampaign
					? {
							...sampleSetupWorkspace.selectedCampaign,
							templateVersionId: body.templateVersionId
						}
					: null,
				template: sampleSetupWorkspace.template
					? {
							...sampleSetupWorkspace.template,
							templateVersionId: selectedVersion.templateVersionId,
							semver: selectedVersion.semver,
							status: selectedVersion.status,
							questionCount: selectedVersion.questions.length
						}
					: null,
				scoring: null,
				campaigns: sampleSetupWorkspace.campaigns.map((campaign) => ({
					...campaign,
					templateVersionId: body.templateVersionId
				}))
			};
			await route.fulfill({
				json: {
					campaignSeriesId: sampleSeriesId,
					templateVersionId: body.templateVersionId
				}
			});
			return;
		}

		await route.fallback();
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	await workflow.getByRole('button', { name: /Build questionnaire/ }).click();
	await expect(workflow.getByText('Edit through a draft version')).toBeVisible();

		await workflow.getByRole('button', { name: 'Load versions' }).click();
		await expect(workflow.getByText('Version 1.0.1')).toBeVisible();
		await workflow.getByRole('button', { name: 'Use this published version' }).click();
		await expect.poll(() => setupTemplateSelections).toEqual([recoveryPublishedTemplateVersionId]);
		await expect(workflow.getByRole('button', { name: 'Save results setup' })).toBeVisible();
		await expect.poll(() => setupWorkspaceRequestCount).toBeGreaterThanOrEqual(2);
		await page.reload();
		await expect(workflow.getByRole('button', { name: 'Save results setup' })).toBeVisible();

		await workflow.getByRole('button', { name: /Build questionnaire/ }).click();
		await workflow.getByRole('button', { name: 'Load versions' }).click();
		await expect(workflow.getByText('Version 1.1.0')).toBeVisible();
		await workflow.getByLabel('New draft version').fill('1.2.0');
		await workflow.getByRole('button', { name: 'Create editable draft' }).click();

	await expect.poll(() => draftCreates).toEqual([{ semver: '1.2.0' }]);
	await expect(workflow.getByText('Draft questionnaire', { exact: true })).toBeVisible();
	await expect(workflow.getByRole('button', { name: 'Save draft questionnaire' })).toBeVisible();
	await workflow.getByRole('button', { name: 'Retire draft result setup' }).click();
	await expect.poll(() => draftScoringRetires).toEqual([
		'd9f8c7ef-71a7-4664-8db2-d0a4f0a00e18'
	]);
	await expect(workflow.getByText('1 draft result setup retired.')).toBeVisible();
	await workflow.getByRole('button', { name: 'Publish saved draft' }).click();
	await expect.poll(() => draftPublishes).toEqual([
		'd9f8c7ef-71a7-4664-8db2-d0a4f0a00e18'
	]);
		await expect.poll(() => setupTemplateSelections).toEqual([
			recoveryPublishedTemplateVersionId,
			'd9f8c7ef-71a7-4664-8db2-d0a4f0a00e18'
		]);
	await expect(workflow.getByRole('button', { name: 'Save results setup' })).toBeVisible();
});

test('setup version-history hydration preserves section and dimension labels', async ({
	page
}) => {
	const draftTemplateVersionId = '75ec89c4-c0c5-4d91-9bf8-c0d24702b4f8';
	const sectionIds = {
		opening: '734cad8a-f64f-4474-8289-bd9d128e0170',
		recovery: '3ef73d14-74df-471f-8a79-8ff889ae5086'
	};
	const scaleId = sampleTemplateVersion.scales[0].id;
	const draftTemplateDetail = {
		...sampleTemplateVersion,
		templateVersionId: draftTemplateVersionId,
		semver: '1.1.0',
		status: 'draft',
		sections: [
			{
				id: sectionIds.opening,
				ordinal: 1,
				code: 'opening_page',
				titleDefault: 'Opening page'
			},
			{
				id: sectionIds.recovery,
				ordinal: 2,
				code: 'recovery_page',
				titleDefault: 'Recovery page'
			}
		],
		questions: [
			{
				id: '7b8306d4-b973-4825-8610-93fe8329d8b8',
				sectionId: sectionIds.opening,
				ordinal: 1,
				code: 'work_intensity',
				type: 'likert',
				scaleId,
				textDefault: 'Work pressure was high during the last two weeks.',
				descriptionDefault: null,
				required: true,
				reverseCoded: false,
				measurementLevel: 'ordinal',
				weight: 1,
				variableLabel: 'Work intensity',
				payload: JSON.stringify({
					scale: { min: 1, max: 5, lowLabel: 'Very low', highLabel: 'Very high' },
					authoring: { dimensionLabel: 'Work intensity' }
				}),
				missingCodes: '[]'
			},
			{
				id: '804584a4-b7c7-4777-8d80-b3f973f62de0',
				sectionId: sectionIds.recovery,
				ordinal: 2,
				code: 'recovery_capacity',
				type: 'likert',
				scaleId,
				textDefault: 'I had enough recovery time after difficult work.',
				descriptionDefault: null,
				required: true,
				reverseCoded: true,
				measurementLevel: 'ordinal',
				weight: 1,
				variableLabel: 'Recovery capacity',
				payload: JSON.stringify({
					scale: { min: 1, max: 5, lowLabel: 'Strongly disagree', highLabel: 'Strongly agree' },
					authoring: { dimensionLabel: 'Recovery capacity' }
				}),
				missingCodes: '[]'
			}
		]
	};

	await page.route('**/template-versions/**', async (route) => {
		const url = route.request().url();
		if (isProductApiPath(url, `/template-versions/${sampleTemplateVersion.templateVersionId}/versions`)) {
			await route.fulfill({
				json: {
					templateId: sampleTemplateVersion.templateId,
					anchorTemplateVersionId: sampleTemplateVersion.templateVersionId,
					versions: [
						{
							templateVersionId: sampleTemplateVersion.templateVersionId,
							semver: '1.0.0',
							status: 'published',
							isLocked: true,
							isGlobal: false,
							createdAt: '2026-06-12T10:00:00Z',
							publishedAt: '2026-06-12T10:10:00Z',
							publishedBy: sampleSessionUserId
						},
						{
							templateVersionId: draftTemplateVersionId,
							semver: '1.1.0',
							status: 'draft',
							isLocked: false,
							isGlobal: false,
							createdAt: '2026-06-13T10:00:00Z',
							publishedAt: null,
							publishedBy: null
						}
					]
				}
			});
			return;
		}

		if (
			route.request().method() === 'GET' &&
			isProductApiPath(url, `/template-versions/${sampleTemplateVersion.templateVersionId}`)
		) {
			await route.fulfill({ json: sampleTemplateVersion });
			return;
		}

		if (
			route.request().method() === 'GET' &&
			isProductApiPath(url, `/template-versions/${draftTemplateVersionId}`)
		) {
			await route.fulfill({ json: draftTemplateDetail });
			return;
		}

		await route.fallback();
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	await workflow.getByRole('button', { name: /Build questionnaire/ }).click();
	await workflow.getByRole('button', { name: 'Load versions' }).click();
	await expect(workflow.getByText('Version 1.1.0')).toBeVisible();
	await workflow.getByRole('button', { name: 'Edit this draft' }).click();

	await expect(workflow.getByText('Draft questionnaire', { exact: true })).toBeVisible();
	const questionRows = workflow.locator('.question-row');
	await expect(questionRows).toHaveCount(2);
	await expect(questionRows.nth(0)).toContainText('Opening page - Work intensity');
	await expect(questionRows.nth(1)).toContainText('Recovery page - Recovery capacity');

	await expect(questionRows.nth(0).getByLabel('Section / page')).toHaveValue('Opening page');
	await expect(questionRows.nth(0).getByLabel('Dimension / construct')).toHaveValue(
		'Work intensity'
	);
	await questionRows.nth(1).getByRole('button').first().click();
	await expect(questionRows.nth(1).getByLabel('Section / page')).toHaveValue('Recovery page');
	await expect(questionRows.nth(1).getByLabel('Dimension / construct')).toHaveValue(
		'Recovery capacity'
	);

	await workflow.locator('summary').filter({ hasText: 'Respondent preview' }).click();
	await expect(workflow.getByText('Question 1 - Opening page - Work intensity')).toBeVisible();
	await expect(workflow.getByText('Question 2 - Recovery page - Recovery capacity')).toBeVisible();

	await workflow.getByRole('button', { name: /Review results setup/ }).click();
	const scoringPlan = workflow
		.getByRole('region', { name: 'Review results setup' })
		.locator('.record-row')
		.filter({ hasText: 'Scoring plan preview' })
		.last();
	await expect(scoringPlan).toContainText(
		'Uses Work intensity, Recovery capacity from 2 selected questions.'
	);
	await expect(scoringPlan).not.toContainText('Opening page');
	await expect(scoringPlan).not.toContainText('Recovery page');
});

test('setup workflow previews respondent-rule audience from the selected campaign', async ({
	page
}) => {
	const previewBodies: unknown[] = [];
	const selectedCampaignId = sampleSetupWorkspace.selectedCampaign?.id ?? '';

	await page.route(
		`**/campaign-series/${sampleSeriesId}/campaigns/${selectedCampaignId}/respondent-rule-preview`,
		async (route) => {
			if (
				!isProductApiPath(
					route.request().url(),
					`/campaign-series/${sampleSeriesId}/campaigns/${selectedCampaignId}/respondent-rule-preview`
				)
			) {
				await route.fallback();
				return;
			}

			previewBodies.push(route.request().postDataJSON());
			await route.fulfill({
				json: sampleRespondentRulePreview
			});
		}
	);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const preview = setup.getByRole('region', { name: 'Preview recipients, then save the selection' });
	await expect(preview).toBeVisible();
	await preview.getByLabel('Send invitations to').selectOption('manager_of_target');
	await preview.getByLabel('Focus person').selectOption(sampleSubjectDirectory.subjects[0].id);
	await preview.getByRole('button', { name: 'Preview recipients' }).click();

	await expect.poll(() => previewBodies).toHaveLength(1);
	expect(previewBodies[0]).toMatchObject({
		targetSubjectId: sampleSubjectDirectory.subjects[0].id,
		groupId: null,
		maxRows: 25
	});
	expect(JSON.parse((previewBodies[0] as { rule: string }).rule)).toEqual({
		kind: 'manager_of_target',
		role: 'manager',
		target_subject_id: sampleSubjectDirectory.subjects[0].id
	});
	await expect(preview.getByText('Recipients found', { exact: true })).toBeVisible();
	await expect(preview.getByText('Ana Analyst to Mira Manager', { exact: true })).toBeVisible();
	await expect(preview.getByText('mira@example.test', { exact: true })).toBeVisible();
	const previewText = await preview.textContent();
	expect(previewText).not.toMatch(/manager_of_target|respondent rule|target subject/i);
});

test('setup workflow saves respondent rules and shows safe assignments', async ({ page }) => {
	const selectedCampaignId = sampleSetupWorkspace.selectedCampaign?.id ?? '';
	const saveBodies: unknown[] = [];
	let savedRules: CampaignRespondentRuleListResponse = {
		campaignId: selectedCampaignId,
		rules: []
	};
	const savedRuleAfterPut: CampaignRespondentRuleListResponse = {
		campaignId: selectedCampaignId,
		rules: [
			{
				id: 'respondent-rule-id',
				ordinal: 1,
				rule: JSON.stringify({
					kind: 'manager_of_target',
					role: 'manager',
					target_subject_id: sampleSubjectDirectory.subjects[0].id
				}),
				ruleKind: 'manager_of_target',
				role: 'manager',
				targetSubjectId: sampleSubjectDirectory.subjects[0].id,
				groupId: null,
				assignmentPairCount: 1,
				issues: []
			}
		]
	};

	await page.route('**/campaigns/*/respondent-rules', async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaigns/${selectedCampaignId}/respondent-rules`)
		) {
			await route.fallback();
			return;
		}

		if (route.request().method() === 'GET') {
			await route.fulfill({ json: savedRules });
			return;
		}

		if (route.request().method() === 'PUT') {
			saveBodies.push(route.request().postDataJSON());
			savedRules = savedRuleAfterPut;
			await route.fulfill({ json: savedRules });
			return;
		}

		await route.fallback();
	});

	await page.route('**/campaigns/*/assignments', async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaigns/${selectedCampaignId}/assignments`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				campaignId: selectedCampaignId,
				assignmentCount: 1,
				assignments: [
					{
						id: 'assignment-id',
						role: 'manager',
						status: 'pending',
						anonymous: false,
						targetSubjectId: sampleSubjectDirectory.subjects[0].id,
						target: {
							id: sampleSubjectDirectory.subjects[0].id,
							label: 'Ana Analyst',
							displayName: 'Ana Analyst',
							email: 'ana@example.test',
							externalId: 'emp-001'
						},
						respondentSubjectId: sampleSubjectDirectory.subjects[1].id,
						respondent: {
							id: sampleSubjectDirectory.subjects[1].id,
							label: 'Mira Manager',
							displayName: 'Mira Manager',
							email: 'mira@example.test',
							externalId: 'mgr-001'
						},
						dueAt: null,
						createdAt: '2026-05-15T12:00:00Z'
					}
				]
			}
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const preview = setup.getByRole('region', { name: 'Preview recipients, then save the selection' });
	await preview.getByLabel('Send invitations to').selectOption('manager_of_target');
	await preview.getByLabel('Focus person').selectOption(sampleSubjectDirectory.subjects[0].id);
	await preview.getByRole('button', { name: 'Save previewed recipients' }).click();

	await expect.poll(() => saveBodies).toHaveLength(1);
	expect(saveBodies[0]).toEqual({
		rules: [
			{
				rule: JSON.stringify({
					kind: 'manager_of_target',
					role: 'manager',
					target_subject_id: sampleSubjectDirectory.subjects[0].id
				})
			}
		]
	});

	const savedSelection = setup.getByRole('region', { name: 'Saved recipient selection' });
	await expect(savedSelection.getByText('Managers of selected people', { exact: true })).toBeVisible();
	await expect(savedSelection.getByText('Ana Analyst', { exact: true })).toBeVisible();
	await expect(savedSelection.getByText('1 invitation pair', { exact: true })).toBeVisible();
	const savedSelectionText = await savedSelection.textContent();
	expect(savedSelectionText).not.toMatch(/manager_of_target|respondent rule/i);

	const assignments = setup.getByRole('region', { name: 'Prepared invitation roster' });
	await expect(assignments.getByText('1 invitation', { exact: true })).toBeVisible();
	await expect(assignments.getByText('Ana Analyst to Mira Manager', { exact: true })).toBeVisible();
	await expect(assignments.getByText('pending', { exact: true })).toBeVisible();

	const assignmentText = await assignments.textContent();
	expect(assignmentText).not.toMatch(/token|hash|recipient|answer/i);
});

test('setup workflow creates a campaign draft from setup-workspace state and refreshes', async ({
	page
}) => {
	const campaignCreates: Array<{ campaignSeriesId: string | null; templateVersionId: string }> = [];
	let setupWorkspaceRequestCount = 0;

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		setupWorkspaceRequestCount += 1;
		await route.fulfill({
			json:
				setupWorkspaceRequestCount === 1
					? campaignDraftSetupWorkspace
					: {
							...campaignDraftSetupWorkspace,
							summary: {
								...campaignDraftSetupWorkspace.summary,
								campaignCount: 1
							}
						}
		});
	});
	await routeSetupProofDependencies(page, {
		onCreateCampaign: (body) => campaignCreates.push(body)
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);
	await page.reload();

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	const resultsStep = workflow.getByRole('button', { name: /Review results setup/ });
	await expect(resultsStep).toContainText('Done');
	await resultsStep.click();
	await expect(workflow.getByText('Result outputs ready')).toBeVisible();
	await workflow.getByRole('button', { name: /Measurement and recipients/ }).click();
	await expect(setup.getByRole('button', { name: 'Save measurement' })).toBeEnabled();
	await setup.getByRole('button', { name: 'Save measurement' }).last().click();

	await expect.poll(() => campaignCreates).toHaveLength(1);
	expect(campaignCreates[0]).toMatchObject({
		campaignSeriesId: sampleSeriesId,
		templateVersionId: campaignDraftSetupWorkspace.template?.templateVersionId ?? ''
	});
	await expect.poll(() => setupWorkspaceRequestCount).toBeGreaterThanOrEqual(2);
	await expect(setup.getByRole('group', { name: 'Protocol progress' })).toBeVisible();
	await expect(setup.getByText('1', { exact: true }).first()).toBeVisible();
});

test('setup workflow creates a future measurement from selected template after locked campaign', async ({
	page
}) => {
	const futureTemplateVersionId = '72374a74-75e5-4783-9c3a-d087861f58a8';
	const futureScoringRuleId = '23c84d64-cf42-4af4-beb1-d99e75888517';
	const setupTemplateSelections: string[] = [];
	const campaignCreates: Array<{ campaignSeriesId: string | null; templateVersionId: string }> = [];
	let setupWorkspaceRequestCount = 0;
	const lockedCampaign = {
		...sampleSetupWorkspace.selectedCampaign!,
		status: 'live',
		latestLaunchAt: '2026-06-13T09:00:00Z'
	};
	const lockedCampaignRow = {
		...sampleSetupWorkspace.campaigns[0],
		status: 'live',
		latestLaunchAt: '2026-06-13T09:00:00Z'
	};
	const futureTemplateDetail = {
		...sampleTemplateVersion,
		templateVersionId: futureTemplateVersionId,
		semver: '1.0.1',
		questions: [
			{
				id: 'b49bcdf8-6a3c-4b7a-a84f-f827f88ef911',
				sectionId: sampleTemplateVersion.sections[0].id,
				ordinal: 1,
				code: 'future_workload',
				type: 'likert',
				scaleId: sampleTemplateVersion.scales[0].id,
				textDefault: 'The next measurement should use the updated workload item.',
				descriptionDefault: null,
				required: true,
				reverseCoded: false,
				measurementLevel: 'ordinal',
				weight: 1,
				variableLabel: 'Future workload',
				payload: '{}',
				missingCodes: '[]'
			}
		]
	};
	const lockedSetupWorkspace: CampaignSeriesSetupWorkspaceResponse = {
		...sampleSetupWorkspace,
		summary: {
			campaignCount: 1,
			liveCampaignCount: 1,
			missingPrerequisiteCount: 1
		},
		selectedCampaign: lockedCampaign,
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
			}
		],
		campaigns: [lockedCampaignRow]
	};
	const futureSetupWorkspace: CampaignSeriesSetupWorkspaceResponse = {
		...lockedSetupWorkspace,
		template: lockedSetupWorkspace.template
			? {
					...lockedSetupWorkspace.template,
					templateVersionId: futureTemplateVersionId,
					semver: '1.0.1',
					questionCount: futureTemplateDetail.questions.length
				}
			: null,
		scoring: lockedSetupWorkspace.scoring
			? {
					...lockedSetupWorkspace.scoring,
					id: futureScoringRuleId,
					templateVersionId: futureTemplateVersionId,
					ruleKey: 'burnout.future'
				}
			: null
	};
	let setupWorkspace = lockedSetupWorkspace;

	await page.route('**/template-versions/**', async (route) => {
		const url = route.request().url();
		if (
			isProductApiPath(url, `/template-versions/${sampleTemplateVersion.templateVersionId}/versions`) ||
			isProductApiPath(url, `/template-versions/${futureTemplateVersionId}/versions`)
		) {
			await route.fulfill({
				json: {
					templateId: sampleTemplateVersion.templateId,
					anchorTemplateVersionId: sampleTemplateVersion.templateVersionId,
					versions: [
						{
							templateVersionId: sampleTemplateVersion.templateVersionId,
							semver: '1.0.0',
							status: 'published',
							isLocked: true,
							isGlobal: false,
							createdAt: '2026-06-12T10:00:00Z',
							publishedAt: '2026-06-12T10:10:00Z',
							publishedBy: sampleSessionUserId
						},
						{
							templateVersionId: futureTemplateVersionId,
							semver: '1.0.1',
							status: 'published',
							isLocked: true,
							isGlobal: false,
							createdAt: '2026-06-13T09:00:00Z',
							publishedAt: '2026-06-13T09:05:00Z',
							publishedBy: sampleSessionUserId
						}
					]
				}
			});
			return;
		}

		if (
			route.request().method() === 'GET' &&
			isProductApiPath(url, `/template-versions/${sampleTemplateVersion.templateVersionId}`)
		) {
			await route.fulfill({ json: sampleTemplateVersion });
			return;
		}

		if (
			route.request().method() === 'GET' &&
			isProductApiPath(url, `/template-versions/${futureTemplateVersionId}`)
		) {
			await route.fulfill({ json: futureTemplateDetail });
			return;
		}

		await route.fallback();
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		setupWorkspaceRequestCount += 1;
		await route.fulfill({ json: setupWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-template`, async (route) => {
		if (
			route.request().method() === 'PUT' &&
			isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-template`)
		) {
			const body = route.request().postDataJSON() as { templateVersionId: string };
			setupTemplateSelections.push(body.templateVersionId);
			setupWorkspace = futureSetupWorkspace;
			await route.fulfill({
				json: {
					campaignSeriesId: sampleSeriesId,
					templateVersionId: body.templateVersionId
				}
			});
			return;
		}

		await route.fallback();
	});
	await routeSetupProofDependencies(page, {
		onCreateCampaign: (body) => campaignCreates.push(body)
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	const workflow = setup.getByRole('group', { name: 'Protocol progress' });
	await workflow.getByRole('button', { name: /Build questionnaire/ }).click();
	await workflow.getByRole('button', { name: 'Load versions' }).click();
	await expect(workflow.getByText('Version 1.0.1')).toBeVisible();
	await workflow.getByRole('button', { name: 'Use this published version' }).click();
	await expect.poll(() => setupTemplateSelections).toEqual([futureTemplateVersionId]);
	await expect.poll(() => setupWorkspaceRequestCount).toBeGreaterThanOrEqual(2);

	await page.reload();

	const resultsStep = workflow.getByRole('button', { name: /Review results setup/ });
	await expect(resultsStep).toContainText('Done');
	await resultsStep.click();
	await expect(workflow.getByText('Result outputs ready')).toBeVisible();
	await workflow.getByRole('button', { name: /Measurement and recipients/ }).click();
	await expect(workflow.getByLabel('Measurement name')).toHaveValue('Measurement 2');
	await expect(setup.getByRole('button', { name: 'Save measurement' })).toBeEnabled();
	await setup.getByRole('button', { name: 'Save measurement' }).last().click();

	await expect.poll(() => campaignCreates).toHaveLength(1);
	expect(campaignCreates[0]).toMatchObject({
		campaignSeriesId: sampleSeriesId,
		templateVersionId: futureTemplateVersionId
	});
});

test('setup workflow keeps setup state visible when a setup action fails', async ({ page }) => {
	await page.route('**/campaigns/*/launch-readiness', async (route) => {
		await route.fulfill({
			status: 500,
			json: {
				title: 'campaign.readiness_failed',
				detail: 'Launch readiness failed.'
			}
		});
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Protocol workspace' });
	await expect(setup.getByText('Launch readiness failed.')).toBeVisible();
	await expect(setup.getByRole('group', { name: 'Protocol progress' })).toBeVisible();
	await expect(setup.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);
});

test('anchors selected-series campaign draft creation to the route series', async ({ page }) => {
	const campaignCreates: Array<{ campaignSeriesId: string | null }> = [];
	let unexpectedSeriesCreates = 0;

	await routeSetupProofDependencies(page, {
		onCreateCampaign: (body) => campaignCreates.push(body)
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: campaignDraftSetupWorkspace });
	});

	await page.route('**/campaign-series', async (route) => {
		if (
			isProductApiPath(route.request().url(), '/campaign-series') &&
			route.request().method() === 'POST'
		) {
			unexpectedSeriesCreates += 1;
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_series_create',
					detail: 'Selected route series should be reused.'
				}
			});
			return;
		}

		await route.fallback();
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);
	await page.getByRole('button', { name: 'Save measurement' }).click();

	expect(unexpectedSeriesCreates).toBe(0);
	await expect.poll(() => campaignCreates).toHaveLength(1);
	expect(campaignCreates[0].campaignSeriesId).toBe(sampleSeriesId);
	await expect(page.getByRole('region', { name: 'Protocol workspace' })).toBeVisible();
});

test('renders selected-series not-found state on child routes', async ({ page }) => {
	const missingSeriesId = '7e94ce52-dfc3-4a86-9882-7694ce9b3c93';
	await page.route(`**/campaign-series/${missingSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${missingSeriesId}/setup-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			status: 404,
			json: {
				title: 'campaign_series.not_found',
				detail: 'Campaign series was not found.'
			}
		});
	});

	await page.goto(`/app/campaign-series/${missingSeriesId}/setup`);

	await expect(page.getByRole('alert')).toContainText('Campaign series was not found');
});

test('retries selected-series child route with the route series id', async ({ page }) => {
	const requestedProductPaths: string[] = [];
	let retrySeriesRequestCount = 0;

	await page.route('**/campaign-series/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.startsWith('/campaign-series/')) {
			await route.fallback();
			return;
		}

		requestedProductPaths.push(pathname);

		if (pathname === `/campaign-series/${retrySeriesId}/reports-widget-manifest`) {
			await route.fulfill({
				json: {
					...sampleReportsWidgetManifest,
					campaignSeriesId: retrySeriesId
				}
			});
			return;
		}

		if (pathname !== `/campaign-series/${retrySeriesId}/reports-workspace`) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'unexpected_path',
					detail: `Unexpected campaign series path: ${pathname}`
				}
			});
			return;
		}

		retrySeriesRequestCount += 1;
		if (retrySeriesRequestCount === 1) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'campaign_series.retry',
					detail: 'Temporary campaign series child surface failure.'
				}
			});
			return;
		}

		await route.fulfill({
			json: {
				...sampleReportsWorkspace,
				series: {
					...sampleReportsWorkspace.series,
					id: retrySeriesId,
					name: 'Retryable child pulse'
				}
			}
		});
	});

	await page.goto(`/app/campaign-series/${retrySeriesId}/reports`);
	await expect(page.getByRole('alert')).toContainText(
		'Temporary campaign series child surface failure'
	);

	await page.getByRole('button', { name: 'Retry surface' }).click();

	const reports = page.getByRole('region', { name: 'Evidence workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('heading', { name: 'Review and export results' })).toBeVisible();
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([
			`/campaign-series/${retrySeriesId}/reports-workspace`,
			`/campaign-series/${retrySeriesId}/reports-workspace`,
			`/campaign-series/${retrySeriesId}/reports-widget-manifest`
		]);
});

test('refreshes selected-series child route on client-side series id changes', async ({ page }) => {
	let releaseOriginalSeries: () => void = () => {};
	const originalSeriesCanRespond = new Promise<void>((resolve) => {
		releaseOriginalSeries = resolve;
	});

	await page.route(`**/campaign-series/${oldFoundationSeriesId}/waves-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${oldFoundationSeriesId}/waves-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await originalSeriesCanRespond;
		await route.fulfill({
			json: {
				...sampleWavesWorkspace,
				series: {
					...sampleWavesWorkspace.series,
					id: oldFoundationSeriesId,
					name: 'Legacy foundation pulse'
				}
			}
		});
	});

	await page.route(`**/campaign-series/${alternateSeriesId}/waves-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${alternateSeriesId}/waves-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				...sampleWavesWorkspace,
				series: {
					...sampleWavesWorkspace.series,
					id: alternateSeriesId,
					name: 'Retention pulse'
				}
			}
		});
	});

	await page.route(
		`**/campaign-series/${alternateSeriesId}/wave-comparison-proof`,
		async (route) => {
			await route.fulfill({
				json: {
					...sampleWaveComparisonProof,
					campaignSeriesId: alternateSeriesId
				}
			});
		}
	);

	await page.goto(`/app/campaign-series/${oldFoundationSeriesId}/waves`);
	await expect(page.getByRole('status')).toContainText('Loading waves context');

	await page.evaluate((href) => {
		(window as Window & { __seriesChildNavigationMarker?: string }).__seriesChildNavigationMarker =
			'client';
		const link = document.createElement('a');
		link.href = href;
		link.textContent = 'Open alternate waves';
		link.dataset.testid = 'open-alternate-waves';
		document.body.append(link);
	}, `/app/campaign-series/${alternateSeriesId}/waves`);
	await page.getByTestId('open-alternate-waves').click();

	await expect(page).toHaveURL(`/app/campaign-series/${alternateSeriesId}/waves`);
	await expect
		.poll(() =>
			page.evaluate(
				() =>
					(window as Window & { __seriesChildNavigationMarker?: string })
						.__seriesChildNavigationMarker
			)
		)
		.toBe('client');

	const waves = page.getByRole('region', { name: 'Rounds and linked repeat responses' });
	const workflow = waves.getByRole('group', { name: 'Measurement comparison workflow' });
	await expect(workflow).toBeVisible();
	await expect(workflow.getByRole('heading', { name: 'Compare repeated measurements' })).toBeVisible();

	releaseOriginalSeries();
	await expect(waves.getByText('Legacy foundation pulse', { exact: true })).toHaveCount(0);
	await expect(workflow.getByRole('heading', { name: 'Compare repeated measurements' })).toBeVisible();
});

test('keeps demo fixtures out of navigation while direct sample demos stay read-only', async ({
	page
}) => {
	await page.goto('/app/demo');

	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: /^Demo fixtures\b/ })).toHaveCount(0);

	await expect(
		page.getByRole('heading', { name: 'Explore finished sample studies.', exact: true })
	).toBeVisible();
	await expect(page.getByText('Read-only samples', { exact: true })).toBeVisible();
	await expect(page.getByText('Sample data', { exact: true })).toBeVisible();
	const samples = page.getByRole('region', { name: 'Read-only sample study library' });
	await expect(samples).toBeVisible();
	await expect(page.getByText('Demo fixture surfaces are unavailable')).toHaveCount(0);
});

test.skip('renders demo fixtures under internal tools when the local flag is enabled', async ({
	page
}) => {
	test.skip(
		process.env.PUBLIC_DEMO_SURFACES_ENABLED !== 'true',
		'Requires PUBLIC_DEMO_SURFACES_ENABLED=true on the preview server.'
	);

	await page.goto('/app');

	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	const internalTools = nav.getByRole('group', { name: 'Internal tools' });
	await expect(internalTools).toBeVisible();
	await expect(internalTools.getByRole('link', { name: /^Demo fixtures\b/ })).toHaveAttribute(
		'href',
		'/app/demo'
	);
});

async function routeAuthenticatedSession(
	page: Page,
	permissions = ['setup.manage', 'team.manage'],
	email: string | null = sampleSessionEmail
) {
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({
			json: {
				userId: sampleSessionUserId,
				tenantId: sampleSessionTenantId,
				...(email ? { email } : {}),
				permissions
			}
		});
	});
}

async function routeCsrfToken(page: Page) {
	await page.route('**/auth/csrf', async (route) => {
		await route.fulfill({ json: { csrfToken: 'test-csrf-token' } });
	});
}

async function routeProductReadModels(page: Page) {
	let microsoftGraphConnectionState = sampleMicrosoftGraphConnectionState;
	let microsoftGraphImportRules = sampleMicrosoftGraphImportRules;
	let tenantSettings = structuredClone(sampleTenantSettings);
	await page.route('**/workspace-overview', async (route) => {
		if (!isProductApiPath(route.request().url(), '/workspace-overview')) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleWorkspaceOverview });
	});

	await page.route('**/tenant-settings/report-branding', async (route) => {
		if (!isProductApiPath(route.request().url(), '/tenant-settings/report-branding')) {
			await route.fallback();
			return;
		}

		const request = route.request().postDataJSON() as {
			organizationLabel: string;
			reportTitle: string;
			accentColorHex: string;
			layoutVariant: string;
		};
		tenantSettings = {
			...tenantSettings,
			reportBranding: {
				organizationLabel: request.organizationLabel,
				reportTitle: request.reportTitle,
				brandingSource: 'tenant_settings',
				logoMode: 'none',
				accentColorHex: request.accentColorHex,
				layoutVariant: request.layoutVariant,
				deferredCustomizations: ['logo_upload', 'custom_fonts', 'product_shell_theming']
			}
		};
		await route.fulfill({ json: tenantSettings.reportBranding });
	});

	await page.route('**/tenant-settings', async (route) => {
		if (!isProductApiPath(route.request().url(), '/tenant-settings')) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: tenantSettings });
	});

	await page.route('**/export-artifacts', async (route) => {
		if (!isProductApiPath(route.request().url(), '/export-artifacts')) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleExportArtifactLibrary });
	});

	await page.route('**/instruments', async (route) => {
		if (!isProductApiPath(route.request().url(), '/instruments')) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleInstrumentLibrary });
	});

	await page.route('**/tenant-members', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/tenant-members')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleTenantMemberRoster });
	});

	await page.route('**/tenant-roles', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/tenant-roles')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleTenantRoleList });
	});

	await page.route('**/subjects', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/subjects')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleSubjectDirectory });
	});

	await page.route('**/subject-groups', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/subject-groups')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleSubjectGroupList });
	});

	await page.route('**/directory-connections/microsoft-graph', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/directory-connections/microsoft-graph')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: microsoftGraphConnectionState });
	});

	await page.route('**/directory-connections/microsoft-graph/import-runs', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/directory-connections/microsoft-graph/import-runs')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleMicrosoftGraphImportRuns });
	});

	await page.route('**/directory-connections/microsoft-graph/import-rules**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		if (!pathname.includes('/directory-connections/microsoft-graph/import-rules')) {
			await route.fallback();
			return;
		}

		if (route.request().method() === 'POST' && pathname.endsWith('/live-preview')) {
			await route.fulfill({ json: sampleMicrosoftGraphLivePreview });
			return;
		}

		if (route.request().method() === 'POST' && pathname.endsWith('/live-apply')) {
			await route.fulfill({
				json: {
					...sampleMicrosoftGraphLivePreview,
					import: {
						...sampleMicrosoftGraphLivePreview.import,
						dryRun: false,
						importRunId: 'live-apply-run-id'
					}
				}
			});
			return;
		}

		if (route.request().method() === 'GET') {
			await route.fulfill({ json: microsoftGraphImportRules });
			return;
		}

		if (route.request().method() === 'POST') {
			const body = route.request().postDataJSON() as {
				name: string;
				markMissingSubjectsStale?: boolean;
			};
			const rule: DirectoryImportRuleResponse = {
				id: `99999999-9999-4999-8999-${String(microsoftGraphImportRules.rules.length + 1).padStart(12, '9')}`,
				directoryConnectionId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
				name: body.name,
				status: 'active',
				stalePolicy: body.markMissingSubjectsStale ? 'mark_stale' : 'none',
				retainedFields: ['external_id', 'email', 'manager_external_id'],
				createdAt: '2026-06-12T12:30:00+00:00',
				updatedAt: '2026-06-12T12:30:00+00:00'
			};
			microsoftGraphImportRules = {
				...microsoftGraphImportRules,
				rules: [...microsoftGraphImportRules.rules, rule]
			};
			microsoftGraphConnectionState = {
				...microsoftGraphConnectionState,
				status: 'consent_required',
				updatedAt: '2026-06-12T12:30:00+00:00'
			};
			await route.fulfill({ json: rule });
			return;
		}

		if (route.request().method() === 'DELETE') {
			const ruleId = pathname.split('/').at(-1);
			const archived = microsoftGraphImportRules.rules.find((rule) => rule.id === ruleId);
			microsoftGraphImportRules = {
				...microsoftGraphImportRules,
				rules: microsoftGraphImportRules.rules.filter((rule) => rule.id !== ruleId)
			};
			await route.fulfill({
				json: {
					...(archived ?? sampleMicrosoftGraphImportRules.rules[0]),
					status: 'archived',
					updatedAt: '2026-06-12T12:35:00+00:00'
				}
			});
			return;
		}

		await route.fallback();
	});

	await page.route('**/directory-connections/microsoft-graph/consent-requests', async (route) => {
		if (
			route.request().method() !== 'POST' ||
			!isProductApiPath(
				route.request().url(),
				'/directory-connections/microsoft-graph/consent-requests'
			)
		) {
			await route.fallback();
			return;
		}

		microsoftGraphConnectionState = {
			...microsoftGraphConnectionState,
			status: 'pending_consent',
			grantedScopes: sampleMicrosoftGraphConsentRequest.requestedScopes,
			updatedAt: '2026-06-12T12:00:00+00:00'
		};
		await route.fulfill({ json: sampleMicrosoftGraphConsentRequest });
	});

	await page.route('**/directory-connections/microsoft-graph/consent-callback', async (route) => {
		if (
			route.request().method() !== 'POST' ||
			!isProductApiPath(
				route.request().url(),
				'/directory-connections/microsoft-graph/consent-callback'
			)
		) {
			await route.fallback();
			return;
		}

		const body = route.request().postDataJSON() as {
			adminConsent?: boolean;
			microsoftTenantId?: string | null;
			error?: string | null;
		};
		const connected = body.adminConsent === true && !!body.microsoftTenantId && !body.error;
		microsoftGraphConnectionState = {
			...microsoftGraphConnectionState,
			status: connected ? 'active' : 'consent_required',
			displayName: connected ? 'Contoso University' : 'Microsoft Graph',
			primaryDomain: connected ? 'contoso.example' : null,
			grantedScopes: connected ? ['User.Read.All'] : [],
			lastConsentAt: connected ? '2026-06-12T12:05:00+00:00' : null,
			updatedAt: '2026-06-12T12:05:00+00:00',
			connected
		};
		await route.fulfill({
			json: {
				tenantId: sampleSubjectDirectory.tenantId,
				consentRequestId: sampleMicrosoftGraphConsentRequest.consentRequestId,
				directoryConnectionId: sampleMicrosoftGraphConsentRequest.directoryConnectionId,
				provider: 'microsoft_graph',
				status: connected ? 'completed' : 'failed',
				connectionStatus: microsoftGraphConnectionState.status,
				connected
			}
		});
	});

	await page.route('**/campaigns/*/respondent-rules', async (route) => {
		if (route.request().method() !== 'GET') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				rules: []
			}
		});
	});

	await page.route('**/campaigns/*/assignments', async (route) => {
		if (route.request().method() !== 'GET') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				assignmentCount: 0,
				assignments: []
			}
		});
	});

	await page.route('**/campaign-series', async (route) => {
		if (!isProductApiPath(route.request().url(), '/campaign-series')) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleCampaignSeriesList });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}`, async (route) => {
		if (!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}`)) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleCampaignSeriesHub });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/setup-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/setup-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleSetupWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/operations-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/operations-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleOperationsWorkspace });
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/reports-workspace`, async (route) => {
		if (
			!isProductApiPath(
				route.request().url(),
				`/campaign-series/${sampleSeriesId}/reports-workspace`
			)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleReportsWorkspace });
	});

	await page.route(
		`**/campaign-series/${sampleSeriesId}/reports-widget-manifest`,
		async (route) => {
			if (
				!isProductApiPath(
					route.request().url(),
					`/campaign-series/${sampleSeriesId}/reports-widget-manifest`
				)
			) {
				await route.fallback();
				return;
			}

			await route.fulfill({ json: sampleReportsWidgetManifest });
		}
	);

	await page.route(`**/campaign-series/${sampleSeriesId}/waves-workspace`, async (route) => {
		if (
			!isProductApiPath(route.request().url(), `/campaign-series/${sampleSeriesId}/waves-workspace`)
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleWavesWorkspace });
	});
}

async function expectElementBefore(first: Locator, second: Locator) {
	const firstBox = await first.boundingBox();
	const secondBox = await second.boundingBox();

	expect(firstBox, 'first element should be visible and measurable').not.toBeNull();
	expect(secondBox, 'second element should be visible and measurable').not.toBeNull();
	expect(firstBox!.y).toBeLessThan(secondBox!.y);
}

async function expectMetricValue(scope: Locator, label: string, value: string) {
	const metric = scope.locator('.metric-card').filter({ hasText: label });

	await expect(metric.getByText(label, { exact: true })).toBeVisible();
	await expect(metric.getByText(value, { exact: true })).toBeVisible();
}

async function expectFieldValue(scope: Locator, label: string, value: string) {
	const field = scope.locator('.record-field').filter({ hasText: label });

	await expect(field.getByText(label, { exact: true })).toBeVisible();
	await expect(field.getByText(value, { exact: true })).toBeVisible();
}

async function routeSetupProofDependencies(
	page: Page,
	options: {
		onCreateCampaign?: (body: {
			campaignSeriesId: string | null;
			templateVersionId: string;
			responseIdentityMode: string;
		}) => void;
	} = {}
) {
	let campaignCreateCount = 0;

	await page.route('**/template-versions', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		await route.fulfill({ status: 201, json: sampleTemplateVersion });
	});

	await page.route('**/scoring-rules', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		await route.fulfill({
			status: 201,
			json: { id: '716b2246-70f7-4728-9f44-150bd3b8da7a' }
		});
	});

	await page.route('**/campaigns', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		campaignCreateCount += 1;
		const body = route.request().postDataJSON() as {
			campaignSeriesId: string | null;
			name: string;
			responseIdentityMode: string;
			templateVersionId: string;
		};
		options.onCreateCampaign?.(body);
		await route.fulfill({
			status: 201,
			json: {
				id: `00000000-0000-4000-8000-${campaignCreateCount.toString().padStart(12, '0')}`,
				campaignSeriesId: body.campaignSeriesId,
				templateVersionId: body.templateVersionId,
				name: body.name,
				status: 'draft',
				responseIdentityMode: body.responseIdentityMode
			}
		});
	});

	await page.route('**/campaigns/*/launch-readiness', async (route) => {
		await route.fulfill({
			json: { campaignId: campaignIdFromPath(route.request().url()), ready: true, issues: [] }
		});
	});

	await page.route('**/campaigns/*/launch', async (route) => {
		await route.fulfill({
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				status: 'live',
				launchSnapshotId: '7a7d76b5-7da2-4c1d-af6a-5d16d52bd672',
				templateVersionId: sampleTemplateVersion.templateVersionId,
				scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
				retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
				disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
				responseIdentityMode: 'anonymous_longitudinal',
				defaultLocale: 'en',
				launchedAt: '2026-05-08T12:00:00Z'
			}
		});
	});

	await page.route('**/campaigns/*/open-link', async (route) => {
		await route.fulfill({
			status: 201,
			json: {
				campaignId: campaignIdFromPath(route.request().url()),
				assignmentId: '98d34e66-5c5b-424a-939d-3075340f880a',
				token: 'opn_test',
				respondentPath: '/r/opn_test'
			}
		});
	});

	await page.route(`**/campaign-series/${sampleSeriesId}/two-wave-proof`, async (route) => {
		await route.fulfill({
			json: {
				campaignSeriesId: sampleSeriesId,
				proofStatus: 'complete',
				expectedWaveCount: 2,
				launchedWaveCount: 2,
				submittedWaveCount: 0,
				linkedTrajectoryCount: 0,
				completeTrajectoryCount: 0,
				waves: []
			}
		});
	});
}

function isProductApiPath(url: string, path: string) {
	return new URL(url).pathname === path;
}

function campaignIdFromPath(url: string) {
	return new URL(url).pathname.split('/')[2];
}

function seriesIdFromPath(url: string) {
	return new URL(url).pathname.split('/')[2];
}

function artifactIdFromPath(url: string) {
	return new URL(url).pathname.split('/')[2];
}

function createReportableReportsWorkspaceWithoutExports(): CampaignSeriesReportsWorkspaceResponse {
	return {
		...sampleReportsWorkspace,
		summary: {
			...sampleReportsWorkspace.summary,
			exportArtifactCount: 0,
			missingPrerequisiteCount: 0
		},
		selectedCampaign: sampleReportsWorkspace.selectedCampaign
			? {
					...sampleReportsWorkspace.selectedCampaign,
					exportArtifactCount: 0,
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
				}
			: null,
		missingPrerequisites: [],
		exportArtifacts: [],
		campaigns: sampleReportsWorkspace.campaigns.map((campaign) =>
			campaign.id === sampleReportsWorkspace.selectedCampaign?.id
				? {
						...campaign,
						exportArtifactCount: 0,
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
					}
				: campaign
		)
	};
}

function createEmptySetupWorkspace(
	seriesId: string,
	name: string
): CampaignSeriesSetupWorkspaceResponse {
	return {
		series: {
			id: seriesId,
			name,
			...ownSeriesOwnership,
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
			},
			{
				code: 'scoring_rule.missing',
				label: 'Scoring rule',
				message: 'Add a scoring rule for the selected template version.',
				severity: 'blocking'
			},
			{
				code: 'consent_document.missing',
				label: 'Consent document',
				message: 'Add a consent document for this series.',
				severity: 'blocking'
			},
			{
				code: 'retention_policy.missing',
				label: 'Retention policy',
				message: 'Add a retention policy for this series.',
				severity: 'blocking'
			},
			{
				code: 'disclosure_policy.missing',
				label: 'Disclosure policy',
				message: 'Add a disclosure policy for this series.',
				severity: 'blocking'
			}
		],
		campaigns: []
	};
}

const sampleWorkspaceOverview: WorkspaceOverviewResponse = {
	tenantId: '11111111-1111-4111-8111-111111111111',
	totals: {
		campaignSeriesCount: 3,
		campaignCount: 9,
		liveCampaignCount: 2,
		submittedResponseCount: 128,
		exportArtifactCount: 5
	},
	commandCenter: {
		items: [
			{
				id: 'series-setup-command',
				title: 'Finish preparation for Quarterly pulse',
				description: 'Consent, retention, disclosure, and results setup still need attention.',
				state: 'blocked',
				surface: 'setup',
				route: `/app/campaign-series/${sampleSeriesId}/setup`,
				actionLabel: 'Open Prepare',
				priority: 20,
				campaignSeriesId: sampleSeriesId,
				campaignId: null,
				requiredPermission: 'setup.manage'
			}
		]
	},
	studyCollections: {
		sampleStudies: [
			{
				id: setupSampleSeriesId,
				name: 'Setup readiness sample',
				...setupSampleStudyOwnership,
				createdAt: '2026-05-01T08:00:00Z',
				updatedAt: '2026-05-07T09:30:00Z',
				campaignCount: 0,
				liveCampaignCount: 0,
				submittedResponseCount: 0,
				latestLaunchAt: null,
				latestSubmissionAt: null,
				readinessStatus: 'not_configured',
				archived: false,
				archivedAt: null,
				archivedByUserId: null,
				archiveReason: null
			},
			{
				id: collectionSampleSeriesId,
				name: 'Collection in progress sample',
				...collectionSampleStudyOwnership,
				createdAt: '2026-05-01T08:00:00Z',
				updatedAt: '2026-05-07T09:30:00Z',
				campaignCount: 1,
				liveCampaignCount: 1,
				submittedResponseCount: 0,
				latestLaunchAt: '2026-05-05T10:15:00Z',
				latestSubmissionAt: null,
				readinessStatus: 'ready',
				archived: false,
				archivedAt: null,
				archivedByUserId: null,
				archiveReason: null
			},
			{
				id: sampleSeriesId,
				name: 'Completed sample',
				...sampleStudyOwnership,
				createdAt: '2026-05-01T08:00:00Z',
				updatedAt: '2026-05-07T09:30:00Z',
				campaignCount: 3,
				liveCampaignCount: 0,
				submittedResponseCount: 64,
				latestLaunchAt: '2026-05-05T10:15:00Z',
				latestSubmissionAt: '2026-05-07T11:20:00Z',
				readinessStatus: 'proof_only',
				archived: false,
				archivedAt: null,
				archivedByUserId: null,
				archiveReason: null
			},
			{
				id: longitudinalSampleSeriesId,
				name: 'Longitudinal wave sample',
				...longitudinalSampleStudyOwnership,
				createdAt: '2026-05-01T08:00:00Z',
				updatedAt: '2026-05-07T09:30:00Z',
				campaignCount: 2,
				liveCampaignCount: 2,
				submittedResponseCount: 12,
				latestLaunchAt: '2026-05-12T10:15:00Z',
				latestSubmissionAt: '2026-05-12T11:20:00Z',
				readinessStatus: 'proof_only',
				archived: false,
				archivedAt: null,
				archivedByUserId: null,
				archiveReason: null
			}
		],
		ownStudies: [
			{
				id: alternateSeriesId,
				name: 'New team study',
				...ownSeriesOwnership,
				createdAt: '2026-05-08T08:00:00Z',
				updatedAt: '2026-05-08T08:00:00Z',
				campaignCount: 0,
				liveCampaignCount: 0,
				submittedResponseCount: 0,
				latestLaunchAt: null,
				latestSubmissionAt: null,
				readinessStatus: 'not_configured',
				archived: false,
				archivedAt: null,
				archivedByUserId: null,
				archiveReason: null
			}
		]
	},
	recentSeries: [
		{
			id: sampleSeriesId,
			name: 'Quarterly pulse',
			...ownSeriesOwnership,
			createdAt: '2026-05-01T08:00:00Z',
			updatedAt: '2026-05-07T09:30:00Z',
			campaignCount: 7,
			liveCampaignCount: 1,
			submittedResponseCount: 64,
			latestLaunchAt: '2026-05-05T10:15:00Z',
			latestSubmissionAt: '2026-05-07T11:20:00Z',
			readinessStatus: 'proof_only',
			archived: false,
			archivedAt: null,
			archivedByUserId: null,
			archiveReason: null
		}
	]
};

const sampleTenantSettings: TenantSettingsWorkspaceResponse = {
	profile: {
		tenantId: '11111111-1111-4111-8111-111111111111',
		slug: 'occupational-health-lab',
		name: 'Occupational Health Lab',
		region: 'eu',
		defaultLocale: 'en',
		status: 'active',
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-12T09:30:00Z'
	},
	counts: {
		campaignSeriesCount: 3,
		campaignCount: 9,
		liveCampaignCount: 2,
		submittedResponseCount: 128,
		subjectCount: 42,
		subjectGroupCount: 6,
		tenantMemberCount: 4,
		tenantRoleCount: 3,
		exportArtifactCount: 5
	},
	reportBranding: {
		organizationLabel: 'Occupational Health Lab',
		reportTitle: 'Campaign series report',
		brandingSource: 'tenant_profile',
		logoMode: 'none',
		accentColorHex: '#2563eb',
		layoutVariant: 'standard',
		deferredCustomizations: ['logo_upload', 'custom_fonts', 'product_shell_theming']
	},
	managementLinks: [
		{
			id: 'campaign-series',
			label: 'Campaign series',
			description: 'Manage tenant study series and selected-series workspaces.',
			route: '/app/campaign-series'
		},
		{
			id: 'team',
			label: 'Team',
			description: 'Manage tenant members and roles.',
			route: '/app/team'
		},
		{
			id: 'directory',
			label: 'People',
			description: 'Review subject records and hierarchy.',
			route: '/app/directory'
		}
	]
};

const sampleInstrumentLibrary: InstrumentSummaryResponse[] = [
	{
		id: '7e4d44f0-4b2a-472a-9e0c-57218f49bcb9',
		code: 'BURNOUT_16',
		version: '1.0.0',
		fullName: 'Tenant burnout pulse',
		rightsStatus: 'tenant_attested',
		validityLabel: 'Tenant-provided validated instrument',
		canStartNewCampaign: true
	},
	{
		id: '4869b463-344d-42b7-8a56-77a0dff1cc21',
		code: 'INTERNAL_DEMO',
		version: '0.1.0',
		fullName: 'Internal demo only',
		rightsStatus: 'unverified_internal_demo',
		validityLabel: 'Not launchable',
		canStartNewCampaign: false
	}
];

const sampleExportArtifactLibrary: ExportArtifactLibraryResponse = {
	tenantId: sampleSessionTenantId,
	summary: {
		totalCount: 2,
		downloadableCount: 1,
		failedCount: 1,
		pendingCount: 0
	},
	artifacts: [
		{
			id: 'd29b1eb2-6d44-4212-9a4a-50d7c9df7101',
			targetKind: 'campaign',
			targetId: sampleCampaignId,
			targetLabel: 'Baseline wave',
			campaignId: sampleCampaignId,
			campaignName: 'Baseline wave',
			artifactType: 'report_proof_csv_codebook',
			status: 'succeeded',
			format: 'csv_codebook',
			fileName: 'baseline-report.csv',
			rowCount: 12,
			byteSize: 2048,
			checksumSha256: 'a'.repeat(64),
			createdAt: '2026-05-16T08:00:00Z',
			completedAt: '2026-05-16T08:00:03Z',
			startedAt: '2026-05-16T08:00:00Z',
			failedAt: null,
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: null,
			canDownload: true,
			campaignStatus: 'closed',
			campaignClosedAt: '2026-05-16T09:00:00Z',
			dataFinality: 'closed_wave'
		},
		{
			id: '435d7fd9-77cf-41d2-b329-3a8edcb307bd',
			targetKind: 'campaign_series',
			targetId: sampleSeriesId,
			targetLabel: 'Response study',
			campaignId: null,
			campaignName: null,
			artifactType: 'campaign_series_response_csv_codebook',
			status: 'failed',
			format: 'csv_codebook',
			fileName: 'responses.csv',
			rowCount: 0,
			byteSize: 0,
			checksumSha256: null,
			createdAt: '2026-05-16T07:00:00Z',
			completedAt: null,
			startedAt: '2026-05-16T07:00:01Z',
			failedAt: '2026-05-16T07:00:02Z',
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: 'export.failed',
			canDownload: false,
			campaignStatus: null,
			campaignClosedAt: null,
			dataFinality: null
		}
	]
};

const sampleTenantMemberRoster: TenantMemberRosterResponse = {
	tenantId: '11111111-1111-4111-8111-111111111111',
	members: [
		{
			userId: '22222222-2222-4222-8222-222222222222',
			email: 'owner@example.test',
			locale: 'en',
			createdAt: '2026-05-10T08:00:00Z',
			lastLoginAt: '2026-05-11T09:00:00Z',
			identityStatus: 'active',
			roles: [
				{
					roleId: '33333333-3333-4333-8333-333333333333',
					code: 'tenant_owner',
					name: 'Tenant Owner',
					scopeType: 'tenant',
					scopeId: null,
					grantedAt: '2026-05-10T08:30:00Z'
				}
			],
			permissions: ['setup.manage', 'team.manage', 'export.read']
		},
		{
			userId: '44444444-4444-4444-8444-444444444444',
			email: 'analyst@example.test',
			locale: 'en',
			createdAt: '2026-05-10T08:00:00Z',
			lastLoginAt: null,
			identityStatus: 'pending_provider_link',
			roles: [
				{
					roleId: '55555555-5555-4555-8555-555555555555',
					code: 'analyst',
					name: 'Analyst',
					scopeType: 'tenant',
					scopeId: null,
					grantedAt: '2026-05-10T08:30:00Z'
				}
			],
			permissions: ['export.read']
		}
	]
};

const sampleTenantRoleList: TenantRoleListResponse = {
	roles: [
		{
			roleId: '33333333-3333-4333-8333-333333333333',
			code: 'tenant_owner',
			name: 'Tenant Owner',
			permissions: ['setup.manage', 'team.manage', 'export.read']
		},
		{
			roleId: '55555555-5555-4555-8555-555555555555',
			code: 'analyst',
			name: 'Analyst',
			permissions: ['export.read']
		}
	]
};

const sampleSubjectDirectory: SubjectDirectoryResponse = {
	tenantId: '11111111-1111-4111-8111-111111111111',
	summary: {
		subjectCount: 2,
		groupCount: 1,
		managerRelationshipCount: 1
	},
	subjects: [
		{
			id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
			displayName: 'Ana Analyst',
			email: 'ana@example.test',
			externalId: 'emp-001',
			locale: 'en',
			attributes: '{}',
			managerSubjectId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
			managerDisplayName: 'Mira Manager',
			directReportCount: 0,
			groups: [
				{
					groupId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
					groupType: 'team',
					groupName: 'Research Team',
					roleInGroup: 'member',
					validFrom: null,
					validTo: null
				}
			]
		},
		{
			id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
			displayName: 'Mira Manager',
			email: 'mira@example.test',
			externalId: 'mgr-001',
			locale: 'en',
			attributes: '{}',
			managerSubjectId: null,
			managerDisplayName: null,
			directReportCount: 1,
			groups: [],
			directoryImportStale: true,
			directoryImportStaleAt: '2026-06-11T10:15:00+00:00'
		}
	]
};

const sampleSubjectGroupList: SubjectGroupListResponse = {
	tenantId: sampleSubjectDirectory.tenantId,
	groups: [
		{
			id: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
			type: 'team',
			name: 'Research Team',
			parentGroupId: null,
			attributes: '{}',
			memberCount: 1
		}
	]
};

const sampleMicrosoftGraphConnectionState: DirectoryConnectionStateResponse = {
	tenantId: sampleSubjectDirectory.tenantId,
	provider: 'microsoft_graph',
	status: 'disconnected',
	displayName: 'Microsoft Graph',
	primaryDomain: null,
	grantedScopes: [],
	lastConsentAt: null,
	lastSuccessfulImportAt: null,
	updatedAt: null,
	connected: false
};

const sampleMicrosoftGraphConsentRequest: MicrosoftGraphConsentRequestResponse = {
	tenantId: sampleSubjectDirectory.tenantId,
	consentRequestId: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
	directoryConnectionId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
	provider: 'microsoft_graph',
	status: 'pending',
	requestedScopes: ['User.Read.All'],
	expiresAt: '2026-06-12T12:20:00+00:00',
	state: 'state-value',
	nonce: 'nonce-value',
	callbackPath: '/app/directory',
	adminConsentUrl:
		'https://login.microsoftonline.com/common/adminconsent?client_id=test-client&redirect_uri=https%3A%2F%2Fplatform.example.test%2Fapp%2Fdirectory&state=state-value'
};

const sampleMicrosoftGraphImportRuns: DirectoryImportRunHistoryResponse = {
	tenantId: sampleSubjectDirectory.tenantId,
	runs: [
		{
			id: 'ffffffff-ffff-4fff-8fff-ffffffffffff',
			directoryConnectionId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
			directoryImportRuleId: 'abababab-abab-4aba-8aba-abababababab',
			previewRunId: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
			provider: 'microsoft_graph',
			mode: 'apply',
			status: 'succeeded',
			rowCount: 3,
			importedRowCount: 3,
			failedRowCount: 0,
			warningCategoryCount: 0,
			warningCategories: [],
			createdAt: '2026-06-12T12:10:00+00:00',
			startedAt: '2026-06-12T12:10:01+00:00',
			completedAt: '2026-06-12T12:10:02+00:00'
		}
	]
};

const sampleMicrosoftGraphImportRules: DirectoryImportRuleListResponse = {
	tenantId: sampleSubjectDirectory.tenantId,
	rules: [
		{
			id: 'abababab-abab-4aba-8aba-abababababab',
			directoryConnectionId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
			name: 'All employees',
			status: 'active',
			stalePolicy: 'mark_stale',
			retainedFields: ['external_id', 'email', 'manager_external_id'],
			createdAt: '2026-06-12T12:00:00+00:00',
			updatedAt: '2026-06-12T12:00:00+00:00'
		}
	]
};

const sampleMicrosoftGraphLivePreview = {
	tenantId: sampleSubjectDirectory.tenantId,
	directoryImportRuleId: 'abababab-abab-4aba-8aba-abababababab',
	directoryConnectionId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
	import: {
		tenantId: sampleSubjectDirectory.tenantId,
		rowCount: 1,
		importedRowCount: 1,
		createdSubjectCount: 1,
		updatedSubjectCount: 0,
		createdGroupCount: 0,
		addedMembershipCount: 0,
		skippedMembershipCount: 0,
		rows: [],
		dryRun: true,
		importRunId: 'live-preview-run-id'
	},
	includedUserCount: 1,
	includedMembershipCount: 0,
	warnings: []
};

const sampleRespondentRulePreview: RespondentRulePreviewResponse = {
	campaignSeriesId: sampleSeriesId,
	campaignId: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
	ruleKind: 'manager_of_target',
	role: 'manager',
	summary: {
		targetCount: 1,
		respondentCount: 1,
		assignmentPairCount: 1,
		skippedCount: 0,
		warningCount: 0,
		truncated: false
	},
	rows: [
		{
			ordinal: 1,
			ruleKind: 'manager_of_target',
			role: 'manager',
			target: {
				id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
				label: 'Ana Analyst',
				displayName: 'Ana Analyst',
				email: 'ana@example.test',
				externalId: 'emp-001'
			},
			respondent: {
				id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
				label: 'Mira Manager',
				displayName: 'Mira Manager',
				email: 'mira@example.test',
				externalId: 'mgr-001'
			}
		}
	],
	warnings: []
};

const sampleCampaignSeriesList: CampaignSeriesListResponse = {
	items: [
		{
			id: sampleSeriesId,
			name: 'Quarterly pulse',
			...ownSeriesOwnership,
			createdAt: '2026-05-01T08:00:00Z',
			updatedAt: '2026-05-07T09:30:00Z',
			campaignCount: 7,
			liveCampaignCount: 2,
			submittedResponseCount: 128,
			latestLaunchAt: '2026-05-05T10:15:00Z',
			latestSubmissionAt: '2026-05-07T11:20:00Z',
			readinessStatus: 'pending',
			archived: false,
			archivedAt: null,
			archivedByUserId: null,
			archiveReason: null
		}
	]
};

const sampleCampaignSeriesHub: CampaignSeriesHubResponse = {
	id: sampleSeriesId,
	name: 'Quarterly pulse',
	...ownSeriesOwnership,
	createdAt: '2026-05-01T08:00:00Z',
	updatedAt: '2026-05-07T09:30:00Z',
	totals: {
		campaignCount: 7,
		liveCampaignCount: 2,
		submittedResponseCount: 128,
		scoreCount: 120,
		exportArtifactCount: 5
	},
	governance: {
		consentStatus: 'proof_only',
		retentionStatus: 'proof_only',
		disclosureStatus: 'pending',
		scoringStatus: 'not_configured'
	},
	lifecycle: [
		{
			id: 'setup',
			label: 'Prepare',
			status: 'ready',
			guidance: 'Governance prerequisites are configured for this series.',
			route: 'setup',
			actionLabel: 'Open Prepare'
		},
		{
			id: 'operations',
			label: 'Collect',
			status: 'ready',
			guidance: 'Collection has submitted responses to monitor.',
			route: 'operations',
			actionLabel: 'Open Collect'
		},
		{
			id: 'reports',
			label: 'Results',
			status: 'proof_only',
			guidance: 'Report preview can be reviewed; create an export file before handoff.',
			route: 'reports',
			actionLabel: 'Open Results'
		},
		{
			id: 'waves',
			label: 'Rounds',
			status: 'not_available',
			guidance:
				'Use anonymous longitudinal campaign identity when this series needs wave comparison.',
			route: 'waves',
			actionLabel: 'Compare rounds'
		}
	],
	campaigns: [
		{
			id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
			name: 'Pulse wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			startAt: '2026-05-05T08:00:00Z',
			endAt: '2026-05-15T18:00:00Z',
			latestLaunchAt: '2026-05-05T10:15:00Z',
			submittedResponseCount: 128,
			scoreCount: 120,
			exportArtifactCount: 5
		}
	],
	archived: false,
	archivedAt: null,
	archivedByUserId: null,
	archiveReason: null
};

const sampleSetupWorkspace: CampaignSeriesSetupWorkspaceResponse = {
	series: {
		id: sampleSeriesId,
		name: 'Quarterly pulse',
		...ownSeriesOwnership,
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-07T09:30:00Z'
	},
	summary: {
		campaignCount: 1,
		liveCampaignCount: 0,
		missingPrerequisiteCount: 1
	},
	selectedCampaign: {
		id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
		name: 'Pulse wave 1',
		status: 'draft',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en',
		templateVersionId: '68933bc3-522b-4974-b7a7-04a7eaf52edc',
		latestLaunchAt: null
	},
	template: {
		templateId: '2a642f70-90ca-4aa7-b7c1-84084360a1a9',
		templateVersionId: '68933bc3-522b-4974-b7a7-04a7eaf52edc',
		templateName: 'Tenant burnout pulse template',
		semver: '1.0.0',
		status: 'published',
		defaultLocale: 'en',
		instrumentId: null,
		questionCount: 5
	},
	scoring: {
		id: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		templateVersionId: '68933bc3-522b-4974-b7a7-04a7eaf52edc',
		ruleKey: 'burnout.total',
		ruleVersion: '1.0.0',
		status: 'draft',
		source: 'template_version'
	},
	policies: {
		consent: { id: '7a8b40d5-5083-4b90-a521-6de19f88d108', version: '1.0.0', status: 'configured' },
		retention: {
			id: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
			version: '1.0.0',
			status: 'configured'
		},
		disclosure: { id: null, version: null, status: 'not_configured' }
	},
	readiness: {
		campaignId: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
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
			id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
			name: 'Pulse wave 1',
			status: 'draft',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			templateVersionId: '68933bc3-522b-4974-b7a7-04a7eaf52edc',
			latestLaunchAt: null
		}
	]
};

const emptySetupWorkspace: CampaignSeriesSetupWorkspaceResponse = {
	...sampleSetupWorkspace,
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

const campaignDraftSetupWorkspace: CampaignSeriesSetupWorkspaceResponse = {
	...sampleSetupWorkspace,
	summary: {
		campaignCount: 0,
		liveCampaignCount: 0,
		missingPrerequisiteCount: 2
	},
	selectedCampaign: null,
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
		}
	],
	campaigns: []
};

const sampleOperationsWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
	series: {
		id: sampleSeriesId,
		name: 'Quarterly pulse',
		...ownSeriesOwnership,
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-07T09:30:00Z'
	},
	summary: {
		campaignCount: 2,
		liveCampaignCount: 1,
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 1,
		sentInvitationCount: 8,
		failedInvitationCount: 1,
		deliveryAttemptCount: 9,
		startedResponseCount: 133,
		draftResponseCount: 5,
		submittedResponseCount: 128,
		latestResponseStartedAt: '2026-05-05T10:45:00Z',
		latestResponseSubmittedAt: '2026-05-05T10:40:00Z',
		collectionStatus: 'has_submissions',
		reportVisibilityStatus: 'ready_for_aggregate_report',
		collectionGuidance: 'Enough submitted responses exist for aggregate report visibility.',
		missingPrerequisiteCount: 0
	},
	selectedCampaign: {
		id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
		name: 'Pulse wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'launch-snapshot-id',
		latestLaunchAt: '2026-05-05T10:15:00Z',
		launchSnapshot: {
			id: 'launch-snapshot-id',
			templateVersionId: 'template-version-id',
			scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
			scoringRuleDocumentHash: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
			consentDocumentId: '7a8b40d5-5083-4b90-a521-6de19f88d108',
			retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
			disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			templateQuestionCount: 8,
			launchedAt: '2026-05-05T10:15:00Z',
			launchedByUserId: 'owner-user-id'
		},
		startedResponseCount: 133,
		draftResponseCount: 5,
		submittedResponseCount: 128,
		latestResponseStartedAt: '2026-05-05T10:45:00Z',
		latestResponseSubmittedAt: '2026-05-05T10:40:00Z',
		collectionStatus: 'has_submissions',
		reportVisibilityStatus: 'ready_for_aggregate_report',
		collectionGuidance: 'Enough submitted responses exist for aggregate report visibility.',
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 1,
		sentInvitationCount: 8,
		failedInvitationCount: 1,
		deliveryAttemptCount: 9,
		latestDeliveryAttemptAt: '2026-05-05T09:30:00Z',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoredSubmittedResponseCount: 128,
		unscoredSubmittedResponseCount: 0,
		notConfiguredSubmittedResponseCount: 0,
		latestScoringActivityAt: '2026-05-05T10:50:00Z',
		scoreCoverageStatus: 'complete'
	},
	missingPrerequisites: [],
	campaigns: [
		{
			id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
			name: 'Pulse wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			latestLaunchSnapshotId: 'launch-snapshot-id',
			latestLaunchAt: '2026-05-05T10:15:00Z',
			launchSnapshot: {
				id: 'launch-snapshot-id',
				templateVersionId: 'template-version-id',
				scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
				scoringRuleDocumentHash: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
				consentDocumentId: '7a8b40d5-5083-4b90-a521-6de19f88d108',
				retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
				disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
				responseIdentityMode: 'anonymous',
				defaultLocale: 'en',
				templateQuestionCount: 8,
				launchedAt: '2026-05-05T10:15:00Z',
				launchedByUserId: 'owner-user-id'
			},
			startedResponseCount: 133,
			draftResponseCount: 5,
			submittedResponseCount: 128,
			latestResponseStartedAt: '2026-05-05T10:45:00Z',
			latestResponseSubmittedAt: '2026-05-05T10:40:00Z',
			collectionStatus: 'has_submissions',
			reportVisibilityStatus: 'ready_for_aggregate_report',
			collectionGuidance: 'Enough submitted responses exist for aggregate report visibility.',
			openLinkAssignmentCount: 1,
			queuedInvitationCount: 1,
			sentInvitationCount: 8,
			failedInvitationCount: 1,
			deliveryAttemptCount: 9,
			latestDeliveryAttemptAt: '2026-05-05T09:30:00Z',
			scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
			scoredSubmittedResponseCount: 128,
			unscoredSubmittedResponseCount: 0,
			notConfiguredSubmittedResponseCount: 0,
			latestScoringActivityAt: '2026-05-05T10:50:00Z',
			scoreCoverageStatus: 'complete'
		},
		{
			id: '6d3271db-494f-401d-af8b-a5c86c9293a8',
			name: 'Draft wave',
			status: 'draft',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			latestLaunchSnapshotId: null,
			latestLaunchAt: null,
			startedResponseCount: 0,
			draftResponseCount: 0,
			submittedResponseCount: 0,
			latestResponseStartedAt: null,
			latestResponseSubmittedAt: null,
			collectionStatus: 'closed_or_inactive',
			reportVisibilityStatus: 'unknown_policy',
			collectionGuidance:
				'Report visibility readiness is unknown because disclosure policy is missing.',
			openLinkAssignmentCount: 0,
			queuedInvitationCount: 0,
			sentInvitationCount: 0,
			failedInvitationCount: 0,
			deliveryAttemptCount: 0,
			latestDeliveryAttemptAt: null,
			scoringRuleId: null,
			scoredSubmittedResponseCount: 0,
			unscoredSubmittedResponseCount: 0,
			notConfiguredSubmittedResponseCount: 0,
			latestScoringActivityAt: null,
			scoreCoverageStatus: 'no_submissions'
		}
	],
	scoreCoverage: {
		submittedResponseCount: 128,
		scoredSubmittedResponseCount: 128,
		unscoredSubmittedResponseCount: 0,
		notConfiguredSubmittedResponseCount: 0,
		campaignsWithScoringRuleCount: 1,
		campaignsWithoutScoringRuleCount: 1,
		latestScoringActivityAt: '2026-05-05T10:50:00Z',
		status: 'complete',
		guidance: 'All submitted responses have successful scoring activity.'
	}
};

const sampleReportsWorkspace: CampaignSeriesReportsWorkspaceResponse = {
	series: {
		id: sampleSeriesId,
		name: 'Quarterly pulse',
		...ownSeriesOwnership,
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-07T09:30:00Z'
	},
	summary: {
		campaignCount: 2,
		liveCampaignCount: 1,
		reportableCampaignCount: 1,
		submittedResponseCount: 128,
		scoreCount: 120,
		exportArtifactCount: 2,
		visibleScoreCount: 115,
		suppressedScoreCount: 5,
		missingPrerequisiteCount: 1
	},
	selectedCampaign: {
		id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
		name: 'Pulse wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'launch-snapshot-id',
		latestLaunchAt: '2026-05-05T10:15:00Z',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		consentDocumentId: '7a8b40d5-5083-4b90-a521-6de19f88d108',
		retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
		disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
		submittedResponseCount: 128,
		scoreCount: 120,
		exportArtifactCount: 2,
		visibleScoreCount: 115,
		suppressedScoreCount: 5,
		disclosureState: 'visible',
		disclosureKMin: 5,
		reportStatus: 'proof_only',
		interpretationStatus: 'not_validated_interpretation',
		latestExportArtifactId: '8e592f74-d0ca-4204-aead-fb00e9e5085a',
		latestExportArtifactFileName: 'report-proof.csv',
		latestExportArtifactStatus: 'succeeded',
		latestExportArtifactCreatedAt: '2026-05-05T11:00:00Z',
		latestExportArtifactCompletedAt: '2026-05-05T11:00:03Z',
		latestExportArtifactStartedAt: null,
		latestExportArtifactFailedAt: null,
		latestExportArtifactExpiresAt: null,
		latestExportArtifactDeletedAt: null,
		latestExportArtifactFailureReasonCode: null,
		latestExportArtifactCanDownload: true
	},
	missingPrerequisites: [
		{
			code: 'export_artifact.missing',
			label: 'Export file',
			message: 'Create a report preview export before handoff.',
			severity: 'advisory'
		}
	],
	exportArtifacts: [
		{
			id: '8e592f74-d0ca-4204-aead-fb00e9e5085a',
			targetKind: 'campaign',
			targetId: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
			targetLabel: 'Pulse wave 1',
			campaignId: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
			campaignName: 'Pulse wave 1',
			artifactType: 'report_proof_csv_codebook',
			status: 'succeeded',
			format: 'csv_codebook',
			fileName: 'report-proof.csv',
			rowCount: 120,
			byteSize: 2048,
			checksumSha256: 'checksum-sha256',
			createdAt: '2026-05-05T11:00:00Z',
			completedAt: '2026-05-05T11:00:03Z',
			startedAt: null,
			failedAt: null,
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: null,
			canDownload: true
		}
	],
	campaigns: [
		{
			id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
			name: 'Pulse wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			latestLaunchSnapshotId: 'launch-snapshot-id',
			latestLaunchAt: '2026-05-05T10:15:00Z',
			scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
			consentDocumentId: '7a8b40d5-5083-4b90-a521-6de19f88d108',
			retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
			disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
			submittedResponseCount: 128,
			scoreCount: 120,
			exportArtifactCount: 2,
			visibleScoreCount: 115,
			suppressedScoreCount: 5,
			disclosureState: 'visible',
			disclosureKMin: 5,
			reportStatus: 'proof_only',
			interpretationStatus: 'not_validated_interpretation',
			latestExportArtifactId: '8e592f74-d0ca-4204-aead-fb00e9e5085a',
			latestExportArtifactFileName: 'report-proof.csv',
			latestExportArtifactStatus: 'succeeded',
			latestExportArtifactCreatedAt: '2026-05-05T11:00:00Z',
			latestExportArtifactCompletedAt: '2026-05-05T11:00:03Z',
			latestExportArtifactStartedAt: null,
			latestExportArtifactFailedAt: null,
			latestExportArtifactExpiresAt: null,
			latestExportArtifactDeletedAt: null,
			latestExportArtifactFailureReasonCode: null,
			latestExportArtifactCanDownload: true
		},
		{
			id: '6d3271db-494f-401d-af8b-a5c86c9293a8',
			name: 'Draft wave',
			status: 'draft',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			latestLaunchSnapshotId: null,
			latestLaunchAt: null,
			scoringRuleId: null,
			consentDocumentId: null,
			retentionPolicyId: null,
			disclosurePolicyId: null,
			submittedResponseCount: 0,
			scoreCount: 0,
			exportArtifactCount: 0,
			visibleScoreCount: 0,
			suppressedScoreCount: 0,
			disclosureState: 'not_available',
			disclosureKMin: null,
			reportStatus: 'blocked',
			interpretationStatus: 'not_available',
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
		}
	],
	scoreCoverage: {
		submittedResponseCount: 128,
		scoredSubmittedResponseCount: 120,
		unscoredSubmittedResponseCount: 8,
		notConfiguredSubmittedResponseCount: 0,
		campaignsWithScoringRuleCount: 1,
		campaignsWithoutScoringRuleCount: 1,
		latestScoringActivityAt: '2026-05-05T10:50:00Z',
		status: 'partial',
		guidance:
			'Some submitted responses still need scoring activity before score-dependent reports are complete.'
	}
};

const sampleReportsWidgetManifest: CampaignSeriesReportsWidgetManifestResponse = {
	campaignSeriesId: sampleSeriesId,
	surface: 'reports',
	surfaceVersion: 'reports-widget-manifest/v1',
	layout: {
		kind: 'dashboard-grid/v1',
		density: 'standard'
	},
	widgets: [
		{
			id: 'report-readiness-summary',
			kind: 'report-readiness-summary/v1',
			title: 'Report readiness',
			size: 'half',
			state: 'blocked',
			message: 'Some report prerequisites still need attention.',
			data: {
				campaignCount: sampleReportsWorkspace.summary.campaignCount,
				liveCampaignCount: sampleReportsWorkspace.summary.liveCampaignCount,
				reportableCampaignCount: sampleReportsWorkspace.summary.reportableCampaignCount,
				submittedResponseCount: sampleReportsWorkspace.summary.submittedResponseCount,
				scoreCount: sampleReportsWorkspace.summary.scoreCount,
				visibleScoreCount: sampleReportsWorkspace.summary.visibleScoreCount,
				suppressedScoreCount: sampleReportsWorkspace.summary.suppressedScoreCount,
				missingPrerequisiteCount: sampleReportsWorkspace.summary.missingPrerequisiteCount,
				missingPrerequisites: sampleReportsWorkspace.missingPrerequisites
			},
			dataSource: null,
			actions: []
		},
		{
			id: 'score-coverage-summary',
			kind: 'score-coverage-summary/v1',
			title: 'Score coverage',
			size: 'half',
			state: 'empty',
			message:
				'Some submitted responses still need scoring activity before score-dependent reports are complete.',
			data: sampleReportsWorkspace.scoreCoverage ?? null,
			dataSource: null,
			actions: []
		},
		{
			id: 'selected-campaign-report-state',
			kind: 'selected-campaign-report-state/v1',
			title: 'Selected campaign report state',
			size: 'half',
			state: 'ready',
			message: null,
			data: {
				campaignId: sampleReportsWorkspace.selectedCampaign!.id,
				name: sampleReportsWorkspace.selectedCampaign!.name,
				status: sampleReportsWorkspace.selectedCampaign!.status,
				responseIdentityMode: sampleReportsWorkspace.selectedCampaign!.responseIdentityMode,
				defaultLocale: sampleReportsWorkspace.selectedCampaign!.defaultLocale,
				latestLaunchAt: sampleReportsWorkspace.selectedCampaign!.latestLaunchAt,
				submittedResponseCount: sampleReportsWorkspace.selectedCampaign!.submittedResponseCount,
				scoreCount: sampleReportsWorkspace.selectedCampaign!.scoreCount,
				visibleScoreCount: sampleReportsWorkspace.selectedCampaign!.visibleScoreCount,
				suppressedScoreCount: sampleReportsWorkspace.selectedCampaign!.suppressedScoreCount,
				disclosureState: sampleReportsWorkspace.selectedCampaign!.disclosureState,
				disclosureKMin: sampleReportsWorkspace.selectedCampaign!.disclosureKMin,
				reportStatus: sampleReportsWorkspace.selectedCampaign!.reportStatus,
				interpretationStatus: sampleReportsWorkspace.selectedCampaign!.interpretationStatus,
				latestExportArtifactId: sampleReportsWorkspace.selectedCampaign!.latestExportArtifactId,
				latestExportArtifactFileName:
					sampleReportsWorkspace.selectedCampaign!.latestExportArtifactFileName,
				latestExportArtifactStatus:
					sampleReportsWorkspace.selectedCampaign!.latestExportArtifactStatus,
				latestExportArtifactCreatedAt:
					sampleReportsWorkspace.selectedCampaign!.latestExportArtifactCreatedAt,
				latestExportArtifactCompletedAt:
					sampleReportsWorkspace.selectedCampaign!.latestExportArtifactCompletedAt,
				latestExportArtifactFailedAt:
					sampleReportsWorkspace.selectedCampaign!.latestExportArtifactFailedAt,
				latestExportArtifactFailureReasonCode:
					sampleReportsWorkspace.selectedCampaign!.latestExportArtifactFailureReasonCode,
				latestExportArtifactCanDownload:
					sampleReportsWorkspace.selectedCampaign!.latestExportArtifactCanDownload,
				closedAt: sampleReportsWorkspace.selectedCampaign!.closedAt ?? null,
				dataFinality: sampleReportsWorkspace.selectedCampaign!.dataFinality ?? 'preliminary_live'
			},
			dataSource: {
				href: `/campaigns/${sampleReportsWorkspace.selectedCampaign!.id}/report-proof`,
				method: 'GET'
			},
			actions: []
		},
		{
			id: 'export-artifact-registry',
			kind: 'export-artifact-registry/v1',
			title: 'Export files',
			size: 'full',
			state: 'ready',
			message: null,
			data: {
				exportArtifactCount: sampleReportsWorkspace.summary.exportArtifactCount,
				artifacts: sampleReportsWorkspace.exportArtifacts
			},
			dataSource: null,
			actions: [
				{
					id: 'create-aggregate-export',
					label: 'Create aggregate export',
					kind: 'api-command/v1',
					href: `/campaigns/${sampleReportsWorkspace.selectedCampaign!.id}/report-proof/exports`,
					method: 'POST',
					enabled: true,
					disabledReason: null
				}
			]
		},
		{
			id: 'visual-analytics-entry',
			kind: 'visual-analytics-entry/v1',
			title: 'Visual analytics',
			size: 'full',
			state: 'ready',
			message: null,
			data: {
				selectedCampaignId: sampleReportsWorkspace.selectedCampaign!.id,
				visibleScoreCount: sampleReportsWorkspace.summary.visibleScoreCount,
				suppressedScoreCount: sampleReportsWorkspace.summary.suppressedScoreCount,
				reportableCampaignCount: sampleReportsWorkspace.summary.reportableCampaignCount
			},
			dataSource: {
				href: `/campaigns/${sampleReportsWorkspace.selectedCampaign!.id}/report-proof`,
				method: 'GET'
			},
			actions: []
		},
		{
			id: 'finality-provenance-summary',
			kind: 'finality-provenance-summary/v1',
			title: 'Finality and provenance',
			size: 'half',
			state: 'ready',
			message: null,
			data: {
				preliminaryLiveReportCount: sampleReportsWorkspace.summary.preliminaryLiveReportCount ?? 1,
				closedWaveReportCount: sampleReportsWorkspace.summary.closedWaveReportCount ?? 0,
				selectedCampaignId: sampleReportsWorkspace.selectedCampaign!.id,
				selectedCampaignStatus: sampleReportsWorkspace.selectedCampaign!.status,
				selectedDataFinality:
					sampleReportsWorkspace.selectedCampaign!.dataFinality ?? 'preliminary_live',
				selectedClosedAt: sampleReportsWorkspace.selectedCampaign!.closedAt ?? null,
				selectedLatestLaunchAt: sampleReportsWorkspace.selectedCampaign!.latestLaunchAt
			},
			dataSource: null,
			actions: []
		}
	]
};

const sampleWavesWorkspace: CampaignSeriesWavesWorkspaceResponse = {
	series: {
		id: sampleSeriesId,
		name: 'Quarterly pulse',
		...ownSeriesOwnership,
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-07T09:30:00Z'
	},
	summary: {
		campaignCount: 2,
		liveCampaignCount: 2,
		longitudinalWaveCount: 2,
		submittedWaveCount: 2,
		linkedTrajectoryCount: 6,
		completeTrajectoryCount: 6,
		comparableScoreCount: 1,
		visibleComparisonCount: 1,
		suppressedComparisonCount: 0,
		blockedComparisonCount: 0,
		missingPrerequisiteCount: 0
	},
	selectedBaselineWave: {
		id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
		name: 'Pulse wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'wave-1-launch-id',
		latestLaunchAt: '2026-05-05T10:15:00Z',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		disclosurePolicyId: 'wave-1-disclosure-id',
		disclosureKMin: 5,
		submittedResponseCount: 6,
		scoreCount: 6,
		linkedTrajectoryCount: 6,
		waveState: 'wave'
	},
	selectedComparisonWave: {
		id: '6d3271db-494f-401d-af8b-a5c86c9293a8',
		name: 'Pulse wave 2',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'wave-2-launch-id',
		latestLaunchAt: '2026-05-12T10:15:00Z',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		disclosurePolicyId: 'wave-2-disclosure-id',
		disclosureKMin: 5,
		submittedResponseCount: 6,
		scoreCount: 6,
		linkedTrajectoryCount: 6,
		waveState: 'wave'
	},
	comparison: {
		status: 'proof_only',
		disclosureState: 'visible',
		compatibilityState: 'compatible',
		interpretationStatus: 'not_validated_interpretation',
		disclosureKMin: 5,
		linkedPairCount: 6,
		visibleScoreCount: 1,
		suppressedScoreCount: 0,
		blockedScoreCount: 0
	},
	missingPrerequisites: [],
	waves: [
		{
			id: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
			name: 'Pulse wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			latestLaunchSnapshotId: 'wave-1-launch-id',
			latestLaunchAt: '2026-05-05T10:15:00Z',
			scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
			scoringRuleKey: 'burnout.total',
			scoringRuleVersion: '1.0.0',
			disclosurePolicyId: 'wave-1-disclosure-id',
			disclosureKMin: 5,
			submittedResponseCount: 6,
			scoreCount: 6,
			linkedTrajectoryCount: 6,
			waveState: 'wave'
		},
		{
			id: '6d3271db-494f-401d-af8b-a5c86c9293a8',
			name: 'Pulse wave 2',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			latestLaunchSnapshotId: 'wave-2-launch-id',
			latestLaunchAt: '2026-05-12T10:15:00Z',
			scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
			scoringRuleKey: 'burnout.total',
			scoringRuleVersion: '1.0.0',
			disclosurePolicyId: 'wave-2-disclosure-id',
			disclosureKMin: 5,
			submittedResponseCount: 6,
			scoreCount: 6,
			linkedTrajectoryCount: 6,
			waveState: 'wave'
		}
	]
};

const sampleTwoWaveProof = {
	campaignSeriesId: sampleSeriesId,
	proofStatus: 'proof_only',
	expectedWaveCount: 2,
	launchedWaveCount: 2,
	submittedWaveCount: 2,
	linkedTrajectoryCount: 6,
	completeTrajectoryCount: 6,
	waves: [
		{
			campaignId: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
			name: 'Pulse wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			submittedResponseCount: 6
		},
		{
			campaignId: '6d3271db-494f-401d-af8b-a5c86c9293a8',
			name: 'Pulse wave 2',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			submittedResponseCount: 6
		}
	]
};

const sampleWaveComparisonProof: CampaignSeriesWaveComparisonProofResponse = {
	campaignSeriesId: sampleSeriesId,
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	baselineWave: {
		campaignId: '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2',
		name: 'Pulse wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-05T10:15:00Z',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'a'.repeat(64),
		submittedResponseCount: 6
	},
	comparisonWave: {
		campaignId: '6d3271db-494f-401d-af8b-a5c86c9293a8',
		name: 'Pulse wave 2',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-12T10:15:00Z',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'a'.repeat(64),
		submittedResponseCount: 6
	},
	disclosurePolicy: {
		id: 'wave-disclosure-id',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'hide_cell'
	},
	scores: [
		{
			dimensionCode: 'total',
			disclosure: 'visible',
			compatibilityStatus: 'compatible',
			baselineSubmittedResponseCount: 6,
			comparisonSubmittedResponseCount: 6,
			linkedPairCount: 6,
			baselineScoreCount: 6,
			comparisonScoreCount: 6,
			baselineMean: 3.8,
			comparisonMean: 3.5,
			aggregateDelta: -0.3,
			pairedDeltaMean: -0.25,
			suppressionReason: null,
			compatibilityReason: null
		},
		{
			dimensionCode: 'exhaustion',
			disclosure: 'suppressed',
			compatibilityStatus: 'compatible',
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
};

const sampleCampaignReportProof = {
	campaignId: sampleReportsWorkspace.selectedCampaign?.id ?? '',
	campaignSeriesId: sampleSeriesId,
	campaignName: 'Pulse wave 1',
	campaignStatus: 'live',
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	launchSnapshot: {
		id: 'launch-snapshot-id',
		templateVersionId: '68933bc3-522b-4974-b7a7-04a7eaf52edc',
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoringRuleDocumentHash: 'a'.repeat(64),
		consentDocumentId: '7a8b40d5-5083-4b90-a521-6de19f88d108',
		retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
		disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
		responseIdentityMode: 'anonymous',
		launchedAt: '2026-05-05T10:15:00Z'
	},
	disclosurePolicy: {
		id: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'hide_cell'
	},
	scores: [
		{
			dimensionCode: 'total',
			disclosure: 'visible',
			submittedResponseCount: 128,
			scoreCount: 120,
			mean: 3.75,
			min: 1,
			max: 5,
			suppressionReason: null,
			interpretation: {
				status: 'tenant_attested',
				source: 'tenant_defined',
				bandCode: 'higher',
				label: 'Higher tenant band',
				provenance: 'Tenant-defined internal interpretation bands; not validated and not official.',
				isValidated: false,
				isOfficial: false
			}
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
};

const sampleReportProofExportArtifact = {
	id: '1f7e0386-8dfd-4bb3-9f1f-f08d91534ad8',
	targetKind: 'campaign',
	targetId: sampleReportsWorkspace.selectedCampaign?.id ?? '',
	targetLabel: sampleReportsWorkspace.selectedCampaign?.name ?? 'Campaign',
	campaignId: sampleReportsWorkspace.selectedCampaign?.id ?? '',
	campaignSeriesId: sampleSeriesId,
	artifactType: 'report_proof_csv_codebook',
	status: 'succeeded',
	format: 'csv_codebook',
	fileName: 'updated-report-proof.csv',
	contentType: 'text/csv',
	rowCount: 1,
	byteSize: 128,
	checksumSha256: 'b'.repeat(64),
	createdAt: '2026-05-05T12:00:00Z',
	completedAt: '2026-05-05T12:00:03Z',
	canDownload: true,
	csvContent: 'dimension_code,mean\n',
	codebookJson: '{}'
};

const sampleResponseExportArtifact = {
	id: '6c6c8b50-9735-4a6f-bbd6-bd31907d08dc',
	targetKind: 'campaign_series',
	targetId: sampleSeriesId,
	targetLabel: sampleReportsWorkspace.series.name,
	campaignId: null,
	campaignSeriesId: sampleSeriesId,
	artifactType: 'campaign_series_response_csv_codebook',
	status: 'succeeded',
	format: 'csv_codebook',
	fileName: 'campaign-series-responses.csv',
	contentType: 'text/csv',
	rowCount: 10,
	byteSize: 256,
	checksumSha256: 'c'.repeat(64),
	createdAt: '2026-05-05T12:10:00Z',
	completedAt: '2026-05-05T12:10:03Z',
	canDownload: true,
	csvContent: 'response_row_id,trajectory_id\n',
	codebookJson: JSON.stringify({
		artifactType: 'campaign_series_response_csv_codebook',
		rowCount: 10,
		campaignCount: 1,
		trajectoryCount: 6,
		trajectoryIdPolicy: 'artifact_local',
		missingTreatment: {
			skipped: '__skipped',
			hiddenByDisplayLogic: '__skipped'
		},
		columns: [
			{
				name: 'response_row_id',
				source: 'response_metadata'
			},
			{
				name: 'trajectory_id',
				source: 'response_metadata'
			},
			{
				name: 'q01',
				source: 'answer',
				questionCode: 'q01',
				valueLabels: {
					o01: 'No',
					o02: 'Yes'
				},
				answerMetadata: {
					choiceScoring: {
						enabled: true,
						optionScores: [
							{ code: 'o01', score: 0 },
							{ code: 'o02', score: 4 }
						]
					}
				},
				missingCodes: {
					skipped: '__skipped'
				}
			},
			{
				name: 'q02',
				source: 'answer',
				questionCode: 'q02',
				answerMetadata: {
					requiredWhenVisible: true
				},
				missingCodes: {
					hiddenByDisplayLogic: '__skipped'
				},
				displayLogic: {
					mode: 'show_when',
					sourceQuestionCode: 'q01',
					operatorName: 'contains',
					value: 'o02',
					requiredWhenVisible: true,
					hiddenAnswerTreatment: '__skipped'
				}
			},
			{
				name: 'score_total',
				source: 'score_output_metadata'
			}
		]
	})
};

const sampleReportPdfArtifact = {
	id: '7af2763c-1901-42cc-ab10-7f6f7ff6f43c',
	targetKind: 'campaign_series',
	targetId: sampleSeriesId,
	targetLabel: sampleReportsWorkspace.series.name,
	campaignId: null,
	campaignSeriesId: sampleSeriesId,
	artifactType: 'campaign_series_report_pdf',
	status: 'succeeded',
	format: 'pdf',
	fileName: 'campaign-series-report.pdf',
	contentType: 'application/pdf',
	rowCount: 1,
	byteSize: 2048,
	checksumSha256: 'd'.repeat(64),
	createdAt: '2026-05-05T12:20:00Z',
	startedAt: '2026-05-05T12:20:01Z',
	completedAt: '2026-05-05T12:20:06Z',
	failedAt: null,
	expiresAt: '2026-05-05T12:35:00Z',
	deletedAt: null,
	failureReasonCode: null,
	canDownload: true,
	csvContent: '',
	codebookJson: JSON.stringify({
		artifactType: 'campaign_series_report_pdf',
		sections: []
	})
};

const sampleReportPdfSignedDownloadUrl = {
	id: sampleReportPdfArtifact.id,
	fileName: sampleReportPdfArtifact.fileName,
	contentType: 'application/pdf',
	byteSize: sampleReportPdfArtifact.byteSize,
	checksumSha256: sampleReportPdfArtifact.checksumSha256,
	url: 'https://object-store.example.test/artifact-bucket/reports/report.pdf?X-Amz-Signature=safe-signature',
	expiresAt: '2026-05-05T12:35:00Z'
};

const sampleFailedReportPdfArtifact = {
	...sampleReportPdfArtifact,
	id: '64e8406c-3f52-46f9-bfb7-6290767be71e',
	status: 'failed',
	fileName: 'campaign-series-report-failed.pdf',
	rowCount: 0,
	byteSize: 0,
	checksumSha256: null,
	completedAt: null,
	failedAt: '2026-05-05T12:22:00Z',
	canDownload: false,
	failureReasonCode: 'report_pdf.rendering_timeout'
};

const sampleRetryReportPdfArtifact = {
	...sampleReportPdfArtifact,
	id: '9ce2a812-74eb-462f-a3fa-e0d8e16c94c1',
	status: 'queued',
	fileName: 'campaign-series-report-retry.pdf',
	rowCount: 0,
	byteSize: 0,
	checksumSha256: null,
	startedAt: null,
	completedAt: null,
	canDownload: false
};

const sampleTemplateVersion = {
	templateId: '2a642f70-90ca-4aa7-b7c1-84084360a1a9',
	templateVersionId: '68933bc3-522b-4974-b7a7-04a7eaf52edc',
	templateName: 'Tenant burnout pulse template',
	semver: '1.0.0',
	status: 'published',
	defaultLocale: 'en',
	instrumentId: null,
	sections: [
		{
			id: 'a14e0781-67d8-4c79-a584-bdc13118814a',
			ordinal: 1,
			code: 'core',
			titleDefault: 'Core'
		}
	],
	scales: [
		{
			id: '49b97640-aa51-43b8-a98a-a6c2cf302cb7',
			code: 'agreement',
			type: 'likert',
			minValue: 1,
			maxValue: 5,
			step: 1,
			naAllowed: false,
			anchors: '[]'
		}
	],
	questions: []
};

const staleCampaignSeriesHub: CampaignSeriesHubResponse = {
	...sampleCampaignSeriesHub,
	id: oldFoundationSeriesId,
	name: 'Legacy foundation pulse',
	totals: {
		campaignCount: 1,
		liveCampaignCount: 0,
		submittedResponseCount: 8,
		scoreCount: 8,
		exportArtifactCount: 1
	},
	campaigns: [
		{
			...sampleCampaignSeriesHub.campaigns[0],
			id: '1010d13c-43a2-4b2b-9ef9-ea0f014940b5',
			name: 'Legacy wave',
			status: 'closed',
			submittedResponseCount: 8,
			scoreCount: 8,
			exportArtifactCount: 1
		}
	]
};

const alternateCampaignSeriesHub: CampaignSeriesHubResponse = {
	...sampleCampaignSeriesHub,
	id: alternateSeriesId,
	name: 'Retention pulse',
	totals: {
		campaignCount: 3,
		liveCampaignCount: 1,
		submittedResponseCount: 42,
		scoreCount: 39,
		exportArtifactCount: 2
	},
	campaigns: [
		{
			...sampleCampaignSeriesHub.campaigns[0],
			id: 'f75cfa72-2c2e-4508-a579-cf35b6c33f6b',
			name: 'Retention wave',
			status: 'scheduled',
			submittedResponseCount: 42,
			scoreCount: 39,
			exportArtifactCount: 2
		}
	]
};
