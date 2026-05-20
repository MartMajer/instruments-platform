import { describe, expect, it, vi } from 'vitest';

import { cleanupFullstackSyntheticStudies } from './fullstack-cleanup.ts';

describe('UXA full-stack synthetic study cleanup', () => {
  it('rejects non-local API URLs before listing studies', async () => {
    const fetch = vi.fn(async () => jsonResponse(200, {}));

    await expect(
      cleanupFullstackSyntheticStudies({
        apiBaseUrl: 'https://validatedscale-api-staging.croat.dev',
        fullstackDevAuth: { enabled: true },
        fetch,
      })
    ).rejects.toThrow('local loopback API URL');
    expect(fetch).not.toHaveBeenCalled();
  });

  it('requires development auth before listing local studies', async () => {
    const fetch = vi.fn(async () => jsonResponse(200, {}));

    await expect(
      cleanupFullstackSyntheticStudies({
        apiBaseUrl: 'http://127.0.0.1:5055',
        fullstackDevAuth: { enabled: false },
        fetch,
      })
    ).rejects.toThrow('--fullstack-dev-auth');
    expect(fetch).not.toHaveBeenCalled();
  });

  it('dry-runs conservative UXA study matches without archiving by default', async () => {
    const fetch = vi.fn(async (url: string, init?: RequestInit) => {
      const parsed = new URL(url);
      expect(init?.method).toBe('GET');
      expect(parsed.pathname).toBe('/campaign-series');
      expect(parsed.searchParams.get('visibility')).toBe('active');

      return jsonResponse(200, {
        items: [
          study('11111111-1111-4111-8111-111111111111', 'UXA full-stack mutation 20260520220000'),
          study('22222222-2222-4222-8222-222222222222', 'Warehouse workload and recovery pulse', 1, 21),
          study('33333333-3333-4333-8333-333333333333', 'Academic workload and recovery follow-up', 2, 32),
          study('44444444-4444-4444-8444-444444444444', 'Owner validation study', 1, 3),
        ],
      });
    });

    const report = await cleanupFullstackSyntheticStudies({
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      fetch,
    });

    expect(report).toEqual(
      expect.objectContaining({
        status: 'dry-run',
        apply: false,
        matchedCount: 3,
        archivedCount: 0,
      })
    );
    expect(report.candidates.map((candidate) => candidate.name)).toEqual([
      'UXA full-stack mutation 20260520220000',
      'Warehouse workload and recovery pulse',
      'Academic workload and recovery follow-up',
    ]);
    expect(fetch).toHaveBeenCalledTimes(1);
  });

  it('archives matched UXA studies only when apply is explicit', async () => {
    const calls: Array<{ method: string; path: string; body?: unknown }> = [];
    const fetch = vi.fn(async (url: string, init?: RequestInit) => {
      const parsed = new URL(url);
      const method = init?.method ?? 'GET';
      calls.push({
        method,
        path: parsed.pathname,
        body: init?.body ? JSON.parse(String(init.body)) : undefined,
      });

      if (method === 'GET' && parsed.pathname === '/campaign-series') {
        return jsonResponse(200, {
          items: [
            study('11111111-1111-4111-8111-111111111111', 'UXA local study 20260520220000'),
            study('22222222-2222-4222-8222-222222222222', 'Owner validation study'),
          ],
        });
      }

      if (method === 'POST' && parsed.pathname.endsWith('/archive')) {
        return jsonResponse(200, {
          id: parsed.pathname.split('/')[2],
          archived: true,
          updatedAt: '2026-05-20T21:50:00Z',
          archivedAt: '2026-05-20T21:50:00Z',
          archivedByUserId: '22222222-2222-4222-8222-222222222222',
          archiveReason: 'Archived by local UXA cleanup',
        });
      }

      throw new Error(`Unexpected request ${method} ${parsed.pathname}`);
    });

    const report = await cleanupFullstackSyntheticStudies({
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      apply: true,
      fetch,
    });

    expect(report).toEqual(
      expect.objectContaining({
        status: 'applied',
        matchedCount: 1,
        archivedCount: 1,
      })
    );
    expect(calls).toEqual([
      { method: 'GET', path: '/campaign-series', body: undefined },
      {
        method: 'POST',
        path: '/campaign-series/11111111-1111-4111-8111-111111111111/archive',
        body: {
          reason:
            'Archived by local UXA cleanup for disposable synthetic full-stack study evidence.',
        },
      },
    ]);
  });
});

function study(
  id: string,
  name: string,
  campaignCount = 0,
  submittedResponseCount = 0
) {
  return {
    id,
    name,
    createdAt: '2026-05-20T21:40:00Z',
    updatedAt: '2026-05-20T21:40:00Z',
    campaignCount,
    liveCampaignCount: 0,
    submittedResponseCount,
    latestLaunchAt: null,
    latestSubmissionAt: null,
    readinessStatus: 'pending',
    archived: false,
    archivedAt: null,
    archivedByUserId: null,
    archiveReason: null,
    studyKind: 'own',
    isSample: false,
    sampleScenario: null,
    readOnlyReason: null,
  };
}

function jsonResponse(status: number, value: unknown) {
  return {
    ok: status >= 200 && status < 300,
    status,
    text: async () => JSON.stringify(value),
    json: async () => value,
  } as Response;
}
