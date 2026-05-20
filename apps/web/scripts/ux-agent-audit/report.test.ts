import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { afterEach, describe, expect, it } from 'vitest';

import type { MissionEvidence } from './evidence';
import { missions } from './missions';
import { personas } from './personas';
import { writeNormalizedReviewReport } from './report';

const temporaryRoots: string[] = [];

async function createTemporaryRoot() {
  const temporaryRoot = await mkdtemp(join(tmpdir(), 'uxa01-report-'));
  temporaryRoots.push(temporaryRoot);
  return temporaryRoot;
}

afterEach(async () => {
  const rootsToRemove = temporaryRoots.splice(0);
  await Promise.all(
    rootsToRemove.map((root) => rm(root, { recursive: true, force: true }))
  );
});

describe('UX persona review report normalizer', () => {
  it('writes pending markdown and JSON when reviewer output is empty', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      runMetadata: {
        runId: 'run-pending',
        createdAt: '2026-05-20T12:00:00.000Z',
      },
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: '',
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));
    const markdown = await readFile(result.markdownPath, 'utf8');

    expect(summary).toEqual(
      expect.objectContaining({
        artifactType: 'ux-agent-audit-review-summary',
        reviewStatus: 'pending',
        findings: [],
        openQuestions: [],
        nextActionTickets: [],
      })
    );
    expect(markdown).toContain('Review pending');
    expect(markdown).toContain('Mission: create-first-study');
    expect(markdown).toContain('Persona: First-time researcher');
  });

  it('normalizes a JSON reviewer response with one finding and ticket wording', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      runMetadata: {
        runId: 'run-reviewed',
        createdAt: '2026-05-20T12:00:00.000Z',
      },
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: JSON.stringify({
        summary:
          'The mission is mostly understandable, but collection readiness wording is ambiguous.',
        findings: [
          {
            severity: 'confusion',
            affectedStep: 'Step 3: collection readiness',
            surface: 'Collection setup',
            userExpectation:
              'I expect to know who receives invitations before I launch.',
            observedConfusion:
              'The page says recipients are prepared, but does not state whether the current audience is selected.',
            suggestedFix:
              'Show the selected recipient scope immediately above the launch readiness action.',
            ticket:
              'Update Collection setup readiness copy to state the selected recipient scope before launch.',
          },
        ],
        openQuestions: [
          'Should launch be blocked until recipient preview has been opened?',
        ],
      }),
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));
    const markdown = await readFile(result.markdownPath, 'utf8');

    expect(summary.reviewStatus).toBe('reviewed');
    expect(summary.findings).toEqual([
      expect.objectContaining({
        severity: 'confusion',
        affectedStep: 'Step 3: collection readiness',
        surface: 'Collection setup',
        ticketReadyWording:
          'Update Collection setup readiness copy to state the selected recipient scope before launch.',
      }),
    ]);
    expect(summary.openQuestions).toEqual([
      'Should launch be blocked until recipient preview has been opened?',
    ]);
    expect(summary.nextActionTickets).toEqual([
      'Update Collection setup readiness copy to state the selected recipient scope before launch.',
    ]);

    expect(markdown).toContain('## Next-action tickets');
    expect(markdown).toContain(
      'Update Collection setup readiness copy to state the selected recipient scope before launch.'
    );
    expect(markdown).toContain('## Findings');
    expect(markdown).toContain('confusion');
  });

  it('keeps prose-only reviewer output in a structured-review-needed state', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput:
        'The page is confusing and should move the launch action closer to the recipient preview.',
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));
    const markdown = await readFile(result.markdownPath, 'utf8');

    expect(summary.reviewStatus).toBe('needs-structured-review');
    expect(summary.findings).toEqual([]);
    expect(markdown).toContain('needs structured JSON reviewer output');
  });

  it('parses the later valid fenced JSON block when an earlier fenced block is invalid', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: [
        'First attempt:',
        '```json',
        '{ invalid json',
        '```',
        'Corrected output:',
        '```json',
        JSON.stringify({
          findings: [
            {
              severity: 'high',
              affectedStep: 'Step 2',
              surface: 'Studies',
              userExpectation: 'Find the next study action.',
              observedConfusion: 'The primary action is buried.',
              suggestedFix: 'Move the action above reference text.',
            },
          ],
        }),
        '```',
      ].join('\n'),
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));

    expect(summary.reviewStatus).toBe('reviewed');
    expect(summary.findings).toEqual([
      expect.objectContaining({
        severity: 'high',
        surface: 'Studies',
      }),
    ]);
  });

  it('normalizes a raw JSON array reviewer response', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: JSON.stringify([
        {
          severity: 'critical',
          affectedStep: 'Step 1',
          surface: 'Sign in',
          userExpectation: 'Start the workflow without guessing.',
          observedConfusion: 'The entry path is ambiguous.',
          suggestedFix: 'Make the primary entry action explicit.',
        },
      ]),
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));

    expect(summary.reviewStatus).toBe('reviewed');
    expect(summary.findings).toEqual([
      expect.objectContaining({
        severity: 'critical',
        affectedStep: 'Step 1',
      }),
    ]);
  });

  it('preserves common medium and low reviewer severity labels honestly', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: JSON.stringify({
        findings: [
          {
            severity: 'medium',
            affectedStep: 'Step 2',
            surface: 'Studies',
            userExpectation: 'Understand where to continue.',
            observedConfusion: 'The heading is unclear.',
            suggestedFix: 'Clarify the heading.',
          },
          {
            severity: 'low',
            affectedStep: 'Step 3',
            surface: 'Collection',
            userExpectation: 'Read supporting copy.',
            observedConfusion: 'The helper text is wordy.',
            suggestedFix: 'Shorten the helper text.',
          },
        ],
      }),
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));

    expect(summary.findings.map((finding: { severity: string }) => finding.severity)).toEqual([
      'medium',
      'low',
    ]);
  });

  it('neutralizes unsafe markdown, HTML, data URIs, and token-like text in reviewer report markdown', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: JSON.stringify({
        summary:
          '<script>alert(1)</script> See ![leak](data:image/png;base64,AAAA) and [unsafe](javascript:alert(1)) token=super-secret-token-123456.',
        findings: [
          {
            severity: 'medium',
            affectedStep: 'Step 2',
            surface: '<img src=x onerror=alert(1)>',
            userExpectation:
              'Open [unsafe](data:text/html;base64,PHNjcmlwdA==) route.',
            observedConfusion:
              'Reviewer pasted apiToken=abcdef1234567890abcdef1234567890.',
            suggestedFix: 'Use plain text instead of <b>HTML</b>.',
            ticket:
              'Fix ![x](javascript:alert(1)) and remove secret=abcdef1234567890abcdef1234567890.',
          },
        ],
        openQuestions: ['Is <iframe srcdoc=x> allowed?'],
      }),
    });

    const markdown = await readFile(result.markdownPath, 'utf8');

    expect(markdown).not.toContain('<script>');
    expect(markdown).not.toContain('<img');
    expect(markdown).not.toContain('<iframe');
    expect(markdown).not.toContain('![leak]');
    expect(markdown).not.toContain('(data:');
    expect(markdown).not.toContain('(javascript:');
    expect(markdown).not.toContain('super-secret-token-123456');
    expect(markdown).not.toContain('abcdef1234567890abcdef1234567890');
    expect(markdown).toContain('[redacted-token]');
  });
});

