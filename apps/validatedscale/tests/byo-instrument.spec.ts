import { expect, test } from '@playwright/test';

/**
 * Bring-your-own instrument: a researcher pastes a validated scale from a
 * paper — items with (R) reverse markers, name, citation, rights attestation.
 * The composer registers a private instrument, binds it, publishes, scores,
 * and the runner serves the pasted items.
 */

test('byo: pasting a scale from a paper becomes a working, attested instrument', async ({
	page,
	browser
}) => {
	test.setTimeout(120_000);
	const stamp = Date.now();
	const studyName = `BYO ${stamp}`;

	await page.goto('/app/studies/new');
	await page.fill('#name', studyName);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');

	// paste-from-paper flow with attestation
	await page.getByRole('button', { name: 'Paste from a paper' }).click();
	await page.getByLabel('Instrument items, one per line').fill(
		[
			'1. I feel confident using new software tools.',
			'2. Learning new tools takes me longer than colleagues. (R)',
			'3. I enjoy exploring features on my own.',
			'- I avoid tools I have not used before. (R)'
		].join('\n')
	);
	await page.fill('#byo-name', `Tool Confidence Scale ${stamp}`);
	await page.fill('#byo-cite', 'Example, A. (2020). The Tool Confidence Scale. J Appl Meas.');
	// the load button stays disabled until rights are attested
	await expect(page.getByRole('button', { name: /Load 4 items/ })).toBeDisabled();
	await page.locator('.byo-attest input').check();
	await page.getByRole('button', { name: /Load 4 items/ }).click();

	// items landed with reverse markers stripped and flags set
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await expect(items.nth(0)).toHaveValue('I feel confident using new software tools.');
	await expect(items.nth(3)).toHaveValue('I avoid tools I have not used before.');
	await expect(page.locator('.item-row .reverse.on')).toHaveCount(2);

	// scoring preview names the reverse-coded items
	await expect(page.locator('.score-preview')).toContainText('reverse-coded: Q02, Q04');

	await page.getByRole('button', { name: /Create questionnaire \(4 items\)/ }).click();

	// the instrument is bound and named
	await expect(page.locator('#instrument')).toContainText(`Tool Confidence Scale ${stamp}`, {
		timeout: 30_000
	});
	await expect(page.locator('#scoring')).toContainText('_score');

	// it collects: launch, respond, count
	const waveInput = page.locator('.add-wave input');
	await expect(waveInput).toBeVisible({ timeout: 20_000 });
	await waveInput.fill('Pilot');
	await page.getByRole('button', { name: 'Add wave' }).click();
	const launch = page.getByRole('button', { name: /Launch Pilot/ });
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
	await respondent.getByRole('checkbox').check();
	await respondent.getByRole('button', { name: 'Begin' }).click();
	await expect(respondent.locator('.item')).toHaveCount(4);
	await expect(respondent.locator('.item').first()).toContainText(
		'I feel confident using new software tools.'
	);
	await phone.close();

	// clean up
	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
});
