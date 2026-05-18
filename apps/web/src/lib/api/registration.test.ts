import { describe, expect, it } from 'vitest';
import { createRegistrationApi } from './registration';
import type { ApiClient } from './client';

describe('registration api', () => {
	it('posts a registration intent without tenant or dev-auth headers', async () => {
		expect.assertions(4);
		let capturedPath = '';
		let capturedInit: RequestInit | undefined;
		const client: ApiClient = {
			async request<T>(path: string, init?: RequestInit) {
				capturedPath = path;
				capturedInit = init;
				return { loginUrl: '/auth/login?registrationToken=token', expiresAt: '2026-05-18T20:00:00Z' } as T;
			},
			async requestText() {
				throw new Error('not used');
			}
		};

		const api = createRegistrationApi(client);
		await api.createIntent({
			email: 'owner@example.test',
			organizationName: 'Example Lab',
			accessCode: 'beta-code',
			returnUrl: '/app'
		});

		expect(capturedPath).toBe('/registration/intents');
		expect(capturedInit?.method).toBe('POST');
		expect(JSON.parse(String(capturedInit?.body))).toEqual({
			email: 'owner@example.test',
			organizationName: 'Example Lab',
			accessCode: 'beta-code',
			returnUrl: '/app'
		});
		expect(new Headers(capturedInit?.headers).has('X-Tenant-Id')).toBe(false);
	});
});
