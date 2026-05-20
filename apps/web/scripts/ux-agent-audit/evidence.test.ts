import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { afterEach, describe, expect, it } from 'vitest';

import { createRunDirectory, writeMissionEvidence } from './evidence';

const temporaryRoots: string[] = [];

const unsafeSegments = [
  ['traversal segment', '..'],
  ['forward path separator', 'bad/name'],
  ['backslash path separator', 'bad\\name'],
  ['trailing dot', 'bad.'],
  ['trailing space', 'bad '],
  ['Windows reserved device name', 'CON'],
  ['Windows reserved device name with extension', 'con.txt'],
  ['Windows reserved COM device', 'COM1'],
  ['Windows reserved LPT device with extension', 'lpt9.json'],
] as const;

async function createTemporaryRoot() {
  const temporaryRoot = await mkdtemp(join(tmpdir(), 'uxa01-evidence-'));
  temporaryRoots.push(temporaryRoot);
  return temporaryRoot;
}

afterEach(async () => {
  const rootsToRemove = temporaryRoots.splice(0);
  await Promise.all(rootsToRemove.map((root) => rm(root, { recursive: true, force: true })));
});

describe('UX audit evidence writer', () => {
  it('creates a run directory and writes run metadata under the provided output root', async () => {
    const outputRoot = await createTemporaryRoot();

    const run = await createRunDirectory({
      outputRoot,
      runId: 'run-2026-05-20-test',
      createdAt: '2026-05-20T12:00:00.000Z',
    });

    expect(run).toEqual({
      runId: 'run-2026-05-20-test',
      runDirectory: join(outputRoot, 'run-2026-05-20-test'),
    });

    const runMetadata = JSON.parse(await readFile(join(run.runDirectory, 'run.json'), 'utf8'));
    expect(runMetadata).toEqual({
      schemaVersion: 1,
      artifactType: 'ux-agent-audit-run',
      runId: 'run-2026-05-20-test',
      createdAt: '2026-05-20T12:00:00.000Z',
    });
  });

  it.each(unsafeSegments)('rejects unsafe runId: %s', async (_label, unsafeRunId) => {
    const outputRoot = await createTemporaryRoot();

    await expect(
      createRunDirectory({
        outputRoot,
        runId: unsafeRunId,
        createdAt: '2026-05-20T12:00:00.000Z',
      })
    ).rejects.toThrow(/safe path segment/);
  });

  it('writes mission evidence JSON and transcript markdown under the mission directory', async () => {
    const outputRoot = await createTemporaryRoot();
    const { runDirectory } = await createRunDirectory({
      outputRoot,
      runId: 'run-with-mission',
      createdAt: '2026-05-20T12:05:00.000Z',
    });

    const paths = await writeMissionEvidence(runDirectory, {
      missionId: 'create-first-study',
      personaId: 'first-time-researcher',
      missionGoal: 'Create a first study from an empty workspace.',
      status: 'completed',
      startedAt: '2026-05-20T12:06:00.000Z',
      completedAt: '2026-05-20T12:07:00.000Z',
      steps: [
        {
          index: 1,
          action: 'Opened the app home route.',
          url: 'http://127.0.0.1:5174/app',
        },
        {
          index: 2,
          action: 'Found the create study action.',
          url: 'http://127.0.0.1:5174/app/studies',
        },
      ],
      screenshots: [
        {
          label: 'studies-empty-state',
          path: 'screenshots/studies-empty-state.png',
        },
      ],
      transcriptMarkdown: [
        '# create-first-study',
        '',
        '- Opened the app home route.',
        '- Found the create study action.',
        '',
      ].join('\n'),
    });

    const missionDirectory = join(runDirectory, 'missions', 'create-first-study');
    expect(paths).toEqual({
      missionDirectory,
      evidencePath: join(missionDirectory, 'evidence.json'),
      transcriptPath: join(missionDirectory, 'transcript.md'),
    });

    const evidence = JSON.parse(await readFile(paths.evidencePath, 'utf8'));
    expect(evidence).toEqual({
      schemaVersion: 1,
      artifactType: 'ux-agent-audit-mission-evidence',
      missionId: 'create-first-study',
      personaId: 'first-time-researcher',
      missionGoal: 'Create a first study from an empty workspace.',
      status: 'completed',
      startedAt: '2026-05-20T12:06:00.000Z',
      completedAt: '2026-05-20T12:07:00.000Z',
      steps: [
        {
          index: 1,
          action: 'Opened the app home route.',
          url: 'http://127.0.0.1:5174/app',
        },
        {
          index: 2,
          action: 'Found the create study action.',
          url: 'http://127.0.0.1:5174/app/studies',
        },
      ],
      screenshots: [
        {
          label: 'studies-empty-state',
          path: 'screenshots/studies-empty-state.png',
        },
      ],
    });

    await expect(readFile(paths.transcriptPath, 'utf8')).resolves.toBe(
      '# create-first-study\n\n- Opened the app home route.\n- Found the create study action.\n'
    );
  });

  it.each(unsafeSegments)('rejects unsafe missionId: %s', async (_label, unsafeMissionId) => {
    const outputRoot = await createTemporaryRoot();
    const { runDirectory } = await createRunDirectory({
      outputRoot,
      runId: 'safe-run-id',
      createdAt: '2026-05-20T12:05:00.000Z',
    });

    await expect(
      writeMissionEvidence(runDirectory, {
        missionId: unsafeMissionId,
        personaId: 'first-time-researcher',
        missionGoal: 'Create a first study from an empty workspace.',
        status: 'blocked',
        startedAt: '2026-05-20T12:06:00.000Z',
        steps: [],
        transcriptMarkdown: '# unsafe mission\n',
      })
    ).rejects.toThrow(/safe path segment/);
  });
});
