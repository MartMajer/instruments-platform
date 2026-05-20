import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

import type { JsonObject, MissionEvidence } from './evidence';
import {
  sanitizeEvidenceForReview,
  sanitizeEvidenceUrl,
  sanitizeReviewText,
  type ReviewPromptRunMetadata,
} from './review-prompt';
import type { MissionDefinition, PersonaDefinition } from './types';

export type ReviewStatus = 'pending' | 'needs-structured-review' | 'reviewed';
export type ReviewFindingSeverity =
  | 'critical'
  | 'high'
  | 'medium'
  | 'low'
  | 'info'
  | 'blocker'
  | 'confusion'
  | 'polish'
  | 'acceptable-beta-limit';

export interface NormalizedReviewFinding {
  id: string;
  severity: ReviewFindingSeverity;
  affectedStep: string;
  surface: string;
  userExpectation: string;
  observedConfusion: string;
  suggestedFix: string;
  ticketReadyWording: string;
}

export interface NormalizedReviewSummary {
  schemaVersion: 1;
  artifactType: 'ux-agent-audit-review-summary';
  reviewStatus: ReviewStatus;
  run: {
    runId: string;
    createdAt: string;
  };
  mission: {
    id: string;
    goal: string;
    status: string;
  };
  persona: {
    id: string;
    displayName: string;
  };
  observationsSummary: JsonObject;
  findings: NormalizedReviewFinding[];
  openQuestions: string[];
  nextActionTickets: string[];
}

export interface WriteNormalizedReviewReportOptions {
  runDirectory: string;
  runMetadata?: ReviewPromptRunMetadata;
  mission: MissionDefinition<string>;
  persona: PersonaDefinition;
  evidence?: MissionEvidence;
  evidencePath?: string;
  reviewerOutput?: string;
  reviewerOutputPath?: string;
}

export interface NormalizedReviewReportPaths {
  markdownPath: string;
  jsonPath: string;
  summary: NormalizedReviewSummary;
}

interface ParsedReviewerOutput {
  reviewStatus: ReviewStatus;
  reviewerSummary: string;
  findings: NormalizedReviewFinding[];
  openQuestions: string[];
}

const meaningfulFindingFields = [
  'affectedStep',
  'step',
  'missionStep',
  'surface',
  'affectedSurface',
  'route',
  'userExpectation',
  'expectation',
  'observedConfusion',
  'observed',
  'problem',
  'suggestedFix',
  'fix',
  'recommendation',
  'ticketReadyWording',
  'ticket',
  'nextAction',
  'nextActionTicket',
];

export async function writeNormalizedReviewReport(
  options: WriteNormalizedReviewReportOptions
): Promise<NormalizedReviewReportPaths> {
  const evidence =
    options.evidence ??
    (await readJson<MissionEvidence>(
      options.evidencePath ??
        join(options.runDirectory, 'missions', options.mission.id, 'evidence.json')
    ));
  const reviewerOutput =
    options.reviewerOutput ??
    (options.reviewerOutputPath
      ? await readFile(options.reviewerOutputPath, 'utf8')
      : '');
  const parsedReview = normalizeReviewerOutput(reviewerOutput);
  const summary = buildReviewSummary(options, evidence, parsedReview);
  const jsonPath = join(options.runDirectory, 'review-summary.json');
  const markdownPath = join(options.runDirectory, 'review-report.md');

  await mkdir(options.runDirectory, { recursive: true });
  await writeFile(jsonPath, `${JSON.stringify(summary, null, 2)}\n`, 'utf8');
  await writeFile(markdownPath, buildMarkdownReport(summary), 'utf8');

  return { markdownPath, jsonPath, summary };
}

export function normalizeReviewerOutput(
  reviewerOutput: string
): ParsedReviewerOutput {
  const trimmed = reviewerOutput.trim();

  if (!trimmed) {
    return {
      reviewStatus: 'pending',
      reviewerSummary:
        'Review pending. No reviewer output has been pasted or imported yet.',
      findings: [],
      openQuestions: [],
    };
  }

  const parsed = parseJsonLike(trimmed);
  if (!parsed) {
    return {
      reviewStatus: 'needs-structured-review',
      reviewerSummary: `Reviewer output needs structured JSON. Paste raw JSON or a fenced json block. Non-JSON output preserved for reference: ${sanitizeReviewText(
        trimmed,
        3600
      )}`,
      findings: [],
      openQuestions: [],
    };
  }

  return normalizeJsonReviewerOutput(parsed);
}

