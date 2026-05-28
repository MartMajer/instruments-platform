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
	ExportArtifactLibraryResponse,
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
const selectedSeriesSurfaceLabels = ['Overview', 'Setup', 'Collect', 'Results', 'Waves'];
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

	await expect(page.getByRole('heading', { name: 'Study cockpit', exact: true })).toBeVisible();
	await expect(
		page.getByText('UX02 product surfaces - read-model bridge', { exact: true })
	).toHaveCount(0);
	await expect(
		page.getByText('GF05 setup APIs - F41-ready workspace', { exact: true })
	).toHaveCount(0);
	await expect(page.getByText('Product workspace', { exact: true })).toBeVisible();
	await expect(page.getByText('Tenant command workspace', { exact: true })).toHaveCount(0);
	await expect(page.getByLabel('Workspace posture')).toHaveCount(0);
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav).toBeVisible();
	const overview = page.getByRole('region', { name: 'Self-serve study cockpit' });
	const commandCenter = overview.getByRole('region', { name: 'Suggested next actions' });
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	const setupCommand = commandCenter.getByRole('link', {
		name: /Finish setup for Quarterly pulse/i
	});
	const totals = overview.getByRole('group', { name: 'Workspace totals' });

	await expect(setupCommand).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/setup`
	);
	await expect(commandCenter.getByText('setup.manage', { exact: true })).toHaveCount(0);
	await expect(sampleStudies.getByText('Sample study', { exact: true })).toHaveCount(4);
	await expect(ownStudies.getByText('Your study', { exact: true })).toBeVisible();
	await expect(totals.getByText('Campaign series', { exact: true })).toBeVisible();
	await expect(totals.getByText('3', { exact: true })).toBeVisible();
	await expect(totals.getByText('Campaigns', { exact: true })).toBeVisible();
	await expect(totals.getByText('9', { exact: true })).toBeVisible();
	await expect(totals.getByText('Live campaigns', { exact: true })).toBeVisible();
	await expect(totals.getByText('2', { exact: true })).toBeVisible();
	await expect(totals.getByText('Submitted responses', { exact: true })).toBeVisible();
	await expect(totals.getByText('128', { exact: true })).toBeVisible();
	await expect(totals.getByText('Export files', { exact: true })).toBeVisible();
	await expect(totals.getByText('5', { exact: true })).toBeVisible();
	await expect(page.locator(`[href*="${oldFoundationSeriesId}"]`)).toHaveCount(0);

	await expect(nav.getByRole('link', { name: /^Home\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: /^Studies\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: /^Team\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: 'Demo fixtures' })).toHaveCount(0);
	for (const label of selectedSeriesSurfaceLabels) {
		await expect(nav.getByRole('link', { name: label })).toHaveCount(0);
	}
});

test('ui03 product workspace uses quiet section styling instead of raised cards', async ({
	page
}) => {
	await page.goto('/app');

	await expect(page.getByRole('heading', { name: 'Study cockpit', exact: true })).toBeVisible();
	await expect(page.getByLabel('Workspace posture')).toHaveCount(0);

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

	await expect(page.getByRole('heading', { name: 'Study cockpit', exact: true })).toBeVisible();
	const overview = page.getByRole('region', { name: 'Self-serve study cockpit' });
	const lifecycle = overview.getByRole('group', { name: 'Study lifecycle' });
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	const totals = overview.getByRole('group', { name: 'Workspace totals' });
	const suggestedActions = overview.getByRole('region', { name: 'Suggested next actions' });

	for (const label of ['Prepare', 'Collect', 'Review', 'Export']) {
		await expect(lifecycle.getByText(label, { exact: true })).toBeVisible();
	}
	await expectElementBefore(lifecycle, totals);
	await expectElementBefore(sampleStudies, ownStudies);
	await expectElementBefore(ownStudies, totals);

	const sample = sampleStudies.getByRole('link', { name: /Completed sample/i });
	await expect(sample).toHaveAttribute('href', `/app/campaign-series/${sampleSeriesId}/reports`);
	await expect(sample.getByText('Sample study', { exact: true })).toBeVisible();
	await expect(sample.getByText('Review sample results', { exact: true })).toBeVisible();

	const own = ownStudies.getByRole('link', { name: /New team study/i });
	await expect(own).toHaveAttribute('href', `/app/campaign-series/${alternateSeriesId}/setup`);
	await expect(own.getByText('Your study', { exact: true })).toBeVisible();
	await expect(own.getByText('Continue setup', { exact: true })).toBeVisible();

	await expect(suggestedActions.getByText('setup.manage', { exact: true })).toHaveCount(0);
	await expect(
		page.getByRole('navigation', { name: 'Product navigation' }).getByRole('link', {
			name: 'Demo fixtures'
		})
	).toHaveCount(0);
});

test('home leads with the action queue before secondary study context', async ({ page }) => {
	await page.goto('/app');

	const overview = page.getByRole('region', { name: 'Self-serve study cockpit' });
	const nextWork = overview.getByRole('region', { name: 'Suggested next actions' });
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	const lifecycle = overview.getByRole('group', { name: 'Study lifecycle' });
	const totals = overview.getByRole('group', { name: 'Workspace totals' });

	await expectElementBefore(nextWork, sampleStudies);
	await expectElementBefore(sampleStudies, ownStudies);
	await expectElementBefore(ownStudies, lifecycle);
	await expectElementBefore(lifecycle, totals);
});

test('home lifecycle and totals do not use metric cards', async ({ page }) => {
	await page.goto('/app');

	const overview = page.getByRole('region', { name: 'Self-serve study cockpit' });
	const lifecycle = overview.getByRole('group', { name: 'Study lifecycle' });
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
			actionLabel: 'Inspect sample setup',
			portfolioActionLabel: 'Inspect setup',
			readOnlyMessage:
				'Setup sample: read-only starter content showing study preparation before launch.'
		},
		{
			id: collectionSampleSeriesId,
			name: 'Collection in progress sample',
			homeHref: `/app/campaign-series/${collectionSampleSeriesId}/operations`,
			portfolioHref: `/app/campaign-series/${collectionSampleSeriesId}/operations`,
			actionLabel: 'Inspect sample collection',
			portfolioActionLabel: 'Inspect collection',
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
				'Longitudinal sample: read-only starter content showing repeated waves and linked trajectory review.'
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

	const cockpit = page.getByRole('region', { name: 'Self-serve study cockpit' });
	const lifecycle = cockpit.getByRole('group', { name: 'Study lifecycle' });
	const homeSamples = cockpit.getByRole('region', { name: 'Sample studies' });

	for (const label of ['Prepare', 'Collect', 'Review', 'Export']) {
		await expect(lifecycle.getByText(label, { exact: true })).toBeVisible();
	}

	for (const sample of expectedSampleStates) {
		const card = homeSamples.getByRole('link', { name: new RegExp(sample.name) });
		await expect(card).toHaveAttribute('href', sample.homeHref);
		await expect(card.getByText('Sample study', { exact: true })).toBeVisible();
		await expect(card.getByText(sample.readOnlyMessage, { exact: true })).toBeVisible();
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

	const portfolio = page.getByRole('region', { name: 'Study portfolio' });
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
	for (const child of ['Setup', 'Operations', 'Reports', 'Waves']) {
		await expect(hub.getByRole('link', { name: child, exact: true })).toBeVisible();
	}

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);
	const setupWorkspace = page.getByRole('region', { name: 'Setup workspace' });
	await expect(setupWorkspace.getByRole('region', { name: 'Sample study read-only state' })).toBeVisible();
	await expect(setupWorkspace.getByRole('region', { name: 'Study preparation' })).toBeVisible();

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

test('renders route guidance before top-level route work panels', async ({ page }) => {
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

test('first-session hierarchy keeps guidance compact and current work first', async ({ page }) => {
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

	const overview = page.getByRole('region', { name: 'Self-serve study cockpit' });
	const sampleStudies = overview.getByRole('region', { name: 'Sample studies' });
	const ownStudies = overview.getByRole('region', { name: 'Your studies' });
	const suggestedActions = overview.getByRole('region', { name: 'Suggested next actions' });

	await expect(
		sampleStudies.getByText(
			'Sample studies appear here when starter content is available. Open Studies to inspect all visible studies.',
			{ exact: true }
		)
	).toBeVisible();
	await expect(
		ownStudies.getByText(
			'Your editable studies appear here after creation or duplication. Open Studies to create tenant-owned work.',
			{ exact: true }
		)
	).toBeVisible();
	await expect(
		suggestedActions.getByText('Open Studies to review visible sample and own study work.', {
			exact: true
		})
	).toBeVisible();
});

test('renders grouped product navigation by intent on the home surface', async ({ page }) => {
	await page.goto('/app');

	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	const studies = nav.getByRole('group', { name: 'Studies' });
	const people = nav.getByRole('group', { name: 'People and access' });
	const admin = nav.getByRole('group', { name: 'Workspace admin' });

	await expect(studies).toBeVisible();
	await expect(people).toBeVisible();
	await expect(admin).toBeVisible();

	for (const link of [
		{ label: /^Home\b/, href: '/app' },
		{ label: /^Studies\b/, href: '/app/campaign-series' },
		{ label: /^Instrument library\b/, href: '/app/instruments' },
		{ label: /^Exports\b/, href: '/app/exports' }
	]) {
		await expect(studies.getByRole('link', { name: link.label })).toHaveAttribute(
			'href',
			link.href
		);
	}

	for (const link of [
		{ label: /^Directory\b/, href: '/app/directory' },
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
	await expect(nav.getByRole('link', { name: /^Demo fixtures\b/ })).toHaveCount(0);
});

test('renders selected-study navigation separately from global studies links', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	const studies = nav.getByRole('group', { name: 'Studies' });
	const selectedStudy = nav.getByRole('group', { name: 'Selected study' });

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
		{ label: /^Setup\b/, href: `/app/campaign-series/${sampleSeriesId}/setup` },
		{ label: /^Collect\b/, href: `/app/campaign-series/${sampleSeriesId}/operations` },
		{ label: /^Results\b/, href: `/app/campaign-series/${sampleSeriesId}/reports` },
		{ label: /^Waves\b/, href: `/app/campaign-series/${sampleSeriesId}/waves` }
	]) {
		await expect(selectedStudy.getByRole('link', { name: link.label })).toHaveAttribute(
			'href',
			link.href
		);
		await expect(studies.getByRole('link', { name: link.label })).toHaveCount(0);
	}
	await expect(selectedStudy.getByRole('link', { name: /^Results\b/ })).toHaveAttribute(
		'aria-current',
		'page'
	);
});

test('renders authenticated app shell session profile without exposing technical ids by default', async ({
	page
}) => {
	await page.goto('/app');

	const session = page.getByRole('region', { name: 'Authenticated tenant session' });
	await expect(session.getByText(sampleSessionEmail, { exact: true })).toBeVisible();
	await expect(
		session.getByText('Setup management and team management access', { exact: true })
	).toBeVisible();

	const posture = session.getByLabel('Session permission posture');
	await expect(posture.getByText('Setup management', { exact: true })).toBeVisible();
	await expect(posture.getByText('Team management', { exact: true })).toBeVisible();
	await expect(session.getByText(sampleSessionUserId, { exact: true })).toBeHidden();
	await expect(session.getByText(sampleSessionTenantId, { exact: true })).toBeHidden();

	await session.getByText('Technical details', { exact: true }).click();

	await expect(session.getByText(sampleSessionUserId, { exact: true })).toBeVisible();
	await expect(session.getByText(sampleSessionTenantId, { exact: true })).toBeVisible();
});

test('renders tenant settings profile, counts, and management links', async ({ page }) => {
	await page.goto('/app/settings');

	await expect(page.getByRole('heading', { name: 'Settings', exact: true })).toBeVisible();
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: 'Settings' })).toHaveAttribute('aria-current', 'page');

	const settings = page.getByRole('region', { name: 'Tenant settings' });
	await expect(settings.getByText('Occupational Health Lab', { exact: true })).toBeVisible();
	const profile = settings.getByRole('group', { name: 'Tenant profile details' });
	await expect(profile.getByText(sampleSessionTenantId, { exact: true })).toBeVisible();
	await expect(profile.getByText('occupational-health-lab', { exact: true })).toBeVisible();
	await expect(profile.getByText('EU', { exact: true })).toBeVisible();
	await expect(profile.getByText('en', { exact: true })).toBeVisible();
	await expect(profile.getByText('Active', { exact: true })).toBeVisible();
	await expect(profile.getByText('2026-05-01T08:00:00Z', { exact: true })).toBeVisible();
	await expect(profile.getByText('2026-05-12T09:30:00Z', { exact: true })).toBeVisible();

	const counts = settings.getByRole('group', { name: 'Tenant workspace counts' });
	await expect(counts.locator('div').filter({ hasText: 'Campaign series' })).toContainText(
		'3'
	);
	await expect(counts.locator('div').filter({ hasText: 'Live campaigns' })).toContainText(
		'2'
	);
	await expect(counts.locator('div').filter({ hasText: 'Submitted responses' })).toContainText(
		'128'
	);
	await expect(counts.locator('div').filter({ hasText: 'Subjects' })).toContainText('42');
	await expect(counts.locator('div').filter({ hasText: 'Tenant members' })).toContainText(
		'4'
	);
	await expect(settings.locator('.metric-card')).toHaveCount(0);

	const links = settings.getByLabel('Tenant management links');
	await expect(links.getByRole('link', { name: /Campaign series/i })).toHaveAttribute(
		'href',
		'/app/campaign-series'
	);
	await expect(links.getByRole('link', { name: /Team/i })).toHaveAttribute('href', '/app/team');
	await expect(links.getByRole('link', { name: /Directory/i })).toHaveAttribute(
		'href',
		'/app/directory'
	);
});

test('renders instrument library summary and visible instruments', async ({ page }) => {
	await page.goto('/app/instruments');

	await expect(page.getByRole('heading', { name: 'Instruments', exact: true })).toBeVisible();
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: /^Instrument library\b/ })).toHaveAttribute(
		'aria-current',
		'page'
	);

	const library = page.getByRole('region', { name: 'Instrument library' });
	const counts = library.getByRole('group', { name: 'Instrument library counts' });
	await expect(counts.locator('div').filter({ hasText: 'Instruments' })).toContainText('2');
	await expect(counts.locator('div').filter({ hasText: 'Launch eligible' })).toContainText(
		'1'
	);
	await expect(counts.locator('div').filter({ hasText: 'Launch blocked' })).toContainText(
		'1'
	);
	await expect(library.locator('.metric-card')).toHaveCount(0);

	const visible = library.getByLabel('Visible instruments');
	const burnout = visible.getByRole('article', { name: 'Tenant burnout pulse' });
	await expect(burnout.getByText('Tenant burnout pulse', { exact: true })).toBeVisible();
	await expect(burnout.getByText('BURNOUT_16 1.0.0', { exact: true })).toBeVisible();
	await expect(burnout.getByText('Tenant attested', { exact: true })).toBeVisible();
	await expect(
		burnout.getByText('Tenant-provided validated instrument', { exact: true })
	).toBeVisible();
	await expect(burnout.getByText('Launch eligible', { exact: true })).toBeVisible();

	const demo = visible.getByRole('article', { name: 'Internal demo only' });
	await expect(demo.getByText('INTERNAL_DEMO 0.1.0', { exact: true })).toBeVisible();
	await expect(demo.getByText('Unverified internal demo', { exact: true })).toBeVisible();
	await expect(demo.getByText('Launch blocked', { exact: true })).toBeVisible();

	await expect(
		library.getByLabel('Instrument management links').getByRole('link', {
			name: /Campaign series/i
		})
	).toHaveAttribute('href', '/app/campaign-series');
});

test('renders export file library summary and latest artifacts', async ({ page }) => {
	await page.goto('/app/exports');

	await expect(page.getByRole('heading', { name: 'Use exports', exact: true })).toBeVisible();
	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: 'Exports' })).toHaveAttribute('aria-current', 'page');

	const library = page.getByRole('region', { name: 'Export workspace' });
	const overview = library.getByRole('group', { name: 'Export overview' });
	const artifacts = library.getByLabel('Export files');
	const reference = library.getByRole('group', { name: 'Export reference' });

	await expect(overview.getByText('Ready downloads', { exact: true })).toBeVisible();
	await expect(
		overview.getByText('1 export file is ready to download.', { exact: true })
	).toBeVisible();
	await expect(overview.getByText('Needs attention', { exact: true })).toBeVisible();
	await expect(
		overview.getByText('1 export file needs attention.', { exact: true })
	).toBeVisible();
	await expect(
		overview.getByText('Exports cover Report summary export and Response dataset export.', {
			exact: true
		})
	).toBeVisible();
	await expect(
		overview.getByText('Export files are tied to Baseline wave and Response study.', { exact: true })
	).toBeVisible();
	await expectElementBefore(overview, artifacts);

	const counts = reference.getByRole('group', { name: 'Export file counts' });
	await expect(counts.locator('div').filter({ hasText: 'Export files' })).toContainText('2');
	await expect(counts.locator('div').filter({ hasText: 'Downloadable' })).toContainText('1');
	await expect(counts.locator('div').filter({ hasText: 'Failed' })).toContainText('1');
	await expect(counts.locator('div').filter({ hasText: 'Pending' })).toContainText('0');
	await expect(library.locator('.metric-card')).toHaveCount(0);

	const report = artifacts.getByRole('article', { name: 'baseline-report.csv' });
	await expect(report.getByText('baseline-report.csv', { exact: true })).toBeVisible();
	await expect(report.getByText('Report summary export', { exact: true })).toBeVisible();
	await expect(report.getByText('Closed wave', { exact: true }).first()).toBeVisible();
	await expect(
		report.getByText('Use this export for report handoff, summary review, or codebook checks.', {
			exact: true
		})
	).toBeVisible();
	await expect(report.getByText('Baseline wave', { exact: true })).toBeVisible();
	await expect(report.getByText('Succeeded', { exact: true })).toBeVisible();
	await expect(report.getByText('Report summary CSV and codebook', { exact: true })).toBeVisible();
	await expect(report.getByText('Download')).toBeVisible();
	await expect(report.getByText('Available', { exact: true })).toBeVisible();

	const response = artifacts.getByRole('article', { name: 'responses.csv' });
	await expect(response.getByText('Response dataset export', { exact: true })).toBeVisible();
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

	const roster = page.getByRole('region', { name: 'Tenant member roster' });
	await expect(roster.getByText('2', { exact: true })).toBeVisible();
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
	await expect(analyst.getByText('Pending provider link', { exact: true })).toBeVisible();
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
	const roster = page.getByRole('region', { name: 'Tenant member roster' });
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

	const guidance = page.getByRole('region', { name: 'Route guidance' });
	const roster = page.getByRole('region', { name: 'Tenant member roster' });
	await expect(guidance.getByText('team management access')).toBeVisible();
	await expectElementBefore(guidance, roster);
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
	await expect(created.getByText('Pending provider link', { exact: true })).toBeVisible();
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

	await expect(page.getByRole('heading', { name: 'Directory', exact: true })).toBeVisible();
	const overview = page.getByRole('region', { name: 'People and targeting overview' });
	const subjectDirectory = page.getByRole('region', { name: 'Subject directory' });
	const subjectGroups = page.getByRole('region', { name: 'Subject groups' });
	const createRecords = page.getByRole('region', { name: 'Create directory records' });
	const relationships = page.getByRole('region', { name: 'Directory relationships' });

	await expect(overview).toBeVisible();
	await expect(overview.getByText('Study targeting', { exact: true })).toBeVisible();
	await expect(overview.getByText('Group respondent rules', { exact: true })).toBeVisible();
	await expect(overview.getByText('Manager relationships', { exact: true })).toBeVisible();
	await expect(overview.getByText('Reports-of-target paths', { exact: true })).toBeVisible();
	const overviewCounts = overview.locator('[aria-label="People and targeting counts"]');
	await expect(overviewCounts.locator('div').filter({ hasText: 'Subjects' })).toContainText('2');
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

test('renders campaign-series list items from the product read model', async ({ page }) => {
	await page.goto('/app/campaign-series');

	const list = page.getByRole('region', { name: 'Study portfolio' });
	const seriesArticle = list.getByRole('article', { name: 'Quarterly pulse' });
	const seriesItem = seriesArticle.getByRole('link', { name: /Quarterly pulse/i });

	await expect(page.getByRole('heading', { name: 'Studies', exact: true })).toBeVisible();
	await expect(seriesItem).toHaveAttribute('href', `/app/campaign-series/${sampleSeriesId}`);
	await expect(seriesArticle.getByText('Pending', { exact: true })).toBeVisible();
	await expect(seriesArticle.getByText('Campaigns', { exact: true })).toBeVisible();
	await expect(seriesArticle.getByText('7', { exact: true })).toBeVisible();
	await expect(seriesArticle.getByText('Submitted responses', { exact: true })).toBeVisible();
	await expect(seriesArticle.getByText('128', { exact: true })).toBeVisible();
	await expect(page.locator(`[href*="${oldFoundationSeriesId}"]`)).toHaveCount(0);

	const nav = page.getByRole('navigation', { name: 'Product navigation' });
	await expect(nav.getByRole('link', { name: /^Home\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: /^Studies\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: /^Team\b/ })).toBeVisible();
	await expect(nav.getByRole('link', { name: 'Demo fixtures' })).toHaveCount(0);
	for (const label of selectedSeriesSurfaceLabels) {
		await expect(nav.getByRole('link', { name: label })).toHaveCount(0);
	}
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

	const portfolio = page.getByRole('region', { name: 'Study portfolio' });
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
	await expect(ownStudy.getByRole('link', { name: /Continue setup/i })).toHaveAttribute(
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

	const portfolio = page.getByRole('region', { name: 'Study portfolio' });
	const createStudy = page.getByRole('region', { name: 'Create your study' });
	const firstPortfolioRow = portfolio
		.getByRole('article', { name: /Quarterly pulse/i })
		.first();
	const filters = portfolio.getByRole('group', { name: 'Study portfolio filters' });

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

	const list = page.getByRole('region', { name: 'Study portfolio' });
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
	await expect(
		page
			.getByRole('region', { name: 'Setup reference' })
			.getByText(createdSeriesId, { exact: true })
	).toBeVisible();
});

test('renders product surfaces read-only without setup management permission', async ({ page }) => {
	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page, []);

	await page.goto('/app/campaign-series');

	const list = page.getByRole('region', { name: 'Study portfolio' });
	await expect(list.getByRole('link', { name: /Quarterly pulse/i })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create study' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: /Rename Quarterly pulse/i })).toHaveCount(0);
	await expect(page.getByRole('button', { name: /Archive Quarterly pulse/i })).toHaveCount(0);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);
	await expect(page.getByRole('region', { name: 'Setup reference' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create instrument import' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Generate new sample values' })).toHaveCount(0);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);
	await expect(page.getByRole('region', { name: 'Collection reference' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Start collection' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Create open link' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Process local delivery' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Remediate missing scores' })).toHaveCount(0);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);
	await expect(page.getByRole('region', { name: 'Results reference' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create client export' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Create response export' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Review export file' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Download CSV' })).toHaveCount(0);

	await page.goto('/app/team');
	const roster = page.getByRole('region', { name: 'Tenant member roster' });
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
	expect(createRequests).toEqual([{ name: 'New routed pulse' }]);
	await expect(
		page
			.getByRole('region', { name: 'Setup reference' })
			.getByText(createdSeriesId, { exact: true })
	).toBeVisible();
});

test('renders selected study overview from the product read model', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const hub = page.getByRole('region', { name: 'Selected study overview' });
	const reference = hub.getByRole('region', { name: 'Study reference' });

	await expect(page.getByRole('heading', { name: 'Study overview', exact: true })).toBeVisible();
	await expect(hub.getByRole('heading', { name: 'Quarterly pulse', exact: true })).toBeVisible();
	await expect(reference.getByText(sampleSeriesId, { exact: true })).toBeVisible();

	const totals = reference.locator('dl').first();
	await expect(totals.getByText('Campaigns', { exact: true })).toBeVisible();
	await expect(totals.getByText('7', { exact: true })).toBeVisible();
	await expect(totals.getByText('Live campaigns', { exact: true })).toBeVisible();
	await expect(totals.getByText('2', { exact: true })).toBeVisible();
	await expect(totals.getByText('Submitted responses', { exact: true })).toBeVisible();
	await expect(totals.getByText('128', { exact: true })).toBeVisible();
	await expect(totals.getByText('Scores', { exact: true })).toBeVisible();
	await expect(totals.getByText('120', { exact: true })).toBeVisible();
	await expect(totals.getByText('Export files', { exact: true })).toBeVisible();
	await expect(totals.getByText('5', { exact: true })).toBeVisible();

	const governance = reference.getByRole('group', { name: 'Governance status' });
	await expect(governance.getByText('Consent', { exact: true })).toBeVisible();
	await expect(governance.getByText('Retention', { exact: true })).toBeVisible();
	await expect(governance.locator('[data-status="proof_only"]')).toHaveCount(2);
	await expect(governance.getByText('Disclosure', { exact: true })).toBeVisible();
	await expect(governance.getByText('Pending', { exact: true })).toBeVisible();
	await expect(governance.getByText('Scoring', { exact: true })).toBeVisible();
	await expect(governance.getByText('Not configured', { exact: true })).toBeVisible();

	const lifecycle = hub.getByRole('group', { name: 'Study lifecycle' });
	await expect(lifecycle).toBeVisible();
	await expect(lifecycle.getByText('Prepare', { exact: true })).toBeVisible();
	await expect(lifecycle.getByText('Collect', { exact: true })).toBeVisible();
	await expect(lifecycle.getByText('Review results', { exact: true })).toBeVisible();
	await expect(lifecycle.getByText('Compare waves', { exact: true })).toBeVisible();
	await expect(lifecycle.getByText('Collection has submitted responses to monitor.')).toBeVisible();
	await expect(lifecycle.getByRole('link', { name: 'Review setup' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/setup`
	);
	await expect(lifecycle.getByRole('link', { name: 'Open operations' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/operations`
	);
	await expect(lifecycle.getByRole('link', { name: 'Open reports' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/reports`
	);
	await expect(lifecycle.getByRole('link', { name: 'Review waves' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/waves`
	);

	const campaign = reference.getByRole('article', { name: 'Pulse wave 1' });
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

	await expect(hub.getByRole('link', { name: 'Setup', exact: true })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/setup`
	);
	await expect(hub.getByRole('link', { name: 'Operations', exact: true })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/operations`
	);
	await expect(hub.getByRole('link', { name: 'Reports', exact: true })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/reports`
	);
	await expect(hub.getByRole('link', { name: 'Waves', exact: true })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/waves`
	);
});

test('renders selected study overview as a lifecycle map before technical reference', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const overview = page.getByRole('region', { name: 'Selected study overview' });
	const summary = overview.getByRole('region', { name: 'Selected study summary' });
	const lifecycle = overview.getByRole('group', { name: 'Study lifecycle' });
	const reference = overview.getByRole('region', { name: 'Study reference' });

	await expect(page.getByRole('heading', { name: 'Study overview', exact: true })).toBeVisible();
	await expect(
		summary.getByRole('heading', { name: 'Quarterly pulse', exact: true })
	).toBeVisible();
	await expect(summary.getByText('Your study', { exact: true })).toBeVisible();
	await expect(summary.getByText('Active', { exact: true })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Campaign series', exact: true })).toHaveCount(0);
	await expect(page.getByRole('region', { name: 'Campaign series hub' })).toHaveCount(0);
	await expect(overview.getByText('Tenant read model', { exact: true })).toHaveCount(0);
	await expect(overview.getByText('Campaign series details', { exact: true })).toHaveCount(0);

	await expect(lifecycle).toBeVisible();
	await expect(lifecycle.getByRole('article', { name: 'Prepare' })).toBeVisible();
	await expect(lifecycle.getByRole('article', { name: 'Collect' })).toBeVisible();
	await expect(lifecycle.getByRole('article', { name: 'Review results' })).toBeVisible();
	await expect(lifecycle.getByRole('article', { name: 'Compare waves' })).toBeVisible();
	await expect(lifecycle.getByText('exports', { exact: false })).toBeVisible();
	await expect(lifecycle.getByRole('link', { name: 'Review setup' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/setup`
	);
	await expect(lifecycle.getByRole('link', { name: 'Open operations' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/operations`
	);
	await expect(lifecycle.getByRole('link', { name: 'Open reports' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/reports`
	);
	await expect(lifecycle.getByRole('link', { name: 'Review waves' })).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/waves`
	);
	await expect(reference.getByText(sampleSeriesId, { exact: true })).toBeVisible();
	await expectElementBefore(lifecycle, reference);
});

test('selected study overview uses action rows instead of lifecycle cards', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const overview = page.getByRole('region', { name: 'Selected study overview' });
	const lifecycle = overview.getByRole('group', { name: 'Study lifecycle' });
	const reference = page.getByRole('region', { name: 'Study reference' });

	await expectElementBefore(lifecycle, reference);
	await expect(lifecycle.locator('.selected-lifecycle-row')).toHaveCount(4);
	await expect(lifecycle.locator('article.record-row')).toHaveCount(0);
	await expect(reference.locator('[class*="rounded"][class*="border"]')).toHaveCount(0);
});

test('selected study reference totals do not use metric cards', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}`);

	const overview = page.getByRole('region', { name: 'Selected study overview' });
	const reference = overview.getByRole('region', { name: 'Study reference' });
	const totals = reference.locator('dl').first();

	await expect(reference).toBeVisible();
	await expect(totals.getByText('Campaigns', { exact: true })).toBeVisible();
	await expect(overview.locator('.metric-card')).toHaveCount(0);
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
	await expect(
		page
			.getByRole('region', { name: 'Setup reference' })
			.getByText(createdSeriesId, { exact: true })
	).toBeVisible();
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
	await expect(
		hub.getByRole('region', { name: 'Study reference' }).getByText(alternateSeriesId, {
			exact: true
		})
	).toBeVisible();
	await expect(hub.getByRole('link', { name: 'Setup', exact: true })).toHaveAttribute(
		'href',
		`/app/campaign-series/${alternateSeriesId}/setup`
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
	await expect(
		hub.getByRole('region', { name: 'Study reference' }).getByText(retrySeriesId, {
			exact: true
		})
	).toBeVisible();
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([`/campaign-series/${retrySeriesId}`, `/campaign-series/${retrySeriesId}`]);
});

test('renders the campaign-series product route map', async ({ page }) => {
	const routes = [
		{ path: '/app/campaign-series', heading: 'Studies', region: 'Study portfolio' },
		{
			path: `/app/campaign-series/${sampleSeriesId}`,
			heading: 'Study overview',
			region: 'Selected study overview'
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			heading: 'Prepare study',
			region: 'Setup workspace'
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			heading: 'Collect responses',
			region: 'Collection workspace'
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			heading: 'Review results',
			region: 'Results workspace'
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			heading: 'Waves',
			region: 'Waves and linked trajectories'
		}
	];

	for (const route of routes) {
		await page.goto(route.path);
		await expect(page.getByRole('heading', { name: route.heading, exact: true })).toBeVisible();
		await expect(page.getByRole('region', { name: route.region })).toBeVisible();

		const nav = page.getByRole('navigation', { name: 'Product navigation' });
		const expectedCurrentNav =
			route.path === '/app/campaign-series'
				? /^Studies\b/
				: route.path.endsWith(`/${sampleSeriesId}`)
					? /^Overview\b/
					: route.path.endsWith('/setup')
						? /^Setup\b/
						: route.path.endsWith('/operations')
							? /^Collect\b/
							: route.path.endsWith('/reports')
								? /^Results\b/
								: new RegExp(`^${route.heading}\\b`);
		await expect(nav.locator('[aria-current="page"]')).toHaveCount(1);
		await expect(nav.getByRole('link', { name: expectedCurrentNav })).toHaveAttribute(
			'aria-current',
			'page'
		);

		if (route.path.includes(sampleSeriesId)) {
			const selectedStudy = nav.getByRole('group', { name: 'Selected study' });
			await expect(selectedStudy).toBeVisible();
			await expect(selectedStudy.getByRole('link', { name: /^Overview\b/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}`
			);
			await expect(selectedStudy.getByRole('link', { name: /^Setup\b/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}/setup`
			);
			await expect(selectedStudy.getByRole('link', { name: /^Collect\b/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}/operations`
			);
			await expect(selectedStudy.getByRole('link', { name: /^Results\b/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}/reports`
			);
			await expect(selectedStudy.getByRole('link', { name: /^Waves\b/ })).toHaveAttribute(
				'href',
				`/app/campaign-series/${sampleSeriesId}/waves`
			);
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
			region: 'Setup workspace',
			expected: ['Quarterly pulse', sampleSeriesId, 'Consent', 'Retention', 'Template', 'Scoring']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			region: 'Collection workspace',
			expected: [
				'Quarterly pulse',
				sampleSeriesId,
				'Pulse wave 1',
				'Collection progress',
				'Open-link assignments',
				'Delivery attempts',
				'Selected-series collection workflow'
			]
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			region: 'Results workspace',
			expected: [
				'Quarterly pulse',
				sampleSeriesId,
				'Reportable campaigns',
				'Visible scores',
				'Suppressed scores',
				'launch-snapshot-id',
				'report-proof.csv',
				'Scores',
				'Export files',
				'Selected-series report snapshot',
				'Selected-series review and export workflow'
			]
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			region: 'Waves and linked trajectories',
			expected: [
				'Quarterly pulse',
				sampleSeriesId,
				'Pulse wave 1',
				'Longitudinal waves',
				'Linked trajectories',
				'Visible comparisons',
				'Selected-series wave comparison snapshot',
				'Selected-series waves workflow'
			]
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

	const setupWorkspace = page.getByRole('region', { name: 'Setup workspace' });
	const readOnlyState = setupWorkspace.getByRole('region', {
		name: 'Sample study read-only state'
	});
	const guidance = setupWorkspace.getByRole('region', { name: 'Route guidance' });
	const preparation = setupWorkspace.getByRole('region', { name: 'Study preparation' });

	await expect(readOnlyState).toBeVisible();
	await expect(readOnlyState.getByText('Sample study', { exact: true })).toBeVisible();
	await expect(
		readOnlyState.getByText(
			'Results sample: read-only starter content showing collected responses, scores, reports, and exports.',
			{ exact: true }
		)
	).toBeVisible();
	await expect(guidance.getByRole('heading', { name: 'Prepare before collection' })).toBeVisible();
	await expect(guidance.getByText('Sample studies are read-only examples')).toBeVisible();
	await expectElementBefore(readOnlyState, guidance);
	await expectElementBefore(guidance, preparation);
});

test('selected-series destinations put primary work before reference context', async ({ page }) => {
	const destinations = [
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			shell: 'Setup workspace',
			primary: { role: 'group' as const, name: 'Preparation actions' },
			reference: { role: 'region' as const, name: 'Setup reference' }
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			shell: 'Collection workspace',
			primary: { role: 'group' as const, name: 'Collection progress' },
			reference: { role: 'region' as const, name: 'Collection reference' }
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			shell: 'Results workspace',
			primary: { role: 'group' as const, name: 'Results overview' },
			reference: { role: 'region' as const, name: 'Results reference' }
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			shell: 'Waves and linked trajectories',
			primary: { role: 'group' as const, name: 'Wave comparison snapshot' },
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
		{ label: 'Hub', href: `/app/campaign-series/${sampleSeriesId}` },
		{ label: 'Setup', href: `/app/campaign-series/${sampleSeriesId}/setup` },
		{ label: 'Operations', href: `/app/campaign-series/${sampleSeriesId}/operations` },
		{ label: 'Reports', href: `/app/campaign-series/${sampleSeriesId}/reports` },
		{ label: 'Waves', href: `/app/campaign-series/${sampleSeriesId}/waves` }
	];
	const destinations = [
		{
			path: `/app/campaign-series/${sampleSeriesId}`,
			shell: 'Selected study overview',
			current: 'Hub',
			primary: { role: 'group' as const, name: 'Study lifecycle' },
			reference: { role: 'region' as const, name: 'Study reference' },
			safetyLabels: ['Pending']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			shell: 'Setup workspace',
			current: 'Setup',
			primary: { role: 'group' as const, name: 'Preparation actions' },
			reference: { role: 'region' as const, name: 'Setup reference' },
			safetyLabels: ['Consent', 'Retention']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			shell: 'Collection workspace',
			current: 'Operations',
			primary: { role: 'group' as const, name: 'Collection progress' },
			reference: { role: 'region' as const, name: 'Collection reference' },
			safetyLabels: ['Preview ready']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			shell: 'Results workspace',
			current: 'Reports',
			primary: { role: 'group' as const, name: 'Results overview' },
			reference: { role: 'region' as const, name: 'Results reference' },
			safetyLabels: ['Preview ready', 'Finality and provenance', 'Suppressed scores']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			shell: 'Waves and linked trajectories',
			current: 'Waves',
			primary: { role: 'group' as const, name: 'Wave comparison snapshot' },
			reference: { role: 'region' as const, name: 'Waves selected-series context' },
			safetyLabels: ['Preview ready']
		}
	];

	for (const destination of destinations) {
		await page.goto(destination.path);
		const shell = page.getByRole('region', { name: destination.shell });
		const workspace = shell.getByRole('group', { name: 'Selected series workspace' });
		const primary = shell.getByRole(destination.primary.role, { name: destination.primary.name });
		const reference = shell.getByRole(destination.reference.role, {
			name: destination.reference.name
		});

		await expect(workspace).toBeVisible();
		await expect(workspace.getByText('Quarterly pulse', { exact: true }).first()).toBeVisible();
		await expect(workspace.getByText(sampleSeriesId, { exact: true })).toBeVisible();

		for (const link of workspaceLinks) {
			await expect(workspace.getByRole('link', { name: link.label, exact: true })).toHaveAttribute(
				'href',
				link.href
			);
		}

		await expect(workspace.locator('[aria-current="page"]')).toHaveCount(1);
		await expect(
			workspace.getByRole('link', { name: destination.current, exact: true })
		).toHaveAttribute('aria-current', 'page');
		await expect(primary).toBeVisible();
		await expect(reference).toBeVisible();
		await expectElementBefore(workspace, primary);
		await expectElementBefore(primary, reference);

		for (const safetyLabel of destination.safetyLabels) {
			await expect(shell.getByText(safetyLabel, { exact: true }).first()).toBeVisible();
		}
	}
});

test('product vocabulary avoids proof-harness phrases in normal app workflows', async ({
	page
}) => {
	const routes = [
		{ path: '/app', surface: 'Self-serve study cockpit', allowedLabels: [] },
		{ path: '/app/campaign-series', surface: 'Study portfolio', allowedLabels: [] },
		{
			path: `/app/campaign-series/${sampleSeriesId}`,
			surface: 'Selected study overview',
			allowedLabels: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/setup`,
			surface: 'Setup workspace',
			allowedLabels: []
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/operations`,
			surface: 'Collection workspace',
			allowedLabels: ['Preview ready']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/reports`,
			surface: 'Results workspace',
			allowedLabels: ['Preview ready', 'not validated interpretation']
		},
		{
			path: `/app/campaign-series/${sampleSeriesId}/waves`,
			surface: 'Waves and linked trajectories',
			allowedLabels: ['Preview ready']
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
		/\bWave comparison proof\b/
	];

	for (const route of routes) {
		await page.goto(route.path);
		await expect(page.getByRole('region', { name: route.surface })).toBeVisible();
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
	const overview = page.getByRole('region', { name: 'Self-serve study cockpit' });
	await expectElementBefore(
		overview.getByRole('group', { name: 'Study lifecycle' }),
		overview.getByRole('group', { name: 'Workspace totals' })
	);

	await page.goto('/app/campaign-series');
	const portfolio = page.getByRole('region', { name: 'Study portfolio' });
	await expect(portfolio.getByRole('group', { name: 'Study portfolio filters' })).toBeVisible();
	await expect(portfolio.getByRole('article', { name: /Quarterly pulse/i })).toBeVisible();

	await page.goto(`/app/campaign-series/${sampleSeriesId}`);
	const hub = page.getByRole('region', { name: 'Selected study overview' });
	await expectElementBefore(
		hub.getByRole('group', { name: 'Study lifecycle' }),
		hub.getByRole('region', { name: 'Study reference' })
	);

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);
	const reports = page.getByRole('region', { name: 'Results workspace' });
	await expectElementBefore(
		reports.getByRole('group', { name: 'Results overview' }),
		reports.getByRole('region', { name: 'Results reference' })
	);
	await expect(reports.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(reports.getByText('Finality and provenance', { exact: true })).toBeVisible();
});

test('waves workflow renders primary actions instead of the proof workbench', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const guidance = waves.getByRole('region', { name: 'Route guidance' });
	const workflow = waves.getByRole('group', { name: 'Waves action workflow' });

	await expect(
		guidance.getByRole('heading', { name: 'Advanced longitudinal context' })
	).toBeVisible();
	await expect(guidance.getByText('single-wave')).toBeVisible();
	await expect(workflow).toBeVisible();
	await expect(
		workflow.getByRole('heading', { name: 'Selected-series waves workflow' })
	).toBeVisible();
	await expect(workflow.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(waves.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);
});

test('waves longitudinal overview leads before mechanics and current task leads workflow', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const overview = waves.getByRole('group', { name: 'Longitudinal analysis overview' });
	const snapshot = waves.getByRole('group', { name: 'Wave comparison snapshot' });
	const workflow = waves.getByRole('group', { name: 'Waves action workflow' });
	const currentTask = workflow.getByRole('region', { name: 'Current waves task' });
	const path = workflow.getByRole('list', { name: 'Waves path' });
	const reference = waves.getByRole('region', { name: 'Waves selected-series context' });

	await expect(overview).toBeVisible();
	await expect(overview.getByRole('heading', { name: 'Longitudinal analysis overview' })).toBeVisible();
	await expect(overview.getByText('Repeated-wave studies', { exact: true })).toBeVisible();
	await expect(overview.getByText('Single-wave studies', { exact: true })).toBeVisible();
	await expectMetricValue(overview, 'Longitudinal waves', '2');
	await expectMetricValue(overview, 'Complete trajectories', '6');
	await expectMetricValue(overview, 'Visible comparisons', '1');
	await expectMetricValue(overview, 'Suppressed comparisons', '0');
	await expectMetricValue(overview, 'Blocked comparisons', '0');
	await expectFieldValue(overview, 'Baseline wave', 'Pulse wave 1');
	await expectFieldValue(overview, 'Comparison wave', 'Pulse wave 2');
	await expectFieldValue(overview, 'Compatibility', 'compatible');
	await expectElementBefore(overview, snapshot);
	await expectElementBefore(overview, workflow);
	await expectElementBefore(overview, reference);
	await expectElementBefore(currentTask, path);
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

	const operations = page.getByRole('region', { name: 'Collection workspace' });
	const workspace = operations.getByRole('region', {
		name: 'Collection reference'
	});

	await expect(
		workspace.getByRole('heading', { name: 'Collection reference', exact: true })
	).toBeVisible();
	await expect(workspace.getByText(sampleSeriesId, { exact: true })).toBeVisible();
	await expect(workspace.getByText('Open-link assignments', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Queued invitations', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Sent invitations', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Failed invitations', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Delivery attempts', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByRole('article', { name: 'Pulse wave 1' })).toBeVisible();
	await expect(workspace.getByRole('article', { name: 'Draft wave' })).toBeVisible();
	await expect(workspace.getByText('launch-snapshot-id', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('2026-05-05T09:30:00Z', { exact: true }).first()).toBeVisible();

	const collectionMonitor = operations.getByRole('group', { name: 'Collection monitor' });
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

	const scoreCoverage = operations.getByRole('group', { name: 'Score coverage' });
	await expect(scoreCoverage).toBeVisible();
	await expect(scoreCoverage.getByText('Scored submitted', { exact: true })).toBeVisible();
	await expect(scoreCoverage.getByText('Unscored submitted', { exact: true })).toBeVisible();
	await expect(
		scoreCoverage.getByText('All submitted responses have successful scoring activity.')
	).toBeVisible();
	await expect(scoreCoverage.getByText('sensitive.recipient@example.test')).toHaveCount(0);
	await expect(scoreCoverage.getByText('sensitive-answer')).toHaveCount(0);
	await expect(scoreCoverage.getByText('opn_')).toHaveCount(0);
	await expect(scoreCoverage.getByText('inv_')).toHaveCount(0);

	const workflow = operations.getByRole('group', { name: 'Collection actions' });
	await expect(workflow).toBeVisible();
	await expect(
		workflow.getByRole('heading', { name: 'Selected-series collection workflow' })
	).toBeVisible();
	await expect(workflow.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(operations.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);
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

	await expect(page.getByRole('heading', { name: 'Collect responses', exact: true })).toBeVisible();

	const collection = page.getByRole('region', { name: 'Collection workspace' });
	const progress = collection.getByRole('group', { name: 'Collection progress' });
	const workflow = collection.getByRole('group', { name: 'Collection actions' });
	const reference = collection.getByRole('region', { name: 'Collection reference' });

	await expect(progress).toBeVisible();
	await expect(workflow).toBeVisible();
	await expect(reference).toBeVisible();
	await expectElementBefore(progress, workflow);
	await expectElementBefore(workflow, reference);

	await expect(progress.getByText('Pulse wave 1 is live', { exact: true })).toBeVisible();
	await expect(progress.getByText('anonymous', { exact: true }).first()).toBeVisible();
	await expect(
		progress.getByText('1 open link, 8 sent invitations, 9 delivery attempts', { exact: true })
	).toBeVisible();
	await expect(
		progress.getByText('133 started, 5 draft, 128 submitted', { exact: true })
	).toBeVisible();
	await expect(
		progress.getByText('128 of 128 submitted responses scored', { exact: true })
	).toBeVisible();
	await expect(progress.getByText('2026-05-05T10:45:00Z', { exact: true })).toBeVisible();
	await expect(progress.getByText('launch-snapshot-id', { exact: true })).toHaveCount(0);
	await expect(progress.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);

	await expect(
		workflow.getByRole('heading', { name: 'Selected-series collection workflow' })
	).toBeVisible();
	await expect(workflow.getByText('Preview ready', { exact: true }).first()).toBeVisible();

	await expect(reference.getByText(sampleSeriesId, { exact: true })).toBeVisible();
	await expect(reference.getByText('launch-snapshot-id', { exact: true }).first()).toBeVisible();
	await expect(reference.getByRole('article', { name: 'Pulse wave 1' })).toBeVisible();
	await expect(reference.getByRole('article', { name: 'Draft wave' })).toBeVisible();
});

test('operations launch snapshot review shows frozen campaign configuration', async ({ page }) => {
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

	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);

	const operations = page.getByRole('region', { name: 'Collection workspace' });
	const workflow = operations.getByRole('group', { name: 'Collection actions' });
	await expect(workflow.getByRole('heading', { name: 'Current collection task' })).toBeVisible();
	await expect(workflow.getByRole('region', { name: 'Current collection task' })).toContainText(
		'Launch readiness'
	);
	await expect(workflow.getByRole('button', { name: 'Check launch readiness' })).toBeVisible();
	await expect(workflow.getByRole('button', { name: 'Start collection' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Create open link' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Queue email invitations' })).toHaveCount(0);
	await expect(workflow.getByRole('button', { name: 'Process local delivery' })).toHaveCount(0);
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

	const operations = page.getByRole('region', { name: 'Collection workspace' });
	await expect(
		operations.getByText('Retryable operations pulse', { exact: false }).first()
	).toBeVisible();
	await expect(operations.getByText(retrySeriesId, { exact: true }).first()).toBeVisible();
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

	const operations = page.getByRole('region', { name: 'Collection workspace' });
	const workflow = operations.getByRole('group', { name: 'Collection actions' });
	const currentTask = workflow.getByRole('region', { name: 'Current collection task' });
	await expect(workflow).toBeVisible();
	await expect(currentTask).toContainText('Launch readiness');
	await expect(workflow.getByRole('button', { name: 'Start collection' })).toHaveCount(0);
	await workflow.getByRole('button', { name: 'Check launch readiness' }).click();
	await expect(currentTask).toContainText('Start collection');
	await expect(workflow.getByRole('button', { name: 'Start collection' })).toBeEnabled();
	await workflow.getByRole('button', { name: 'Start collection' }).click();
	await expect(currentTask).toContainText('Open-link entry');
	const openLinkButton = workflow.getByRole('button', { name: 'Create open link' });
	await expect(openLinkButton).toBeEnabled();
	await openLinkButton.click();
	await expect(workflow.getByText('/r/opn_test', { exact: true }).first()).toBeVisible();
	await expect(currentTask).toContainText('Invitation batch');
	await workflow.getByRole('button', { name: 'Queue email invitations' }).click();
	await expect(workflow.getByText('ada.ops@example.com', { exact: true })).toBeVisible();
	await expect(currentTask).toContainText('Local delivery');
	await workflow.getByRole('button', { name: 'Process local delivery' }).click();
	await expect(workflow.getByText('local', { exact: true }).first()).toBeVisible();

	expect(readinessCampaignIds).toEqual([selectedCampaignId]);
	expect(launchCampaignIds).toEqual([selectedCampaignId]);
	expect(openLinkCampaignIds).toEqual([selectedCampaignId]);
	expect(invitationCampaignIds).toEqual([selectedCampaignId]);
	expect(deliveryCampaignIds).toEqual([selectedCampaignId]);
	await expect.poll(() => operationsWorkspaceRequestCount).toBeGreaterThanOrEqual(6);
	await expect(operations.getByText('Campaigns', { exact: true }).first()).toBeVisible();
	await expect(operations.getByText('3', { exact: true }).first()).toBeVisible();
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
	const workspace = reports.getByRole('region', {
		name: 'Results reference'
	});

	await expect(
		workspace.getByRole('heading', { name: 'Results reference', exact: true })
	).toBeVisible();
	await expect(
		reports
			.getByRole('group', { name: 'Selected series workspace' })
			.getByText('Quarterly pulse', {
				exact: true
			})
			.first()
	).toBeVisible();
	await expect(workspace.getByText(sampleSeriesId, { exact: true })).toBeVisible();
	await expect(workspace.getByText('Reportable campaigns', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Visible scores', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Suppressed scores', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByRole('group', { name: 'Results selected campaign' })).toBeVisible();
	await expect(workspace.getByText('launch-snapshot-id', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('report-proof.csv', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByRole('article', { name: 'Pulse wave 1' })).toBeVisible();
	await expect(workspace.getByRole('article', { name: 'Draft wave' })).toBeVisible();

	const scoreCoverage = reports.getByRole('group', { name: 'Score coverage' });
	await expect(scoreCoverage).toBeVisible();
	await expect(scoreCoverage.getByText('Scored submitted', { exact: true })).toBeVisible();
	await expect(scoreCoverage.getByText('Unscored submitted', { exact: true })).toBeVisible();
	await expect(
		scoreCoverage.getByText(
			'Some submitted responses still need scoring activity before score-dependent reports are complete.'
		)
	).toBeVisible();

	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	await expect(workflow).toBeVisible();
	await expect(
		workflow.getByRole('heading', { name: 'Selected-series review and export workflow' })
	).toBeVisible();
	await expect(workflow.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(reports.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);
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

	await expect(page.getByRole('heading', { name: 'Review results', exact: true })).toBeVisible();

	const reports = page.getByRole('region', { name: 'Results workspace' });
	const overview = reports.getByRole('group', { name: 'Results overview' });
	const widgets = reports.getByRole('region', { name: 'Results summary' });
	const snapshot = reports.getByRole('group', { name: 'Report snapshot' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	const reference = reports.getByRole('region', { name: 'Results reference' });

	await expect(overview).toBeVisible();
	await expect(
		overview.getByText('Pulse wave 1 has preview results from 128 submitted responses.', {
			exact: true
		})
	).toBeVisible();
	await expect(
		overview.getByText(
			'115 visible scores, 5 suppressed scores, 8 submitted responses still unscored.',
			{ exact: true }
		)
	).toBeVisible();
	await expect(
		overview.getByText(
			'Results are from a live campaign with not validated interpretation and no closed-wave finality yet.',
			{ exact: true }
		)
	).toBeVisible();
	await expect(
		overview.getByText('Latest export report-proof.csv is downloadable.', { exact: true })
	).toBeVisible();
	await expect(overview.getByText(sampleSeriesId, { exact: true })).toHaveCount(0);
	await expect(overview.getByText('launch-snapshot-id', { exact: true })).toHaveCount(0);

	await expect(widgets).toBeVisible();
	await expect(snapshot).toBeVisible();
	await expect(workflow).toBeVisible();
	await expect(
		workflow.getByRole('heading', { name: 'Selected-series review and export workflow' })
	).toBeVisible();
	await expect(reference).toBeVisible();
	await expect(reference.getByText(sampleSeriesId, { exact: true })).toBeVisible();
	await expect(reference.getByText('launch-snapshot-id', { exact: true }).first()).toBeVisible();
	await expect(reference.getByText('export_artifact.missing', { exact: true })).toBeVisible();
	await expect(reference.getByRole('article', { name: 'Pulse wave 1' })).toBeVisible();

	await expectElementBefore(overview, widgets);
	await expectElementBefore(widgets, snapshot);
	await expectElementBefore(snapshot, workflow);
	await expectElementBefore(workflow, reference);
});

test('Results summary render known and unsupported manifest widgets before workflow', async ({
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
	const widgets = reports.getByRole('region', { name: 'Results summary' });
	await expect(widgets).toBeVisible();

	for (const title of [
		'Report readiness',
		'Score coverage',
		'Selected campaign report state',
		'Export files',
		'Visual analytics',
		'Finality and provenance'
	]) {
		await expect(widgets.getByRole('article', { name: title })).toBeVisible();
	}

	await expect(widgets.getByText('Unsupported', { exact: true })).toHaveCount(0);
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
	await expect(reports.getByRole('group', { name: 'Report snapshot' })).toBeVisible();
	await expect(reports.getByRole('group', { name: 'Review and export actions' })).toBeVisible();

	releaseManifest();
	const widgets = reports.getByRole('region', { name: 'Results summary' });
	await expect(widgets.getByRole('article', { name: 'Score coverage' })).toBeVisible();
});

test('report snapshot renders selected campaign aggregate proof as route content', async ({
	page
}) => {
	const reportProofCampaignIds: string[] = [];

	await page.route('**/campaigns/*/report-proof', async (route) => {
		reportProofCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({ json: sampleCampaignReportProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Results workspace' });
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

test('report dashboard renders selected campaign decision surface semantics', async ({ page }) => {
	const reportProofCampaignIds: string[] = [];

	await page.route('**/campaigns/*/report-proof', async (route) => {
		reportProofCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({ json: sampleCampaignReportProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	const reports = page.getByRole('region', { name: 'Results workspace' });
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

test('report snapshot blocks without calling report proof when no reportable campaign is selected', async ({
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
	const snapshot = reports.getByRole('group', { name: 'Report snapshot' });

	await expect(snapshot.getByText('Not available', { exact: true })).toBeVisible();
	await expect(
		snapshot.getByText('Create or select a campaign before loading the report snapshot.')
	).toBeVisible();
	await expect(snapshot.getByRole('button', { name: 'Refresh report snapshot' })).toBeDisabled();
	await expect(reports.getByRole('group', { name: 'Review and export actions' })).toBeVisible();
	await expect.poll(() => reportProofCampaignIds).toEqual([]);
});

test('report snapshot keeps endpoint failures local and recovers on retry', async ({ page }) => {
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
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

test('reports workflow exposes one current reports task for a reportable campaign', async ({
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
	await expect(reports.getByText('Retryable reports pulse', { exact: true }).first()).toBeVisible();
	await expect(reports.getByText(retrySeriesId, { exact: true }).first()).toBeVisible();
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
	const exportCampaignIds: string[] = [];
	const responseExportSeriesIds: string[] = [];
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
	await page.route('**/campaigns/*/report-proof/exports', async (route) => {
		exportCampaignIds.push(campaignIdFromPath(route.request().url()));
		await route.fulfill({ status: 201, json: sampleReportProofExportArtifact });
	});
	await page.route('**/campaign-series/*/response-exports', async (route) => {
		responseExportSeriesIds.push(seriesIdFromPath(route.request().url()));
		await route.fulfill({ status: 201, json: sampleResponseExportArtifact });
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
		if (pathname.endsWith('/download')) {
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
	const workflow = reports.getByRole('group', { name: 'Review and export actions' });
	await expect(workflow).toBeVisible();
	const currentTask = workflow.getByRole('region', { name: 'Current review task' });
	await expect(currentTask).toContainText('Report preview');
	const reportProofButton = currentTask.getByRole('button', { name: 'View report preview' });
	await expect(reportProofButton).toBeEnabled();
	await reportProofButton.click();
	await expect(workflow.getByRole('region', { name: 'Report preview' })).toBeVisible();

	await expect(currentTask).toContainText('Export file');
	const exportButton = currentTask.getByRole('button', { name: 'Create client export' });
	await expect(exportButton).toBeEnabled();
	await exportButton.click();
	await expect(workflow.getByRole('region', { name: 'Report export result' })).toBeVisible();

	await expect(currentTask).toContainText('Response export');
	const responseExportButton = currentTask.getByRole('button', { name: 'Create response export' });
	await expect(responseExportButton).toBeEnabled();
	await responseExportButton.click();
	await expect(workflow.getByRole('region', { name: 'Response export result' })).toBeVisible();

	await expect(currentTask).toContainText('Review export file');
	await currentTask.getByRole('button', { name: 'Review export file' }).click();
	await expect(currentTask).toContainText('CSV download');
	await currentTask.getByRole('button', { name: 'Download CSV' }).click();
	await expect(currentTask.getByText('Downloaded CSV', { exact: true })).toBeVisible();
	await expect(
		currentTask.getByText('response_row_id,trajectory_id', { exact: true })
	).toBeVisible();

	expect(reportProofCampaignIds).toEqual([
		sampleReportsWorkspace.selectedCampaign?.id,
		sampleReportsWorkspace.selectedCampaign?.id
	]);
	expect(exportCampaignIds).toEqual([sampleReportsWorkspace.selectedCampaign?.id]);
	expect(responseExportSeriesIds).toEqual([sampleSeriesId]);
	expect(fetchedArtifactIds).toEqual([sampleResponseExportArtifact.id]);
	expect(downloadedArtifactIds).toEqual([sampleResponseExportArtifact.id]);
	await expect.poll(() => reportsWorkspaceRequestCount).toBeGreaterThanOrEqual(3);
	await expect(
		reports.getByText('updated-report-proof.csv', { exact: true }).first()
	).toBeVisible();
	await expect(reports.getByText('Export files', { exact: true }).first()).toBeVisible();
	await expect(reports.getByText('1', { exact: true }).first()).toBeVisible();
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

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const workspace = waves.getByRole('region', {
		name: 'Waves selected-series context'
	});

	await expect(
		workspace.getByRole('heading', { name: 'Quarterly pulse', exact: true })
	).toBeVisible();
	await expect(workspace.getByText(sampleSeriesId, { exact: true })).toBeVisible();
	await expect(workspace.getByText('Longitudinal waves', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Linked trajectories', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Complete trajectories', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('Visible comparisons', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByRole('group', { name: 'Waves selected waves' })).toBeVisible();
	await expect(workspace.getByRole('group', { name: 'Waves provenance' })).toBeVisible();
	await expect(workspace.getByText('wave-1-launch-id', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByText('burnout.total', { exact: true }).first()).toBeVisible();
	await expect(workspace.getByRole('article', { name: 'Pulse wave 1' })).toBeVisible();
	await expect(workspace.getByRole('article', { name: 'Pulse wave 2' })).toBeVisible();

	const workflow = waves.getByRole('group', { name: 'Waves action workflow' });
	await expect(workflow).toBeVisible();
	await expect(
		workflow.getByRole('heading', { name: 'Selected-series waves workflow' })
	).toBeVisible();
	await expect(workflow.getByText('Preview ready', { exact: true }).first()).toBeVisible();
	await expect(waves.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);
	await expect
		.poll(() => requestedProductPaths)
		.toEqual([`/campaign-series/${sampleSeriesId}/waves-workspace`]);
});

test('wave comparison snapshot renders selected wave proof as route content', async ({ page }) => {
	const comparisonSeriesIds: string[] = [];

	await page.route(`**/campaign-series/${sampleSeriesId}/wave-comparison-proof`, async (route) => {
		comparisonSeriesIds.push(new URL(route.request().url()).pathname.split('/')[2]);
		await route.fulfill({ json: sampleWaveComparisonProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const snapshot = waves.getByRole('group', { name: 'Wave comparison snapshot' });

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

test('wave dashboard renders selected comparison decision surface semantics', async ({ page }) => {
	const comparisonSeriesIds: string[] = [];

	await page.route(`**/campaign-series/${sampleSeriesId}/wave-comparison-proof`, async (route) => {
		comparisonSeriesIds.push(new URL(route.request().url()).pathname.split('/')[2]);
		await route.fulfill({ json: sampleWaveComparisonProof });
	});

	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const snapshot = waves.getByRole('group', { name: 'Wave comparison snapshot' });
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

	const scoreRows = dashboard.getByRole('region', { name: 'Wave comparison rows' });
	await expect(scoreRows.getByRole('article', { name: 'Wave comparison total' })).toBeVisible();
	await expect(scoreRows.getByText('aggregate delta -0.30', { exact: true })).toBeVisible();
	await expect(scoreRows.getByText('paired delta -0.25', { exact: true })).toBeVisible();
	await expect(
		scoreRows.getByRole('article', { name: 'Wave comparison exhaustion' })
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

test('wave comparison snapshot blocks without calling proof when comparison is not ready', async ({
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
	const snapshot = waves.getByRole('group', { name: 'Wave comparison snapshot' });

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

test('wave comparison snapshot keeps endpoint failures local and recovers on retry', async ({
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
	const snapshot = waves.getByRole('group', { name: 'Wave comparison snapshot' });

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

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const workspace = waves.getByRole('region', { name: 'Waves selected-series context' });
	await expect(waves.getByRole('heading', { name: 'Retryable waves pulse' })).toBeVisible();
	await expect(workspace.getByText(retrySeriesId, { exact: true })).toBeVisible();
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

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const workflow = waves.getByRole('group', { name: 'Waves action workflow' });
	const currentTask = workflow.getByRole('region', { name: 'Current waves task' });

	await expect(workflow.getByRole('heading', { name: 'Current waves task' })).toBeVisible();
	await expect(currentTask).toContainText('Linked trajectory check');
	await expect(workflow.getByRole('button', { name: 'View wave comparison preview' })).toHaveCount(
		0
	);

	const twoWaveButton = currentTask.getByRole('button', {
		name: 'Run linked trajectory check'
	});
	await expect(twoWaveButton).toBeEnabled();
	await twoWaveButton.click();
	await expect(workflow.getByRole('region', { name: 'Linked trajectory check' })).toBeVisible();

	await expect(currentTask).toContainText('Wave comparison preview');
	const comparisonButton = currentTask.getByRole('button', {
		name: 'View wave comparison preview'
	});
	await expect(comparisonButton).toBeEnabled();
	await comparisonButton.click();

	expect(twoWaveSeriesIds).toEqual([sampleSeriesId]);
	expect(comparisonSeriesIds).toEqual([sampleSeriesId, sampleSeriesId]);
	await expect.poll(() => wavesWorkspaceRequestCount).toBeGreaterThanOrEqual(3);
	await expect(currentTask.getByRole('region', { name: 'Wave comparison preview' })).toBeVisible();
	await expect(workflow.getByText('complete trajectories 6', { exact: true })).toBeVisible();
	await expect(currentTask.getByText('paired delta -0.25', { exact: true })).toBeVisible();
	await expect(waves.getByText('Visible comparisons', { exact: true }).first()).toBeVisible();
	await expect(waves.getByText('2', { exact: true }).first()).toBeVisible();
});

test('setup workflow renders primary setup actions instead of the proof workbench', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Setup workspace' });
	await expect(setup.getByRole('group', { name: 'Preparation actions' })).toBeVisible();
	await expect(
		setup.getByRole('heading', { name: 'Preparation actions', exact: true })
	).toBeVisible();
	await expect(setup.getByRole('region', { name: 'Proof action workbench' })).toHaveCount(0);
});

test('setup route leads with a study preparation checklist before setup reference', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	await expect(page.getByRole('heading', { name: 'Prepare study', exact: true })).toBeVisible();

	const setup = page.getByRole('region', { name: 'Setup workspace' });
	const preparation = setup.getByRole('region', { name: 'Study preparation' });
	await expect(preparation.getByRole('heading', { name: 'Preparation checklist' })).toBeVisible();
	await expect(preparation.getByText('Instrument and template', { exact: true })).toBeVisible();
	await expect(preparation.getByText('Scoring', { exact: true })).toBeVisible();
	await expect(preparation.getByText('Policies', { exact: true })).toBeVisible();
	await expect(preparation.getByText('Wave draft', { exact: true })).toBeVisible();
	await expect(preparation.getByText('Launch readiness', { exact: true })).toBeVisible();
	await expect(
		preparation
			.getByLabel('Policies', { exact: true })
			.getByText('Disclosure policy: Add a disclosure policy for this series.', {
				exact: true
			})
	).toBeVisible();

	const reference = setup.getByRole('region', { name: 'Setup reference' });
	await expect(reference).toBeVisible();
	await expect(reference.getByText(sampleSeriesId, { exact: true })).toBeVisible();
	await expect(reference.getByText('disclosure_policy.missing', { exact: true })).toBeVisible();

	const preparationBox = await preparation.boundingBox();
	const referenceBox = await reference.boundingBox();
	expect(preparationBox?.y ?? 0).toBeLessThan(referenceBox?.y ?? 0);
});

test('setup action hierarchy starts editable studies on the current setup task', async ({
	page
}) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	const setup = page.getByRole('region', { name: 'Setup workspace' });
	const preparation = setup.getByRole('region', { name: 'Study preparation' });
	const actions = setup.getByRole('group', { name: 'Preparation actions' });
	const currentTask = actions.getByRole('region', {
		name: /Build questionnaire|Review results setup|Measurement and recipients|Ready to collect/
	});
	const setupPath = actions.getByRole('list', { name: 'Setup path' });

	await expect(preparation).toBeVisible();
	await expect(actions).toBeVisible();
	await expect(currentTask).toBeVisible();
	await expect(setupPath).toBeVisible();
	await expectElementBefore(preparation, actions);
	await expectElementBefore(currentTask, setupPath);
});

test('setup policy review details are visible in the setup workspace', async ({ page }) => {
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

	const setup = page.getByRole('region', { name: 'Setup workspace' });
	const workflow = setup.getByRole('group', { name: 'Study setup progress' });
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
		questions: Array<{
			ordinal: number;
			code: string;
			textDefault: string;
			type: string;
			required: boolean;
			reverseCoded: boolean;
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

	const setup = page.getByRole('region', { name: 'Setup workspace' });
	const workflow = setup.getByRole('group', { name: 'Study setup progress' });
	await expect(workflow).toContainText('Questionnaire');

	const questionRows = workflow.locator('.question-row');
	await expect(questionRows).toHaveCount(3);
	await workflow.getByRole('button', { name: 'Add question' }).click();
	await expect(questionRows).toHaveCount(4);

	await questionRows
		.nth(3)
		.getByLabel('Question text')
		.fill('I can recover focus after a difficult interruption.');
	await questionRows.nth(3).getByLabel('Reverse score this question', { exact: true }).check();
	await questionRows.nth(1).getByRole('button', { name: 'Remove' }).click();

	await workflow.getByRole('button', { name: 'Save questionnaire' }).click();
	await expect.poll(() => templateBodies).toHaveLength(1);
	expect(templateBodies[0]).toMatchObject({
		instrumentId: createdInstrumentId,
		questions: [
			{
				ordinal: 1,
				code: 'q01',
				type: 'likert',
				reverseCoded: false
			},
			{
				ordinal: 2,
				code: 'q03',
				type: 'likert',
				reverseCoded: true
			},
			{
				ordinal: 3,
				code: 'q04',
				textDefault: 'I can recover focus after a difficult interruption.',
				type: 'likert',
				required: true,
				reverseCoded: true
			}
		]
	});
	expect(templateBodies[0].questions.map((question) => question.code)).not.toContain('q02');

	await expect(workflow).toContainText('Review results setup');
	await expect(workflow.getByRole('heading', { name: 'Total score' })).toBeVisible();
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

	const setup = page.getByRole('region', { name: 'Setup workspace' });
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
		role: 'manager'
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

	const setup = page.getByRole('region', { name: 'Setup workspace' });
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
	await expect(savedSelection.getByText("One person's manager", { exact: true })).toBeVisible();
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

	const setup = page.getByRole('region', { name: 'Setup workspace' });
	await expect(setup.getByRole('button', { name: 'Save collection wave' })).toBeEnabled();
	await setup.getByRole('button', { name: 'Save collection wave' }).click();

	await expect.poll(() => campaignCreates).toHaveLength(1);
	expect(campaignCreates[0]).toMatchObject({
		campaignSeriesId: sampleSeriesId,
		templateVersionId: campaignDraftSetupWorkspace.template?.templateVersionId ?? ''
	});
	await expect.poll(() => setupWorkspaceRequestCount).toBeGreaterThanOrEqual(2);
	await expect(setup.getByText('Campaigns', { exact: true }).first()).toBeVisible();
	await expect(setup.getByText('1', { exact: true }).first()).toBeVisible();
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

	const setup = page.getByRole('region', { name: 'Setup workspace' });
	await setup.getByRole('button', { name: 'Check launch readiness' }).click();

	await expect(setup.getByText('Launch readiness failed.')).toBeVisible();
	await expect(setup.getByText(sampleSeriesId, { exact: true }).first()).toBeVisible();
	await expect(setup.getByText('Quarterly pulse', { exact: true }).first()).toBeVisible();
	await expect(setup.getByRole('heading', { name: 'Preparation checklist' })).toBeVisible();
	await expect(setup.getByRole('region', { name: 'Setup reference' })).toBeVisible();
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
	await page.getByRole('button', { name: 'Create wave draft' }).click();

	expect(unexpectedSeriesCreates).toBe(0);
	await expect.poll(() => campaignCreates).toHaveLength(1);
	expect(campaignCreates[0].campaignSeriesId).toBe(sampleSeriesId);
	await expect(page.getByText(sampleSeriesId, { exact: true }).last()).toBeVisible();
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

	const reports = page.getByRole('region', { name: 'Results workspace' });
	await expect(reports.getByText('Retryable child pulse', { exact: true }).first()).toBeVisible();
	await expect(reports.getByText(retrySeriesId, { exact: true }).first()).toBeVisible();
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

	const waves = page.getByRole('region', { name: 'Waves and linked trajectories' });
	const workspace = waves.getByRole('region', { name: 'Waves selected-series context' });
	await expect(waves.getByRole('heading', { name: 'Retention pulse', exact: true })).toBeVisible();
	await expect(workspace.getByText(alternateSeriesId, { exact: true })).toBeVisible();

	releaseOriginalSeries();
	await expect(
		waves.getByRole('heading', { name: 'Legacy foundation pulse', exact: true })
	).toHaveCount(0);
	await expect(waves.getByRole('heading', { name: 'Retention pulse', exact: true })).toBeVisible();
});

test('keeps demo fixtures gated by default', async ({ page }) => {
	await page.goto('/app/demo');

	await expect(page.getByRole('heading', { name: 'Demo fixtures', exact: true })).toBeVisible();
	await expect(page.getByText('Demo fixture surfaces are unavailable')).toBeVisible();
	await expect(page.getByText('Demo data')).toHaveCount(0);
});

test('renders demo fixtures under internal tools when the local flag is enabled', async ({
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
	await page.route('**/workspace-overview', async (route) => {
		if (!isProductApiPath(route.request().url(), '/workspace-overview')) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleWorkspaceOverview });
	});

	await page.route('**/tenant-settings', async (route) => {
		if (!isProductApiPath(route.request().url(), '/tenant-settings')) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: sampleTenantSettings });
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
				title: 'Finish setup for Quarterly pulse',
				description: 'Consent, retention, disclosure, and scoring setup still need attention.',
				state: 'blocked',
				surface: 'setup',
				route: `/app/campaign-series/${sampleSeriesId}/setup`,
				actionLabel: 'Open setup',
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
			label: 'Directory',
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
			groups: []
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
			label: 'Setup',
			status: 'ready',
			guidance: 'Governance prerequisites are configured for this series.',
			route: 'setup',
			actionLabel: 'Review setup'
		},
		{
			id: 'operations',
			label: 'Operations',
			status: 'ready',
			guidance: 'Collection has submitted responses to monitor.',
			route: 'operations',
			actionLabel: 'Open operations'
		},
		{
			id: 'reports',
			label: 'Reports',
			status: 'proof_only',
			guidance: 'Report preview can be reviewed; create an export file before handoff.',
			route: 'reports',
			actionLabel: 'Open reports'
		},
		{
			id: 'waves',
			label: 'Waves',
			status: 'not_available',
			guidance:
				'Use anonymous longitudinal campaign identity when this series needs wave comparison.',
			route: 'waves',
			actionLabel: 'Review waves'
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
		status: 'draft',
		defaultLocale: 'en',
		instrumentId: null,
		questionCount: 5
	},
	scoring: {
		id: '716b2246-70f7-4728-9f44-150bd3b8da7a',
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
	codebookJson: '{}'
};

const sampleTemplateVersion = {
	templateId: '2a642f70-90ca-4aa7-b7c1-84084360a1a9',
	templateVersionId: '68933bc3-522b-4974-b7a7-04a7eaf52edc',
	templateName: 'Tenant burnout pulse template',
	semver: '1.0.0',
	status: 'draft',
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
