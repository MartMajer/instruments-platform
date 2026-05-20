import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => {
  const bodyInnerText = vi.fn(async () =>
    'Contact researcher@example.test with participant code ABCD-1234 and token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.signature.'
  );
  const buttonEvaluateAll = vi.fn(async () => ['Email researcher@example.test']);
  const linkEvaluateAll = vi.fn(async () => [
    {
      text: 'Continue with code ABCD-1234',
      href: 'https://validatedscale.test/respond/11111111-1111-4111-8111-111111111111?invitationToken=secret#answers',
    },
  ]);
  const screenshot = vi.fn(async () => undefined);
  const bodyWaitFor = vi.fn(async () => undefined);
  const structuralWaitFor = vi.fn(async () => undefined);
  const page = {
    goto: vi.fn(async () => undefined),
    title: vi.fn(async () => 'UX audit safety'),
    url: vi.fn(() =>
      'http://127.0.0.1:5174/respond?participantCode=ABCD-1234#answers'
    ),
    waitForFunction: vi.fn(async () => undefined),
    waitForLoadState: vi.fn(async () => {
      throw new Error('networkidle should not be required for snapshot readiness');
    }),
    waitForTimeout: vi.fn(async () => undefined),
    locator: vi.fn((selector: string): any => {
      if (selector === 'body') {
        return { innerText: bodyInnerText, waitFor: bodyWaitFor };
      }

      if (selector === 'button:visible') {
        return { evaluateAll: buttonEvaluateAll };
      }

      if (selector === 'a:visible') {
        return { evaluateAll: linkEvaluateAll };
      }

      return {
        evaluateAll: vi.fn(async () => []),
        first: vi.fn(() => ({ waitFor: structuralWaitFor })),
        innerText: vi.fn(async () => ''),
      };
    }),
    screenshot,
  };
  const context = {
    newPage: vi.fn(async () => page),
  };
  const browser = {
    newContext: vi.fn(async () => context),
    close: vi.fn(async () => undefined),
  };

  return {
    browser,
    bodyWaitFor,
    bodyInnerText,
    buttonEvaluateAll,
    createRunDirectory: vi.fn(async () => ({ runDirectory: 'C:\\safe-run' })),
    linkEvaluateAll,
    mkdir: vi.fn(async () => undefined),
    page,
    screenshot,
    structuralWaitFor,
    writeMissionEvidence: vi.fn(async () => ({
      evidencePath: 'C:\\safe-run\\missions\\mission\\evidence.json',
      transcriptPath: 'C:\\safe-run\\missions\\mission\\transcript.md',
    })),
  };
});

vi.mock('@playwright/test', () => ({
  chromium: {
    launch: vi.fn(async () => mocks.browser),
  },
}));

vi.mock('node:fs/promises', () => ({
  mkdir: mocks.mkdir,
}));

vi.mock('./evidence.ts', () => ({
  createRunDirectory: mocks.createRunDirectory,
  writeMissionEvidence: mocks.writeMissionEvidence,
}));

import {
  captureBrowserEvidence,
  resolveFullstackDevAuthHeaders,
  sanitizeEvidenceUrl,
  sanitizeVisibleTextForEvidence,
  toSafeCapturedLink,
  waitForPageReadyForSnapshot,
} from './browser.ts';

