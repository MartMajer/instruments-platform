import { beforeEach, describe, expect, it, vi } from 'vitest';

import { captureAutonomousBrowserEvidence, captureBrowserEvidence } from './browser.ts';
import { missions } from './missions';
import { writeNormalizedReviewReport } from './report.ts';
import { writeReviewPromptForMission } from './review-prompt.ts';
import {
  parseAutonomousRunnerOptions,
  parseRunnerOptions,
  runAudit,
  runAutonomousAudit,
} from './run';

vi.mock('./browser.ts', () => ({
  captureBrowserEvidence: vi.fn(async () => ({
    title: 'Local app',
    url: 'http://127.0.0.1:5174/app',
    visibleTextExcerpt: 'Local app shell',
    buttons: [],
    links: [],
  })),
  captureAutonomousBrowserEvidence: vi.fn(async () => ({
    title: 'Autonomous fixture',
    url: 'http://127.0.0.1:5174/app/demo',
    visibleTextExcerpt: 'Demo fixtures',
    buttons: [],
    links: [],
    status: 'blocked',
    runDirectory: 'C:\\safe-run',
    evidencePath:
      'C:\\safe-run\\missions\\fixture-first-study-setup\\evidence.json',
    transcriptPath:
      'C:\\safe-run\\missions\\fixture-first-study-setup\\transcript.md',
    reviewerOutput:
      '{"summary":"Autonomous review","findings":[{"severity":"confusion","surface":"Setup","observedConfusion":"Blocked setup","suggestedFix":"Clarify setup","ticketReadyWording":"Clarify setup blockers."}],"openQuestions":[]}',
  })),
}));

vi.mock('./report.ts', () => ({
  writeNormalizedReviewReport: vi.fn(async () => ({
    markdownPath: 'C:\\safe-run\\review-report.md',
    jsonPath: 'C:\\safe-run\\review-summary.json',
    summary: {
      reviewStatus: 'reviewed',
      findings: [{ id: 'F1' }],
      nextActionTickets: ['Clarify setup blockers.'],
    },
  })),
}));

vi.mock('./review-prompt.ts', () => ({
  writeReviewPromptForMission: vi.fn(async () => ({
    promptPath:
      'C:\\safe-run\\missions\\prepare-audience\\review-prompt.md',
  })),
}));

