import { expect, test } from '@playwright/test';
import { brandTenant, contrastWithWhite, parseRgb } from './support/branding';

/**
 * The Settings respondent-branding editor: the live preview mirrors the runner
 * header + primary action, reflects the contrast-guarded accent as the picker
 * moves, and saving preserves the logo when only the accent changes.
 *
 * Requires the API on localhost:5055 with dev auth and the seeded dev tenant.
 */

async function setColor(input: import('@playwright/test').Locator, value: string) {
	await input.evaluate((el, v) => {
		(el as HTMLInputElement).value = v;
		el.dispatchEvent(new Event('input', { bubbles: true }));
	}, value);
}

test('respondent-branding editor previews the contrast-guarded accent and preserves the logo on save', async ({
	page,
	request
}) => {
	// Start from a known branded state: dark accent + the stand-in logo.
	await brandTenant(request, '#1f4fd1');

	await page.goto('/app/settings');
	const panel = page.locator('section', { hasText: 'Respondent branding' });
	await expect(panel).toBeVisible();

	// The saved logo loads into the preview, and the saved accent lands on the
	// mock primary action verbatim (it is already legible).
	await expect(panel.locator('.preview-logo')).toBeVisible();
	const begin = panel.locator('.preview-begin');
	expect(parseRgb(await begin.evaluate((el) => getComputedStyle(el).backgroundColor))).toEqual([
		31, 79, 209
	]);

	// Move the picker to a near-yellow that fails contrast: the preview shows the
	// auto-corrected (legible) accent and an "adjusted" note appears.
	await setColor(panel.locator('#a-accent'), '#ffe100');
	await expect(panel.locator('.adjust-note')).toBeVisible();
	const corrected = parseRgb(await begin.evaluate((el) => getComputedStyle(el).backgroundColor));
	expect(corrected).not.toEqual([255, 225, 0]);
	expect(contrastWithWhite(corrected)).toBeGreaterThanOrEqual(4.5);

	await panel.screenshot({ path: 'test-results/app-branding/settings-editor.png' });

	// Back to the dark accent, then save with the logo untouched — the save must
	// preserve the logo (the object key is echoed, not cleared).
	await setColor(panel.locator('#a-accent'), '#1f4fd1');
	await panel.getByRole('button', { name: 'Save respondent branding' }).click();
	await expect(panel.getByText('Saved. Respondents now see this branding.')).toBeVisible({
		timeout: 10_000
	});

	await page.reload();
	await expect(panel.locator('.preview-logo')).toBeVisible();
});
