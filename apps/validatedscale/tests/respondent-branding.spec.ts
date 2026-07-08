import { expect, test } from '@playwright/test';
import { brandTenant, contrastWithWhite, parseRgb } from './support/branding';

/**
 * Respondent branding, end to end against the real backend:
 * brand the tenant (accent + platform-hosted logo) via the authenticated API,
 * mint a real open link, and confirm the runner renders the tenant's logo and
 * applies the accent — including that a contrast-failing accent is auto-corrected
 * to a legible variant. Isolation across tenants is proven at the integration
 * layer (ResponseCaptureStoreTests); the dev-auth harness here is single-tenant.
 *
 * Requires the API on localhost:5055 with dev auth and the seeded dev tenant.
 */

async function mintOpenLink(page: import('@playwright/test').Page): Promise<string> {
	const studyName = `Brand E2E ${Date.now()}`;
	await page.goto('/app/studies/new');
	await page.fill('#name', studyName);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');

	await page.fill('#tpl-name', `${studyName} questionnaire`);
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await items.nth(0).fill('I have enough time for my tasks.');
	await items.nth(1).fill('I feel supported by my team.');
	await items.nth(2).fill('My workload is predictable.');
	await page.getByRole('button', { name: /Create questionnaire/ }).click();

	await page.getByRole('button', { name: 'Publish new consent version' }).click();
	await page.fill('#c-version', '2.0.0');
	await page.fill('#c-title', 'Informed consent — brand E2E');
	await page.fill('#c-body', 'This study is anonymous. You can stop at any time.');
	await page.getByRole('button', { name: /Publish — retires/ }).click();
	await expect(page.locator('.consent-note')).toContainText('Published v2.0.0', { timeout: 15_000 });

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
	expect(url).toContain('/r/');
	return url!;
}

test('respondent runner renders the tenant logo and a contrast-guarded accent', async ({
	page,
	browser,
	request
}) => {
	// Brand the workspace with a legible dark accent + logo, then mint a link.
	const dark = await brandTenant(request, '#1f4fd1');
	expect(dark.hasLogo).toBe(true);
	expect(dark.effectiveAccentColorHex).toBe('#1f4fd1'); // already legible, unchanged

	const mintedUrl = await mintOpenLink(page);
	const token = mintedUrl.match(/\/r\/([^/?#]+)/)?.[1];
	expect(token).toBeTruthy();
	const runnerUrl = `http://localhost:5174/r/${token}`;

	// Open the runner as a respondent (anonymous origin, phone viewport).
	const context = await browser.newContext({ viewport: { width: 420, height: 900 } });
	const runner = await context.newPage();
	await runner.goto(runnerUrl);
	await runner.waitForSelector('html[data-hydrated="true"]');

	// The tenant's platform-hosted logo renders in the brand header, as a data URI.
	const logo = runner.locator('.brand-logo');
	await expect(logo).toBeVisible();
	expect((await logo.getAttribute('src'))?.startsWith('data:image/png')).toBeTruthy();

	// The accent lands on the primary action, exactly as set (it was legible).
	const begin = runner.getByRole('button', { name: 'Begin' });
	await expect(begin).toBeVisible();
	expect(parseRgb(await begin.evaluate((el) => getComputedStyle(el).backgroundColor))).toEqual([
		31, 79, 209
	]);

	// Consenting reveals the accent wash too — a fuller shot for the eyeball.
	await runner.getByRole('checkbox').check();
	await runner.screenshot({ path: 'test-results/app-branding/respondent-dark.png' });

	// Re-brand with a near-yellow that fails contrast against white button text.
	const light = await brandTenant(request, '#ffe100');
	expect(light.accentColorHex).toBe('#ffe100');
	expect(light.effectiveAccentColorHex).not.toBe('#ffe100'); // auto-corrected server-side

	await runner.reload();
	await runner.waitForSelector('html[data-hydrated="true"]');
	const correctedRgb = parseRgb(
		await runner.getByRole('button', { name: 'Begin' }).evaluate((el) => getComputedStyle(el).backgroundColor)
	);
	// Not the raw illegible yellow, and legible with white text (WCAG AA).
	expect(correctedRgb).not.toEqual([255, 225, 0]);
	expect(contrastWithWhite(correctedRgb)).toBeGreaterThanOrEqual(4.5);

	await runner.getByRole('checkbox').check();
	await runner.screenshot({ path: 'test-results/app-branding/respondent-light.png' });

	await context.close();

	// Leave the workspace on the good dark branding for the owner's own eyeball.
	await brandTenant(request, '#1f4fd1');

	// Tidy up the throwaway study so it never pollutes the portfolio.
	try {
		await page.getByRole('link', { name: 'Protocol' }).click();
		await page.getByRole('button', { name: 'Archive study' }).click();
		await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
		await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
			timeout: 15_000
		});
	} catch {
		// Cleanup is best-effort; the assertions above are what matter.
	}
});
