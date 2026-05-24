import { expect, test } from '@playwright/test';

test('renders an app-first study operations product entry at the root route', async ({ page }) => {
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

	await expect(
		page.getByRole('heading', {
			name: /Run research and wellbeing studies without rebuilding the data stack/i
		})
	).toBeVisible();
	await expect(page.getByRole('link', { name: 'Create workspace' }).first()).toHaveAttribute(
		'href',
		'/register'
	);
	await expect(page.getByRole('link', { name: 'Sign in' }).first()).toHaveAttribute(
		'href',
		'/signin'
	);
	await expect(page.getByRole('navigation', { name: 'Setup stages' })).toHaveCount(0);
	await expect(page.getByRole('heading', { name: 'Tenant setup workspace' })).toHaveCount(0);
});
test('renders the workspace registration route', async ({ page }) => {
	await page.goto('/register');

	await expect(page.getByRole('heading', { name: 'Create your workspace.' })).toBeVisible();
	await expect(page.getByLabel('Email')).toBeVisible();
	await expect(page.getByLabel('Workspace name')).toBeVisible();
	await expect(page.getByLabel('Access code')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create account' })).toBeVisible();
	await expect(page.getByRole('navigation', { name: 'Setup stages' })).toHaveCount(0);
});
