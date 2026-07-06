import { chromium } from '@playwright/test';

/**
 * Warm the Vite dev servers before any spec runs. On a cold dev server the
 * first request per route pays module compilation, and the first browser
 * visit pays client-bundle compilation; under test parallelism both
 * occasionally exceed action timeouts and read as flakes (or worse: a click
 * lands before hydration and a native form submit swallows the interaction).
 */
const routes = [
	'/',
	'/signin',
	'/register',
	'/app',
	'/app/studies',
	'/app/studies/new',
	'/app/people',
	'/app/instruments',
	'/app/exports',
	'/app/settings',
	'/r/warmup-token'
];

// Routes whose client bundle must be hydrated at least once per server, so
// interactive specs never race cold-compile hydration.
const browserWarmRoutes = ['/signin', '/register', '/app'];

export default async function globalSetup() {
	if (process.env.SPECTRA_NO_SERVER) {
		return;
	}

	const bases = ['http://localhost:5173', 'http://localhost:5174'];

	await Promise.all(
		bases.flatMap((base) =>
			routes.map(async (route) => {
				try {
					await fetch(`${base}${route}`, { redirect: 'manual' });
				} catch {
					// Warmup only; the webServer readiness check owns availability.
				}
			})
		)
	);

	const browser = await chromium.launch();
	try {
		for (const base of bases) {
			const page = await browser.newPage();
			for (const route of browserWarmRoutes) {
				try {
					await page.goto(`${base}${route}`, { waitUntil: 'networkidle', timeout: 30_000 });
				} catch {
					// Best effort.
				}
			}
			await page.close();
		}
	} finally {
		await browser.close();
	}
}
