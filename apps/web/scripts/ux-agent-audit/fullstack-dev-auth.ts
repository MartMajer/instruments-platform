import type { AutonomousFullstackDevAuthOptions } from './types.ts';

const defaultDevTenantId = '11111111-1111-4111-8111-111111111111';
const defaultDevUserId = '22222222-2222-4222-8222-222222222222';
const defaultFullstackDevPermissions = ['setup.manage', 'team.manage', 'export.read'];

export function resolveFullstackDevAuthHeaders(
  options: AutonomousFullstackDevAuthOptions | undefined
) {
  if (options?.enabled !== true) {
    return undefined;
  }

  const tenantId = options.tenantId?.trim() || defaultDevTenantId;
  const userId = options.userId?.trim() || defaultDevUserId;
  const permissions = options.permissions?.length
    ? options.permissions
    : defaultFullstackDevPermissions;
  const headers: Record<string, string> = {
    'X-Tenant-Id': tenantId,
    'X-Dev-User-Id': userId,
    'X-Dev-Tenant-Memberships': tenantId,
    'X-Dev-Permissions': permissions.join(' '),
  };
  const email = options.email?.trim();
  if (email) {
    headers['X-Dev-Email'] = email;
  }

  return headers;
}

export function describeFullstackDevAuth(
  options: AutonomousFullstackDevAuthOptions | undefined
) {
  return options?.enabled === true ? 'enabled' : 'disabled';
}
