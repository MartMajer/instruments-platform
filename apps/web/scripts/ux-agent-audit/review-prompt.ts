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
const urlPattern = /https?:\/\/[^\s)"'<>]+/gi;
const dataUriPattern = /\bdata:[^\s)"'<>`]+/gi;
const relativePathWithQueryPattern =
  /(^|[\s(["'`])((?:\/|\.{1,2}\/)[^\s)"'<>`]*[?#][^\s)"'<>`]*)/g;
const bareLocalPathPattern =
  /(^|[\s([{"'`])((?:[A-Za-z]:[\\/]|\\\\|\/(?:Users|home|private|tmp)[\\/]|\/var\/folders[\\/]|~[\\/]|\.{2}[\\/])[^ \t\r\n)"'<>`,;]*)/gi;
const markdownLinkPattern = /!?\[([^\]\r\n]*)\]\(([^)\r\n]*)\)/g;
const secretAssignmentPattern =
  /\b(?:api[-_]?token|access[-_]?token|refresh[-_]?token|invitationToken|token|secret|password|authorization|cookie)\s*=\s*[^\s),.;\]}]+/gi;
const unsafeObservationKeyPattern =
  /(raw|body|html|screenshotdata|image|token|secret|email|query|fragment|cookie|authorization|password|salt|answer)/i;
const safeObservationKeys = new Set([
  'appAccessible',
  'actionLog',
  'autonomousMode',
  'avoidedActions',
  'avoidedUnsafePersistedArtifacts',
  'blockedReason',
  'buttons',
  'captureMode',
  'disabled',
  'fields',
  'headings',
  'label',
  'links',
  'localOnly',
  'localFullTranscript',
  'missionBoundary',
  'navigationPolicy',
  'pageUrlCapture',
  'pages',
  'path',
  'placeholder',
  'productEntryPath',
  'routeNavigationOnly',
  'safeCapturePolicy',
  'screenshotCapture',
  'selectedStudyPathFound',
  'signInBlocked',
  'startUrl',
  'statusMessages',
  'targetFixturePaths',
  'targetProductPaths',
  'text',
  'title',
  'url',
  'value',
  'visibleControls',
  'visibleTextCapture',
  'visibleTextExcerpt',
  'visibleText',
  'visitedFixturePaths',
  'visitedProductPaths',
  'visitedWorkflowSurfaces',
  'personaFindings',
  'personaGoal',
  'personaGoalAssessment',
  'name',
  'role',
  'domainKnowledge',
  'patience',
  'appGoal',
  'successCriteria',
  'confusionTriggers',
  'hardFailureTriggers',
  'reviewerInstructions',
  'checkedCriteriaCount',
  'criterion',
  'evidence',
  'visitedTargetCount',
  'targetCount',
  'unresolvedFindingCount',
  'reviewFocus',
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
      'Use only the local audit evidence below. It may include full visible text captured from the local dev app so you can review what a user would see. Do not use outside product knowledge.',
      'Treat screenshots as referenced artifacts only. Never request or invent raw tokens, emails, UUIDs, participant codes, secrets, cookies, or query-string state.',
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
      '## Reviewer output format',
      '',
      'Return raw JSON or a fenced `json` block only. Do not answer in prose outside the JSON.',
      'Use this shape: {"summary":"...","findings":[{"severity":"blocker|confusion|polish|acceptable-beta-limit|critical|high|medium|low|info","affectedStep":"...","surface":"...","userExpectation":"...","observedConfusion":"...","suggestedFix":"...","ticketReadyWording":"..."}],"openQuestions":["..."]}.',
      'If there are no findings, return structured JSON with an empty findings array and a short summary explaining why.',
      '',
      '## Local audit evidence',
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
      path: sanitizeEvidenceFilename(screenshot.path),
    })),
    observations: sanitizeObservationObject(evidence.observations ?? {}),
  };
}

export function sanitizeEvidenceUrl(url: string) {
  const value = (url ?? '').trim();

  if (!value) {
    return '';
  }

  const scheme = value.match(/^([a-z][a-z0-9+.-]*):/i)?.[1]?.toLowerCase();
  if (scheme && scheme !== 'http' && scheme !== 'https') {
    return '[redacted-uri]';
  }

  try {
    const parsed = new URL(value);
    return `${classifyEvidenceUrl(parsed)}${sanitizeEvidenceUrlPath(
      parsed.pathname
    )}`;
  } catch {
    return sanitizeEvidenceUrlPath(stripQueryAndFragment(value));
  }
}

