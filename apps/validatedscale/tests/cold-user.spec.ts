import { expect, test } from '@playwright/test';

/**
 * Cold-user friction pins: the explanations a first-time researcher depends on
 * must exist and speak product vocabulary (study/wave/Field/Evidence), never
 * backend vocabulary (campaign series/Operations/Reports).
 */

test('today: attention items never speak backend vocabulary', async ({ page }) => {
	await page.goto('/app');
	await expect(page.locator('.main h2').first()).toContainText('Needs attention');
	const attention = page.locator('.attention');
	if (await attention.count()) {
		const text = await attention.innerText();
		for (const marker of ['campaign series', 'in Operations.', 'in Reports.', 'Reports surface']) {
			expect(text, `backend vocabulary leaked: "${marker}"`).not.toContain(marker);
		}
	}
});

test('studies: the example-studies affordance is always at hand', async ({ page }) => {
	await page.goto('/app/studies');
	await expect(page.getByRole('button', { name: 'Add example studies' })).toBeVisible();
});

test('instruments: the claim boundary is stated where imports happen', async ({ page }) => {
	await page.goto('/app/instruments');
	await expect(page.locator('.hint')).toContainText(
		'Nothing here is an official platform publication'
	);
});

test('new study: the page explains what a study is before asking for a name', async ({ page }) => {
	await page.goto('/app/studies/new');
	await expect(page.locator('.hint')).toContainText('A study holds one protocol');
});