describe('UX audit browser evidence safety', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('removes query strings and fragments from captured page URLs', () => {
    expect(
      sanitizeEvidenceUrl(
        'https://validatedscale.test/respond?invitationToken=secret#answers'
      )
    ).toBe('https://validatedscale.test/respond');

    expect(sanitizeEvidenceUrl('/app/studies?tenantId=secret#setup')).toBe(
      '/app/studies'
    );
  });

  it('redacts emails, UUIDs, token-like values, and participant-code-like values from visible text', () => {
    const sanitized = sanitizeVisibleTextForEvidence(
      'Email researcher@example.test, tenant 11111111-1111-4111-8111-111111111111, code ABCD-1234, token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.signature.',
      240
    );

    expect(sanitized).toContain('[redacted-email]');
    expect(sanitized).toContain('[redacted-uuid]');
    expect(sanitized).toContain('[redacted-code]');
    expect(sanitized).toContain('[redacted-token]');
    expect(sanitized).not.toContain('researcher@example.test');
    expect(sanitized).not.toContain(
      '11111111-1111-4111-8111-111111111111'
    );
    expect(sanitized).not.toContain('ABCD-1234');
    expect(sanitized).not.toContain('eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9');
    expect(sanitized.length).toBeLessThanOrEqual(240);
  });

  it('captures safe link labels and paths without full hrefs', () => {
    const link = toSafeCapturedLink({
      text: 'Open invite for researcher@example.test using code ABCD-1234',
      href: 'https://validatedscale.test/respond/11111111-1111-4111-8111-111111111111?invitationToken=secret#answers',
    });

    expect(link).toEqual({
      text: 'Open invite for [redacted-email] using code [redacted-code]',
      path: '/respond/[redacted-uuid]',
    });
    expect(link).not.toHaveProperty('href');
  });

  it('builds local fullstack development auth headers only when explicitly enabled', () => {
    expect(resolveFullstackDevAuthHeaders({ enabled: false })).toBeUndefined();

    expect(
      resolveFullstackDevAuthHeaders({
        enabled: true,
        tenantId: '33333333-3333-4333-8333-333333333333',
        userId: '44444444-4444-4444-8444-444444444444',
        email: 'ux-agent@example.test',
        permissions: ['setup.manage', 'team.manage'],
      })
    ).toEqual({
      'X-Tenant-Id': '33333333-3333-4333-8333-333333333333',
      'X-Dev-User-Id': '44444444-4444-4444-8444-444444444444',
      'X-Dev-Tenant-Memberships': '33333333-3333-4333-8333-333333333333',
      'X-Dev-Permissions': 'setup.manage team.manage',
      'X-Dev-Email': 'ux-agent@example.test',
    });
  });

  it('does not capture screenshots or visible body text by default when writing evidence', async () => {
    const capture = await captureBrowserEvidence({
      baseUrl: 'http://127.0.0.1:5174/app?tenantId=secret#home',
      missionGoal: 'Open the local app safely.',
      missionId: 'safe-capture-defaults',
      outputRoot: 'C:\\safe-output',
      personaId: 'first-time-researcher',
      viewport: 'desktop',
    });

    expect(mocks.bodyInnerText).not.toHaveBeenCalled();
    expect(mocks.mkdir).not.toHaveBeenCalled();
    expect(mocks.screenshot).not.toHaveBeenCalled();
    expect(capture.screenshotPath).toBeUndefined();
    expect(capture.visibleTextExcerpt).toBe('');

    const evidence = mocks.writeMissionEvidence.mock.calls[0]?.[1] as any;
    expect(evidence.steps[0].url).toBe('http://127.0.0.1:5174/respond');
    expect(evidence.screenshots).toEqual([]);
    expect(evidence.observations.url).toBe('http://127.0.0.1:5174/respond');
    expect(evidence.observations.visibleTextExcerpt).toBe('');
    expect(evidence.observations.buttons).toEqual(['Email [redacted-email]']);
    expect(evidence.observations.links).toEqual([
      {
        text: 'Continue with code [redacted-code]',
        path: '/respond/[redacted-uuid]',
      },
    ]);
    expect(evidence.observations.safeCapturePolicy).toEqual(
      expect.objectContaining({
        screenshotCapture: 'disabled-by-default',
        visibleTextCapture: 'disabled-by-default',
      })
    );
  });

  it('waits for document readiness and a short stable period before route snapshots', async () => {
    await waitForPageReadyForSnapshot(mocks.page as any);

    expect(mocks.page.waitForFunction).toHaveBeenCalledWith(
      expect.any(Function),
      undefined,
      { timeout: 3000 }
    );
    expect(mocks.page.locator).toHaveBeenCalledWith('body');
    expect(mocks.page.locator).toHaveBeenCalledWith(
      'main, [role="main"], nav, form, button, a'
    );
    expect(mocks.bodyWaitFor).toHaveBeenCalledWith({
      state: 'visible',
      timeout: 2000,
    });
    expect(mocks.structuralWaitFor).toHaveBeenCalledWith({
      state: 'visible',
      timeout: 2000,
    });
    expect(mocks.page.waitForLoadState).toHaveBeenCalledWith('networkidle', {
      timeout: 2000,
    });
    expect(mocks.page.waitForTimeout).toHaveBeenCalledWith(125);
  });
});
