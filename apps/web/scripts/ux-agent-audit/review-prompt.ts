import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';

import type { JsonObject, JsonValue, MissionEvidence } from './evidence';
import type { MissionDefinition, PersonaDefinition } from './types';

export interface ReviewPromptRunMetadata {
  runId?: string;
  createdAt?: string;
}

export interface BuildReviewPromptOptions {
  mission: MissionDefinition<string>;
  persona: PersonaDefinition;
  evidence: MissionEvidence;
  runMetadata?: ReviewPromptRunMetadata;
}

export interface WriteReviewPromptForMissionOptions {
  runDirectory: string;
  mission: MissionDefinition<string>;
  persona: PersonaDefinition;
  evidence?: MissionEvidence;
  evidencePath?: string;
  runMetadata?: ReviewPromptRunMetadata;
}

export interface ReviewPromptPaths {
  promptPath: string;
}

const emailPattern = /\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/gi;
const uuidPattern =
  /\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b/gi;
const jwtLikePattern =
  /\b[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{3,}\.[A-Za-z0-9_-]{3,}\b/g;
const longTokenPattern =
  /\b(?=[A-Za-z0-9_-]{24,}\b)(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9_-]+\b/g;
const participantCodeLikePattern = /\b[A-Z0-9]{4,}(?:[-_][A-Z0-9]{3,})+\b/g;
const urlPattern = /https?:\/\/[^\s)"'<>]+/g;
const unsafeObservationKeyPattern =
  /(raw|body|html|screenshotdata|image|token|secret|email|query|fragment|cookie|authorization|password|salt|answer)/i;
const safeObservationKeys = new Set([
  'appAccessible',
  'avoidedActions',
  'avoidedUnsafePersistedArtifacts',
  'blockedReason',
  'buttons',
  'label',
  'links',
  'missionBoundary',
  'navigationPolicy',
  'pageUrlCapture',
  'pages',
  'path',
  'routeNavigationOnly',
  'safeCapturePolicy',
  'screenshotCapture',
  'selectedStudyPathFound',
  'signInBlocked',
  'startUrl',
  'text',
  'title',
  'url',
  'visibleControls',
  'visibleTextCapture',
  'visibleTextExcerpt',
  'visitedWorkflowSurfaces',
]);

export function buildReviewPrompt(options: BuildReviewPromptOptions) {
  const evidence = sanitizeEvidenceForReview(options.evidence);
  const guidance = options.persona.reviewGuidance.length
    ? options.persona.reviewGuidance
    : ['Review the evidence as this persona would experience the product.'];

  return normalizeMarkdown(
    [
      '# UX persona review prompt',
      '',
      `You are acting as the selected persona: ${sanitizeReviewText(
        options.persona.displayName
      )}.`,
      'Review with minimal app knowledge. Do not assume internal product intent, backend behavior, or undocumented roadmap context.',
      'Use only the sanitized evidence below. Do not use raw screenshots, raw body text, raw URLs with query strings/fragments, tokens, emails, UUIDs, participant codes, secrets, or outside product knowledge.',
      '',
      '## Mission context',
      '',
      `- Run: ${sanitizeReviewText(
        options.runMetadata?.runId ?? 'unknown-local-run'
      )}`,
      `- Created at: ${sanitizeReviewText(
        options.runMetadata?.createdAt ?? 'unknown'
      )}`,
      `- Mission: ${sanitizeReviewText(options.mission.id)}`,
      `- Goal: ${sanitizeReviewText(options.mission.goal)}`,
      `- Persona: ${sanitizeReviewText(options.persona.displayName)} (${sanitizeReviewText(
        options.persona.id
      )})`,
      `- Mission status: ${sanitizeReviewText(options.evidence.status)}`,
      '',
      '## Persona guidance',
      '',
      ...guidance.map((item) => `- ${sanitizeReviewText(item)}`),
      '',
      '## Severity rubric',
      '',
      '- blocker: The persona cannot complete the mission or is likely to abandon.',
      '- confusion: The persona can continue, but wording or layout creates wrong expectations.',
      '- polish: Cosmetic or wording improvement that does not block comprehension.',
      '- acceptable-beta-limit: A real limitation that is clear enough for the current private beta.',
      '',
      '## Required finding schema',
      '',
      'For each concrete UX finding, provide:',
      '',
      '- severity',
      '- affected step/surface',
      '- user expectation',
      '- observed confusion',
      '- suggested fix',
      '- ticket-ready wording',
      '',
      'Prefer a short list of evidence-backed findings over a long complaint list. If evidence is incomplete, say what review is blocked by the missing evidence instead of guessing.',
      '',
      '## Sanitized evidence',
      '',
      'Screenshot references are filenames only. The prompt does not embed raw screenshot images.',
      '',
      '```json',
      JSON.stringify(evidence, null, 2),
      '```',
    ].join('\n')
  );
}

export async function writeReviewPromptForMission(
  options: WriteReviewPromptForMissionOptions
): Promise<ReviewPromptPaths> {
  const evidence =
    options.evidence ??
    (await readJson<MissionEvidence>(
      options.evidencePath ??
        join(options.runDirectory, 'missions', options.mission.id, 'evidence.json')
    ));
  const runMetadata =
    options.runMetadata ??
    (await readOptionalJson<ReviewPromptRunMetadata>(
      join(options.runDirectory, 'run.json')
    ));
  const prompt = buildReviewPrompt({
    mission: options.mission,
    persona: options.persona,
    evidence,
    runMetadata,
  });
  const promptPath = join(
    options.runDirectory,
    'missions',
    options.mission.id,
    'review-prompt.md'
  );

  await mkdir(dirname(promptPath), { recursive: true });
  await writeFile(promptPath, prompt, 'utf8');

  return { promptPath };
}

export function sanitizeEvidenceForReview(evidence: MissionEvidence): JsonObject {
  return {
    missionId: sanitizeReviewText(evidence.missionId),
    personaId: sanitizeReviewText(evidence.personaId),
    missionGoal: sanitizeReviewText(evidence.missionGoal),
    status: sanitizeReviewText(evidence.status),
    startedAt: sanitizeReviewText(String(evidence.startedAt)),
    ...(evidence.completedAt
      ? { completedAt: sanitizeReviewText(String(evidence.completedAt)) }
      : {}),
    steps: evidence.steps.map((step) => ({
      index: step.index,
      action: sanitizeReviewText(step.action),
      ...(step.url ? { url: sanitizeEvidenceUrl(step.url) } : {}),
      ...(step.notes ? { notes: sanitizeReviewText(step.notes) } : {}),
    })),
    screenshotReferences: (evidence.screenshots ?? []).map((screenshot) => ({
      label: sanitizeReviewText(screenshot.label, 180),
      path: sanitizeEvidencePath(screenshot.path),
    })),
    observations: sanitizeObservationObject(evidence.observations ?? {}),
  };
}

export function sanitizeEvidenceUrl(url: string) {
  const value = (url ?? '').trim();

  if (!value) {
    return '';
  }

  try {
    const parsed = new URL(value);
    return `${parsed.origin}${sanitizeEvidencePath(parsed.pathname)}`;
  } catch {
    return sanitizeEvidencePath(stripQueryAndFragment(value));
  }
}

export function sanitizeReviewText(text: string, maxCharacters = 2000) {
  const normalized = (text ?? '').replace(/\s+/g, ' ').trim();
  return normalized
    .replace(urlPattern, (url) => sanitizeEvidenceUrl(url))
    .replace(emailPattern, '[redacted-email]')
    .replace(uuidPattern, '[redacted-uuid]')
    .replace(jwtLikePattern, '[redacted-token]')
    .replace(longTokenPattern, '[redacted-token]')
    .replace(participantCodeLikePattern, '[redacted-code]')
    .slice(0, normalizeTextLimit(maxCharacters));
}

function sanitizeObservationObject(observations: JsonObject): JsonObject {
  const sanitized: JsonObject = {};

  for (const [key, value] of Object.entries(observations)) {
    const sanitizedValue = sanitizeObservationValue(key, value);
    if (sanitizedValue !== undefined) {
      sanitized[sanitizeReviewText(key, 120)] = sanitizedValue;
    }
  }

  return sanitized;
}

function sanitizeObservationValue(
  key: string,
  value: JsonValue
): JsonValue | undefined {
  if (isUnsafeObservationKey(key)) {
    return undefined;
  }

  if (value === null || typeof value === 'number' || typeof value === 'boolean') {
    return value;
  }

  if (typeof value === 'string') {
    return isUrlLikeKey(key)
      ? sanitizeEvidenceUrl(value)
      : sanitizeReviewText(value);
  }

  if (Array.isArray(value)) {
    return value
      .map((entry) => sanitizeObservationArrayEntry(key, entry))
      .filter((entry): entry is JsonValue => entry !== undefined);
  }

  return sanitizeObservationObject(value);
}

function sanitizeObservationArrayEntry(
  parentKey: string,
  value: JsonValue
): JsonValue | undefined {
  if (value === null || typeof value === 'number' || typeof value === 'boolean') {
    return value;
  }

  if (typeof value === 'string') {
    return isUrlLikeKey(parentKey)
      ? sanitizeEvidenceUrl(value)
      : sanitizeReviewText(value);
  }

  if (Array.isArray(value)) {
    return value
      .map((entry) => sanitizeObservationArrayEntry(parentKey, entry))
      .filter((entry): entry is JsonValue => entry !== undefined);
  }

  return sanitizeObservationObject(value);
}

function isUnsafeObservationKey(key: string) {
  return unsafeObservationKeyPattern.test(key) && !safeObservationKeys.has(key);
}

function isUrlLikeKey(key: string) {
  return key === 'url' || key === 'path' || key === 'startUrl';
}

function sanitizeEvidencePath(path: string) {
  return sanitizeReviewText(stripQueryAndFragment(path), 400);
}

function stripQueryAndFragment(value: string) {
  return value.split(/[?#]/)[0] ?? '';
}

function normalizeTextLimit(value: number) {
  if (!Number.isFinite(value)) {
    return 2000;
  }

  return Math.max(0, Math.min(4000, Math.floor(value)));
}

function normalizeMarkdown(markdown: string) {
  return markdown.endsWith('\n') ? markdown : `${markdown}\n`;
}

async function readJson<T>(filePath: string): Promise<T> {
  return JSON.parse(await readFile(filePath, 'utf8')) as T;
}

async function readOptionalJson<T>(filePath: string): Promise<T | undefined> {
  try {
    return await readJson<T>(filePath);
  } catch {
    return undefined;
  }
}
