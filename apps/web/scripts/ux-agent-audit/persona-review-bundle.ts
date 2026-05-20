import { mkdir, readdir, readFile, writeFile } from 'node:fs/promises';
import { basename, extname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  normalizeReviewerOutput,
  type NormalizedReviewFinding,
  type ReviewFindingSeverity,
  type ReviewStatus,
} from './report.ts';

export interface PersonaReviewInput {
  reviewerId: string;
  displayName?: string;
  rawOutput: string;
}

export interface PersonaReviewBundleOptions {
  runDirectory: string;
  missionId: string;
  reviews: PersonaReviewInput[];
  outputDirectory?: string;
}

export interface PersonaReviewBundleFinding extends NormalizedReviewFinding {
  reviewerId: string;
  reviewerDisplayName: string;
  themeId: PersonaReviewThemeId;
}

export interface PersonaReviewTheme {
  id: PersonaReviewThemeId;
  label: string;
  topSeverity: ReviewFindingSeverity;
  findingCount: number;
  reviewerIds: string[];
  ticketReadyWording: string[];
}

export interface PersonaReviewBundle {
  schemaVersion: 1;
  artifactType: 'ux-agent-audit-persona-review-bundle';
  run: {
    runDirectory: string;
  };
  mission: {
    id: string;
  };
  reviewStatus: ReviewStatus;
  reviewerCount: number;
  reviews: Array<{
    reviewerId: string;
    displayName: string;
    status: ReviewStatus;
    summary: string;
    findingCount: number;
    openQuestionCount: number;
  }>;
  themes: PersonaReviewTheme[];
  findings: PersonaReviewBundleFinding[];
  openQuestions: string[];
  nextActionTickets: string[];
}

export interface PersonaReviewBundlePaths {
  jsonPath: string;
  markdownPath: string;
  bundle: PersonaReviewBundle;
}

export type PersonaReviewThemeId =
  | 'results-client-readiness'
  | 'results-export-readiness'
  | 'interpretation-validity'
  | 'collection-lifecycle'
  | 'audience-recipient-context'
  | 'questionnaire-scoring-semantics'
  | 'primary-copy-language'
  | 'other';

const themeLabels: Record<PersonaReviewThemeId, string> = {
  'results-client-readiness': 'Results client readiness',
  'results-export-readiness': 'Results export readiness',
  'interpretation-validity': 'Interpretation validity',
  'collection-lifecycle': 'Collection lifecycle',
  'audience-recipient-context': 'Audience and recipient context',
  'questionnaire-scoring-semantics': 'Questionnaire and scoring semantics',
  'primary-copy-language': 'Primary-path wording',
  other: 'Other UX findings',
};

const severityRank: Record<ReviewFindingSeverity, number> = {
  critical: 8,
  blocker: 7,
  high: 6,
  medium: 5,
  confusion: 4,
  low: 3,
  polish: 2,
  'acceptable-beta-limit': 1,
  info: 0,
};

export async function writePersonaReviewBundle(
  options: PersonaReviewBundleOptions
): Promise<PersonaReviewBundlePaths> {
  const bundle = buildPersonaReviewBundle(options);
  const outputDirectory = options.outputDirectory ?? options.runDirectory;
  const jsonPath = join(outputDirectory, 'persona-review-bundle.json');
  const markdownPath = join(outputDirectory, 'persona-review-bundle.md');

  await mkdir(outputDirectory, { recursive: true });
  await writeFile(jsonPath, `${JSON.stringify(bundle, null, 2)}\n`, 'utf8');
  await writeFile(markdownPath, buildPersonaReviewBundleMarkdown(bundle), 'utf8');

  return { jsonPath, markdownPath, bundle };
}

