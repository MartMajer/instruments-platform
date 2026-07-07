import { expect, test } from '@playwright/test';

test('people: add person, filter finds them', async ({ page }) => {
	const name = `E2E Person ${Date.now()}`;
	await page.goto('/app/people');
	await page.getByRole('button', { name: 'Add person' }).click();
	await page.fill('#p-name', name);
	await page.fill('#p-email', `e2e.${Date.now()}@example.org`);
	await page.locator('form.author').getByRole('button', { name: 'Add person' }).click();
	await page.getByLabel('Find a person or group').fill(name);
	await expect(page.locator('tbody')).toContainText(name, { timeout: 15_000 });
});

test('people: CSV dry run previews without saving', async ({ page }) => {
	await page.goto('/app/people');
	await page.getByRole('button', { name: 'Import CSV' }).click();
	await page.fill('#csv', 'display_name,email\nDry Run Only,dry@example.org');
	await page.getByRole('button', { name: 'Preview (dry run)' }).click();
	await expect(page.locator('.note')).toContainText('Nothing was saved', { timeout: 15_000 });
});

test('settings: report branding saves', async ({ page }) => {
	await page.goto('/app/settings');
	const label = `E2E Org ${Date.now() % 10000}`;
	await page.fill('#b-label', label);
	await page.getByRole('button', { name: 'Save branding' }).click();
	await expect(page.locator('.note').first()).toContainText('Saved', { timeout: 15_000 });
	await page.reload();
	await expect(page.locator('#b-label')).toHaveValue(label);
});

test('exports: library lists artifacts and downloads one', async ({ page }) => {
	await page.goto('/app/exports');
	const download = page.waitForEvent('download', { timeout: 20_000 });
	await page.locator('.dl').first().click();
	expect((await download).suggestedFilename()).toContain('.csv');
});

test('study: sample duplicates into an own editable study', async ({ page }) => {
	await page.goto('/app/studies');
	await page.getByLabel('Find a study').fill('Ergonomics risk');
	await page.locator('.bucket a.name').first().click();
	await page.waitForLoadState('networkidle');

	await page.getByRole('button', { name: 'Duplicate as my study' }).click();
	const dupName = `Dup copy ${Date.now()}`;
	await page.getByRole('dialog').locator('input').fill(dupName);
	await page.getByRole('dialog').getByRole('button', { name: 'Duplicate' }).click();
	await expect(page.locator('h1')).toContainText(dupName, { timeout: 30_000 });
	// the copy is an own study: the sample-specific verb is gone, the own one remains
	await expect(page.getByRole('button', { name: 'Duplicate study', exact: true })).toBeVisible();

	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({ timeout: 15_000 });
});

test('study: an own study duplicates into a fresh draft with the protocol carried over', async ({
	page
}) => {
	// source: a gallery study (own, with instrument + scoring attached)
	await page.goto('/app/instruments');
	await page.getByRole('button', { name: 'Use PHQ-4' }).click();
	const sourceName = `Own dup source ${Date.now()}`;
	await page.getByRole('dialog').locator('input').fill(sourceName);
	await page.getByRole('dialog').getByRole('button', { name: 'Create study' }).click();
	await page.waitForURL('**/app/studies/*', { timeout: 30_000 });
	const sourceUrl = page.url();

	await page.getByRole('button', { name: 'Duplicate study', exact: true }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Duplicate' }).click();
	await expect(page.locator('h1')).toContainText('(copy)', { timeout: 30_000 });
	// the protocol came along: instrument binding and scoring rule
	await expect(page.locator('#instrument')).toContainText('PHQ-4');
	await expect(page.locator('#scoring')).toContainText('phq4_score');

	// clean up the copy, then the source
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
	await page.goto(sourceUrl);
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
});

test('gallery: NMQ loads 27 yes/no items with choice-map scoring', async ({ page }) => {
	await page.goto('/app/instruments');
	await page.getByRole('button', { name: 'Use NMQ' }).click();
	await page.getByRole('dialog').locator('input').fill(`NMQ E2E ${Date.now()}`);
	await page.getByRole('dialog').getByRole('button', { name: 'Create study' }).click();
	await page.waitForURL('**/app/studies/*', { timeout: 30_000 });
	await expect(page.locator('#instrument')).toContainText('27');
	await expect(page.locator('#scoring')).toContainText('nmq_regions_affected');

	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({ timeout: 15_000 });
});

test('register: invalid access code gets an honest message, not a generic outage', async ({ page }) => {
	await page.goto('/register');
	await page.getByRole('heading', { name: 'Create a workspace' }).waitFor();
	await page.fill('#email', 'someone@example.org');
	await page.fill('#org', 'Test Org');
	await page.fill('#code', 'definitely-wrong-code');
	await page.getByRole('button', { name: 'Continue' }).click();
	// the honest reason must surface (access-code message on staging, backend detail locally) —
	// never the generic outage text this bug used to show
	const error = page.locator('.error');
	await expect(error).toBeVisible({ timeout: 15_000 });
	await expect(error).not.toContainText('unavailable right now');
	await expect(error).toContainText(/access code|not enabled/);
});