function buildReviewSummary(
  options: WriteNormalizedReviewReportOptions,
  evidence: MissionEvidence,
  parsedReview: ParsedReviewerOutput
): NormalizedReviewSummary {
  const sanitizedEvidence = sanitizeEvidenceForReview(evidence);
  const runId = sanitizeReviewText(options.runMetadata?.runId ?? 'unknown-local-run');
  const createdAt = sanitizeReviewText(options.runMetadata?.createdAt ?? 'unknown');
  const nextActionTickets = parsedReview.findings
    .map((finding) => finding.ticketReadyWording)
    .filter(Boolean);

  return {
    schemaVersion: 1,
    artifactType: 'ux-agent-audit-review-summary',
    reviewStatus: parsedReview.reviewStatus,
    run: {
      runId,
      createdAt,
    },
    mission: {
      id: sanitizeReviewText(options.mission.id),
      goal: sanitizeReviewText(options.mission.goal),
      status: sanitizeReviewText(evidence.status),
    },
    persona: {
      id: sanitizeReviewText(options.persona.id),
      displayName: sanitizeReviewText(options.persona.displayName),
    },
    observationsSummary: {
      missionStatus: sanitizeReviewText(evidence.status),
      stepCount: evidence.steps.length,
      screenshotReferences: sanitizedEvidence.screenshotReferences ?? [],
      reviewerSummary: parsedReview.reviewerSummary,
    },
    findings: parsedReview.findings,
    openQuestions: parsedReview.openQuestions,
    nextActionTickets,
  };
}

function normalizeJsonReviewerOutput(value: unknown): ParsedReviewerOutput {
  const source = Array.isArray(value) ? { findings: value } : value;
  if (!isRecord(source)) {
    return needsStructuredReview(
      `Parsed JSON was ${describeJsonShape(
        value
      )}, not the required review object with a findings array.`
    );
  }

  if (!Object.prototype.hasOwnProperty.call(source, 'findings')) {
    return needsStructuredReview(
      'Parsed JSON object did not include the required findings array.'
    );
  }

  if (!Array.isArray(source.findings)) {
    return needsStructuredReview(
      'Parsed JSON object included findings, but findings was not an array.'
    );
  }

  const reviewerSummary = firstString(
    source.summary,
    source.observationsSummary,
    source.overallSummary,
    source.reviewSummary
  );
  const openQuestions = asArray(source.openQuestions)
    .map((question) =>
      typeof question === 'string' ? sanitizeReviewText(question) : ''
    )
    .filter(Boolean);

  if (source.findings.length === 0) {
    if (reviewerSummary) {
      return {
        reviewStatus: 'reviewed',
        reviewerSummary: sanitizeReviewText(reviewerSummary, 4000),
        findings: [],
        openQuestions,
      };
    }

    return needsStructuredReview(
      'Parsed JSON included an empty findings array, but no summary explained why zero findings is intentional.'
    );
  }

  const findings = source.findings
    .map((finding, index) => normalizeFinding(finding, index))
    .filter((finding): finding is NormalizedReviewFinding => Boolean(finding));

  if (findings.length === 0) {
    return needsStructuredReview(
      'Parsed JSON findings array did not contain any valid finding objects.'
    );
  }

  return {
    reviewStatus: 'reviewed',
    reviewerSummary: reviewerSummary
      ? sanitizeReviewText(reviewerSummary, 4000)
      : 'Reviewer output imported as structured JSON.',
    findings,
    openQuestions,
  };
}

