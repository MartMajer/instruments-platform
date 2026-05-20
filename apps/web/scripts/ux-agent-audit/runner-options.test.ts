import { beforeEach, describe, expect, it, vi } from 'vitest';

import { captureBrowserEvidence } from './browser.ts';
import { missions } from './missions';
import { writeReviewPromptForMission } from './review-prompt.ts';
import { parseRunnerOptions, runAudit } from './run';

vi.mock('./browser.ts', () => ({
  captureBrowserEvidence: vi.fn(async () => ({
    title: 'Local app',
    url: 'http://127.0.0.1:5174/app',
    visibleTextExcerpt: 'Local app shell',
    buttons: [],
    links: [],
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
      outputRoot: '../../artifacts/ux-agent-runs/test',
    });
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

  it('does not request screenshots or visible text for product-page missions by default', async () => {
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
        includeSanitizedVisibleText: false,
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
});
