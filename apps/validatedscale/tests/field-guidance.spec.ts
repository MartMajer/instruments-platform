import { expect, test } from '@playwright/test';

/**
 * "What do I do next" clarity:
 *  - launching a wave points the researcher to Field;
 *  - Field offers email invitations only where they actually work
 *    (anonymous waves, not identified), and refuses with the real reason
 *    instead of a phantom "email readiness" check.
 */

async function makeStudyWithWave(page: import('@playwright/test').Page, name: string, identity: string) {
	await page.goto('/app/studies/new');
	await page.fill('#name', name);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await items.nth(0).fill('The workload is sustainable.');
	await page.locator('.item-row').nth(2).getByRole('button', { name: /Remove item/ }).click();
	await page.locator('.item-row').nth(1).getByRole('button', { name: /Remove item/ }).click();
	await page.getByRole('button', { name: /Create questionnaire/ }).click();
	const waveInput = page.locator('.add-wave input');
	await expect(waveInput).toBeVisible({ timeout: 20_000 });
	await waveInput.fill('Baseline');
	await page.locator('.add-wave select').first().selectOption(identity);
	await page.getByRole('button', { name: 'Add wave' }).click();
}

test('guidance: adding a wave points to launch, launching points to Field', async ({ page }) => {
	test.setTimeout(90_000);
	await makeStudyWithWave(page, `Guidance ${Date.now()}`, 'anonymous');

	// after adding: pointed to the launch check
	await expect(page.locator('.next-step')).toContainText('Launch check', { timeout: 10_000 });

	// launch, then the live banner offers Field
	const launch = page.getByRole('button', { name: /Launch Baseline/ });
	await expect(launch).toBeVisible({ timeout: 20_000 });
	await launch.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();

	const liveBanner = page.locator('.next-step.live');
	await expect(liveBanner).toContainText('Your wave is live', { timeout: 20_000 });
	await liveBanner.getByRole('link', { name: /Open Field/ }).click();
	await page.waitForURL('**/field');

	// clean up
	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({ timeout: 15_000 });
});

test('invite: an open-link wave refuses email invitations with the real reason, not "readiness"', async ({
	page
}) => {
	test.setTimeout(90_000);

	// anonymous wave with an open link: invite is offered but refuses honestly
	await makeStudyWithWave(page, `Invite anon ${Date.now()}`, 'anonymous');
	const launch = page.getByRole('button', { name: /Launch Baseline/ });
	await expect(launch).toBeVisible({ timeout: 20_000 });
	await launch.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();
	await page.getByRole('link', { name: 'Field', exact: true }).click();

	await page.getByRole('button', { name: 'Invite & deliver' }).click();
	await page.getByRole('button', { name: 'Create open link' }).click();
	await expect(page.locator('.minted-url')).toBeVisible({ timeout: 15_000 });

	// with an open link active, an email invite is refused with the real reason
	await page.getByLabel('Recipient emails').fill('someone@example.org');
	await page.getByRole('button', { name: 'Invite by email' }).click();
	const result = page.locator('.invite-result');
	await expect(result).toContainText('open link', { timeout: 15_000 });
	await expect(result).not.toContainText('readiness');

	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({ timeout: 15_000 });
});

test('delivery: a queued email invitation shows up as a named recipient with a status', async ({
	page
}) => {
	test.setTimeout(90_000);
	// anonymous wave, no open link → email invitations are allowed
	await makeStudyWithWave(page, `Delivery view ${Date.now()}`, 'anonymous');
	const launch = page.getByRole('button', { name: /Launch Baseline/ });
	await expect(launch).toBeVisible({ timeout: 20_000 });
	await launch.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();
	await page.getByRole('link', { name: 'Field', exact: true }).click();

	const email = `deliver.${Date.now()}@example.org`;
	await page.getByRole('button', { name: 'Invite & deliver' }).click();
	await page.getByLabel('Recipient emails').fill(email);
	await page.getByRole('button', { name: 'Invite by email' }).click();
	await expect(page.locator('.invite-result')).toContainText('invited by email', { timeout: 15_000 });

	// the "where did it go" answer: the recipient appears in the same panel with a status
	const row = page.locator('.delivery-list li', { hasText: email });
	await expect(row).toBeVisible({ timeout: 15_000 });
	await expect(row.locator('.st')).toBeVisible();

	await page.getByRole('link', { name: 'Protocol' }).click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({ timeout: 15_000 });
});
