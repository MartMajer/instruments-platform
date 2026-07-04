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
			},
			async requestBlob() {
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

	it('loads the pending registration session', async () => {
		let capturedPath = '';
		const client: ApiClient = {
			async request<T>(path: string) {
				capturedPath = path;
				return { email: 'owner@example.test' } as T;
			},
			async requestText() {
				throw new Error('not used');
			},
			async requestBlob() {
				throw new Error('not used');
			}
		};

		const api = createRegistrationApi(client);
		const session = await api.getSession();

		expect(capturedPath).toBe('/registration/session');
		expect(session.email).toBe('owner@example.test');
	});

	it('requests an existing workspace sign-in URL by email', async () => {
		let capturedPath = '';
		let capturedBody: unknown = null;
		const api = createRegistrationApi({
			request<T>(path: string, init?: RequestInit) {
				capturedPath = path;
				capturedBody = init?.body ? JSON.parse(init.body.toString()) : null;
				return {
					loginUrl: '/auth/login?tenantId=11111111-1111-4111-8111-111111111111'
				} as T;
			},
			async requestText() {
				throw new Error('not used');
			},
			async requestBlob() {
				throw new Error('not used');
			}
		});

		await api.createExistingWorkspaceSignIn({
			email: 'owner@example.test',
			returnUrl: '/app'
		});

		expect(capturedPath).toBe('/registration/workspace-sign-in');
		expect(capturedBody).toEqual({
			email: 'owner@example.test',
			returnUrl: '/app'
		});
	});

	it('creates a workspace from the pending registration session', async () => {
		expect.assertions(4);
		let capturedPath = '';
		let capturedInit: RequestInit | undefined;
		const client: ApiClient = {
			async request<T>(path: string, init?: RequestInit) {
				capturedPath = path;
				capturedInit = init;
				return { appUrl: '/app', tenantId: 'tenant-id', email: 'owner@example.test' } as T;
			},
			async requestText() {
				throw new Error('not used');
			},
			async requestBlob() {
				throw new Error('not used');
			}
		};

		const api = createRegistrationApi(client);
		await api.createWorkspace({
			organizationName: 'Example Lab',
			accessCode: 'beta-code',
			returnUrl: '/app'
		});

		expect(capturedPath).toBe('/registration/workspaces');
		expect(capturedInit?.method).toBe('POST');
		expect(JSON.parse(String(capturedInit?.body))).toEqual({
			organizationName: 'Example Lab',
			accessCode: 'beta-code',
			returnUrl: '/app'
		});
		expect(new Headers(capturedInit?.headers).has('X-Tenant-Id')).toBe(false);
	});
});
