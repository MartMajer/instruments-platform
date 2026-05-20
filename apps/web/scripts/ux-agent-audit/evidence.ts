import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

const schemaVersion = 1;

export interface CreateRunDirectoryOptions {
  outputRoot: string;
  runId?: string;
  createdAt?: string | Date;
}

export interface RunDirectory {
  runId: string;
  runDirectory: string;
}

export type MissionEvidenceStatus = 'completed' | 'blocked' | 'error';

export interface MissionEvidenceStep {
  index: number;
  action: string;
  url?: string;
  notes?: string;
}

export interface MissionEvidenceScreenshot {
  label: string;
  path: string;
}

export interface MissionEvidence {
  missionId: string;
  personaId: string;
  missionGoal: string;
  status: MissionEvidenceStatus;
  startedAt: string | Date;
  completedAt?: string | Date;
  steps: MissionEvidenceStep[];
  screenshots?: MissionEvidenceScreenshot[];
  observations?: Record<string, unknown>;
  transcriptMarkdown?: string;
}

export interface MissionEvidencePaths {
  missionDirectory: string;
  evidencePath: string;
  transcriptPath: string;
}

export async function createRunDirectory(options: CreateRunDirectoryOptions): Promise<RunDirectory> {
  const createdAt = toIsoString(options.createdAt ?? new Date());
  const runId = options.runId ?? createDefaultRunId(createdAt);
  assertSafePathSegment(runId, 'runId');

  const runDirectory = join(options.outputRoot, runId);
  await mkdir(runDirectory, { recursive: true });

  await writeJson(join(runDirectory, 'run.json'), {
    schemaVersion,
    artifactType: 'ux-agent-audit-run',
    runId,
    createdAt,
  });

  return { runId, runDirectory };
}

export async function writeMissionEvidence(
  runDirectory: string,
  evidence: MissionEvidence
): Promise<MissionEvidencePaths> {
  assertSafePathSegment(evidence.missionId, 'missionId');

  const missionDirectory = join(runDirectory, 'missions', evidence.missionId);
  const evidencePath = join(missionDirectory, 'evidence.json');
  const transcriptPath = join(missionDirectory, 'transcript.md');

  await mkdir(missionDirectory, { recursive: true });

  await writeJson(evidencePath, buildMissionEvidenceJson(evidence));
  await writeFile(
    transcriptPath,
    normalizeMarkdown(evidence.transcriptMarkdown ?? buildTranscript(evidence)),
    'utf8'
  );

  return { missionDirectory, evidencePath, transcriptPath };
}

function buildMissionEvidenceJson(evidence: MissionEvidence) {
  return {
    schemaVersion,
    artifactType: 'ux-agent-audit-mission-evidence',
    missionId: evidence.missionId,
    personaId: evidence.personaId,
    missionGoal: evidence.missionGoal,
    status: evidence.status,
    startedAt: toIsoString(evidence.startedAt),
    ...(evidence.completedAt ? { completedAt: toIsoString(evidence.completedAt) } : {}),
    steps: evidence.steps,
    ...(evidence.screenshots ? { screenshots: evidence.screenshots } : {}),
    ...(evidence.observations ? { observations: evidence.observations } : {}),
  };
}

function buildTranscript(evidence: MissionEvidence) {
  const lines = [
    `# ${evidence.missionId}`,
    '',
    `Status: ${evidence.status}`,
    `Persona: ${evidence.personaId}`,
    '',
    '## Steps',
  ];

  for (const step of evidence.steps) {
    lines.push(`${step.index}. ${step.action}${step.url ? ` (${step.url})` : ''}`);
  }

  return lines.join('\n');
}

function normalizeMarkdown(markdown: string) {
  return markdown.endsWith('\n') ? markdown : `${markdown}\n`;
}

function writeJson(filePath: string, value: unknown) {
  return writeFile(filePath, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
}

function toIsoString(value: string | Date) {
  return typeof value === 'string' ? value : value.toISOString();
}

function createDefaultRunId(createdAt: string) {
  return `run-${createdAt.replace(/[:.]/g, '-').replace(/[^A-Za-z0-9_-]/g, '-')}`;
}

function assertSafePathSegment(value: string, fieldName: string) {
  if (!/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(value) || value.includes('..')) {
    throw new Error(`${fieldName} must be a safe path segment`);
  }
}
