export const defaultDevTenantId = '11111111-1111-4111-8111-111111111111';
export const defaultDevUserId = '22222222-2222-4222-8222-222222222222';

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

export function createLoginUrlFromEnv(env: Record<string, string | undefined>) {
	const loginUrl = env.PUBLIC_AUTH_LOGIN_URL?.trim() || '/auth/login';
	const tenantId = env.PUBLIC_TENANT_ID?.trim();

	if (!tenantId || /[?&]tenantId=/.test(loginUrl)) {
		return loginUrl;
	}

	const hashIndex = loginUrl.indexOf('#');
	const pathAndQuery = hashIndex >= 0 ? loginUrl.slice(0, hashIndex) : loginUrl;
	const hash = hashIndex >= 0 ? loginUrl.slice(hashIndex) : '';
	const separator = pathAndQuery.includes('?') ? '&' : '?';

	return `${pathAndQuery}${separator}tenantId=${encodeURIComponent(tenantId)}${hash}`;
}
