import { env } from '$env/dynamic/public';
import { ApiError } from '$lib/api/client';
import type { AuthSessionResponse } from '$lib/api/setup';
import { createLoginUrlFromEnv, createLogoutUrlFromEnv } from '$lib/api/session-headers';
import { api } from './client';

type SessionStatus = 'unknown' | 'loading' | 'authenticated' | 'anonymous';

export const session = $state<{
	status: SessionStatus;
	current: AuthSessionResponse | null;
}>({
	status: 'unknown',
	current: null
});

export async function loadSession(): Promise<AuthSessionResponse | null> {
	session.status = 'loading';

	try {
		const current = await api().request<AuthSessionResponse>('/auth/session');
		session.current = current;
		session.status = 'authenticated';
		return current;
	} catch (error) {
		session.current = null;
		if (error instanceof ApiError && (error.status === 401 || error.status === 403)) {
			session.status = 'anonymous';
			return null;
		}

		session.status = 'anonymous';
		return null;
	}
}

export function hasPermission(permission: string): boolean {
	return session.current?.permissions.includes(permission) ?? false;
}

export function loginUrl(tenantId?: string | null, loginHint?: string | null): string {
	return createLoginUrlFromEnv(env, tenantId, loginHint);
}

export function logoutUrl(): string {
	return createLogoutUrlFromEnv(env);
}
