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

export type ReviewStatus = 'pending' | 'reviewed';
export type ReviewFindingSeverity =
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
      reviewStatus: 'reviewed',
      reviewerSummary: sanitizeReviewText(trimmed, 4000),
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
    return {
      reviewStatus: 'reviewed',
      reviewerSummary: 'Reviewer output was JSON, but not an object.',
      findings: [],
      openQuestions: [],
    };
  }

  const findings = asArray(source.findings)
    .map((finding, index) => normalizeFinding(finding, index))
    .filter((finding): finding is NormalizedReviewFinding => Boolean(finding));
  const reviewerSummary = firstString(
    source.summary,
    source.observationsSummary,
    source.overallSummary,
    source.reviewSummary
  );

  return {
    reviewStatus: 'reviewed',
    reviewerSummary: reviewerSummary
      ? sanitizeReviewText(reviewerSummary, 4000)
      : 'Reviewer output imported as structured JSON.',
    findings,
    openQuestions: asArray(source.openQuestions)
      .map((question) =>
        typeof question === 'string' ? sanitizeReviewText(question) : ''
      )
      .filter(Boolean),
  };
}

function normalizeFinding(
  value: unknown,
  index: number
): NormalizedReviewFinding | undefined {
  if (!isRecord(value)) {
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
  } else if (summary.findings.length === 0) {
    lines.push('No structured findings were provided by the reviewer.');
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
    lines.push('- None until reviewer output is available.');
  } else {
    lines.push(...summary.nextActionTickets.map((ticket) => `- ${ticket}`));
  }

  return normalizeMarkdown(lines.join('\n'));
}

function parseJsonLike(text: string): unknown | undefined {
  try {
    return JSON.parse(text);
  } catch {
    const fenced = /```(?:json)?\s*([\s\S]*?)```/i.exec(text);
    if (fenced) {
      try {
        return JSON.parse(fenced[1]);
      } catch {
        return undefined;
      }
    }

    const start = text.indexOf('{');
    const end = text.lastIndexOf('}');
    if (start >= 0 && end > start) {
      try {
        return JSON.parse(text.slice(start, end + 1));
      } catch {
        return undefined;
      }
    }

    return undefined;
  }
}

function normalizeSeverity(value: string | undefined): ReviewFindingSeverity {
  const normalized = (value ?? '').trim().toLowerCase();

  if (normalized === 'blocker' || normalized === 'critical') {
    return 'blocker';
  }

  if (normalized === 'polish' || normalized === 'medium') {
    return 'polish';
  }

  if (
    normalized === 'acceptable-beta-limit' ||
    normalized === 'acceptable beta limit' ||
    normalized === 'beta-limit' ||
    normalized === 'low'
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
