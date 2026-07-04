import { defineConfig } from '@playwright/test';

export default defineConfig({
	testDir: './tests',
	timeout: 60_000,
	retries: 0,
	use: {
		baseURL: process.env.SPECTRA_BASE_URL ?? 'http://localhost:5173'
	},
	webServer: process.env.SPECTRA_NO_SERVER
		? undefined
		: [
				{
					command: 'npm run dev -- --port 5173',
					url: 'http://localhost:5173',
					reuseExistingServer: true,
					env: {
						PUBLIC_DEV_AUTH_ENABLED: 'true',
						PUBLIC_API_BASE_URL: process.env.PUBLIC_API_BASE_URL ?? 'http://localhost:5055'
					}
				},
				{
					// anonymous server: no dev-auth headers, the staging-visitor condition.
					// port 5174 is in the API's dev CORS allowlist.
					command: 'npm run dev -- --port 5174',
					url: 'http://localhost:5174',
					reuseExistingServer: true,
					env: {
						PUBLIC_API_BASE_URL: process.env.PUBLIC_API_BASE_URL ?? 'http://localhost:5055'
					}
				}
			]
});
