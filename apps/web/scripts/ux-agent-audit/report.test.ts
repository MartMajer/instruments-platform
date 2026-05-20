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
