import { expect, type APIRequestContext } from '@playwright/test';

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

/** Brand the dev tenant with an accent + the stand-in logo, via the real API. */
export async function brandTenant(request: APIRequestContext, accentColorHex: string) {
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
			logoContentType: logo.logoContentType
		}
	});
	expect(put.ok(), `branding write failed: ${put.status()}`).toBeTruthy();
	return put.json();
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