function requireMission(missionId: string) {
  const mission = missions.find((entry) => entry.id === missionId);
  if (!mission) {
    throw new Error(`Expected mission fixture ${missionId}`);
  }

  return mission;
}

function completedEvidence(): MissionEvidence {
  return {
    missionId: 'create-first-study',
    personaId: 'first-time-researcher',
    missionGoal:
      'Find how a first-time researcher would create or open a first study.',
    status: 'completed',
    startedAt: '2026-05-20T12:00:00.000Z',
    completedAt: '2026-05-20T12:02:00.000Z',
    steps: [
      {
        index: 1,
        action: 'Opened sign in.',
        url: 'http://127.0.0.1:5174/signin',
      },
      {
        index: 2,
        action: 'Opened studies.',
        url: 'http://127.0.0.1:5174/app/campaign-series',
      },
      {
        index: 3,
        action: 'Inspected collection readiness.',
        url: 'http://127.0.0.1:5174/app/campaign-series/study-local-1/operations',
      },
    ],
    screenshots: [
      {
        label: 'study-collection',
        path: 'screenshots/study-collection.png',
      },
    ],
    observations: {
      visibleControls: ['Create study', 'Preview recipients', 'Launch wave'],
      visitedWorkflowSurfaces: ['setup', 'collection', 'results'],
    },
  };
}
