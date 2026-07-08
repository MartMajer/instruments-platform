import { expect, test } from '@playwright/test';
import { brandTenant } from './support/branding';

/**
 * App-shell branding (Phase 2): the researcher topbar shows the tenant logo in
 * place of the ValidatedScale mark, and the tenant accent (contrast-guarded)
 * drives the shell chrome — the active-nav underline and primary buttons — via
 * the same CSS-var mechanism as the respondent runner.
 *
 * Requires the API on localhost:5055 with dev auth and the seeded dev tenant.
 */

test('the researcher topbar carries the tenant logo and accent', async ({ page, request }) => {
	await brandTenant(request, '#1f4fd1');

	await page.goto('/app');

	// The tenant logo replaces the ValidatedScale mark, loaded as an authed blob.
	const logo = page.locator('.topbar .brand-logo.tenant');
	await expect(logo).toBeVisible({ timeout: 15_000 });
	expect((await logo.getAttribute('src'))?.startsWith('blob:')).toBeTruthy();

	// The guarded accent token is applied across the shell.
	const accent = await page
		.locator('.app')
		.evaluate((el) => getComputedStyle(el).getPropertyValue('--tenant-accent').trim());
	expect(accent).toBe('#1f4fd1');

	// The active-nav underline no longer shows the default stain-bright violet.
	const underline = await page
		.locator('.topbar nav a.active')
		.evaluate((el) => getComputedStyle(el).borderBottomColor);
	expect(underline).not.toBe('rgb(156, 139, 245)');
	expect(underline).not.toBe('rgba(0, 0, 0, 0)');

	await page.locator('.topbar').screenshot({ path: 'test-results/app-branding/shell-topbar.png' });

	// A fuller shot: topbar + a primary action carrying the accent.
	await page.goto('/app/studies/new');
	await expect(page.locator('.topbar .brand-logo.tenant')).toBeVisible({ timeout: 15_000 });
	await page.screenshot({
		path: 'test-results/app-branding/shell.png',
		clip: { x: 0, y: 0, width: 1280, height: 440 }
	});
});
