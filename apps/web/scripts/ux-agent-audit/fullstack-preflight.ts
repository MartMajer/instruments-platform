import { resolveFullstackDevAuthHeaders } from './fullstack-dev-auth.ts';
import type { AutonomousFullstackDevAuthOptions } from './types.ts';

export type FullstackPreflightStatus = 'ready' | 'blocked';
export type FullstackPreflightCheckStatus = 'passed' | 'failed' | 'skipped';

export interface FullstackPreflightOptions {
  apiBaseUrl: string;
  fullstackDevAuth: AutonomousFullstackDevAuthOptions;
  timeoutMs?: number;
  fetch?: typeof fetch;
}

export interface FullstackPreflightCheck {
  id: 'api-health' | 'dev-auth-session' | 'tenant-study-read-model';
  label: string;
  status: FullstackPreflightCheckStatus;
  detail: string;
  guidance?: string;
}

export interface FullstackPreflightReport {
  status: FullstackPreflightStatus;
  apiBaseUrl: string;
  checks: FullstackPreflightCheck[];
}

const defaultTimeoutMs = 5000;

export async function checkFullstackPreflight(
  options: FullstackPreflightOptions
): Promise<FullstackPreflightReport> {
  const apiBaseUrl = normalizeBaseUrl(options.apiBaseUrl);
  const fetchImpl = options.fetch ?? fetch;
  const timeoutMs = options.timeoutMs ?? defaultTimeoutMs;
  const headers = resolveFullstackDevAuthHeaders(options.fullstackDevAuth);
  const checks: FullstackPreflightCheck[] = [];

  const health = await safeFetch(fetchImpl, apiBaseUrl, '/health', timeoutMs);
  if (!health.ok) {
    checks.push({
      id: 'api-health',
      label: 'Local API health',
      status: 'failed',
      detail: health.detail,
      guidance:
        'Start the local API/database stack, then rerun the UXA02 full-stack preflight.',
    });
    checks.push(skippedCheck('dev-auth-session', 'Development-auth session'));
    checks.push(skippedCheck('tenant-study-read-model', 'Tenant study read model'));
    return { status: 'blocked', apiBaseUrl, checks };
  }

  checks.push({
    id: 'api-health',
    label: 'Local API health',
    status: 'passed',
    detail: health.detail,
  });

  const session = await safeFetch(fetchImpl, apiBaseUrl, '/auth/session', timeoutMs, headers);
  if (!session.ok) {
    checks.push({
      id: 'dev-auth-session',
      label: 'Development-auth session',
      status: 'failed',
      detail: session.detail,
      guidance:
        'Start the API with Authentication__Dev__Enabled=true and pass --fullstack-dev-auth with a tenant/user that belongs to the local seed.',
    });
    checks.push(skippedCheck('tenant-study-read-model', 'Tenant study read model'));
    return { status: 'blocked', apiBaseUrl, checks };
  }

  checks.push({
    id: 'dev-auth-session',
    label: 'Development-auth session',
    status: 'passed',
    detail: session.detail,
  });

  const studies = await safeFetch(fetchImpl, apiBaseUrl, '/campaign-series', timeoutMs, headers);
  if (!studies.ok) {
    checks.push({
      id: 'tenant-study-read-model',
      label: 'Tenant study read model',
      status: 'failed',
      detail: studies.detail,
      guidance:
        'Seed the local tenant/workspace data or verify the dev-auth tenant id before running fullstack-create-study.',
    });
    return { status: 'blocked', apiBaseUrl, checks };
  }

  checks.push({
    id: 'tenant-study-read-model',
    label: 'Tenant study read model',
    status: 'passed',
    detail: studies.detail,
  });

  return { status: 'ready', apiBaseUrl, checks };
}

function normalizeBaseUrl(value: string) {
  return value.trim().replace(/\/+$/u, '');
}

function skippedCheck(
  id: FullstackPreflightCheck['id'],
  label: string
): FullstackPreflightCheck {
  return {
    id,
    label,
    status: 'skipped',
    detail: 'Skipped because an earlier full-stack preflight check failed.',
  };
}

async function safeFetch(
  fetchImpl: typeof fetch,
  apiBaseUrl: string,
  path: string,
  timeoutMs: number,
  headers?: Record<string, string>
) {
  try {
    const response = await fetchImpl(new URL(path, `${apiBaseUrl}/`).toString(), {
      headers,
      signal: AbortSignal.timeout(timeoutMs),
    });

    return {
      ok: response.ok,
      detail: response.ok ? `HTTP ${response.status}` : `HTTP ${response.status}`,
    };
  } catch (error) {
    return {
      ok: false,
      detail: error instanceof Error ? error.message : 'Request failed.',
    };
  }
}
