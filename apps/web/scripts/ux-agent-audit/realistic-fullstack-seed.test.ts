import { describe, expect, it } from 'vitest';

import { getRealisticAuditCase } from './realistic-cases.ts';
import { seedRealisticFullstackCase } from './realistic-fullstack-seed.ts';

describe('realistic fullstack case seeding', () => {
  it('rejects non-local API URLs before sending synthetic response data', async () => {
    const auditCase = requiredCase();
    const calls: string[] = [];

    await expect(
      seedRealisticFullstackCase({
        apiBaseUrl: 'https://validatedscale-api-staging.croat.dev',
        fullstackDevAuth: { enabled: true },
        seriesId: '33333333-3333-4333-8333-333333333333',
        realisticCase: auditCase,
        fetchImpl: async (url) => {
          calls.push(String(url));
          return jsonResponse({});
        },
      })
    ).rejects.toThrow('local loopback API URL');
    expect(calls).toEqual([]);
  });

  it('creates campaign, submitted responses, scores, and app-visible target paths', async () => {
    const auditCase = requiredCase();
    const calls: Array<{ method: string; path: string; body?: unknown }> = [];
    let sessionIndex = 0;

    const result = await seedRealisticFullstackCase({
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      seriesId: '33333333-3333-4333-8333-333333333333',
      realisticCase: auditCase,
      fetchImpl: async (url, init) => {
        const parsed = new URL(String(url));
        const method = init?.method ?? 'GET';
        const body = init?.body ? JSON.parse(String(init.body)) : undefined;
        calls.push({ method, path: parsed.pathname, body });

        if (parsed.pathname === '/template-versions') {
          return jsonResponse({
            templateVersionId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
            questions: auditCase.questions.map((question, index) => ({
              id: `10000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
              code: `q${String(index + 1).padStart(2, '0')}`,
            })),
          });
        }

        if (parsed.pathname === '/scoring-rules') {
          return jsonResponse({ id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb' });
        }

        if (parsed.pathname === '/campaigns') {
          return jsonResponse({ id: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc' });
        }

        if (parsed.pathname.endsWith('/launch-readiness')) {
          return jsonResponse({ campaignId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', ready: true, issues: [] });
        }

        if (parsed.pathname.endsWith('/launch')) {
          return jsonResponse({
            campaignId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
            status: 'live',
            launchSnapshotId: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
          });
        }

        if (parsed.pathname.endsWith('/invitation-batches')) {
          return jsonResponse({
            campaignId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
            requestedRecipientCount: 21,
            createdInvitationCount: 21,
            invitations: Array.from({ length: 21 }, (_, index) => ({
              assignmentId: `20000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
              token: `token-${String(index + 1).padStart(3, '0')}`,
              respondentPath: `/r/token-${String(index + 1).padStart(3, '0')}`,
              recipient: `sim-${String(index + 1).padStart(3, '0')}@example.test`,
              status: 'queued',
            })),
          });
        }

        if (/^\/respondent\/open-links\/[^/]+$/u.test(parsed.pathname)) {
          return jsonResponse({
            campaignId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
            assignmentId: '20000000-0000-4000-8000-000000000001',
            templateVersionId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
            name: 'Baseline warehouse pulse - May 2026',
            status: 'live',
            responseIdentityMode: 'anonymous',
            requiresParticipantCode: false,
            defaultLocale: 'en',
            consentDocument: {
              id: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
              requiredGrants: ['data_processing', 'research_participation'],
              optionalGrants: [],
            },
            questions: auditCase.questions.map((question, index) => ({
              id: `10000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
              code: `q${String(index + 1).padStart(2, '0')}`,
            })),
          });
        }

        if (/^\/respondent\/open-links\/[^/]+\/sessions$/u.test(parsed.pathname)) {
          sessionIndex += 1;
          return jsonResponse({
            id: `30000000-0000-4000-8000-${String(sessionIndex).padStart(12, '0')}`,
            assignmentId: '20000000-0000-4000-8000-000000000001',
            locale: 'en',
            startedAt: '2026-05-20T19:30:00Z',
            submittedAt: null,
            timeTakenMs: null,
          });
        }

        if (parsed.pathname.endsWith('/answers')) {
          return jsonResponse({ sessionId: parsed.pathname.split('/')[3], savedAnswerCount: 10 });
        }

        if (parsed.pathname.endsWith('/submit')) {
          return jsonResponse({ id: parsed.pathname.split('/')[3], submittedAt: '2026-05-20T19:35:00Z' });
        }

        if (parsed.pathname.endsWith('/scores')) {
          return jsonResponse({
            sessionId: parsed.pathname.split('/')[3],
            scores: [{ dimensionCode: 'recovery', value: 3.8, nValid: 2, nExpected: 2 }],
          });
        }

        throw new Error(`Unhandled ${method} ${parsed.pathname}`);
      },
    });

    expect(result).toEqual(
        expect.objectContaining({
          seedMode: 'local-fullstack-api',
          seriesId: '33333333-3333-4333-8333-333333333333',
          campaignId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
        campaignName: 'Baseline warehouse pulse - May 2026',
        submittedResponseCount: 21,
        scoredResponseCount: 21,
        operationsPath: '/app/campaign-series/33333333-3333-4333-8333-333333333333/operations',
        reportsPath: '/app/campaign-series/33333333-3333-4333-8333-333333333333/reports',
      })
    );
    expect(calls.filter((call) => call.path.endsWith('/invitation-batches'))).toHaveLength(1);
    expect(calls.filter((call) => /^\/respondent\/open-links\/[^/]+$/u.test(call.path))).toHaveLength(21);
    expect(calls.filter((call) => call.path.endsWith('/submit'))).toHaveLength(21);
    expect(calls.filter((call) => call.path.endsWith('/scores'))).toHaveLength(21);
    expect(calls.find((call) => call.path.endsWith('/invitation-batches'))?.body).toEqual({
      recipients: Array.from({ length: 21 }, (_, index) => ({
        email: `sim-${String(index + 1).padStart(3, '0')}@example.test`,
      })),
    });
    expect(calls.find((call) => call.path === '/campaigns')?.body).toEqual(
      expect.objectContaining({
        campaignSeriesId: '33333333-3333-4333-8333-333333333333',
        name: 'Baseline warehouse pulse - May 2026',
        responseIdentityMode: 'anonymous',
      })
    );
    expect(calls.find((call) => call.path === '/template-versions')?.body).toEqual(
      expect.objectContaining({
        scales: [
          expect.objectContaining({
            anchors: JSON.stringify([
              { value: 1, label: 'Strongly disagree' },
              { value: 5, label: 'Strongly agree' },
            ]),
          }),
        ],
      })
    );
  });

  it('creates two anonymous-longitudinal waves with linked participant codes and wave targets', async () => {
    const auditCase = getRealisticAuditCase('academic-workload-recovery-followup');
    if (!auditCase) {
      throw new Error('missing academic repeated-wave case');
    }

    const calls: Array<{ method: string; path: string; body?: unknown }> = [];
    const campaignIds = [
      'c1000000-0000-4000-8000-000000000001',
      'c1000000-0000-4000-8000-000000000002',
    ];
    const launchIds = [
      'd1000000-0000-4000-8000-000000000001',
      'd1000000-0000-4000-8000-000000000002',
    ];
    let campaignIndex = 0;
    let sessionIndex = 0;

    const result = await seedRealisticFullstackCase({
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      seriesId: '33333333-3333-4333-8333-333333333333',
      realisticCase: auditCase,
      mode: 'repeated-wave',
      fetchImpl: async (url, init) => {
        const parsed = new URL(String(url));
        const method = init?.method ?? 'GET';
        const body = init?.body ? JSON.parse(String(init.body)) : undefined;
        calls.push({ method, path: parsed.pathname, body });

        if (parsed.pathname === '/template-versions') {
          return jsonResponse({
            templateVersionId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
            questions: auditCase.questions.map((question, index) => ({
              id: `10000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
              code: `q${String(index + 1).padStart(2, '0')}`,
            })),
          });
        }

        if (parsed.pathname === '/scoring-rules') {
          return jsonResponse({ id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb' });
        }

        if (parsed.pathname === '/campaigns') {
          const id = campaignIds[campaignIndex];
          campaignIndex += 1;
          return jsonResponse({ id });
        }

        if (parsed.pathname.endsWith('/launch-readiness')) {
          return jsonResponse({ ready: true, issues: [] });
        }

        if (parsed.pathname.endsWith('/launch')) {
          const waveIndex = campaignIds.findIndex((id) => parsed.pathname.includes(id));
          return jsonResponse({
            campaignId: campaignIds[waveIndex],
            status: 'live',
            launchSnapshotId: launchIds[waveIndex],
          });
        }

        if (parsed.pathname.endsWith('/open-link')) {
          const waveIndex = campaignIds.findIndex((id) => parsed.pathname.includes(id));
          return jsonResponse({
            campaignId: campaignIds[waveIndex],
            assignmentId: `20000000-0000-4000-8000-${String(waveIndex + 1).padStart(12, '0')}`,
            token: `wave-${waveIndex + 1}-open-link-token`,
            respondentPath: `/r/wave-${waveIndex + 1}-open-link-token`,
          });
        }

        if (/^\/respondent\/open-links\/[^/]+$/u.test(parsed.pathname)) {
          return jsonResponse({
            campaignId: campaignIds[0],
            assignmentId: '20000000-0000-4000-8000-000000000001',
            templateVersionId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
            name: 'Academic workload wave',
            status: 'live',
            responseIdentityMode: 'anonymous_longitudinal',
            requiresParticipantCode: true,
            defaultLocale: 'en',
            consentDocument: {
              id: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
              requiredGrants: ['data_processing', 'research_participation'],
              optionalGrants: [],
            },
            questions: auditCase.questions.map((question, index) => ({
              id: `10000000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`,
              code: `q${String(index + 1).padStart(2, '0')}`,
            })),
          });
        }

        if (/^\/respondent\/open-links\/[^/]+\/sessions$/u.test(parsed.pathname)) {
          sessionIndex += 1;
          return jsonResponse({
            id: `30000000-0000-4000-8000-${String(sessionIndex).padStart(12, '0')}`,
            assignmentId: '20000000-0000-4000-8000-000000000001',
            locale: 'en',
            startedAt: '2026-05-20T19:30:00Z',
            submittedAt: null,
            timeTakenMs: null,
          });
        }

        if (parsed.pathname.endsWith('/answers')) {
          return jsonResponse({ sessionId: parsed.pathname.split('/')[3], savedAnswerCount: 10 });
        }

        if (parsed.pathname.endsWith('/submit')) {
          return jsonResponse({ id: parsed.pathname.split('/')[3], submittedAt: '2026-05-20T19:35:00Z' });
        }

        if (parsed.pathname.endsWith('/scores')) {
          return jsonResponse({
            sessionId: parsed.pathname.split('/')[3],
            scores: [{ dimensionCode: 'teaching_load', value: 3.6, nValid: 2, nExpected: 2 }],
          });
        }

        if (parsed.pathname.endsWith('/close')) {
          return jsonResponse({
            id: parsed.pathname.split('/')[4],
            status: 'closed',
            updatedAt: '2026-05-20T19:40:00Z',
            closedAt: '2026-05-20T19:40:00Z',
            closeReason: 'UX audit repeated-wave seed completed',
          });
        }

        throw new Error(`Unhandled ${method} ${parsed.pathname}`);
      },
    });

    expect(result).toEqual(
      expect.objectContaining({
        seedMode: 'local-fullstack-api',
        waveCount: 2,
        invitationCount: 32,
        submittedResponseCount: 32,
        scoredResponseCount: 32,
        wavesPath: '/app/campaign-series/33333333-3333-4333-8333-333333333333/waves',
        reportsPath: '/app/campaign-series/33333333-3333-4333-8333-333333333333/reports',
      })
    );
    expect(result.campaigns).toEqual([
      expect.objectContaining({
        campaignName: 'Baseline academic workload survey - May 2026',
        responseIdentityMode: 'anonymous_longitudinal',
        closed: true,
      }),
      expect.objectContaining({
        campaignName: 'Follow-up academic workload survey - June 2026',
        responseIdentityMode: 'anonymous_longitudinal',
        closed: true,
      }),
    ]);
    expect(calls.filter((call) => call.path === '/campaigns').map((call) => call.body)).toEqual([
      expect.objectContaining({
        name: 'Baseline academic workload survey - May 2026',
        responseIdentityMode: 'anonymous_longitudinal',
      }),
      expect.objectContaining({
        name: 'Follow-up academic workload survey - June 2026',
        responseIdentityMode: 'anonymous_longitudinal',
      }),
    ]);
    expect(calls.filter((call) => call.path.endsWith('/open-link'))).toHaveLength(2);
    expect(calls.filter((call) => call.path.endsWith('/invitation-batches'))).toHaveLength(0);
    expect(calls.filter((call) => /^\/respondent\/open-links\/[^/]+\/sessions$/u.test(call.path))).toHaveLength(32);
    expect(calls.filter((call) => call.path.endsWith('/close'))).toHaveLength(2);
    const sessionBodies = calls
      .filter((call) => /^\/respondent\/open-links\/[^/]+\/sessions$/u.test(call.path))
      .map((call) => call.body as { participantCode?: string });
    expect(sessionBodies[0]?.participantCode).toBe('uxa-sim-001');
    expect(sessionBodies[16]?.participantCode).toBe('uxa-sim-001');
  });
});

function requiredCase() {
  const auditCase = getRealisticAuditCase('osh-warehouse-workload-recovery-pulse');
  if (!auditCase) {
    throw new Error('missing OSH warehouse case');
  }

  return auditCase;
}

function jsonResponse(value: unknown) {
  return {
    ok: true,
    status: 200,
    text: async () => JSON.stringify(value),
    json: async () => value,
  } as Response;
}
