import { expect, test } from '@playwright/test';
import { brandTenant, contrastWithWhite, mintOpenLink, parseRgb } from './support/branding';

/**
 * Tenant app branding, end to end against the real backend. These share one dev
 * tenant's branding state, so they run serially in one file — parallel files
 * would race on the same tenant. Each test re-brands at its start.
 *
 * Isolation across tenants is proven hermetically at the integration layer
 * (ResponseCaptureStoreTests); the dev-auth harness here is single-tenant.
 * Requires the API on localhost:5055 with dev auth and the seeded dev tenant.
 */

test.describe.configure({ mode: 'serial' });

function contrast(a: [number, number, number], b: [number, number, number]): number {
	const lum = ([r, g, bl]: [number, number, number]) => {
		const ch = (c: number) => {
			const s = c / 255;
			return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
		};
		return 0.2126 * ch(r) + 0.7152 * ch(g) + 0.0722 * ch(bl);
	};
	const la = lum(a);
	const lb = lum(b);
	return (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
}

async function setColor(input: import('@playwright/test').Locator, value: string) {
	await input.evaluate((el, v) => {
		(el as HTMLInputElement).value = v;
		el.dispatchEvent(new Event('input', { bubbles: true }));
	}, value);
}

test('respondent runner renders the tenant logo and a contrast-guarded accent', async ({
	page,
	browser,
	request
}) => {
	const dark = await brandTenant(request, '#1f4fd1');
	expect(dark.hasLogo).toBe(true);
	expect(dark.effectiveAccentColorHex).toBe('#1f4fd1');

	const url = await mintOpenLink(page);
	const token = url.match(/\/r\/([^/?#]+)/)?.[1];
	const runnerUrl = `http://localhost:5174/r/${token}`;

	const context = await browser.newContext({ viewport: { width: 420, height: 900 } });
	const runner = await context.newPage();
	await runner.goto(runnerUrl);
	await runner.waitForSelector('html[data-hydrated="true"]');

	const logo = runner.locator('.brand-logo');
	await expect(logo).toBeVisible();
	expect((await logo.getAttribute('src'))?.startsWith('data:image/png')).toBeTruthy();

	const begin = runner.getByRole('button', { name: 'Begin' });
	await expect(begin).toBeVisible();
	expect(parseRgb(await begin.evaluate((el) => getComputedStyle(el).backgroundColor))).toEqual([
		31, 79, 209
	]);

	await runner.getByRole('checkbox').check();
	await runner.screenshot({ path: 'test-results/app-branding/respondent-dark.png' });

	// Re-brand with a near-yellow that fails contrast against white button text.
	const light = await brandTenant(request, '#ffe100');
	expect(light.effectiveAccentColorHex).not.toBe('#ffe100');

	await runner.reload();
	await runner.waitForSelector('html[data-hydrated="true"]');
	const corrected = parseRgb(
		await runner.getByRole('button', { name: 'Begin' }).evaluate((el) => getComputedStyle(el).backgroundColor)
	);
	expect(corrected).not.toEqual([255, 225, 0]);
	expect(contrastWithWhite(corrected)).toBeGreaterThanOrEqual(4.5);

	await runner.getByRole('checkbox').check();
	await runner.screenshot({ path: 'test-results/app-branding/respondent-light.png' });
	await context.close();
});

test('the researcher topbar carries the tenant logo and accent', async ({ page, request }) => {
	await brandTenant(request, '#1f4fd1');

	await page.goto('/app');
	const logo = page.locator('.topbar .brand-logo.tenant');
	await expect(logo).toBeVisible({ timeout: 15_000 });
	expect((await logo.getAttribute('src'))?.startsWith('blob:')).toBeTruthy();

	const accent = await page
		.locator('.app')
		.evaluate((el) => getComputedStyle(el).getPropertyValue('--tenant-accent').trim());
	expect(accent).toBe('#1f4fd1');

	const underline = await page
		.locator('.topbar nav a.active')
		.evaluate((el) => getComputedStyle(el).borderBottomColor);
	expect(underline).not.toBe('rgb(156, 139, 245)');
	expect(underline).not.toBe('rgba(0, 0, 0, 0)');

	// Co-brand: the ValidatedScale attribution is retained beside the tenant logo.
	await expect(page.locator('.topbar .cobrand')).toBeVisible();
});

test('the Settings editor previews the guarded accent and preserves the logo on save', async ({
	page,
	request
}) => {
	await brandTenant(request, '#1f4fd1');

	await page.goto('/app/settings');
	const panel = page.locator('section', { hasText: 'Respondent branding' });
	await expect(panel).toBeVisible();

	await expect(panel.locator('.preview-logo')).toBeVisible();
	const begin = panel.locator('.preview-begin');
	expect(parseRgb(await begin.evaluate((el) => getComputedStyle(el).backgroundColor))).toEqual([
		31, 79, 209
	]);

	await setColor(panel.locator('#a-accent'), '#ffe100');
	await expect(panel.locator('.adjust-note')).toBeVisible();
	const corrected = parseRgb(await begin.evaluate((el) => getComputedStyle(el).backgroundColor));
	expect(corrected).not.toEqual([255, 225, 0]);
	expect(contrastWithWhite(corrected)).toBeGreaterThanOrEqual(4.5);

	await panel.screenshot({ path: 'test-results/app-branding/settings-editor.png' });

	await setColor(panel.locator('#a-accent'), '#1f4fd1');
	await panel.getByRole('button', { name: 'Save respondent branding' }).click();
	await expect(panel.getByText('Saved. Respondents now see this branding.')).toBeVisible({
		timeout: 10_000
	});

	await page.reload();
	await expect(panel.locator('.preview-logo')).toBeVisible();
});

const DARK_THEME = {
	topbarColorHex: '#0b1e3f',
	backgroundColorHex: '#0d1117',
	surfaceColorHex: '#141c2b',
	inkColorHex: '#e6edf3'
};
const THEME_ACCENT = '#17c0b0';

test('a full surface theme repaints the researcher shell, legibly', async ({ page, request }) => {
	await brandTenant(request, THEME_ACCENT, DARK_THEME);

	await page.goto('/app');
	await expect(page.locator('.topbar .brand-logo.tenant')).toBeVisible({ timeout: 15_000 });

	const topbarBg = parseRgb(
		await page.locator('.topbar').evaluate((el) => getComputedStyle(el).backgroundColor)
	);
	expect(topbarBg).toEqual([11, 30, 63]); // #0b1e3f

	// Custom properties hold the raw hex we set.
	const ground = await page
		.locator('.app')
		.evaluate((el) => getComputedStyle(el).getPropertyValue('--color-ground').trim());
	expect(ground).toBe('#0d1117');

	// Topbar text stays legible on the dark topbar.
	const navColor = parseRgb(
		await page.locator('.topbar nav a.active').evaluate((el) => getComputedStyle(el).color)
	);
	expect(contrast(navColor, topbarBg)).toBeGreaterThanOrEqual(4.5);

	await expect(page.locator('.topbar .cobrand')).toBeVisible();

	await page.screenshot({
		path: 'test-results/app-branding/theme-shell.png',
		clip: { x: 0, y: 0, width: 1280, height: 520 }
	});
});

test('a full surface theme repaints the survey, legibly', async ({ page, browser, request }) => {
	await brandTenant(request, THEME_ACCENT, DARK_THEME);
	const url = await mintOpenLink(page);
	const token = url.match(/\/r\/([^/?#]+)/)?.[1];

	const context = await browser.newContext({ viewport: { width: 420, height: 900 } });
	const runner = await context.newPage();
	await runner.goto(`http://localhost:5174/r/${token}`);
	await runner.waitForSelector('html[data-hydrated="true"]');

	await expect(runner.locator('.brand-logo')).toBeVisible();

	const sheetBg = parseRgb(
		await runner.locator('.sheet').evaluate((el) => getComputedStyle(el).backgroundColor)
	);
	expect(sheetBg).toEqual([20, 28, 43]); // #141c2b
	const titleColor = parseRgb(
		await runner.locator('.study-title').evaluate((el) => getComputedStyle(el).color)
	);
	expect(contrast(titleColor, sheetBg)).toBeGreaterThanOrEqual(4.5);

	const begin = runner.getByRole('button', { name: 'Begin' });
	const beginBg = parseRgb(await begin.evaluate((el) => getComputedStyle(el).backgroundColor));
	const beginColor = parseRgb(await begin.evaluate((el) => getComputedStyle(el).color));
	expect(contrast(beginColor, beginBg)).toBeGreaterThanOrEqual(4.5);

	await runner.getByRole('checkbox').check();
	await runner.screenshot({ path: 'test-results/app-branding/theme-runner.png' });
	await context.close();

	// Leave the workspace on a tasteful branded state for the owner's eyeball.
	await brandTenant(request, '#1f4fd1');
});
