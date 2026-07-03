import { expect, test } from '@playwright/test';

const routes: Array<[string, string]> = [
	['/', 'Measurement you'],
	['/signin', 'Sign in'],
	['/register', 'Create a workspace'],
	['/app', ''],
	['/app/studies', 'Studies'],
	['/app/studies/new', 'Register a study'],
	['/app/instruments', 'Instruments'],
	['/app/people', 'People'],
	['/app/exports', 'Exports'],
	['/app/settings', 'Settings']
];

for (const [path, marker] of routes) {
	test(`route ${path} renders`, async ({ page }) => {
		const failures: string[] = [];
		page.on('pageerror', (error) => failures.push(error.message));
		await page.goto(path);
		if (marker) {
			await expect(page.locator('h1').first()).toContainText(marker, { timeout: 15_000 });
		} else {
			// dynamic headline (Today) — assert the page rendered a non-empty h1
			await expect(page.locator('h1').first()).not.toBeEmpty({ timeout: 15_000 });
		}
		expect(failures).toEqual([]);
	});
}
