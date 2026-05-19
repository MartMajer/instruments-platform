export const defaultDevTenantId = '11111111-1111-4111-8111-111111111111';
export const defaultDevUserId = '22222222-2222-4222-8222-222222222222';
export const lastTenantIdStorageKey = 'instruments-platform.last-tenant-id';
export const lastWorkspaceEmailStorageKey = 'instruments-platform.last-workspace-email';

export function createSessionHeadersFromEnv(
	env: Record<string, string | undefined>
): HeadersInit | undefined {
	if (env.PUBLIC_DEV_AUTH_ENABLED === 'true') {
		const tenantId = env.PUBLIC_DEV_TENANT_ID || defaultDevTenantId;
		const userId = env.PUBLIC_DEV_USER_ID || defaultDevUserId;

		return {
			'X-Tenant-Id': tenantId,
			'X-Dev-User-Id': userId,
			'X-Dev-Tenant-Memberships': tenantId,
			'X-Dev-Permissions': 'setup.manage'
		};
	}

	return undefined;
}

export function createLoginUrlFromEnv(
	env: Record<string, string | undefined>,
	tenantIdOverride?: string | null,
	loginHintOverride?: string | null
) {
	const loginUrl = createAuthEndpointUrl(env, env.PUBLIC_AUTH_LOGIN_URL, '/auth/login');
	const tenantId = normalizeTenantId(tenantIdOverride);
	const loginHint = loginHintOverride?.trim();

	return setLoginQueryParameters(loginUrl, tenantId, loginHint);
}

export function createLogoutUrlFromEnv(env: Record<string, string | undefined>) {
	return createAuthEndpointUrl(env, env.PUBLIC_AUTH_LOGOUT_URL, '/auth/logout');
}

function createAuthEndpointUrl(
	env: Record<string, string | undefined>,
	configuredUrl: string | undefined,
	fallbackPath: string
) {
	const configured = configuredUrl?.trim();
	if (configured) {
		return configured;
	}

	const apiBaseUrl = env.PUBLIC_API_BASE_URL?.trim();
	if (apiBaseUrl && /^https?:\/\//i.test(apiBaseUrl)) {
		return new URL(fallbackPath, `${apiBaseUrl.replace(/\/+$/, '')}/`).toString();
	}

	return fallbackPath;
}

function setLoginQueryParameters(loginUrl: string, tenantId: string, loginHint: string | undefined) {
	const isAbsoluteUrl = /^https?:\/\//i.test(loginUrl);
	const parsedUrl = new URL(loginUrl, 'http://localhost');

	if (tenantId) {
		parsedUrl.searchParams.set('tenantId', tenantId);
	} else {
		parsedUrl.searchParams.delete('tenantId');
	}

	if (loginHint) {
		parsedUrl.searchParams.set('login_hint', loginHint);
	} else {
		parsedUrl.searchParams.delete('login_hint');
	}

	if (isAbsoluteUrl) {
		return parsedUrl.toString();
	}

	return `${parsedUrl.pathname}${parsedUrl.search}${parsedUrl.hash}`;
}

export function readLastTenantId(storage: Storage | undefined) {
	const value = storage?.getItem(lastTenantIdStorageKey)?.trim() ?? '';
	return normalizeTenantId(value);
}

export function rememberLastTenantId(storage: Storage | undefined, tenantId: string) {
	const value = normalizeTenantId(tenantId);
	if (!value) {
		return;
	}

	storage?.setItem(lastTenantIdStorageKey, value);
}

export function normalizeTenantId(value: string | null | undefined) {
	const normalized = value?.trim() ?? '';

	return isTenantId(normalized) ? normalized : '';
}

export function readLastWorkspaceEmail(storage: Storage | undefined) {
	const value = storage?.getItem(lastWorkspaceEmailStorageKey)?.trim() ?? '';
	return isEmailHint(value) ? value : '';
}

export function rememberLastWorkspaceEmail(
	storage: Storage | undefined,
	email: string | null | undefined
) {
	const value = email?.trim() ?? '';
	if (!isEmailHint(value)) {
		return;
	}

	storage?.setItem(lastWorkspaceEmailStorageKey, value);
}

function isTenantId(value: string) {
	return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
		value
	);
}

function isEmailHint(value: string) {
	return value.length > 3 && value.length <= 320 && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}
