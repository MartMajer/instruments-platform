import { expect, test } from '@playwright/test';

/**
 * The in-app guide: reachable from the account menu, states the load-bearing
 * invariants in both locales, and holds the established hr register.
 */

test('help: the guide states the invariants and is reachable from the account menu', async ({
	page
}) => {
	await page.goto('/app');
	await page.waitForSelector('html[data-hydrated="true"]');
	await page.locator('.account-trigger').click();
	await page.getByRole('menuitem', { name: 'How ValidatedScale works' }).click();
	await page.waitForURL('**/app/help');

	await expect(page.locator('h1')).toContainText('How ValidatedScale works');
	// the invariants a reader must leave with
	await expect(page.locator('#evidence')).toContainText('k = 5');
	await expect(page.locator('#evidence')).toContainText('cannot be lowered');
	await expect(page.locator('#study')).toContainText('snapshot');
	await expect(page.locator('#study')).toContainText('immutable');
	await expect(page.locator('#rights')).toContainText('Withdrawal requests');
	// glossary carries the product vocabulary
	await expect(page.locator('#glossary')).toContainText('Open link');
	await expect(page.locator('#glossary')).toContainText('Participant code');
});

test('help: the guide reads in Croatian with the established register', async ({ page }) => {
	await page.addInitScript(() => localStorage.setItem('validatedscale.app-locale', 'hr'));
	await page.goto('/app/help');
	await page.waitForSelector('html[data-hydrated="true"]');

	await expect(page.locator('h1')).toContainText('Kako ValidatedScale radi');
	// the owner-set register: krug for wave, Prikupljanje for Field, Nalazi for Evidence
	await expect(page.locator('#collect')).toContainText('krug');
	await expect(page.locator('#collect')).toContainText('Prikupljanje');
	await expect(page.locator('#evidence h2')).toContainText('Nalaza');
	await expect(page.locator('#evidence')).toContainText('k = 5');
	await expect(page.locator('#glossary')).toContainText('Pojmovnik');
});
