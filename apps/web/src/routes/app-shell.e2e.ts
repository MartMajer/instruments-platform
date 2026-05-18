import { expect, test } from '@playwright/test';

test('renders an app-first private beta product entry at the root route', async ({ page }) => {
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({
			json: {
				userId: '22222222-2222-4222-8222-222222222222',
				tenantId: '11111111-1111-4111-8111-111111111111',
				permissions: ['setup.manage']
			}
		});
	});

	await page.goto('/');

	await expect(page.getByRole('heading', { name: 'Instruments Platform' })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Open workspace' })).toHaveAttribute(
		'href',
		'/app'
	);
	await expect(page.getByRole('link', { name: 'Sign in' })).toHaveAttribute(
		'href',
		'/auth/login'
	);
	await expect(page.getByRole('navigation', { name: 'Setup stages' })).toHaveCount(0);
	await expect(page.getByRole('heading', { name: 'Tenant setup workspace' })).toHaveCount(0);
});
