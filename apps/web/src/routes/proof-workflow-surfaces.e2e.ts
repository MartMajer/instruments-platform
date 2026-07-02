import { expect, test, type Page } from '@playwright/test';

// The all-in-one proof workflow moved off the product study surfaces onto the
// internal /proof-lab route; these tests pin that placement.
const proofLabPath = '/proof-lab';

test.beforeEach(async ({ page }) => {
	await routeAuthenticatedSession(page);
});

test('keeps the proof workflow behind the internal proof lab boundary', async ({ page }) => {
	await page.goto(proofLabPath);

	const boundary = page.getByRole('region', { name: 'Internal proof lab boundary' });
	await expect(boundary).toBeVisible();
	await expect(boundary).toContainText('Internal proof lab');
	await expect(boundary).toContainText(
		'This route keeps the all-in-one proof workflow out of the product entry path.'
	);
});

test('places setup workflow actions on the proof lab', async ({ page }) => {
	await page.goto(proofLabPath);

	await expect(page.getByRole('button', { name: 'Create instrument import' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Save questionnaire' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Save result outputs' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create wave draft' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Check launch readiness' })).toBeVisible();
});

test('places launch and delivery actions on the proof lab', async ({ page }) => {
	await page.goto(proofLabPath);

	await expect(page.getByRole('button', { name: 'Launch wave' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create open link' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Queue email invitations' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Process local delivery' })).toBeVisible();
});

test('places report and two-wave proof actions on the proof lab', async ({ page }) => {
	await page.goto(proofLabPath);

	await expect(page.getByRole('button', { name: 'Load response lab' })).toBeVisible();
	await expect(page.getByRole('region', { name: 'Two-wave proof' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create two-wave proof' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Refresh two-wave proof' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'View wave comparison proof' })).toBeVisible();
});

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
