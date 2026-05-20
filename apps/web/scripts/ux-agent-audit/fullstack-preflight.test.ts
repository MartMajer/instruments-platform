import { describe, expect, it, vi } from 'vitest';

import { checkFullstackPreflight } from './fullstack-preflight.ts';

describe('UXA02 full-stack preflight', () => {
  it('passes when API health, dev-auth session, and tenant studies are reachable', async () => {
    const fetch = vi.fn(async (url: string) => {
      if (url.endsWith('/health')) {
        return jsonResponse(200, { status: 'ok' });
      }

      if (url.endsWith('/auth/session')) {
        return jsonResponse(200, {
          userId: '22222222-2222-4222-8222-222222222222',
          tenantId: '11111111-1111-4111-8111-111111111111',
          permissions: ['setup.manage'],
        });
      }

      if (url.endsWith('/campaign-series')) {
        return jsonResponse(200, { items: [] });
      }

      throw new Error(`Unexpected URL ${url}`);
    });

    const report = await checkFullstackPreflight({
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      fetch,
    });

    expect(report.status).toBe('ready');
    expect(report.checks.map((check) => [check.id, check.status])).toEqual([
      ['api-health', 'passed'],
      ['dev-auth-session', 'passed'],
      ['tenant-study-read-model', 'passed'],
    ]);
  });

  it('blocks with setup guidance when the local API is unavailable', async () => {
    const report = await checkFullstackPreflight({
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      fetch: vi.fn(async () => {
        throw new Error('connect ECONNREFUSED');
      }),
    });

    expect(report.status).toBe('blocked');
    expect(report.checks[0]).toEqual(
      expect.objectContaining({
        id: 'api-health',
        status: 'failed',
        guidance: expect.stringContaining('Start the local API/database stack'),
      })
    );
    expect(report.checks.slice(1).every((check) => check.status === 'skipped')).toBe(true);
  });

  it('blocks when development authentication is not enabled or accepted', async () => {
    const fetch = vi.fn(async (url: string) => {
      if (url.endsWith('/health')) {
        return jsonResponse(200, { status: 'ok' });
      }

      if (url.endsWith('/auth/session')) {
        return jsonResponse(401, { title: 'Unauthorized' });
      }

      throw new Error(`Unexpected URL ${url}`);
    });

    const report = await checkFullstackPreflight({
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      fetch,
    });

    expect(report.status).toBe('blocked');
    expect(report.checks[1]).toEqual(
      expect.objectContaining({
        id: 'dev-auth-session',
        status: 'failed',
        guidance: expect.stringContaining('Authentication__Dev__Enabled=true'),
      })
    );
    expect(report.checks[2].status).toBe('skipped');
  });
});

function jsonResponse(status: number, body: unknown) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
    text: async () => JSON.stringify(body),
  } as Response;
}
