import { expect, test, type Page } from '@playwright/test';

const sampleSeriesId = '985c65ad-f919-4c87-a40d-7445868dc587';

test.beforeEach(async ({ page }) => {
	await routeAuthenticatedSession(page);
});

test('places setup workflow actions on the setup surface', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/setup`);

	await expect(page.getByRole('region', { name: 'Setup workspace' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create instrument import' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create template version' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create scoring rule' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create campaign draft' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Check launch readiness' })).toBeVisible();
});

test('places launch and delivery actions on the operations surface', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/operations`);

	await expect(page.getByRole('region', { name: 'Campaign operations' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Launch campaign' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create open link' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Queue email invitations' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Process local delivery' })).toBeVisible();
});

test('places the report proof entry point on the reports surface', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/reports`);

	await expect(page.getByRole('region', { name: 'Reports and governed exports' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Load response lab' })).toBeVisible();
	await expect(page.getByRole('region', { name: 'Response lab' })).toBeVisible();
});

test('places two-wave proof actions on the waves surface', async ({ page }) => {
	await page.goto(`/app/campaign-series/${sampleSeriesId}/waves`);

	await expect(page.getByRole('region', { name: 'Waves and linked trajectories' })).toBeVisible();
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
