import { expect, test } from '@playwright/test';

/** Archive the study currently open on a Protocol page. */
async function archiveCurrentStudy(page: import('@playwright/test').Page) {
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
}

test('gallery: one click creates a ready GAD-7 study', async ({ page }) => {
	await page.goto('/app/instruments');
	await page.getByRole('button', { name: 'Use GAD-7' }).click();
	await page.getByRole('dialog').locator('input').fill(`Gallery E2E ${Date.now()}`);
	await page.getByRole('dialog').getByRole('button', { name: 'Create study' }).click();
	await page.waitForURL('**/app/studies/*', { timeout: 30_000 });

	await expect(page.locator('#instrument')).toContainText('GAD-7');
	await expect(page.locator('#instrument')).toContainText('7');
	await expect(page.locator('#scoring')).toContainText('gad7_score');

	await archiveCurrentStudy(page);
});

test('gallery: item preview modal shows verbatim items before use', async ({ page }) => {
	await page.goto('/app/instruments');
	await page.getByRole('button', { name: 'View all 7 items' }).click();
	const modal = page.getByRole('dialog');
	await expect(modal).toContainText('Feeling nervous, anxious, or on edge');
	await expect(modal).toContainText('Spitzer');
	await page.keyboard.press('Escape');
	await expect(modal).not.toBeVisible();
});

test('revision: attached questionnaire edits into a new published version', async ({ page }) => {
	await page.goto('/app/instruments');
	await page.getByRole('button', { name: 'Use PHQ-4' }).click();
	await page.getByRole('dialog').locator('input').fill(`Revision E2E ${Date.now()}`);
	await page.getByRole('dialog').getByRole('button', { name: 'Create study' }).click();
	await page.waitForURL('**/app/studies/*', { timeout: 30_000 });

	await page.getByRole('button', { name: 'Revise questionnaire' }).click();
	const first = page.locator('.item-row input[aria-label^="Item"]').first();
	await expect(first).toHaveValue(/Feeling nervous/, { timeout: 15_000 });
	await page.getByRole('button', { name: '+ Add item' }).click();
	await page
		.locator('.item-row input[aria-label^="Item"]')
		.nth(4)
		.fill('Trouble sleeping because of worry');
	await page.getByRole('button', { name: /Publish revised version/ }).click();

	await expect(page.locator('#instrument')).toContainText('v1.1.0', { timeout: 20_000 });
	await expect(page.locator('#instrument')).toContainText('5');

	await archiveCurrentStudy(page);
});

test('locale: HR toggle translates the shell', async ({ page }) => {
	await page.goto('/app/studies');
	await page.getByRole('button', { name: 'Account' }).click();
	await page.getByRole('group', { name: 'Language' }).getByRole('button', { name: 'HR' }).click();
	await expect(page.locator('.topbar nav')).toContainText('Studije');
	await expect(page.locator('h1')).toContainText('Studije');
	// reset for other tests
	await page.evaluate(() => localStorage.setItem('validatedscale.app-locale', 'en'));
	await page.reload();
	await expect(page.locator('.topbar nav')).toContainText('Studies');
});
