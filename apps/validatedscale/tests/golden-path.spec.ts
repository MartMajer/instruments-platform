import { expect, test } from '@playwright/test';

/**
 * The golden path, end to end against a real local backend:
 * register study → compose questionnaire → add wave → launch →
 * mint open link → respond on mobile → submit → field shows the submission.
 *
 * Requires the API on localhost:5055 with dev auth and the seeded dev tenant.
 */
test('researcher authors, launches, collects and sees evidence', async ({ page, browser }) => {
	const studyName = `Golden path ${Date.now()}`;

	await page.goto('/app/studies/new');
	await page.fill('#name', studyName);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');

	// compose a 3-item questionnaire, one reverse-coded
	await page.fill('#tpl-name', `${studyName} questionnaire`);
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await items.nth(0).fill('I have enough time for my tasks.');
	await items.nth(1).fill('I feel exhausted after a normal shift.');
	await items.nth(2).fill('My workload is predictable.');
	await page.locator('.item-row').nth(1).locator('.reverse').click();
	await page.getByRole('button', { name: /Create questionnaire/ }).click();

	// wave + launch
	const waveInput = page.locator('.add-wave input');
	await expect(waveInput).toBeVisible({ timeout: 20_000 });
	await waveInput.fill('Baseline');
	await page.getByRole('button', { name: 'Add wave' }).click();
	const launch = page.getByRole('button', { name: /Launch/ });
	await expect(launch).toBeVisible({ timeout: 20_000 });
	page.on('dialog', (dialog) => void dialog.accept());
	await launch.click();

	// field: mint the open link
	await page.getByRole('link', { name: 'Field' }).click();
	await page.getByRole('button', { name: 'Create open link' }).click();
	const url = (await page.locator('.minted-url').textContent({ timeout: 15_000 }))?.trim();
	expect(url).toContain('/r/');

	// respond on a mobile viewport
	const phone = await browser.newContext({ viewport: { width: 390, height: 844 } });
	const respondent = await phone.newPage();
	await respondent.goto(url!);
	await respondent.getByRole('checkbox').check();
	await respondent.getByRole('button', { name: 'Begin' }).click();
	for (const [row, rating] of [[0, '4'], [1, '2'], [2, '5']] as const) {
		await respondent.locator('.item').nth(row).getByRole('radio', { name: rating }).click();
	}
	await respondent.getByRole('button', { name: 'Submit answers' }).click();
	await expect(respondent.locator('h1')).toContainText('Thank you', { timeout: 15_000 });
	await phone.close();

	// field shows the submission
	await page.reload();
	await expect(page.locator('.tile.accent .value')).toHaveText('1', { timeout: 15_000 });
});
