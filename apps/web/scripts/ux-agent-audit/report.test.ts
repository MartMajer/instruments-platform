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

  it('keeps invalid JSON reviewer shapes in a structured-review-needed state', async () => {
    const invalidOutputs = [
      JSON.stringify('No UX findings.'),
      '123',
      JSON.stringify({ result: 'ok', items: [] }),
      JSON.stringify([]),
      JSON.stringify([123, 'bad', { note: 'missing required finding fields' }]),
    ];

    for (const [index, reviewerOutput] of invalidOutputs.entries()) {
      const runDirectory = join(await createTemporaryRoot(), `invalid-${index}`);
      const result = await writeNormalizedReviewReport({
        runDirectory,
        mission: requireMission('create-first-study'),
        persona: personas['first-time-researcher'],
        evidence: completedEvidence(),
        reviewerOutput,
      });

      const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));

      expect(summary.reviewStatus).toBe('needs-structured-review');
      expect(summary.findings).toEqual([]);
      expect(summary.observationsSummary.reviewerSummary).toContain(
        'required review schema'
      );
    }
  });

  it('accepts explicit empty findings only when the reviewer explains the intentional empty review', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: JSON.stringify({
        summary:
          'No UX findings: the sanitized evidence shows the mission completed and the next action was clear.',
        findings: [],
        openQuestions: [],
      }),
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));
    const markdown = await readFile(result.markdownPath, 'utf8');

    expect(summary.reviewStatus).toBe('reviewed');
    expect(summary.findings).toEqual([]);
    expect(summary.observationsSummary.reviewerSummary).toContain(
      'No UX findings'
    );
    expect(markdown).toContain(
      'The reviewer explicitly reported zero findings'
    );
    expect(markdown).toContain(
      'None. Reviewer explicitly reported zero findings.'
    );
  });

  it('renders persona goal context from autonomous evidence', async () => {
    const runDirectory = await createTemporaryRoot();
    const evidence = completedEvidence();
    evidence.observations = {
      ...evidence.observations,
      personaGoal: {
        name: 'Dr. Ana Kovac',
        role: 'Academic researcher preparing a first self-serve study in the product.',
        appGoal:
          'Starting from /app, create and prepare a first study for launch.',
        successCriteria: [
          'The next action is obvious.',
          'Questionnaire requirements are explained.',
        ],
      },
      personaGoalAssessment: {
        status: 'completed',
        checkedCriteriaCount: 2,
        visitedTargetCount: 3,
        targetCount: 3,
        successCriteria: [
          {
            criterion: 'The next action is obvious.',
            status: 'observed',
            evidence: 'Observed in Setup progress: Continue setup is visible.',
          },
          {
            criterion: 'Questionnaire requirements are explained.',
            status: 'unclear',
            evidence: 'No direct transcript evidence found for this criterion.',
          },
        ],
      },
    };
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence,
      reviewerOutput: JSON.stringify({
        summary:
          'No UX findings: the persona goal was reviewed against the captured transcript.',
        findings: [],
        openQuestions: [],
      }),
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));
    const markdown = await readFile(result.markdownPath, 'utf8');

    expect(summary.observationsSummary.personaGoal).toEqual(
      expect.objectContaining({
        name: 'Dr. Ana Kovac',
        appGoal: expect.stringContaining('/app'),
      })
    );
    expect(markdown).toContain('## Persona goal');
    expect(markdown).toContain('Dr. Ana Kovac');
    expect(markdown).toContain('Starting from /app');
    expect(markdown).toContain('Criteria checked: 2');
    expect(markdown).toContain('Criterion evidence');
    expect(markdown).toContain('observed');
    expect(markdown).toContain('No direct transcript evidence');
  });

  it('rejects empty findings when the reviewer does not explain why the empty review is intentional', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: JSON.stringify({ findings: [] }),
    });

    const summary = JSON.parse(await readFile(result.jsonPath, 'utf8'));

    expect(summary.reviewStatus).toBe('needs-structured-review');
    expect(summary.findings).toEqual([]);
    expect(summary.observationsSummary.reviewerSummary).toContain(
      'empty findings array'
    );
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
          '<script>alert(1)</script> See data:text/plain;base64,AAAA, ![leak](data:image/png;base64,AAAA), and [unsafe](javascript:alert(1)) token=super-secret-token-123456.',
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

  it('redacts bare local paths and raw URL origins from normalized report text', async () => {
    const runDirectory = await createTemporaryRoot();
    const result = await writeNormalizedReviewReport({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: completedEvidence(),
      reviewerOutput: JSON.stringify({
        summary:
          'Checked C:\\Users\\Martin\\secret\\notes.txt, /Users/martin/private/audit.md, ~/private/cache.json, ../private/file.txt, HTTPS://tenant-alpha.example.test/app/campaign-series/study-local-1?token=secret#frag, and https://tenant-alpha.example.test/app/campaign-series/study-local-1?token=secret#frag.',
        findings: [
          {
            severity: 'medium',
            affectedStep: 'Step 2',
            surface: 'C:\\Users\\Martin\\surface.html',
            userExpectation:
              'Open /Users/martin/private/expectation.txt without leaking local paths.',
            observedConfusion:
              'Saw ~/private/cache.json and ../private/file.txt in the report text.',
            suggestedFix:
              'Document https://docs.vendor.example/docs/review?tenant=secret#frag without leaking the host.',
          },
        ],
      }),
    });

    const summaryText = await readFile(result.jsonPath, 'utf8');
    const markdown = await readFile(result.markdownPath, 'utf8');
    const combined = `${summaryText}\n${markdown}`;

    expect(combined).toContain('[redacted-path]');
    expect(combined).toContain('[app-url]/app/campaign-series/study-local-1');
    expect(combined).toContain('[external-url]/docs/review');
    expect(combined).not.toContain('C:\\\\Users');
    expect(combined).not.toContain('C:\\Users');
    expect(combined).not.toContain('/Users/martin/private');
    expect(combined).not.toContain('~/private');
    expect(combined).not.toContain('../private');
    expect(combined).not.toContain('tenant-alpha.example.test');
    expect(combined).not.toContain('docs.vendor.example');
    expect(combined).not.toContain('?token=secret');
    expect(combined).not.toContain('?tenant=secret');
    expect(combined).not.toContain('#frag');
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
