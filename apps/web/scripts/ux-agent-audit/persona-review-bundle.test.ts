import { mkdtemp, readFile, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { describe, expect, it } from 'vitest';

import {
  buildPersonaReviewBundle,
  classifyFindingTheme,
  readPersonaReviewsFromDirectory,
  writePersonaReviewBundle,
} from './persona-review-bundle.ts';
import type { NormalizedReviewFinding } from './report.ts';

describe('persona review bundle', () => {
  it('groups multiple structured persona reviews into themes and tickets', () => {
    const result = buildPersonaReviewBundle({
      runDirectory: 'artifacts/local/run-1',
      missionId: 'fullstack-osh-warehouse-pulse',
      reviews: [
        {
          reviewerId: 'osh-consultant',
          rawOutput: JSON.stringify({
            summary: 'Results are not client-ready.',
            findings: [
              {
                severity: 'high',
                affectedStep: 'Step 10',
                surface: 'Results / Reports and exports',
                userExpectation: 'I need to know if the report is client-ready.',
                observedConfusion:
                  'Ready for review appears beside report readiness blocked.',
                suggestedFix:
                  'Separate preview readiness from client handoff readiness.',
                ticketReadyWording:
                  'Separate preview readiness from client handoff readiness on Results.',
              },
              {
                severity: 'medium',
                affectedStep: 'Step 9',
                surface: 'Collection',
                userExpectation: 'I need one lifecycle state.',
                observedConfusion: 'The page says live, close collection, and closed not available.',
                suggestedFix: 'Show one live/closed status and one next action.',
                ticketReadyWording:
                  'Unify collection lifecycle status into one primary banner.',
              },
            ],
            openQuestions: ['Can live results be exported?'],
          }),
        },
        {
          reviewerId: 'professor',
          rawOutput: JSON.stringify({
            summary: 'Interpretation validity is too weak.',
            findings: [
              {
                severity: 'blocker',
                affectedStep: 'Step 10',
                surface: 'Results interpretation',
                userExpectation: 'I need to know if findings are scientifically valid.',
                observedConfusion:
                  'Score coverage ready appears near interpretation not validated.',
                suggestedFix: 'Separate operational readiness from interpretation validity.',
                ticketReadyWording:
                  'Separate operational readiness from interpretation validity on Results.',
              },
            ],
            openQuestions: ['Can live results be exported?'],
          }),
        },
      ],
    });

    expect(result.reviewStatus).toBe('reviewed');
    expect(result.reviewerCount).toBe(2);
    expect(result.findings).toHaveLength(3);
    expect(result.openQuestions).toEqual(['Can live results be exported?']);
    expect(result.themes.map((theme) => theme.id)).toEqual([
      'interpretation-validity',
      'results-client-readiness',
      'collection-lifecycle',
    ]);
    expect(result.nextActionTickets).toContain(
      'Separate operational readiness from interpretation validity on Results.'
    );
  });

  it('keeps malformed reviewer output visible as a structured-review blocker', () => {
    const result = buildPersonaReviewBundle({
      runDirectory: 'artifacts/local/run-1',
      missionId: 'fullstack-osh-warehouse-pulse',
      reviews: [{ reviewerId: 'ergonomics', rawOutput: 'Looks bad, fix it.' }],
    });

    expect(result.reviewStatus).toBe('needs-structured-review');
    expect(result.findings).toEqual([]);
    expect(result.reviews[0]?.summary).toContain('needs structured JSON');
  });

  it('reads review files and writes JSON and markdown bundle artifacts', async () => {
    const root = await mkdtemp(join(tmpdir(), 'ux-review-bundle-'));
    const reviewDirectory = join(root, 'reviews');
    await writeFile(
      join(root, 'ignore.csv'),
      'not a review',
      'utf8'
    ).catch(async () => undefined);
    await writeFile(
      join(reviewDirectory, 'osh.json'),
      JSON.stringify({
        summary: 'Audience is unclear.',
        findings: [
          {
            severity: 'confusion',
            surface: 'Collection audience',
            observedConfusion: 'No recipient count is shown.',
            suggestedFix: 'Show recipient count.',
            ticketReadyWording: 'Show selected audience and recipient count.',
          },
        ],
      }),
      'utf8'
    ).catch(async (error: NodeJS.ErrnoException) => {
      if (error.code !== 'ENOENT') {
        throw error;
      }

      const { mkdir } = await import('node:fs/promises');
      await mkdir(reviewDirectory, { recursive: true });
      await writeFile(
        join(reviewDirectory, 'osh.json'),
        JSON.stringify({
          summary: 'Audience is unclear.',
          findings: [
            {
              severity: 'confusion',
              surface: 'Collection audience',
              observedConfusion: 'No recipient count is shown.',
              suggestedFix: 'Show recipient count.',
              ticketReadyWording: 'Show selected audience and recipient count.',
            },
          ],
        }),
        'utf8'
      );
    });

    const reviews = await readPersonaReviewsFromDirectory(reviewDirectory);
    const paths = await writePersonaReviewBundle({
      runDirectory: root,
      missionId: 'fullstack-osh-warehouse-pulse',
      reviews,
    });

    expect(paths.bundle.findings).toHaveLength(1);
    expect(await readFile(paths.jsonPath, 'utf8')).toContain(
      'ux-agent-audit-persona-review-bundle'
    );
    expect(await readFile(paths.markdownPath, 'utf8')).toContain(
      'Show selected audience and recipient count.'
    );
  });
});

describe('classifyFindingTheme', () => {
  it('classifies questionnaire scoring before generic result wording', () => {
    const finding = {
      surface: 'Results and questionnaire setup',
      affectedStep: 'Step 6',
      userExpectation: 'I need scoring direction.',
      observedConfusion: 'Reverse scored appears without dimension meaning.',
      suggestedFix: 'Show higher-is-risk copy.',
      ticketReadyWording: 'Clarify questionnaire scoring semantics.',
    } as NormalizedReviewFinding;

    expect(classifyFindingTheme(finding)).toBe('questionnaire-scoring-semantics');
  });
});
