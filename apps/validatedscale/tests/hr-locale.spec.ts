import { expect, test, type Page } from '@playwright/test';

/**
 * Croatian completeness: drive the researcher surfaces in hr and pin the
 * load-bearing translations, including strings recomposed from backend tokens
 * (command center, collection guidance, prerequisites). Instrument item text
 * stays verbatim English by design (claim boundary), so assertions target the
 * app chrome, never study content.
 */

// UI chrome that must never appear in hr. Chosen to not collide with verbatim
// instrument items or study names.
const englishChromeMarkers = [
	'Needs attention',
	'Recent studies',
	'Add example studies',
	'Register a study',
	'Launch check',
	'All prerequisites hold',
	'can be reviewed in',
	'submitted response',
	'live campaign',
	'Who takes part',
	'Consent & data guarantees',
	'Share the public link'
];

async function expectNoEnglishChrome(page: Page) {
	const text = await page.locator('body').innerText();
	for (const marker of englishChromeMarkers) {
		expect(text, `English chrome leaked: "${marker}"`).not.toContain(marker);
	}
}

test.use({
	storageState: { cookies: [], origins: [] }
});

test.describe('with hr preset', () => {
	test.beforeEach(async ({ page }) => {
		await page.addInitScript(() => {
			localStorage.setItem('validatedscale.app-locale', 'hr');
		});
	});

	test('hr: Today speaks Croatian, including backend-composed attention items', async ({
		page
	}) => {
		await page.goto('/app');
		await expect(page.locator('.side h2').first()).toContainText('Radni prostor');
		await expect(page.locator('.main h2').first()).toContainText('Zahtijeva pažnju');
		// Attention items are recomposed client-side from stable ids + params;
		// whatever the workspace state, none of them may render backend English.
		await expectNoEnglishChrome(page);
	});

	test('hr: gallery study drives Protocol, Field and Evidence in Croatian', async ({ page }) => {
		await page.goto('/app/instruments');
		await expect(page.locator('h1')).toContainText('Instrumenti');
		await page.getByRole('button', { name: 'Koristi GAD-7' }).click();
		await page.getByRole('dialog').locator('input').fill(`HR E2E ${Date.now()}`);
		await page.getByRole('dialog').getByRole('button', { name: 'Stvori studiju' }).click();
		await page.waitForURL('**/app/studies/*', { timeout: 30_000 });

		// Protocol in hr
		await expect(page.locator('h2', { hasText: 'Tko sudjeluje' })).toBeVisible();
		await expect(page.locator('#policies')).toContainText('Privola');
		await expectNoEnglishChrome(page);

		// Field in hr: prerequisites and guidance are recomposed from backend tokens
		await page.goto(`${page.url()}/field`);
		await expect(page.locator('h1')).toContainText('Prikupljanje');
		await expect(page.locator('.guidance')).not.toContainText('Share the public link');
		await expectNoEnglishChrome(page);

		// Evidence in hr
		await page.goto(page.url().replace('/field', '/evidence'));
		await expect(page.locator('h1')).toContainText('Nalazi');
		await expectNoEnglishChrome(page);

		// clean up: back to Protocol, archive
		await page.goto(page.url().replace('/evidence', ''));
		await page.getByRole('button', { name: 'Arhiviraj studiju' }).click();
		await page.getByRole('dialog').getByRole('button', { name: 'Arhiviraj' }).click();
		await expect(page.getByRole('button', { name: 'Vrati iz arhive' })).toBeVisible({
			timeout: 15_000
		});
	});

	test('hr: signin and register gates are Croatian with a visible language toggle', async ({
		page
	}) => {
		await page.goto('/signin');
		await expect(page.locator('h1')).toContainText('Prijava');
		await expect(page.getByRole('group', { name: 'Language / Jezik' })).toBeVisible();

		await page.goto('/register');
		await expect(page.locator('h1')).toContainText('Stvorite radni prostor');
		await expect(page.locator('label[for="code"]')).toContainText('Pristupni kod');
	});
});

test('hr: the language toggle on the gate persists into the app shell', async ({ page }) => {
	// Fresh context: locale defaults to en; the gate toggle must persist into /app.
	await page.goto('/signin');
	await page.waitForSelector('html[data-hydrated="true"]', { timeout: 30_000 });
	await page
		.getByRole('group', { name: 'Language / Jezik' })
		.getByRole('button', { name: 'HR' })
		.click();
	await expect(page.locator('h1')).toContainText('Prijava');

	await page.goto('/app/studies');
	await expect(page.locator('h1')).toContainText('Studije');
});
