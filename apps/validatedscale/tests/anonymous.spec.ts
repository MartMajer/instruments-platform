import { expect, test } from '@playwright/test';

/**
 * These run against the auth-less server (port 5174) — the condition a real
 * staging visitor is in. The dev-auth suite cannot catch pre-auth breakage
 * (e.g. the CSRF-prefetch 401 that silently killed registration on staging).
 */
test.use({ baseURL: 'http://localhost:5174' });

test('anonymous: registration submit reaches the backend and shows the real reason', async ({ page }) => {
	const apiCalls: string[] = [];
	page.on('response', (response) => {
		const url = new URL(response.url());
		if (url.port === '5055') apiCalls.push(`${response.status()} ${url.pathname}`);
	});

	await page.goto('/register');
	await page.getByRole('heading', { name: 'Create a workspace' }).waitFor();
	await page.fill('#email', 'someone@example.org');
	await page.fill('#org', 'Test Org');
	await page.fill('#code', 'wrong-code');
	await page.getByRole('button', { name: 'Continue' }).click();

	const error = page.locator('.error');
	await expect(error).toBeVisible({ timeout: 15_000 });
	await expect(error).not.toContainText('unavailable right now');
	// the POST must actually reach /registration/intents (pre-fix, the CSRF 401 aborted it)
	expect(apiCalls.some((call) => call.includes('/registration/intents'))).toBe(true);
});

test('anonymous: signin lookup reaches the backend', async ({ page }) => {
	await page.goto('/signin');
	await page.fill('#email', 'nobody@example.org');
	// the invariant is that the POST reaches the backend at all (pre-fix, the
	// CSRF 401 aborted it client-side); the outcome may be an error message or
	// a redirect to the identity provider depending on environment config
	const lookup = page.waitForResponse(
		(response) => response.url().includes('/registration/workspace-sign-in'),
		{ timeout: 15_000 }
	);
	await page.getByRole('button', { name: 'Continue' }).click();
	const response = await lookup;
	expect(response.status()).toBeLessThan(500);
});
