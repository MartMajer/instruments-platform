import { describe, expect, it, vi } from 'vitest';
import { ApiError, createApiClient } from './client';

describe('createApiClient', () => {
	it('uses the local API base URL by default', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ ok: true }));
		const client = createApiClient({ fetch });

		await client.request('/health');

		expect(fetch).toHaveBeenCalledWith(
			'http://localhost:5055/health',
			expect.objectContaining({ headers: expect.any(Headers) })
		);
	});

	it('includes browser credentials by default', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ ok: true }));
		const client = createApiClient({ fetch });

		await client.request('/auth/session');

		expect(fetch).toHaveBeenCalledWith(
			'http://localhost:5055/auth/session',
			expect.objectContaining({ credentials: 'include' })
		);
	});

	it('preserves explicit request credentials', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ ok: true }));
		const client = createApiClient({ fetch });

		await client.request('/auth/session', { credentials: 'omit' });

		expect(fetch).toHaveBeenCalledWith(
			'http://localhost:5055/auth/session',
			expect.objectContaining({ credentials: 'omit' })
		);
	});

	it('joins base URL and path without duplicate slashes', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ ok: true }));
		const client = createApiClient({ baseUrl: 'https://api.example.test/root/', fetch });

		await client.request('/instruments/private-imports');

		expect(fetch).toHaveBeenCalledWith(
			'https://api.example.test/root/instruments/private-imports',
			expect.objectContaining({ headers: expect.any(Headers) })
		);
	});

	it('adds configured default headers to requests', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ ok: true }));
		const client = createApiClient({
			baseUrl: 'https://api.example.test',
			defaultHeaders: {
				'X-Tenant-Id': 'tenant-1',
				'X-Dev-Permissions': 'setup.manage'
			},
			fetch
		});

		await client.request('/instruments');

		const init = fetch.mock.calls[0][1];
		if (!init) {
			throw new Error('Expected request init.');
		}
		const headers = new Headers(init.headers);
		expect(headers.get('X-Tenant-Id')).toBe('tenant-1');
		expect(headers.get('X-Dev-Permissions')).toBe('setup.manage');
	});

	it('throws ApiError with status and response body for non-success responses', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ errors: { name: ['Required'] } }, 422));
		const client = createApiClient({ baseUrl: 'https://api.example.test', fetch });

		await expect(client.request('/instruments')).rejects.toBeInstanceOf(ApiError);
		await expect(client.request('/instruments')).rejects.toMatchObject({
			status: 422,
			body: { errors: { name: ['Required'] } }
		});
	});

	it('does not fetch csrf for safe requests when csrf is enabled', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ ok: true }));
		const client = createApiClient({ baseUrl: 'https://api.example.test', fetch, csrf: true });

		await client.request('/auth/session');

		expect(fetch).toHaveBeenCalledTimes(1);
		expect(fetch).toHaveBeenCalledWith(
			'https://api.example.test/auth/session',
			expect.objectContaining({ headers: expect.any(Headers) })
		);
	});

	it('fetches csrf token before unsafe requests when csrf is enabled', async () => {
		const fetch = createFetchMock(async (input) => {
			return String(input).endsWith('/auth/csrf')
				? jsonResponse({ csrfToken: 'csrf-token-1' })
				: jsonResponse({ ok: true });
		});
		const client = createApiClient({ baseUrl: 'https://api.example.test', fetch, csrf: true });

		await client.request('/campaigns', { method: 'POST' });

		expect(fetch).toHaveBeenCalledTimes(2);
		expect(fetch.mock.calls[0][0]).toBe('https://api.example.test/auth/csrf');
		expect(fetch.mock.calls[1][0]).toBe('https://api.example.test/campaigns');

		const headers = new Headers(fetch.mock.calls[1][1]?.headers);
		expect(headers.get('X-CSRF-TOKEN')).toBe('csrf-token-1');
	});

	it('reuses cached csrf token for later unsafe requests', async () => {
		const fetch = createFetchMock(async (input) => {
			return String(input).endsWith('/auth/csrf')
				? jsonResponse({ csrfToken: 'csrf-token-1' })
				: jsonResponse({ ok: true });
		});
		const client = createApiClient({ baseUrl: 'https://api.example.test', fetch, csrf: true });

		await client.request('/campaigns', { method: 'POST' });
		await client.request('/campaigns/campaign-id/launch', { method: 'POST' });

		expect(fetch).toHaveBeenCalledTimes(3);
		expect(fetch.mock.calls[0][0]).toBe('https://api.example.test/auth/csrf');
		expect(fetch.mock.calls[1][0]).toBe('https://api.example.test/campaigns');
		expect(fetch.mock.calls[2][0]).toBe('https://api.example.test/campaigns/campaign-id/launch');
	});

	it('preserves explicit csrf header without fetching a token', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ ok: true }));
		const client = createApiClient({ baseUrl: 'https://api.example.test', fetch, csrf: true });

		await client.request('/campaigns', {
			method: 'POST',
			headers: { 'X-CSRF-TOKEN': 'explicit-token' }
		});

		expect(fetch).toHaveBeenCalledTimes(1);

		const headers = new Headers(fetch.mock.calls[0][1]?.headers);
		expect(headers.get('X-CSRF-TOKEN')).toBe('explicit-token');
	});

	it('does not fetch csrf when csrf is not enabled', async () => {
		const fetch = createFetchMock(async () => jsonResponse({ ok: true }));
		const client = createApiClient({ baseUrl: 'https://api.example.test', fetch });

		await client.request('/respondent/open-links/token/sessions', { method: 'POST' });

		expect(fetch).toHaveBeenCalledTimes(1);
		expect(fetch.mock.calls[0][0]).toBe(
			'https://api.example.test/respondent/open-links/token/sessions'
		);
	});
});

function createFetchMock(implementation: typeof globalThis.fetch) {
	return vi.fn<typeof globalThis.fetch>(implementation);
}

function jsonResponse(body: unknown, status = 200) {
	return new Response(JSON.stringify(body), {
		status,
		headers: { 'content-type': 'application/json' }
	});
}
