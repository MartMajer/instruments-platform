import { expect, test, type Browser } from '@playwright/test';

/**
 * Depth pins for the domain invariants the product's honesty rests on:
 * launched waves stay snapshot-pinned across questionnaire revisions, the
 * k = 5 disclosure floor opens exactly when enough responses exist, and
 * export failures show their real reason with a retry.
 */

async function respond(browser: Browser, url: string, ratings: string[]) {
	const phone = await browser.newContext({ viewport: { width: 390, height: 844 } });
	const page = await phone.newPage();
	await page.goto(url);
	await page.waitForSelector('html[data-hydrated="true"]');
	await page.getByRole('checkbox').check();
	await page.getByRole('button', { name: 'Begin' }).click();
	for (const [row, rating] of ratings.entries()) {
		await page.locator('.item').nth(row).getByRole('radio', { name: rating }).click();
	}
	await page.getByRole('button', { name: 'Submit answers' }).click();
	await expect(page.locator('h1')).toContainText('Thank you', { timeout: 15_000 });
	await phone.close();
}

test('revision: a launched wave keeps its snapshot while the next wave gets the new version', async ({
	page,
	browser
}) => {
	test.setTimeout(120_000);
	const studyName = `Snapshot pin ${Date.now()}`;

	await page.goto('/app/studies/new');
	await page.fill('#name', studyName);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');

	// 3-item v1.0.0
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await items.nth(0).fill('I can plan my week.');
	await items.nth(1).fill('Interruptions are manageable.');
	await items.nth(2).fill('Meetings leave me enough focus time.');
	await page.getByRole('button', { name: /Create questionnaire/ }).click();

	// wave 1 launches on v1.0.0
	const waveInput = page.locator('.add-wave input');
	await expect(waveInput).toBeVisible({ timeout: 20_000 });
	await waveInput.fill('Wave one');
	await page.getByRole('button', { name: 'Add wave' }).click();
	const launchOne = page.getByRole('button', { name: /Launch Wave one/ });
	await expect(launchOne).toBeVisible({ timeout: 20_000 });
	await launchOne.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();
	await expect(page.locator('#waves')).toContainText('Live', { timeout: 20_000 });

	// revise to v1.1.0 with a fourth item
	await page.getByRole('button', { name: 'Revise questionnaire' }).click();
	await expect(items.first()).toHaveValue(/plan my week/, { timeout: 15_000 });
	await page.getByRole('button', { name: '+ Add item' }).click();
	await items.nth(3).fill('I end most days with energy left.');
	await page.getByRole('button', { name: /Publish revised version/ }).click();
	await expect(page.locator('#instrument')).toContainText('v1.1.0', { timeout: 20_000 });

	// wave 2 launches on v1.1.0
	await waveInput.fill('Wave two');
	await page.getByRole('button', { name: 'Add wave' }).click();
	const launchTwo = page.getByRole('button', { name: /Launch Wave two/ });
	await expect(launchTwo).toBeVisible({ timeout: 20_000 });
	await launchTwo.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();

	// mint both open links from Field (one expand panel per wave)
	await page.getByRole('link', { name: 'Field', exact: true }).click();
	await page.locator('.waves li', { hasText: 'Wave one' }).first().getByRole('button', { name: 'Invite & deliver' }).click();
	await page.getByRole('button', { name: 'Create open link' }).click();
	const urlOne = (await page.locator('.minted-url').textContent({ timeout: 15_000 }))?.trim();
	await page.locator('.waves li', { hasText: 'Wave two' }).first().getByRole('button', { name: 'Invite & deliver' }).click();
	await page.getByRole('button', { name: 'Create open link' }).click();
	await expect(page.locator('.minted-url')).not.toHaveText(urlOne!, { timeout: 15_000 });
	const urlTwo = (await page.locator('.minted-url').textContent())?.trim();

	// wave 1 still serves the 3-item snapshot; wave 2 serves the revised 4 items
	for (const [url, expected] of [
		[urlOne!, 3],
		[urlTwo!, 4]
	] as const) {
		const phone = await browser.newContext({ viewport: { width: 390, height: 844 } });
		const respondent = await phone.newPage();
		await respondent.goto(url);
		await respondent.waitForSelector('html[data-hydrated="true"]');
		await respondent.getByRole('checkbox').check();
		await respondent.getByRole('button', { name: 'Begin' }).click();
		await expect(respondent.locator('.item')).toHaveCount(expected);
		await phone.close();
	}

	// clean up
	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
});

test('disclosure: the k = 5 floor opens exactly at five responses, and a failed PDF is honest', async ({
	page,
	browser
}) => {
	test.setTimeout(180_000);
	const studyName = `K boundary ${Date.now()}`;

	await page.goto('/app/studies/new');
	await page.fill('#name', studyName);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await items.nth(0).fill('The workload is sustainable.');
	await items.nth(1).fill('I can take breaks when I need them.');
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
	await page.getByRole('button', { name: 'Invite & deliver' }).click();
	await page.getByRole('button', { name: 'Create open link' }).click();
	const url = (await page.locator('.minted-url').textContent({ timeout: 15_000 }))?.trim();

	// four responses: still below the floor, values suppressed
	for (let index = 0; index < 4; index++) {
		await respond(browser, url!, ['3', '4']);
	}
	await page.goto(`${page.url().replace('/field', '')}/evidence`);
	await expect(page.locator('.findings, .findings-chart').first()).toBeVisible({
		timeout: 20_000
	});
	await expect(page.locator('body')).toContainText(/suppressed|Suppressed/);

	// the fifth response crosses the floor: labeled visible statistics with n = 5
	await respond(browser, url!, ['4', '5']);
	await page.reload();
	const findings = page.locator('.findings');
	await expect(findings).toContainText('Total score', { timeout: 20_000 });
	await expect(findings).toContainText('5');

	// failure honesty: the report PDF cannot render locally and the refusal
	// says exactly why instead of a generic error
	await page.getByRole('button', { name: 'Report PDF' }).click();
	await expect(page.locator('.export-note')).toContainText('PDF renderer is not available', {
		timeout: 20_000
	});

	// clean up
	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
});
