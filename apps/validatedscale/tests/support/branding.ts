import { expect, type APIRequestContext, type Page } from '@playwright/test';

export const API = process.env.PUBLIC_API_BASE_URL ?? 'http://localhost:5055';
export const DEV_TENANT = '11111111-1111-4111-8111-111111111111';

export const devHeaders = {
	'X-Tenant-Id': DEV_TENANT,
	'X-Dev-User-Id': '22222222-2222-4222-8222-222222222222',
	'X-Dev-Tenant-Memberships': DEV_TENANT,
	'X-Dev-Permissions': 'setup.manage'
};

// A 96×96 RGB PNG (blue mark, white border) — a stand-in platform-hosted logo.
export const LOGO_B64 =
	'iVBORw0KGgoAAAANSUhEUgAAAGAAAABgCAIAAABt+uBvAAAAtklEQVR4nO3QsQ3CAAADwcySxZibaUJL' +
	'EeEyL3HWD2DdcdnPHU8fqA/QGKAxQGOAxm6Aztf7bwMECBAgQOEAAQIECFA4QIAAAQIUDhAgQIAAhQME' +
	'CBAgQOEAAQIECFA4QIAAAQIUDhAgQIAAhQMECBAgQOEAAQIECFA4QIAAAQIUDhAgQIAAhQMECBAgQOEA' +
	'AQIECFA4QIAAAQIUDhAgQIAAhQMECNDzQPY9QGOAxgCNARoDNPYBVxx681fryp8AAAAASUVORK5CYII=';

export type ThemeTokens = {
	topbarColorHex?: string;
	backgroundColorHex?: string;
	surfaceColorHex?: string;
	inkColorHex?: string;
};

/** Brand the dev tenant with an accent (+ optional surface theme) + the stand-in logo. */
export async function brandTenant(
	request: APIRequestContext,
	accentColorHex: string,
	theme: ThemeTokens = {}
) {
	const upload = await request.post(`${API}/tenant-settings/app-branding/logo`, {
		headers: { ...devHeaders, 'Content-Type': 'image/png' },
		data: Buffer.from(LOGO_B64, 'base64')
	});
	expect(upload.ok(), `logo upload failed: ${upload.status()}`).toBeTruthy();
	const logo = await upload.json();

	const put = await request.put(`${API}/tenant-settings/app-branding`, {
		headers: { ...devHeaders, 'Content-Type': 'application/json' },
		data: {
			accentColorHex,
			logoObjectKey: logo.logoObjectKey,
			logoContentType: logo.logoContentType,
			...theme
		}
	});
	expect(put.ok(), `branding write failed: ${put.status()}`).toBeTruthy();
	return put.json();
}

/** Author a throwaway study and mint an open link, returning the /r/ URL. */
export async function mintOpenLink(page: Page): Promise<string> {
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

export function parseRgb(value: string): [number, number, number] {
	const match = value.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
	if (!match) throw new Error(`not an rgb color: ${value}`);
	return [Number(match[1]), Number(match[2]), Number(match[3])];
}

export function contrastWithWhite([r, g, b]: [number, number, number]): number {
	const channel = (c: number) => {
		const s = c / 255;
		return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
	};
	const luminance = 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
	return 1.05 / (luminance + 0.05);
}
