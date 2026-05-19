import { describe, expect, it } from 'vitest';
import {
	createLoginUrlFromEnv,
	createLogoutUrlFromEnv,
	createSessionHeadersFromEnv,
	readLastWorkspaceEmail,
	rememberLastWorkspaceEmail
} from './session-headers';

const tenantA = '11111111-1111-4111-8111-111111111111';
const tenantB = '22222222-2222-4222-8222-222222222222';

describe('createSessionHeadersFromEnv', () => {
	it('does not create headers when no auth mode is configured', () => {
		expect(createSessionHeadersFromEnv({})).toBeUndefined();
	});

	it('does not create tenant headers for cookie sessions', () => {
		expect(
			createSessionHeadersFromEnv({
				PUBLIC_TENANT_ID: 'tenant-a'
			})
		).toBeUndefined();
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

	it('does not use the public tenant id as an implicit login target', () => {
		expect(createLoginUrlFromEnv({ PUBLIC_TENANT_ID: tenantA })).toBe('/auth/login');
	});

	it('adds explicit tenant id to custom login URLs without replacing existing query', () => {
		expect(
			createLoginUrlFromEnv(
				{
					PUBLIC_AUTH_LOGIN_URL: '/auth/login?returnUrl=/app'
				},
				tenantA
			)
		).toBe(`/auth/login?returnUrl=%2Fapp&tenantId=${tenantA}`);
	});

	it('replaces configured tenant id with the explicit workspace tenant', () => {
		expect(
			createLoginUrlFromEnv(
				{
					PUBLIC_AUTH_LOGIN_URL: `/auth/login?tenantId=${tenantB}`
				},
				tenantA
			)
		).toBe(`/auth/login?tenantId=${tenantA}`);
	});

	it('adds login hint when workspace email is known', () => {
		expect(
			createLoginUrlFromEnv(
				{
					PUBLIC_AUTH_LOGIN_URL: '/auth/login?returnUrl=/app'
				},
				tenantA,
				'owner@example.test'
			)
		).toBe(
			`/auth/login?returnUrl=%2Fapp&tenantId=${tenantA}&login_hint=owner%40example.test`
		);
	});

	it('derives auth endpoints from the public API base URL', () => {
		const env = {
			PUBLIC_API_BASE_URL: 'https://api.example.test'
		};

		expect(createLoginUrlFromEnv(env, tenantA)).toBe(
			`https://api.example.test/auth/login?tenantId=${tenantA}`
		);
		expect(createLogoutUrlFromEnv(env)).toBe('https://api.example.test/auth/logout');
	});

	it('remembers the last workspace email for account-targeted sign-in', () => {
		const storage = new MapStorage();

		rememberLastWorkspaceEmail(storage, ' owner@example.test ');

		expect(readLastWorkspaceEmail(storage)).toBe('owner@example.test');
	});
});

class MapStorage implements Storage {
	private readonly values = new Map<string, string>();

	get length() {
		return this.values.size;
	}

	clear() {
		this.values.clear();
	}

	getItem(key: string) {
		return this.values.get(key) ?? null;
	}

	key(index: number) {
		return Array.from(this.values.keys())[index] ?? null;
	}

	removeItem(key: string) {
		this.values.delete(key);
	}

	setItem(key: string, value: string) {
		this.values.set(key, value);
	}
}
