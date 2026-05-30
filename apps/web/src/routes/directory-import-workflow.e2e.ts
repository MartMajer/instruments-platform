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
					totalItemCount: 1,
					returnedItemCount: 1,
					itemsTruncated: false,
					sampleLimit: 25,
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

test('keeps large Microsoft preview and people directory bounded', async ({ page }) => {
	await page.unroute('**/subjects**');
	await routeLargeSubjectDirectory(page);
	await routeGraphImportWorkspaceWithSavedRule(page);
	await page.route(`**/directory-import-rules/${ruleId}/preview`, async (route) => {
		await route.fulfill({
			json: {
				runId: previewRunId,
				ruleId,
				status: 'previewed',
				summary: {
					matchedUserCount: 40,
					createSubjectCount: 40,
					updateSubjectCount: 0,
					noChangeCount: 0,
					warningCount: 0,
					totalItemCount: 40,
					returnedItemCount: 25,
					itemsTruncated: true,
					sampleLimit: 25,
					retainedFields: ['id', 'displayName', 'mail', 'department']
				},
				items: Array.from({ length: 25 }, (_, index) => ({
					action: 'create_subject',
					status: 'planned',
					issueCode: null,
					displayName: `Preview Person ${index + 1}`,
					email: `preview${index + 1}@example.test`
				}))
			}
		});
	});

	await page.goto('/app/directory');

	const graphImport = page.getByRole('region', { name: 'Microsoft Graph directory import' });
	await graphImport.getByRole('button', { name: 'Preview import' }).click();
	await expect(graphImport.getByText('Showing 25 of 40 planned actions')).toBeVisible();
	await expect(graphImport.getByTestId('graph-preview-row')).toHaveCount(25);
	await expect(graphImport.getByText('Preview Person 26')).toHaveCount(0);

	const peopleDirectory = page.getByTestId('workspace-people-directory');
	await expect(peopleDirectory.getByText('Showing 25 of 2,000 people')).toBeVisible();
	await expect(peopleDirectory.getByTestId('directory-person-row')).toHaveCount(25);
	await expect(peopleDirectory.getByText('Person 2000')).toHaveCount(0);
});

