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
	const loginUrl = env.PUBLIC_AUTH_LOGIN_URL?.trim() || '/auth/login';
	const tenantId = tenantIdOverride?.trim() || env.PUBLIC_TENANT_ID?.trim();
	const loginHint = loginHintOverride?.trim();

	if ((!tenantId || /[?&]tenantId=/.test(loginUrl)) && !loginHint) {
		return loginUrl;
	}

	const hashIndex = loginUrl.indexOf('#');
	const pathAndQuery = hashIndex >= 0 ? loginUrl.slice(0, hashIndex) : loginUrl;
	const hash = hashIndex >= 0 ? loginUrl.slice(hashIndex) : '';
	const parameters: string[] = [];

	if (tenantId && !/[?&]tenantId=/.test(loginUrl)) {
		parameters.push(`tenantId=${encodeURIComponent(tenantId)}`);
	}

	if (loginHint && !/[?&]login_hint=/.test(loginUrl)) {
		parameters.push(`login_hint=${encodeURIComponent(loginHint)}`);
	}

	if (parameters.length === 0) {
		return loginUrl;
	}

	const separator = pathAndQuery.includes('?') ? '&' : '?';
	return `${pathAndQuery}${separator}${parameters.join('&')}${hash}`;
}

export function readLastTenantId(storage: Storage | undefined) {
	const value = storage?.getItem(lastTenantIdStorageKey)?.trim() ?? '';
	return isTenantId(value) ? value : '';
}

export function rememberLastTenantId(storage: Storage | undefined, tenantId: string) {
	const value = tenantId.trim();
	if (!isTenantId(value)) {
		return;
	}

	storage?.setItem(lastTenantIdStorageKey, value);
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
