import { resolveFullstackDevAuthHeaders } from './fullstack-dev-auth.ts';
import { getRealisticAuditCase } from './realistic-cases.ts';
import type { AutonomousFullstackDevAuthOptions } from './types.ts';

export interface FullstackCleanupOptions {
  apiBaseUrl: string;
  fullstackDevAuth: AutonomousFullstackDevAuthOptions;
  apply?: boolean;
  timeoutMs?: number;
  fetch?: typeof fetch;
}

export interface FullstackCleanupCandidate {
  id: string;
  name: string;
  reason: string;
  campaignCount: number;
  submittedResponseCount: number;
}

export interface FullstackCleanupReport {
  status: 'dry-run' | 'applied';
  apiBaseUrl: string;
  apply: boolean;
  matchedCount: number;
  archivedCount: number;
  candidates: FullstackCleanupCandidate[];
}

type CampaignSeriesListResponse = {
  items: CampaignSeriesListItemResponse[];
};

type CampaignSeriesListItemResponse = {
  id: string;
  name: string;
  campaignCount?: number;
  submittedResponseCount?: number;
  archived?: boolean;
};

const defaultTimeoutMs = 5000;
const cleanupArchiveReason =
  'Archived by local UXA cleanup for disposable synthetic full-stack study evidence.';

export async function cleanupFullstackSyntheticStudies(
  options: FullstackCleanupOptions
): Promise<FullstackCleanupReport> {
  const apiBaseUrl = normalizeLocalApiBaseUrl(options.apiBaseUrl);
  const headers = resolveFullstackDevAuthHeaders(options.fullstackDevAuth);
  if (!headers) {
    throw new Error('Full-stack cleanup requires --fullstack-dev-auth.');
  }

  const fetchImpl = options.fetch ?? fetch;
  const timeoutMs = options.timeoutMs ?? defaultTimeoutMs;
  const list = await requestJson<CampaignSeriesListResponse>(
    fetchImpl,
    apiBaseUrl,
    '/campaign-series?visibility=active',
    {
      method: 'GET',
      headers,
      timeoutMs,
    }
  );
  const candidates = list.items
    .filter((item) => item.archived !== true)
    .map(toCleanupCandidate)
    .filter((item): item is FullstackCleanupCandidate => Boolean(item));

  if (options.apply === true) {
    for (const candidate of candidates) {
      await requestJson(
        fetchImpl,
        apiBaseUrl,
        `/campaign-series/${encodeURIComponent(candidate.id)}/archive`,
        {
          method: 'POST',
          headers,
          body: { reason: cleanupArchiveReason },
          timeoutMs,
        }
      );
    }
  }

  return {
    status: options.apply === true ? 'applied' : 'dry-run',
    apiBaseUrl,
    apply: options.apply === true,
    matchedCount: candidates.length,
    archivedCount: options.apply === true ? candidates.length : 0,
    candidates,
  };
}

function toCleanupCandidate(
  item: CampaignSeriesListItemResponse
): FullstackCleanupCandidate | undefined {
  const reason = cleanupReasonForStudyName(item.name);
  if (!reason) {
    return undefined;
  }

  return {
    id: item.id,
    name: item.name,
    reason,
    campaignCount: item.campaignCount ?? 0,
    submittedResponseCount: item.submittedResponseCount ?? 0,
  };
}

function cleanupReasonForStudyName(name: string) {
  const normalized = name.trim();
  if (/^UXA (?:full-stack mutation|local study)\b/u.test(normalized)) {
    return 'UXA generated mutation mission study name.';
  }

  const realisticCases = [
    getRealisticAuditCase('osh-warehouse-workload-recovery-pulse'),
    getRealisticAuditCase('academic-workload-recovery-followup'),
  ].filter(Boolean);
  const matchingCase = realisticCases.find((auditCase) => auditCase?.studyName === normalized);
  if (matchingCase) {
    return `UXA realistic case ${matchingCase.id}.`;
  }

  return undefined;
}

async function requestJson<T = unknown>(
  fetchImpl: typeof fetch,
  apiBaseUrl: string,
  path: string,
  options: {
    method: 'GET' | 'POST';
    headers: Record<string, string>;
    body?: unknown;
    timeoutMs: number;
  }
): Promise<T> {
  const response = await fetchImpl(new URL(path, `${apiBaseUrl}/`).toString(), {
    method: options.method,
    headers: {
      ...options.headers,
      ...(options.body === undefined ? {} : { 'content-type': 'application/json' }),
    },
    ...(options.body === undefined ? {} : { body: JSON.stringify(options.body) }),
    signal: AbortSignal.timeout(options.timeoutMs),
  });

  if (!response.ok) {
    throw new Error(
      `Full-stack cleanup API request failed: ${options.method} ${path} returned ${response.status} ${await response.text()}`
    );
  }

  return (await response.json()) as T;
}

function normalizeLocalApiBaseUrl(value: string) {
  const url = new URL(value);
  const localHosts = new Set(['localhost', '127.0.0.1', '::1', '0.0.0.0']);

  if (url.protocol !== 'http:' || !localHosts.has(url.hostname)) {
    throw new Error('Full-stack cleanup requires a local loopback API URL.');
  }

  url.pathname = url.pathname.replace(/\/+$/g, '');
  url.search = '';
  url.hash = '';

  return url.toString();
}
