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

	// publish a custom consent version before launching
	await page.getByRole('button', { name: 'Publish new consent version' }).click();
	await page.fill('#c-version', '2.0.0');
	await page.fill('#c-title', 'Informed consent — golden path');
	await page.fill('#c-body', 'This study is anonymous. Results only appear for groups of five or more. You can stop at any time.');
	await page.getByRole('button', { name: /Publish — retires/ }).click();
	await expect(page.locator('.consent-note')).toContainText('Published v2.0.0', { timeout: 15_000 });

	// wave + launch
	const waveInput = page.locator('.add-wave input');
	await expect(waveInput).toBeVisible({ timeout: 20_000 });
	await waveInput.fill('Baseline');
	await page.getByRole('button', { name: 'Add wave' }).click();
	const launch = page.getByRole('button', { name: /Launch Baseline/ });
	await expect(launch).toBeVisible({ timeout: 20_000 });
	await launch.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();

	// field: mint the open link
	await page.getByRole('link', { name: 'Field' }).click();
	await page.getByRole('button', { name: 'Create open link' }).click();
	const url = (await page.locator('.minted-url').textContent({ timeout: 15_000 }))?.trim();
	expect(url).toContain('/r/');

	// respond on a mobile viewport
	const phone = await browser.newContext({ viewport: { width: 390, height: 844 } });
	const respondent = await phone.newPage();
	await respondent.goto(url!);
	// the launched wave must carry the custom consent version published above
	await expect(respondent.locator('.consent-version')).toContainText('v2.0.0');
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

	// clean up: archive the test study so it never pollutes the portfolio
	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
});