export function buildPersonaReviewBundle(
  options: PersonaReviewBundleOptions
): PersonaReviewBundle {
  const normalizedReviews = options.reviews.map((review) => {
    const parsed = normalizeReviewerOutput(review.rawOutput);
    const displayName = review.displayName ?? review.reviewerId;

    return {
      reviewerId: review.reviewerId,
      displayName,
      parsed,
      findings: parsed.findings.map((finding) => ({
        ...finding,
        reviewerId: review.reviewerId,
        reviewerDisplayName: displayName,
        themeId: classifyFindingTheme(finding),
      })),
    };
  });

  const findings = normalizedReviews.flatMap((review) => review.findings);
  const themes = buildThemes(findings);
  const openQuestions = uniqueOrdered(
    normalizedReviews.flatMap((review) => review.parsed.openQuestions)
  );
  const nextActionTickets = uniqueOrdered(
    themes.flatMap((theme) => theme.ticketReadyWording)
  );
  const reviewStatus = resolveBundleReviewStatus(
    normalizedReviews.map((review) => review.parsed.reviewStatus)
  );

  return {
    schemaVersion: 1,
    artifactType: 'ux-agent-audit-persona-review-bundle',
    run: {
      runDirectory: options.runDirectory,
    },
    mission: {
      id: options.missionId,
    },
    reviewStatus,
    reviewerCount: normalizedReviews.length,
    reviews: normalizedReviews.map((review) => ({
      reviewerId: review.reviewerId,
      displayName: review.displayName,
      status: review.parsed.reviewStatus,
      summary: review.parsed.reviewerSummary,
      findingCount: review.parsed.findings.length,
      openQuestionCount: review.parsed.openQuestions.length,
    })),
    themes,
    findings,
    openQuestions,
    nextActionTickets,
  };
}

export async function readPersonaReviewsFromDirectory(
  reviewDirectory: string
): Promise<PersonaReviewInput[]> {
  const entries = await readdir(reviewDirectory, { withFileTypes: true });
  const files = entries
    .filter((entry) => entry.isFile())
    .map((entry) => entry.name)
    .filter((name) => ['.json', '.txt', '.md'].includes(extname(name).toLowerCase()))
    .sort((left, right) => left.localeCompare(right));

  return await Promise.all(
    files.map(async (name) => ({
      reviewerId: basename(name, extname(name)),
      displayName: basename(name, extname(name)),
      rawOutput: await readFile(join(reviewDirectory, name), 'utf8'),
    }))
  );
}

export function classifyFindingTheme(
  finding: NormalizedReviewFinding
): PersonaReviewThemeId {
  const text = [
    finding.surface,
    finding.affectedStep,
    finding.userExpectation,
    finding.observedConfusion,
    finding.suggestedFix,
    finding.ticketReadyWording,
  ]
    .join(' ')
    .toLowerCase();

  if (containsAny(text, ['questionnaire', 'scoring', 'reverse scored', 'dimension'])) {
    return 'questionnaire-scoring-semantics';
  }

  if (containsAny(text, ['audience', 'recipient', 'roster', 'invited', 'segment'])) {
    return 'audience-recipient-context';
  }

  if (containsAny(text, ['collection', 'live', 'closed', 'preliminary'])) {
    return 'collection-lifecycle';
  }

  if (containsAny(text, ['interpretation', 'validated', 'scientific', 'client findings'])) {
    return 'interpretation-validity';
  }

  if (containsAny(text, ['client-ready', 'client ready', 'client handoff', 'handoff'])) {
    return 'results-client-readiness';
  }

  if (containsAny(text, ['export', 'csv', 'download', 'artifact'])) {
    return 'results-export-readiness';
  }

  if (containsAny(text, ['results', 'report'])) {
    return 'results-client-readiness';
  }

  if (
    containsAny(text, [
      'wording',
      'copy',
      'proof foundation',
      'technical',
      'api',
      'provenance',
    ])
  ) {
    return 'primary-copy-language';
  }

  return 'other';
}

function buildThemes(findings: PersonaReviewBundleFinding[]): PersonaReviewTheme[] {
  const byTheme = new Map<PersonaReviewThemeId, PersonaReviewBundleFinding[]>();
  for (const finding of findings) {
    const current = byTheme.get(finding.themeId) ?? [];
    current.push(finding);
    byTheme.set(finding.themeId, current);
  }

  return Array.from(byTheme.entries())
    .map(([themeId, themeFindings]) => {
      const topSeverity = themeFindings
        .map((finding) => finding.severity)
        .sort((left, right) => severityRank[right] - severityRank[left])[0];

      return {
        id: themeId,
        label: themeLabels[themeId],
        topSeverity,
        findingCount: themeFindings.length,
        reviewerIds: uniqueOrdered(themeFindings.map((finding) => finding.reviewerId)),
        ticketReadyWording: uniqueOrdered(
          themeFindings.map((finding) => finding.ticketReadyWording)
        ),
      };
    })
    .sort((left, right) => {
      const severityDelta =
        severityRank[right.topSeverity] - severityRank[left.topSeverity];
      if (severityDelta !== 0) {
        return severityDelta;
      }

      return right.findingCount - left.findingCount;
    });
}

