import { expect, test, type Page } from '@playwright/test';

const tenantId = '11111111-1111-4111-8111-111111111111';
const connectionId = '22222222-2222-4222-8222-222222222222';
const ruleId = '33333333-3333-4333-8333-333333333333';
const previewRunId = '44444444-4444-4444-8444-444444444444';

test.beforeEach(async ({ page }) => {
	await routeAuthenticatedSession(page);
	await routeCsrfToken(page);
	await routeSubjectDirectory(page);
});

test('runs the Microsoft Graph directory import preview workflow from saved connection and rule', async ({
	page
}) => {
	let createdRuleRequest: Record<string, unknown> | null = null;

	await routeGraphImportWorkspace(page);
	await page.route('**/directory-import-rules', async (route) => {
		if (
			route.request().method() !== 'POST' ||
			!isProductApiPath(route.request().url(), '/directory-import-rules')
		) {
			await route.fallback();
			return;
		}

		createdRuleRequest = route.request().postDataJSON();
		await route.fulfill({
			json: {
				id: ruleId,
				connectionId,
				name: 'Third year students',
				criteria: createdRuleRequest?.criteria,
				fieldSelection: createdRuleRequest?.fieldSelection,
				mirrorMode: true,
				mirrorConfirmedAt: '2026-05-29T18:15:00Z',
				createdAt: '2026-05-29T18:15:00Z',
				updatedAt: '2026-05-29T18:15:00Z'
			}
		});
	});
	await page.route(`**/directory-import-rules/${ruleId}/preview`, async (route) => {
		await route.fulfill({
			json: {
				runId: previewRunId,
				ruleId,
				status: 'previewed',
				summary: {
					matchedUserCount: 6,
					createSubjectCount: 3,
					updateSubjectCount: 1,
					noChangeCount: 2,
					warningCount: 0,
					retainedFields: ['id', 'displayName', 'mail', 'department']
				},
				items: [
					{
						action: 'create_subject',
						status: 'planned',
						issueCode: null,
						displayName: 'Ana Analyst',
						email: 'ana@example.test'
					}
				]
			}
		});
	});
	await page.route(`**/directory-import-runs/${previewRunId}/apply`, async (route) => {
		await route.fulfill({
			json: {
				runId: '55555555-5555-4555-8555-555555555555',
				previewRunId,
				ruleId,
				status: 'applied',
				summary: {
					createdSubjectCount: 3,
					updatedSubjectCount: 1,
					noChangeSubjectCount: 2,
					createdGroupCount: 1,
					addedMembershipCount: 3,
					setManagerCount: 1,
					warningCount: 0
				}
			}
		});
	});

	await page.goto('/app/directory');

	const graphImport = page.getByRole('region', { name: 'Microsoft Graph directory import' });
	await expect(
		graphImport
			.getByRole('article', { name: 'Algebra sandbox' })
			.getByText('Algebra sandbox', { exact: true })
	).toBeVisible();
	await expect(graphImport.getByText('Active', { exact: true })).toBeVisible();
	await expect(graphImport.getByLabel('Microsoft tenant ID')).toBeHidden();
	await graphImport.getByText('Manual connection fallback').click();
	await expect(graphImport.getByLabel('Microsoft tenant ID')).toBeVisible();

	await graphImport.getByLabel('Rule name').fill('Third year students');
	await graphImport.getByLabel('Departments').fill('Psychology, Sociology');
	await graphImport.getByLabel('Job title contains').fill('manager');
	await graphImport.getByLabel('Include manager links').check();
	await graphImport.getByLabel('Mirror Microsoft directory').check();
	await expect(graphImport.getByRole('button', { name: 'Save import rule' })).toBeDisabled();

	await graphImport.getByLabel('Mirror confirmation').fill('MIRROR MICROSOFT DIRECTORY');
	await expect(graphImport.getByRole('button', { name: 'Save import rule' })).toBeEnabled();
	await graphImport.getByRole('button', { name: 'Save import rule' }).click();
	await expect.poll(() => createdRuleRequest).not.toBeNull();

	expect(createdRuleRequest).toMatchObject({
		connectionId,
		name: 'Third year students',
		criteria: {
			departments: ['Psychology', 'Sociology'],
			jobTitleContains: 'manager',
			includeManagerChain: true
		},
		fieldSelection: {
			fields: ['displayName', 'mail', 'userPrincipalName', 'department', 'jobTitle']
		},
		mirrorMode: true,
		mirrorConfirmation: 'MIRROR MICROSOFT DIRECTORY'
	});

	await expect(graphImport.getByRole('button', { name: 'Apply import' })).toBeDisabled();
	await graphImport.getByRole('button', { name: 'Preview import' }).click();

	const previewCounts = graphImport.getByRole('group', { name: 'Microsoft Graph preview counts' });
	await expect(previewCounts.getByText('Create subjects')).toBeVisible();
	await expect(previewCounts.getByText('3', { exact: true })).toBeVisible();
	await expect(previewCounts.getByText('Update subjects')).toBeVisible();
	await expect(previewCounts.getByText('1', { exact: true })).toBeVisible();
	await expect(previewCounts.getByText('No change')).toBeVisible();
	await expect(previewCounts.getByText('2', { exact: true })).toBeVisible();
	await expect(graphImport.getByRole('button', { name: 'Apply import' })).toBeEnabled();

	const connectionColumn = graphImport.getByTestId('graph-connection-column');
	const ruleColumn = graphImport.getByTestId('graph-rule-column');
	const connectionBox = await connectionColumn.boundingBox();
	const ruleBox = await ruleColumn.boundingBox();
	expect(connectionBox).not.toBeNull();
	expect(ruleBox).not.toBeNull();
	expect(connectionBox!.width).toBeLessThanOrEqual(ruleBox!.width);
});