export function sanitizeReviewText(text: string, maxCharacters = 2000) {
  const normalized = (text ?? '').replace(/\s+/g, ' ').trim();
  return normalized
    .replace(markdownLinkPattern, (_match, label: string, target: string) => {
      const safeLabel = label ? sanitizePlainText(label, 160) : 'link';
      const safeTarget = sanitizeMarkdownTarget(target);
      return `${safeLabel} (${safeTarget})`;
    })
    .replace(dataUriPattern, '[redacted-uri]')
    .replace(urlPattern, (url) => sanitizeEvidenceUrl(url))
    .replace(
      relativePathWithQueryPattern,
      (_match, prefix: string, path: string) => `${prefix}${sanitizeEvidenceUrl(path)}`
    )
    .replace(bareLocalPathPattern, (_match, prefix: string) => {
      return `${prefix}[redacted-path]`;
    })
    .replace(secretAssignmentPattern, (assignment) => {
      const key = assignment.split('=')[0]?.trim() ?? 'token';
      return `${key}=[redacted-token]`;
    })
    .replace(emailPattern, '[redacted-email]')
    .replace(uuidPattern, '[redacted-uuid]')
    .replace(jwtLikePattern, '[redacted-token]')
    .replace(longTokenPattern, '[redacted-token]')
    .replace(participantCodeLikePattern, '[redacted-code]')
    .replace(/[<>&]/g, (character) => htmlEscapeMap[character] ?? character)
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

function sanitizeEvidenceUrlPath(path: string) {
  const stripped = stripQueryAndFragment(path).trim().replace(/\\/g, '/');

  if (!stripped) {
    return '';
  }

  if (isUnsafePathReference(stripped)) {
    return sanitizeEvidenceFilename(stripped);
  }

  const startsWithSlash = stripped.startsWith('/');
  const segments = stripped
    .split('/')
    .filter(Boolean)
    .map((segment) => sanitizeReviewText(segment, 120))
    .filter(Boolean);

  if (segments.length === 0) {
    return startsWithSlash ? '/' : '';
  }

  return `${startsWithSlash ? '/' : ''}${segments.join('/')}`;
}

function sanitizeEvidenceFilename(path: string) {
  const stripped = stripQueryAndFragment(path).trim().replace(/\\/g, '/');
  const filename = stripped.split('/').filter(Boolean).at(-1) ?? '';

  if (!filename || filename === '.' || filename === '..') {
    return '[redacted-filename]';
  }

  return sanitizeReviewText(filename, 180);
}

function stripQueryAndFragment(value: string) {
  return value.split(/[?#]/)[0] ?? '';
}

function classifyEvidenceUrl(url: URL) {
  if (isLocalUrlHost(url.hostname)) {
    return '[local-url]';
  }

  if (isAppRoutePath(url.pathname)) {
    return '[app-url]';
  }

  return '[external-url]';
}

function isLocalUrlHost(hostname: string) {
  const normalized = hostname.toLowerCase().replace(/^\[|\]$/g, '');

  return (
    normalized === 'localhost' ||
    normalized === '::1' ||
    normalized === '0:0:0:0:0:0:0:1' ||
    normalized === '0.0.0.0' ||
    normalized.startsWith('127.') ||
    normalized.endsWith('.localhost')
  );
}

function isAppRoutePath(pathname: string) {
  const normalized = pathname.toLowerCase();

  return (
    normalized === '/app' ||
    normalized.startsWith('/app/') ||
    normalized === '/respond' ||
    normalized.startsWith('/respond/') ||
    normalized === '/signin' ||
    normalized === '/register' ||
    normalized.startsWith('/auth/')
  );
}

function isUnsafePathReference(path: string) {
  const normalized = path.toLowerCase();

  return (
    path.includes('..') ||
    /^[A-Za-z]:\//.test(path) ||
    path.startsWith('~') ||
    normalized.includes('/users/') ||
    normalized.startsWith('/home/') ||
    normalized.startsWith('/private/') ||
    normalized.startsWith('/tmp/') ||
    normalized.startsWith('/var/folders/')
  );
}

function sanitizeMarkdownTarget(target: string) {
  const trimmed = (target ?? '').trim();

  if (/^(?:data|javascript|vbscript):/i.test(trimmed)) {
    return '[redacted-uri]';
  }

  if (isBareLocalPathReference(trimmed)) {
    return '[redacted-path]';
  }

  if (/^https?:\/\//i.test(trimmed)) {
    return sanitizeEvidenceUrl(trimmed);
  }

  if (/^(?:\/|\.{1,2}\/)/.test(trimmed)) {
    return sanitizeEvidenceUrl(trimmed);
  }

  return sanitizePlainText(trimmed, 240);
}

function isBareLocalPathReference(value: string) {
  const normalized = value.trim().replace(/\\/g, '/').toLowerCase();

  return (
    /^[a-z]:\//.test(normalized) ||
    normalized.startsWith('//') ||
    normalized.startsWith('/users/') ||
    normalized.startsWith('/home/') ||
    normalized.startsWith('/private/') ||
    normalized.startsWith('/tmp/') ||
    normalized.startsWith('/var/folders/') ||
    normalized.startsWith('~/') ||
    normalized.startsWith('../')
  );
}

function sanitizePlainText(text: string, maxCharacters = 2000) {
  return (text ?? '')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(dataUriPattern, '[redacted-uri]')
    .replace(bareLocalPathPattern, (_match, prefix: string) => {
      return `${prefix}[redacted-path]`;
    })
    .replace(/[<>&]/g, (character) => htmlEscapeMap[character] ?? character)
    .slice(0, normalizeTextLimit(maxCharacters));
}

const htmlEscapeMap: Record<string, string> = {
  '<': '&lt;',
  '>': '&gt;',
  '&': '&amp;',
};

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
