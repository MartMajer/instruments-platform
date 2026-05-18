import { describe, expect, it } from 'vitest';
import { createLoginUrlFromEnv, createSessionHeadersFromEnv } from './session-headers';

describe('createSessionHeadersFromEnv', () => {
	it('does not create headers when no auth mode is configured', () => {
		expect(createSessionHeadersFromEnv({})).toBeUndefined();
	});

	it('creates tenant header for non-development cookie sessions', () => {
		const headers = new Headers(
			createSessionHeadersFromEnv({
				PUBLIC_TENANT_ID: 'tenant-a'
			})
		);

		expect(headers.get('x-tenant-id')).toBe('tenant-a');
		expect(headers.has('x-dev-user-id')).toBe(false);
	});

	it('creates development auth headers when development auth is enabled', () => {
		const headers = new Headers(
			createSessionHeadersFromEnv({
				PUBLIC_DEV_AUTH_ENABLED: 'true',
				PUBLIC_DEV_TENANT_ID: 'dev-tenant',
				PUBLIC_DEV_USER_ID: 'dev-user'
			})
		);

		expect(headers.get('x-tenant-id')).toBe('dev-tenant');
		expect(headers.get('x-dev-user-id')).toBe('dev-user');
		expect(headers.get('x-dev-tenant-memberships')).toBe('dev-tenant');
		expect(headers.get('x-dev-permissions')).toBe('setup.manage');
	});

	it('prefers development auth tenant over public tenant when both are configured', () => {
		const headers = new Headers(
			createSessionHeadersFromEnv({
				PUBLIC_TENANT_ID: 'cookie-tenant',
				PUBLIC_DEV_AUTH_ENABLED: 'true',
				PUBLIC_DEV_TENANT_ID: 'dev-tenant',
				PUBLIC_DEV_USER_ID: 'dev-user'
			})
		);

		expect(headers.get('x-tenant-id')).toBe('dev-tenant');
	});

	it('adds public tenant id to the default login URL', () => {
		expect(createLoginUrlFromEnv({ PUBLIC_TENANT_ID: 'tenant-a' })).toBe(
			'/auth/login?tenantId=tenant-a'
		);
	});

	it('adds public tenant id to custom login URLs without replacing existing query', () => {
		expect(
			createLoginUrlFromEnv({
				PUBLIC_AUTH_LOGIN_URL: '/auth/login?returnUrl=/app',
				PUBLIC_TENANT_ID: 'tenant-a'
			})
		).toBe('/auth/login?returnUrl=/app&tenantId=tenant-a');
	});

	it('does not add public tenant id when the configured login URL already has one', () => {
		expect(
			createLoginUrlFromEnv({
				PUBLIC_AUTH_LOGIN_URL: '/auth/login?tenantId=tenant-b',
				PUBLIC_TENANT_ID: 'tenant-a'
			})
		).toBe('/auth/login?tenantId=tenant-b');
	});
});