function resolveBundleReviewStatus(statuses: ReviewStatus[]): ReviewStatus {
  if (statuses.length === 0 || statuses.includes('pending')) {
    return 'pending';
  }

  if (statuses.includes('needs-structured-review')) {
    return 'needs-structured-review';
  }

  return 'reviewed';
}

function buildPersonaReviewBundleMarkdown(bundle: PersonaReviewBundle) {
  const lines = [
    '# UX persona review bundle',
    '',
    `Review status: ${bundle.reviewStatus}`,
    `Mission: ${bundle.mission.id}`,
    `Reviewers: ${bundle.reviewerCount}`,
    '',
    '## Themes',
    '',
  ];

  if (bundle.themes.length === 0) {
    lines.push('- No findings were reported by the imported persona reviews.');
  } else {
    for (const theme of bundle.themes) {
      lines.push(
        `- ${theme.label}: ${theme.topSeverity}, ${theme.findingCount} finding(s), reviewers ${theme.reviewerIds.join(', ')}`
      );
    }
  }

  lines.push('', '## Next-action tickets', '');
  if (bundle.nextActionTickets.length === 0) {
    lines.push('- None.');
  } else {
    lines.push(...bundle.nextActionTickets.map((ticket) => `- ${ticket}`));
  }

  lines.push('', '## Persona summaries', '');
  for (const review of bundle.reviews) {
    lines.push(
      `- ${review.displayName}: ${review.status}, ${review.findingCount} finding(s). ${review.summary}`
    );
  }

  if (bundle.openQuestions.length > 0) {
    lines.push('', '## Open questions', '');
    lines.push(...bundle.openQuestions.map((question) => `- ${question}`));
  }

  return `${lines.join('\n')}\n`;
}

function uniqueOrdered(values: string[]) {
  const seen = new Set<string>();
  const result: string[] = [];

  for (const value of values) {
    const normalized = value.trim();
    const key = normalized.toLowerCase();
    if (!normalized || seen.has(key)) {
      continue;
    }

    seen.add(key);
    result.push(normalized);
  }

  return result;
}

function containsAny(text: string, fragments: string[]) {
  return fragments.some((fragment) => text.includes(fragment));
}

interface CliOptions {
  runDirectory: string;
  missionId: string;
  reviewDirectory: string;
  outputDirectory?: string;
}

function parseCliOptions(args: string[]): CliOptions {
  const values = new Map<string, string>();
  for (let index = 0; index < args.length; index += 1) {
    const flag = args[index];
    if (!flag.startsWith('--')) {
      throw new Error(`Unexpected argument: ${flag}`);
    }

    if (!['--run-dir', '--mission', '--review-dir', '--out-dir'].includes(flag)) {
      throw new Error(`Unknown option: ${flag}`);
    }

    const value = args[index + 1];
    if (!value || value.startsWith('--')) {
      throw new Error(`Missing value for ${flag}`);
    }

    values.set(flag, value);
    index += 1;
  }

  const runDirectory = values.get('--run-dir');
  const reviewDirectory = values.get('--review-dir');
  if (!runDirectory) {
    throw new Error('Missing required option: --run-dir');
  }

  if (!reviewDirectory) {
    throw new Error('Missing required option: --review-dir');
  }

  return {
    runDirectory,
    missionId: values.get('--mission') ?? 'fullstack-osh-warehouse-pulse',
    reviewDirectory,
    ...(values.has('--out-dir') ? { outputDirectory: values.get('--out-dir') } : {}),
  };
}

async function main() {
  const options = parseCliOptions(process.argv.slice(2));
  const reviews = await readPersonaReviewsFromDirectory(options.reviewDirectory);
  const result = await writePersonaReviewBundle({
    runDirectory: options.runDirectory,
    missionId: options.missionId,
    reviews,
    outputDirectory: options.outputDirectory,
  });

  console.log(
    JSON.stringify(
      {
        jsonPath: result.jsonPath,
        markdownPath: result.markdownPath,
        reviewStatus: result.bundle.reviewStatus,
        reviewers: result.bundle.reviewerCount,
        findings: result.bundle.findings.length,
        themes: result.bundle.themes.length,
        nextActionTickets: result.bundle.nextActionTickets.length,
      },
      null,
      2
    )
  );
}

const currentFilePath = fileURLToPath(import.meta.url);
if (process.argv[1] && resolve(process.argv[1]) === currentFilePath) {
  main().catch((error: unknown) => {
    console.error(error instanceof Error ? error.message : String(error));
    process.exitCode = 1;
  });
}
