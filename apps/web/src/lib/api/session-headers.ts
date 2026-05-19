export const defaultDevTenantId = '11111111-1111-4111-8111-111111111111';
export const defaultDevUserId = '22222222-2222-4222-8222-222222222222';
export const lastTenantIdStorageKey = 'instruments-platform.last-tenant-id';

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
	tenantIdOverride?: string | null
) {
	const loginUrl = env.PUBLIC_AUTH_LOGIN_URL?.trim() || '/auth/login';
	const tenantId = tenantIdOverride?.trim() || env.PUBLIC_TENANT_ID?.trim();

	if (!tenantId || /[?&]tenantId=/.test(loginUrl)) {
		return loginUrl;
	}

	const hashIndex = loginUrl.indexOf('#');
	const pathAndQuery = hashIndex >= 0 ? loginUrl.slice(0, hashIndex) : loginUrl;
	const hash = hashIndex >= 0 ? loginUrl.slice(hashIndex) : '';
	const separator = pathAndQuery.includes('?') ? '&' : '?';

	return `${pathAndQuery}${separator}tenantId=${encodeURIComponent(tenantId)}${hash}`;
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

function isTenantId(value: string) {
	return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
		value
	);
}
