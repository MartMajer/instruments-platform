import { expect, test } from '@playwright/test';

/**
 * The identified-queue respondent flow against the real backend, end to end:
 * CSV-import a manager with two reports → identified wave with manager-review
 * recipients → launch → mint personal respondent links on Field → the manager
 * opens one link, consents once, and answers a queue of two assignments.
 *
 * People rows use stable external ids, so reruns update instead of duplicating.
 */
test('identified 360: a manager answers a queue of two reports', async ({ page, browser }) => {
	const studyName = `Identified 360 ${Date.now()}`;

	// 1. roster with a manager relationship
	await page.goto('/app/people');
	await page.getByRole('button', { name: 'Import CSV' }).click();
	await page.fill(
		'#csv',
		[
			'display_name,email,external_id,group_name,manager_external_id',
			'E2E 360 Ana,e2e360.ana@example.org,e2e-360-a1,E2E 360 pilot,',
			'E2E 360 Marko,e2e360.marko@example.org,e2e-360-m1,E2E 360 pilot,e2e-360-a1',
			'E2E 360 Petra,e2e360.petra@example.org,e2e-360-p1,E2E 360 pilot,e2e-360-a1'
		].join('\n')
	);
	await page.getByRole('button', { name: 'Import', exact: true }).click();
	await expect(page.locator('.note').first()).toBeVisible({ timeout: 15_000 });

	// 2. study with a 2-item questionnaire
	await page.goto('/app/studies/new');
	await page.fill('#name', studyName);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await items.nth(0).fill('This colleague communicates clearly.');
	await items.nth(1).fill('This colleague supports the team.');
	await page.locator('.item-row').nth(2).getByRole('button', { name: /Remove item/ }).click();
	await page.getByRole('button', { name: /Create questionnaire/ }).click();

	// 3. identified wave
	const waveInput = page.locator('.add-wave input');
	await expect(waveInput).toBeVisible({ timeout: 20_000 });
	await waveInput.fill('Manager review');
	await page.locator('.add-wave select').first().selectOption('identified');
	await page.getByRole('button', { name: 'Add wave' }).click();

	// 4. manager-review recipients: one assignment per report
	await page.getByRole('button', { name: 'Recipients' }).click();
	await page.getByLabel('Recipient rule').selectOption('manager_review');
	await page.getByRole('button', { name: 'Save recipients' }).click();
	await expect(page.locator('.recipients-note').first()).toContainText('resolved', {
		timeout: 15_000
	});

	// 5. launch
	const launch = page.getByRole('button', { name: /Launch Manager review/ });
	await expect(launch).toBeVisible({ timeout: 20_000 });
	await launch.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();
	await expect(page.locator('#waves')).toContainText('Live', { timeout: 20_000 });

	// 6. field: personal respondent links (Ana carries two assignments)
	await page.getByRole('link', { name: 'Field', exact: true }).click();
	await page.getByRole('button', { name: 'Create respondent links' }).click();
	const anaRow = page.locator('.queue-links li', { hasText: 'E2E 360 Ana' });
	await expect(anaRow).toContainText('2', { timeout: 15_000 });
	const url = (await anaRow.locator('.minted-url').textContent())?.trim();
	expect(url).toContain('/r/');

	// 7. the manager's queue: consent once, then two assignments
	const phone = await browser.newContext({ viewport: { width: 390, height: 844 } });
	const respondent = await phone.newPage();
	await respondent.goto(url!);
	await respondent.waitForSelector('html[data-hydrated="true"]');
	await respondent.getByRole('checkbox').check();
	await respondent.getByRole('button', { name: 'Begin' }).click();
	await expect(respondent.locator('.queue-list li')).toHaveCount(2, { timeout: 15_000 });

	for (const target of ['E2E 360 Marko', 'E2E 360 Petra']) {
		await respondent
			.locator('.queue-list li', { hasText: target })
			.getByRole('button')
			.click();
		for (const row of [0, 1]) {
			await respondent.locator('.item').nth(row).getByRole('radio', { name: '4' }).click();
		}
		await respondent.getByRole('button', { name: 'Submit answers' }).click();
	}
	// both submitted: the queue collapses into the thank-you state
	await expect(respondent.locator('h1')).toContainText('Thank you', { timeout: 15_000 });
	await phone.close();

	// 8. field counts both submissions
	await page.reload();
	await expect(page.locator('.tile.accent .value')).toHaveText('2', { timeout: 15_000 });

	// clean up
	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
});
