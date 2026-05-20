import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { afterEach, describe, expect, it } from 'vitest';

import type { MissionEvidence } from './evidence';
import { missions } from './missions';
import { personas } from './personas';
import {
  buildReviewPrompt,
  writeReviewPromptForMission,
} from './review-prompt';

const temporaryRoots: string[] = [];

async function createTemporaryRoot() {
  const temporaryRoot = await mkdtemp(join(tmpdir(), 'uxa01-review-prompt-'));
  temporaryRoots.push(temporaryRoot);
  return temporaryRoot;
}

afterEach(async () => {
  const rootsToRemove = temporaryRoots.splice(0);
  await Promise.all(
    rootsToRemove.map((root) => rm(root, { recursive: true, force: true }))
  );
});

describe('UX persona review prompt generation', () => {
  it('uses mission and persona context while excluding unsafe evidence fields', () => {
    const mission = requireMission('create-first-study');
    const persona = personas['first-time-researcher'];
    const prompt = buildReviewPrompt({
      mission,
      persona,
      runMetadata: {
        runId: 'run-2026-05-20-review',
        createdAt: '2026-05-20T12:00:00.000Z',
      },
      evidence: unsafeEvidence(),
    });

    expect(prompt).toContain('First-time researcher');
    expect(prompt).toContain(mission.goal);
    expect(prompt).toContain('minimal app knowledge');
    expect(prompt).toContain('Use only the sanitized evidence');
    expect(prompt).toContain('blocker');
    expect(prompt).toContain('confusion');
    expect(prompt).toContain('polish');
    expect(prompt).toContain('acceptable-beta-limit');
    expect(prompt).toContain('severity');
    expect(prompt).toContain('affected step/surface');
    expect(prompt).toContain('user expectation');
    expect(prompt).toContain('observed confusion');
    expect(prompt).toContain('suggested fix');
    expect(prompt).toContain('ticket-ready wording');
    expect(prompt).toContain('screenshots/studies-empty-state.png');
    expect(prompt).toContain('http://127.0.0.1:5174/respond');
    expect(prompt).toContain('[redacted-email]');
    expect(prompt).toContain('[redacted-uuid]');
    expect(prompt).toContain('[redacted-code]');
    expect(prompt).toContain('[redacted-token]');

    expect(prompt).not.toContain('researcher@example.test');
    expect(prompt).not.toContain('11111111-1111-4111-8111-111111111111');
    expect(prompt).not.toContain('ABCD-1234');
    expect(prompt).not.toContain('invitationToken');
    expect(prompt).not.toContain('?invitationToken=secret');
    expect(prompt).not.toContain('#answers');
    expect(prompt).not.toContain('raw body text that must never be copied');
    expect(prompt).not.toContain('data:image/png;base64');
  });

  it('writes review-prompt.md next to the mission evidence pack', async () => {
    const outputRoot = await createTemporaryRoot();
    const runDirectory = join(outputRoot, 'run-with-prompt');

    const result = await writeReviewPromptForMission({
      runDirectory,
      mission: requireMission('create-first-study'),
      persona: personas['first-time-researcher'],
      evidence: unsafeEvidence(),
    });

    expect(result.promptPath).toBe(
      join(
        runDirectory,
        'missions',
        'create-first-study',
        'review-prompt.md'
      )
    );

    const prompt = await readFile(result.promptPath, 'utf8');

    expect(prompt).toContain('First-time researcher');
    expect(prompt).toContain('Use only the sanitized evidence');
    expect(prompt).not.toContain('researcher@example.test');
    expect(prompt).not.toContain('raw body text that must never be copied');
  });
});

function requireMission(missionId: string) {
  const mission = missions.find((entry) => entry.id === missionId);
  if (!mission) {
    throw new Error(`Expected mission fixture ${missionId}`);
  }

  return mission;
}

function unsafeEvidence(): MissionEvidence {
  return {
    missionId: 'create-first-study',
    personaId: 'first-time-researcher',
    missionGoal:
      'Find how a first-time researcher would create or open a first study.',
    status: 'blocked',
    startedAt: '2026-05-20T12:00:00.000Z',
    completedAt: '2026-05-20T12:01:00.000Z',
    steps: [
      {
        index: 1,
        action:
          'Opened respondent link for researcher@example.test using code ABCD-1234.',
        url: 'http://127.0.0.1:5174/respond?invitationToken=secret#answers',
      },
    ],
    screenshots: [
      {
        label: 'studies-empty-state',
        path: 'screenshots/studies-empty-state.png',
      },
    ],
    observations: {
      rawBodyText:
        'raw body text that must never be copied researcher@example.test',
      rawScreenshot: 'data:image/png;base64,unsafe-screenshot',
      token:
        'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.signature',
      visibleTextExcerpt:
        'Contact researcher@example.test, tenant 11111111-1111-4111-8111-111111111111, participant ABCD-1234, token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.signature.',
      pages: [
        {
          label: 'respondent-entry',
          title: 'Respondent entry',
          url: 'http://127.0.0.1:5174/respond?invitationToken=secret#answers',
          visibleTextExcerpt:
            'Continue as participant ABCD-1234 for researcher@example.test.',
          buttons: ['Continue as ABCD-1234'],
          links: [
            {
              text: 'Resume survey for researcher@example.test',
              path: '/respond?invitationToken=secret#answers',
            },
          ],
          screenshotData: 'data:image/png;base64,unsafe-page-screenshot',
        },
      ],
    },
  };
}