test('starts Microsoft Graph admin consent from the Directory page', async ({ page }) => {
	let startedConsent = false;

	await routeEmptyGraphImportWorkspace(page);
	await page.route('**/directory-connections/microsoft-graph/admin-consent/start', async (route) => {
		if (
			route.request().method() !== 'POST' ||
			!isProductApiPath(
				route.request().url(),
				'/directory-connections/microsoft-graph/admin-consent/start'
			)
		) {
			await route.fallback();
			return;
		}

		startedConsent = true;
		await route.fulfill({
			json: {
				authorizationUrl: '/mock-microsoft-admin-consent'
			}
		});
	});

	await page.goto('/app/directory');

	const graphImport = page.getByRole('region', { name: 'Microsoft Graph directory import' });
	await graphImport.getByRole('button', { name: 'Connect Microsoft tenant' }).click();

	await expect.poll(() => startedConsent).toBe(true);
	await page.waitForURL('**/mock-microsoft-admin-consent');
});

async function routeAuthenticatedSession(page: Page) {
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({
			json: {
				userId: '99999999-9999-4999-8999-999999999999',
				tenantId,
				email: 'owner@example.test',
				permissions: ['setup.manage']
			}
		});
	});
}

async function routeCsrfToken(page: Page) {
	await page.route('**/auth/csrf', async (route) => {
		await route.fulfill({ json: { csrfToken: 'test-csrf-token' } });
	});
}

async function routeSubjectDirectory(page: Page) {
	await page.route('**/subjects', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/subjects')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({
			json: {
				tenantId,
				summary: {
					subjectCount: 0,
					groupCount: 0,
					managerRelationshipCount: 0
				},
				subjects: []
			}
		});
	});
	await page.route('**/subject-groups', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/subject-groups')
		) {
			await route.fallback();
			return;
		}

		await route.fulfill({ json: { tenantId, groups: [] } });
	});
}

async function routeGraphImportWorkspace(page: Page) {
	await page.route('**/directory-imports/workspace', async (route) => {
		await route.fulfill({
			json: {
				tenantId,
				connections: [
					{
						id: connectionId,
						provider: 'microsoft_graph',
						externalTenantId: 'customer-tenant',
						displayName: 'Algebra sandbox',
						primaryDomain: 'algebra.example',
						grantedScopes: ['User.Read.All', 'Group.Read.All', 'GroupMember.Read.All'],
						status: 'active',
						lastSuccessfulSyncAt: null,
						createdAt: '2026-05-29T18:00:00Z'
					}
				],
				rules: [],
				recentRuns: []
			}
		});
	});
}

async function routeEmptyGraphImportWorkspace(page: Page) {
	await page.route('**/directory-imports/workspace', async (route) => {
		await route.fulfill({
			json: {
				tenantId,
				connections: [],
				rules: [],
				recentRuns: []
			}
		});
	});
}

function isProductApiPath(url: string, path: string) {
	return new URL(url).pathname === path;
}