function normalizeFinding(
  value: unknown,
  index: number
): NormalizedReviewFinding | undefined {
  if (!isRecord(value) || !hasMeaningfulFindingContent(value)) {
    return undefined;
  }

  const affectedStep =
    firstString(value.affectedStep, value.step, value.missionStep) ??
    'Unspecified step';
  const surface =
    firstString(value.surface, value.affectedSurface, value.route) ??
    'Unspecified surface';
  const userExpectation =
    firstString(value.userExpectation, value.expectation) ??
    'No user expectation provided.';
  const observedConfusion =
    firstString(value.observedConfusion, value.observed, value.problem) ??
    'No observed confusion provided.';
  const suggestedFix =
    firstString(value.suggestedFix, value.fix, value.recommendation) ??
    'No suggested fix provided.';
  const ticketReadyWording =
    firstString(
      value.ticketReadyWording,
      value.ticket,
      value.nextAction,
      value.nextActionTicket
    ) ?? buildTicketReadyWording(surface, suggestedFix, observedConfusion);

  return {
    id: `F${index + 1}`,
    severity: normalizeSeverity(firstString(value.severity)),
    affectedStep: sanitizeReviewText(affectedStep),
    surface: sanitizeReviewText(surface),
    userExpectation: sanitizeReviewText(userExpectation),
    observedConfusion: sanitizeReviewText(observedConfusion),
    suggestedFix: sanitizeReviewText(suggestedFix),
    ticketReadyWording: sanitizeReviewText(ticketReadyWording),
  };
}

function needsStructuredReview(reason: string): ParsedReviewerOutput {
  return {
    reviewStatus: 'needs-structured-review',
    reviewerSummary: sanitizeReviewText(
      `Reviewer output parsed as JSON but did not match the required review schema. ${reason}`,
      4000
    ),
    findings: [],
    openQuestions: [],
  };
}

function describeJsonShape(value: unknown) {
  if (Array.isArray(value)) {
    return 'an array';
  }

  if (value === null) {
    return 'null';
  }

  return `a ${typeof value}`;
}

function hasMeaningfulFindingContent(value: Record<string, unknown>) {
  return meaningfulFindingFields.some((field) => {
    const fieldValue = value[field];
    return typeof fieldValue === 'string' && fieldValue.trim().length > 0;
  });
}

function buildMarkdownReport(summary: NormalizedReviewSummary) {
  const lines = [
    '# UX audit review report',
    '',
    `Review status: ${summary.reviewStatus}`,
    '',
    '## Metadata',
    '',
    `- Run: ${summary.run.runId}`,
    `- Created at: ${summary.run.createdAt}`,
    `- Mission: ${summary.mission.id}`,
    `- Persona: ${summary.persona.displayName}`,
    '',
    '## Observations summary',
    '',
    `- Mission status: ${summary.observationsSummary.missionStatus}`,
    `- Steps captured: ${summary.observationsSummary.stepCount}`,
    `- Screenshot references: ${formatScreenshotCount(
      summary.observationsSummary.screenshotReferences
    )}`,
    `- Reviewer summary: ${summary.observationsSummary.reviewerSummary}`,
    '',
    '## Findings',
    '',
  ];

  if (summary.reviewStatus === 'pending') {
    lines.push('Review pending. Generate or paste reviewer output, then normalize it.');
  } else if (summary.reviewStatus === 'needs-structured-review') {
    lines.push(
      'Review needs structured JSON reviewer output. Paste raw JSON or a fenced `json` block, then normalize again.'
    );
  } else if (summary.findings.length === 0) {
    lines.push(
      'The reviewer explicitly reported zero findings in the structured review output.'
    );
  } else {
    for (const finding of summary.findings) {
      lines.push(
        `### ${finding.id}: ${finding.severity}`,
        '',
        `- Affected step/surface: ${finding.affectedStep} / ${finding.surface}`,
        `- User expectation: ${finding.userExpectation}`,
        `- Observed confusion: ${finding.observedConfusion}`,
        `- Suggested fix: ${finding.suggestedFix}`,
        `- Ticket-ready wording: ${finding.ticketReadyWording}`,
        ''
      );
    }
  }

  lines.push('', '## Open questions', '');
  if (summary.openQuestions.length === 0) {
    lines.push('- None recorded.');
  } else {
    lines.push(...summary.openQuestions.map((question) => `- ${question}`));
  }

  lines.push('', '## Next-action tickets', '');
  if (summary.nextActionTickets.length === 0) {
    if (summary.reviewStatus === 'reviewed' && summary.findings.length === 0) {
      lines.push('- None. Reviewer explicitly reported zero findings.');
    } else {
      lines.push('- None until reviewer output is available.');
    }
  } else {
    lines.push(...summary.nextActionTickets.map((ticket) => `- ${ticket}`));
  }

  return normalizeMarkdown(lines.join('\n'));
}

