import { expect, test } from '@playwright/test';

/**
 * Respondent runner accessibility: the survey completes keyboard-only.
 * Scales follow the radio pattern — one tab stop per question, arrow keys
 * move and select — and the document carries the survey's language.
 */

test('a11y: a respondent completes the survey with the keyboard alone', async ({
	page,
	browser
}) => {
	test.setTimeout(120_000);
	const studyName = `A11y ${Date.now()}`;

	// study with two likert items
	await page.goto('/app/studies/new');
	await page.fill('#name', studyName);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await items.nth(0).fill('The pace of work is reasonable.');
	await items.nth(1).fill('I know what is expected of me.');
	await page.locator('.item-row').nth(2).getByRole('button', { name: /Remove item/ }).click();
	await page.getByRole('button', { name: /Create questionnaire/ }).click();

	const waveInput = page.locator('.add-wave input');
	await expect(waveInput).toBeVisible({ timeout: 20_000 });
	await waveInput.fill('Baseline');
	await page.getByRole('button', { name: 'Add wave' }).click();
	const launch = page.getByRole('button', { name: /Launch Baseline/ });
	await expect(launch).toBeVisible({ timeout: 20_000 });
	await launch.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();

	await page.getByRole('link', { name: 'Field', exact: true }).click();
	await page.getByRole('button', { name: 'Create open link' }).click();
	const url = (await page.locator('.minted-url').textContent({ timeout: 15_000 }))?.trim();

	const phone = await browser.newContext({ viewport: { width: 390, height: 844 } });
	const respondent = await phone.newPage();
	await respondent.goto(url!);
	await respondent.waitForSelector('html[data-hydrated="true"]');

	// the page declares its language for screen readers
	expect(await respondent.evaluate(() => document.documentElement.lang)).toBe('en');

	// consent: Space toggles the checkbox, Enter activates Begin
	await respondent.getByRole('checkbox').focus();
	await respondent.keyboard.press('Space');
	await expect(respondent.getByRole('checkbox')).toBeChecked();
	await respondent.getByRole('button', { name: 'Begin' }).focus();
	await respondent.keyboard.press('Enter');
	await expect(respondent.locator('.item')).toHaveCount(2, { timeout: 15_000 });

	// each scale is one tab stop; arrows move and select (radio pattern)
	for (const [index, presses] of [
		[0, 3],
		[1, 4]
	] as const) {
		const scale = respondent.locator('.item').nth(index).getByRole('radiogroup');
		await scale.locator('.scale-btn').first().focus();
		for (let press = 0; press < presses; press++) {
			await respondent.keyboard.press('ArrowRight');
		}
		await expect(scale.locator('.scale-btn[aria-checked="true"]')).toHaveText(
			String(presses + 1)
		);
	}

	// submit with the keyboard
	await respondent.getByRole('button', { name: 'Submit answers' }).focus();
	await respondent.keyboard.press('Enter');
	await expect(respondent.locator('h1')).toContainText('Thank you', { timeout: 15_000 });
	await phone.close();

	// clean up
	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
});