describe('UX audit runner option parsing', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('parses local audit runner CLI options', () => {
    const options = parseRunnerOptions([
      '--base-url',
      'http://127.0.0.1:5174',
      '--mission',
      'create-first-study',
      '--persona',
      'first-time-researcher',
      '--viewport',
      'desktop',
      '--headless',
      'false',
      '--output',
      '../../artifacts/ux-agent-runs/test',
    ]);

    expect(options).toEqual({
      baseUrl: 'http://127.0.0.1:5174',
      missionFilter: 'create-first-study',
      personaOverride: 'first-time-researcher',
      viewportOverride: 'desktop',
      headless: false,
      captureMode: 'local-full',
      outputRoot: '../../artifacts/ux-agent-runs/test',
    });
  });

  it('refuses non-local base URLs because the harness is local-only', () => {
    expect(() =>
      parseRunnerOptions([
        '--base-url',
        'https://validatedscale-staging.croat.dev',
        '--mission',
        'create-first-study',
      ])
    ).toThrow('UX audit harness is local-only');
  });

  it('allows explicit safe capture mode but defaults to local-full', () => {
    expect(
      parseRunnerOptions([
        '--base-url',
        'http://localhost:5174',
        '--capture-mode',
        'safe',
      ]).captureMode
    ).toBe('safe');

    expect(
      parseRunnerOptions([
        '--base-url',
        'http://localhost:5174',
      ]).captureMode
    ).toBe('local-full');
  });

  it('defaults persona and viewport from the mission contract', () => {
    const options = parseRunnerOptions([
      '--base-url',
      'http://127.0.0.1:5174',
      '--mission',
      'prepare-audience',
    ]);

    expect(options.personaOverride).toBe('osh-consultant');
    expect(options.viewportOverride).toBe('tablet');
  });

  it('rejects an unknown mission id', () => {
    expect(() =>
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--mission',
        'missing-mission',
      ])
    ).toThrow('Unknown mission: missing-mission');
  });

  it('rejects an unknown persona id', () => {
    expect(() =>
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--persona',
        'missing-persona',
      ])
    ).toThrow('Unknown persona: missing-persona');
  });

  it('rejects an invalid viewport override', () => {
    expect(() =>
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--viewport',
        'wide',
      ])
    ).toThrow('Unsupported viewport: wide');
  });

  it('rejects unknown flags', () => {
    expect(() =>
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--not-a-runner-option',
        'value',
      ])
    ).toThrow('Unknown option: --not-a-runner-option');
  });

  it('parses headless boolean values and treats bare headless as true', () => {
    expect(
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--headless',
        'true',
      ]).headless
    ).toBe(true);

    expect(
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--headless',
        'false',
      ]).headless
    ).toBe(false);

    expect(
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--headless',
      ]).headless
    ).toBe(true);
  });

  it('rejects invalid headless values', () => {
    expect(() =>
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--headless',
        'sometimes',
      ])
    ).toThrow('Invalid --headless: sometimes');
  });

  it('passes resolved mission contract metadata and headless mode to browser capture', async () => {
    const mission = missions.find((entry) => entry.id === 'prepare-audience');
    if (!mission) {
      throw new Error('Expected prepare-audience mission fixture');
    }

    await runAudit(
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--mission',
        'prepare-audience',
        '--headless',
        'false',
      ])
    );

    expect(captureBrowserEvidence).toHaveBeenCalledWith(
      expect.objectContaining({
        missionId: 'prepare-audience',
        personaId: 'osh-consultant',
        missionGoal: mission.goal,
        viewport: 'tablet',
        headless: false,
      })
    );
  });

  it('requests local-full transcripts for product-page missions by default', async () => {
    await runAudit(
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--mission',
        'create-first-study',
      ])
    );

    expect(captureBrowserEvidence).toHaveBeenCalledWith(
      expect.objectContaining({
        missionId: 'create-first-study',
        captureScreenshots: false,
        includeSanitizedVisibleText: true,
        captureMode: 'local-full',
        executeFixedMission: true,
      })
    );
  });

  it('writes a persona review prompt when mission execution creates evidence', async () => {
    vi.mocked(captureBrowserEvidence).mockResolvedValueOnce({
      title: 'Local app',
      url: 'http://127.0.0.1:5174/app',
      visibleTextExcerpt: '',
      buttons: [],
      links: [],
      runDirectory: 'C:\\safe-run',
      evidencePath:
        'C:\\safe-run\\missions\\prepare-audience\\evidence.json',
    });

    await runAudit(
      parseRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--mission',
        'prepare-audience',
      ])
    );

    expect(writeReviewPromptForMission).toHaveBeenCalledWith(
      expect.objectContaining({
        runDirectory: 'C:\\safe-run',
        evidencePath:
          'C:\\safe-run\\missions\\prepare-audience\\evidence.json',
        mission: expect.objectContaining({ id: 'prepare-audience' }),
        persona: expect.objectContaining({ id: 'osh-consultant' }),
      })
    );
  });

  it('parses autonomous runner options for local fixture missions', () => {
    const options = parseAutonomousRunnerOptions([
      '--base-url',
      'http://127.0.0.1:5174',
      '--mission',
      'fixture-first-study-setup',
      '--headless',
      'false',
      '--output',
      '../../artifacts/ux-agent-runs/test',
    ]);

    expect(options).toEqual({
      baseUrl: 'http://127.0.0.1:5174',
      missionFilter: 'fixture-first-study-setup',
      headless: false,
      captureMode: 'local-full',
      dataMode: 'fixture',
      fullstackDevAuth: { enabled: false },
      outputRoot: '../../artifacts/ux-agent-runs/test',
    });
  });

  it('parses explicit fullstack data mode for autonomous runs', () => {
    const options = parseAutonomousRunnerOptions([
      '--base-url',
      'http://127.0.0.1:5174',
      '--mission',
      'fixture-first-study-setup',
      '--data-mode',
      'fullstack',
    ]);

    expect(options.dataMode).toBe('fullstack');
  });

  it('parses explicit local fullstack development auth options', () => {
    const options = parseAutonomousRunnerOptions([
      '--base-url',
      'http://127.0.0.1:5174',
      '--mission',
      'fullstack-create-study',
      '--data-mode',
      'fullstack',
      '--fullstack-dev-auth',
      '--fullstack-tenant-id',
      '33333333-3333-4333-8333-333333333333',
      '--fullstack-user-id',
      '44444444-4444-4444-8444-444444444444',
      '--fullstack-email',
      'ux-agent@example.test',
      '--fullstack-permissions',
      'setup.manage,team.manage,export.read',
    ]);

    expect(options.fullstackDevAuth).toEqual({
      enabled: true,
      tenantId: '33333333-3333-4333-8333-333333333333',
      userId: '44444444-4444-4444-8444-444444444444',
      email: 'ux-agent@example.test',
      permissions: ['setup.manage', 'team.manage', 'export.read'],
    });
  });

  it('rejects unknown autonomous data modes', () => {
    expect(() =>
      parseAutonomousRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--data-mode',
        'staging',
      ])
    ).toThrow('Unsupported data mode: staging');
  });

  it('fails closed before browser launch when a fixture-only mission is requested in fullstack mode', async () => {
    await expect(
      runAutonomousAudit(
        parseAutonomousRunnerOptions([
          '--base-url',
          'http://127.0.0.1:5174',
          '--mission',
          'fixture-first-study-setup',
          '--data-mode',
          'fullstack',
        ])
      )
    ).rejects.toThrow('does not support fullstack data mode');
    expect(captureAutonomousBrowserEvidence).not.toHaveBeenCalled();
  });

  it('runs a fullstack-capable mission without fixture data mode', async () => {
    await runAutonomousAudit(
      parseAutonomousRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--mission',
        'fullstack-workspace-inspection',
        '--data-mode',
        'fullstack',
      ])
    );

    expect(captureAutonomousBrowserEvidence).toHaveBeenCalledWith(
      expect.objectContaining({
        missionId: 'fullstack-workspace-inspection',
        autonomousDataMode: 'fullstack',
        fullstackDevAuth: { enabled: false },
      })
    );
  });

  it('passes local fullstack development auth options to browser capture', async () => {
    await runAutonomousAudit(
      parseAutonomousRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--mission',
        'fullstack-create-study',
        '--data-mode',
        'fullstack',
        '--fullstack-dev-auth',
      ])
    );

    expect(captureAutonomousBrowserEvidence).toHaveBeenCalledWith(
      expect.objectContaining({
        missionId: 'fullstack-create-study',
        autonomousDataMode: 'fullstack',
        fullstackDevAuth: { enabled: true },
      })
    );
  });

  it('runs autonomous fixture missions with screenshots, transcript, prompt, and normalized report', async () => {
    await runAutonomousAudit(
      parseAutonomousRunnerOptions([
        '--base-url',
        'http://127.0.0.1:5174',
        '--mission',
        'fixture-first-study-setup',
      ])
    );

    expect(captureAutonomousBrowserEvidence).toHaveBeenCalledWith(
      expect.objectContaining({
        missionId: 'fixture-first-study-setup',
        captureScreenshots: true,
        includeSanitizedVisibleText: true,
        captureMode: 'local-full',
        autonomousDataMode: 'fixture',
        fullstackDevAuth: { enabled: false },
      })
    );
    expect(writeReviewPromptForMission).toHaveBeenCalledWith(
      expect.objectContaining({
        runDirectory: 'C:\\safe-run',
        evidencePath:
          'C:\\safe-run\\missions\\fixture-first-study-setup\\evidence.json',
      })
    );
    expect(writeNormalizedReviewReport).toHaveBeenCalledWith(
      expect.objectContaining({
        runDirectory: 'C:\\safe-run',
        reviewerOutput: expect.stringContaining('"findings"'),
      })
    );
  });
});