function parseJsonLike(text: string): unknown | undefined {
  for (const candidate of jsonCandidates(text)) {
    const parsed = tryParseJson(candidate);
    if (parsed !== undefined) {
      return parsed;
    }
  }

  return undefined;
}

function normalizeSeverity(value: string | undefined): ReviewFindingSeverity {
  const normalized = (value ?? '').trim().toLowerCase();

  if (
    normalized === 'critical' ||
    normalized === 'high' ||
    normalized === 'medium' ||
    normalized === 'low' ||
    normalized === 'info'
  ) {
    return normalized;
  }

  if (normalized === 'informational' || normalized === 'information') {
    return 'info';
  }

  if (normalized === 'blocker') {
    return 'blocker';
  }

  if (normalized === 'polish') {
    return 'polish';
  }

  if (
    normalized === 'acceptable-beta-limit' ||
    normalized === 'acceptable beta limit' ||
    normalized === 'beta-limit'
  ) {
    return 'acceptable-beta-limit';
  }

  return 'confusion';
}

function buildTicketReadyWording(
  surface: string,
  suggestedFix: string,
  observedConfusion: string
) {
  const action = suggestedFix || observedConfusion;
  return `Improve ${surface}: ${action}`;
}

function firstString(...values: unknown[]) {
  for (const value of values) {
    if (typeof value === 'string' && value.trim()) {
      return sanitizeReviewText(value);
    }
  }

  return undefined;
}

function asArray(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

function jsonCandidates(text: string) {
  const candidates = [text.trim()];
  const fencedBlockPattern = /```(?:[A-Za-z0-9_-]+)?\s*([\s\S]*?)```/g;
  let fencedBlock: RegExpExecArray | null;

  while ((fencedBlock = fencedBlockPattern.exec(text))) {
    candidates.push(fencedBlock[1]?.trim() ?? '');
  }

  candidates.push(...extractBalancedJsonCandidates(text));

  return candidates.filter(Boolean);
}

function tryParseJson(candidate: string) {
  try {
    return JSON.parse(candidate);
  } catch {
    return undefined;
  }
}

function extractBalancedJsonCandidates(text: string) {
  const candidates: string[] = [];

  for (let start = 0; start < text.length; start += 1) {
    const opening = text[start];
    if (opening !== '{' && opening !== '[') {
      continue;
    }

    const candidate = extractBalancedJsonCandidate(text, start, opening);
    if (candidate) {
      candidates.push(candidate);
    }
  }

  return candidates;
}

function extractBalancedJsonCandidate(
  text: string,
  start: number,
  opening: string
) {
  const closingForOpening: Record<string, string> = { '{': '}', '[': ']' };
  const stack = [closingForOpening[opening]];
  let inString = false;
  let escaped = false;

  for (let index = start + 1; index < text.length; index += 1) {
    const character = text[index];

    if (inString) {
      if (escaped) {
        escaped = false;
      } else if (character === '\\') {
        escaped = true;
      } else if (character === '"') {
        inString = false;
      }
      continue;
    }

    if (character === '"') {
      inString = true;
      continue;
    }

    if (character === '{' || character === '[') {
      stack.push(closingForOpening[character]);
      continue;
    }

    if (character === '}' || character === ']') {
      if (character !== stack.at(-1)) {
        return undefined;
      }

      stack.pop();
      if (stack.length === 0) {
        return text.slice(start, index + 1);
      }
    }
  }

  return undefined;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === 'object' && !Array.isArray(value);
}

function formatScreenshotCount(value: unknown) {
  if (!Array.isArray(value)) {
    return '0';
  }

  return String(value.length);
}

function normalizeMarkdown(markdown: string) {
  return markdown.endsWith('\n') ? markdown : `${markdown}\n`;
}

async function readJson<T>(filePath: string): Promise<T> {
  return JSON.parse(await readFile(filePath, 'utf8')) as T;
}