test('presents imported people as an operational directory with details and safe deactivate', async ({
	page
}) => {
	await page.unroute('**/subjects**');
	const longExternalId = 'msgraph:customer-tenant:00000000-0000-0000-0000-000000000001';
	const requestedSubjectUrls: string[] = [];
	let deactivateRequested = false;
	await routeOperationalSubjectDirectory(page, requestedSubjectUrls, longExternalId);
	await routeGraphImportWorkspaceWithSavedRule(page);
	await page.route('**/subjects/*/deactivate', async (route) => {
		if (route.request().method() !== 'POST') {
			await route.fallback();
			return;
		}

		deactivateRequested = true;
		await route.fulfill({
			json: {
				id: '66666666-6666-4666-8666-000000000001',
				displayName: 'Adele Vance',
				email: 'adelev@example.test',
				externalId: longExternalId,
				locale: 'en',
				attributes: '{"directory_status":"deactivated"}',
				managerSubjectId: null,
				managerDisplayName: null,
				directReportCount: 0,
				source: 'microsoft_graph',
				sourceLabel: 'Microsoft 365',
				status: 'deactivated',
				statusLabel: 'Deactivated',
				department: 'Retail',
				jobTitle: 'Store lead',
				employeeType: 'Staff',
				officeLocation: 'Zagreb',
				groups: []
			}
		});
	});

	await page.goto('/app/directory');

	const peopleDirectory = page.getByTestId('workspace-people-directory');
	await expect(peopleDirectory.getByRole('columnheader', { name: 'External id' })).toHaveCount(0);
	await expect(peopleDirectory.getByTestId('directory-person-row')).toContainText('Adele Vance');
	await expect(peopleDirectory.getByTestId('directory-person-row')).toContainText('Microsoft 365');
	await expect(peopleDirectory.getByTestId('directory-person-row')).toContainText('Retail');
	await expect(peopleDirectory.getByTestId('directory-person-row')).not.toContainText(longExternalId);

	await peopleDirectory.getByLabel('Source', { exact: true }).selectOption('microsoft_graph');
	await peopleDirectory.getByLabel('Status', { exact: true }).selectOption('active');
	await peopleDirectory
		.getByLabel('Group', { exact: true })
		.selectOption('77777777-7777-4777-8777-000000000001');
	await peopleDirectory.getByLabel('Manager', { exact: true }).selectOption('assigned');
	await peopleDirectory.getByLabel('Contact', { exact: true }).selectOption('has_email');
	await peopleDirectory.getByLabel('Sort by', { exact: true }).selectOption('department_asc');
	await peopleDirectory.getByRole('button', { name: 'Apply filters' }).click();
	await expect.poll(() => requestedSubjectUrls.at(-1) ?? '').toContain('source=microsoft_graph');
	expect(requestedSubjectUrls.at(-1)).toContain('status=active');
	expect(requestedSubjectUrls.at(-1)).toContain('groupId=77777777-7777-4777-8777-000000000001');
	expect(requestedSubjectUrls.at(-1)).toContain('manager=assigned');
	expect(requestedSubjectUrls.at(-1)).toContain('contact=has_email');
	expect(requestedSubjectUrls.at(-1)).toContain('sort=department_asc');

	await peopleDirectory.getByRole('button', { name: 'View Adele Vance' }).click();
	const drawer = page.getByRole('dialog', { name: 'Person details' });
	await expect(drawer.getByText('Adele Vance')).toBeVisible();
	await drawer.getByText('Technical details').click();
	await expect(drawer.getByText(longExternalId)).toBeVisible();
	await expect(drawer.getByText('Miriam Graham')).toBeVisible();
	await drawer.getByRole('button', { name: 'Deactivate person' }).click();
	await drawer.getByLabel('Reason').fill('Wrong cohort');
	await drawer.getByRole('button', { name: 'Confirm deactivate' }).click();
	await expect.poll(() => deactivateRequested).toBe(true);
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
	await page.route('**/subjects**', async (route) => {
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
					filteredSubjectCount: 0,
					returnedSubjectCount: 0,
					groupCount: 0,
					managerRelationshipCount: 0,
					pageOffset: 0,
					pageSize: 0,
					hasMore: false
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

async function routeLargeSubjectDirectory(page: Page) {
	await page.route('**/subjects**', async (route) => {
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
					subjectCount: 2000,
					filteredSubjectCount: 2000,
					returnedSubjectCount: 25,
					groupCount: 18,
					managerRelationshipCount: 340,
					pageOffset: 0,
					pageSize: 25,
					hasMore: true
				},
				subjects: Array.from({ length: 25 }, (_, index) => ({
					id: `66666666-6666-4666-8666-${String(index + 1).padStart(12, '0')}`,
					displayName: `Person ${index + 1}`,
					email: `person${index + 1}@example.test`,
					externalId: `msgraph:tenant:person-${index + 1}`,
					locale: 'en',
					attributes: '{}',
					managerSubjectId: null,
					managerDisplayName: null,
					directReportCount: 0,
					groups: [
						{
							groupId: `77777777-7777-4777-8777-${String(index + 1).padStart(12, '0')}`,
							groupType: 'department',
							groupName: 'Retail',
							roleInGroup: 'member',
							validFrom: null,
							validTo: null
						}
					]
				}))
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

async function routeOperationalSubjectDirectory(
	page: Page,
	requestedUrls: string[],
	longExternalId: string
) {
	await page.route('**/subjects**', async (route) => {
		if (
			route.request().method() !== 'GET' ||
			!isProductApiPath(route.request().url(), '/subjects')
		) {
			await route.fallback();
			return;
		}

		requestedUrls.push(route.request().url());
		await route.fulfill({
			json: {
				tenantId,
				summary: {
					subjectCount: 1,
					filteredSubjectCount: 1,
					returnedSubjectCount: 1,
					groupCount: 1,
					managerRelationshipCount: 1,
					pageOffset: 0,
					pageSize: 25,
					hasMore: false
				},
				subjects: [
					{
						id: '66666666-6666-4666-8666-000000000001',
						displayName: 'Adele Vance',
						email: 'adelev@example.test',
						externalId: longExternalId,
						locale: 'en',
						attributes:
							'{"department":"Retail","job_title":"Store lead","employee_type":"Staff","office_location":"Zagreb","directory_source":"microsoft_graph"}',
						managerSubjectId: '66666666-6666-4666-8666-000000000002',
						managerDisplayName: 'Miriam Graham',
						directReportCount: 3,
						source: 'microsoft_graph',
						sourceLabel: 'Microsoft 365',
						status: 'active',
						statusLabel: 'Active',
						department: 'Retail',
						jobTitle: 'Store lead',
						employeeType: 'Staff',
						officeLocation: 'Zagreb',
						groups: [
							{
								groupId: '77777777-7777-4777-8777-000000000001',
								groupType: 'department',
								groupName: 'Retail',
								roleInGroup: 'member',
								validFrom: null,
								validTo: null
							}
						]
					}
				]
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

		await route.fulfill({
			json: {
				tenantId,
				groups: [
					{
						id: '77777777-7777-4777-8777-000000000001',
						type: 'department',
						name: 'Retail',
						parentGroupId: null,
						attributes: '{}',
						memberCount: 1
					}
				]
			}
		});
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

async function routeGraphImportWorkspaceWithSavedRule(page: Page) {
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
				rules: [
					{
						id: ruleId,
						connectionId,
						name: 'All current students',
						criteria: {},
						fieldSelection: {
							fields: ['displayName', 'mail', 'userPrincipalName', 'department', 'jobTitle']
						},
						mirrorMode: false,
						mirrorConfirmedAt: null,
						createdAt: '2026-05-29T18:15:00Z',
						updatedAt: '2026-05-29T18:15:00Z'
					}
				],
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
