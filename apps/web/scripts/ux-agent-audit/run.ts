import { readFile } from 'node:fs/promises';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

import { captureBrowserEvidence } from './browser.ts';
import type { MissionEvidence } from './evidence.ts';
import { hasFixedMissionExecutor } from './mission-executor.ts';
import { missions } from './missions.ts';
import { personas } from './personas.ts';
import { writeNormalizedReviewReport } from './report.ts';
import {
  writeReviewPromptForMission,
  type ReviewPromptRunMetadata,
} from './review-prompt.ts';
import type {
  MissionDefinition,
  PersonaDefinition,
  ViewportPreset,
} from './types.ts';

export interface RunnerOptions {
  baseUrl: string;
  missionFilter: string;
  personaOverride: string;
  viewportOverride: ViewportPreset;
  headless: boolean;
  outputRoot: string;
}

export interface NormalizeReviewOptions {
  runDirectory: string;
  missionFilter: string;
  personaOverride?: string;
  reviewerOutputPath?: string;
  reviewerOutput?: string;
}

const allowedFlags = new Set([
  '--base-url',
  '--headless',
  '--mission',
  '--output',
  '--persona',
  '--viewport',
]);
const normalizeReviewAllowedFlags = new Set([
  '--mission',
  '--persona',
  '--review-input',
  '--review-text',
  '--run-dir',
]);
const allowedViewports = new Set<ViewportPreset>(['desktop', 'tablet', 'mobile']);
const defaultMissionId = 'auth-enter-workspace';
const defaultOutputRoot = '../../artifacts/ux-agent-runs/local';
const missionCatalog: readonly MissionDefinition<string>[] = missions;
const personaCatalog: Record<string, PersonaDefinition> = personas;

export function parseRunnerOptions(args: string[]): RunnerOptions {
  const values = new Map<string, string>();

  for (let index = 0; index < args.length; index += 1) {
    const flag = args[index];
    if (!flag.startsWith('--')) {
      throw new Error(`Unexpected argument: ${flag}`);
    }

    if (!allowedFlags.has(flag)) {
      throw new Error(`Unknown option: ${flag}`);
    }

    if (flag === '--headless') {
      const nextValue = args[index + 1];
      if (!nextValue || nextValue.startsWith('--')) {
        values.set(flag, 'true');
        continue;
      }

      values.set(flag, nextValue);
      index += 1;
      continue;
    }

    const value = args[index + 1];
    if (!value || value.startsWith('--')) {
      throw new Error(`Missing value for ${flag}`);
    }

    values.set(flag, value);
    index += 1;
  }

  const baseUrl = values.get('--base-url');
  if (!baseUrl) {
    throw new Error('Missing required option: --base-url');
  }

  validateUrl(baseUrl);

  const missionId = values.get('--mission') ?? defaultMissionId;
  const { mission, persona } = resolveAuditContracts(
    missionId,
    values.get('--persona')
  );
  const viewportOverride = parseViewport(
    values.get('--viewport') ?? mission.viewport ?? persona.defaultViewport
  );
  const headless = parseHeadless(values.get('--headless') ?? 'true');

  return {
    baseUrl,
    missionFilter: mission.id,
    personaOverride: persona.id,
    viewportOverride,
    headless,
    outputRoot: values.get('--output') ?? defaultOutputRoot,
  };
}

export function parseNormalizeReviewOptions(args: string[]): NormalizeReviewOptions {
  const values = new Map<string, string>();

  for (let index = 0; index < args.length; index += 1) {
    const flag = args[index];
    if (!flag.startsWith('--')) {
      throw new Error(`Unexpected argument: ${flag}`);
    }

    if (!normalizeReviewAllowedFlags.has(flag)) {
      throw new Error(`Unknown normalize-review option: ${flag}`);
    }

    const value = args[index + 1];
    if (!value || value.startsWith('--')) {
      throw new Error(`Missing value for ${flag}`);
    }

    values.set(flag, value);
    index += 1;
  }

  const runDirectory = values.get('--run-dir');
  if (!runDirectory) {
    throw new Error('Missing required option: --run-dir');
  }

  if (values.has('--review-input') && values.has('--review-text')) {
    throw new Error('Use either --review-input or --review-text, not both');
  }

  return {
    runDirectory,
    missionFilter: values.get('--mission') ?? defaultMissionId,
    personaOverride: values.get('--persona'),
    reviewerOutputPath: values.get('--review-input'),
    reviewerOutput: values.get('--review-text'),
  };
}

export async function runAudit(options: RunnerOptions) {
  const { mission, persona } = resolveAuditContracts(
    options.missionFilter,
    options.personaOverride
  );
  const viewport = parseViewport(options.viewportOverride);
  const executeFixedMission = hasFixedMissionExecutor(mission.id);

  const result = await captureBrowserEvidence({
    baseUrl: options.baseUrl,
    missionId: mission.id,
    personaId: persona.id,
    missionGoal: mission.goal,
    viewport,
    headless: options.headless,
    outputRoot: options.outputRoot,
    captureScreenshots: false,
    includeSanitizedVisibleText: false,
    executeFixedMission,
  });
  const prompt = result.runDirectory
    ? await writeReviewPromptForMission({
        runDirectory: result.runDirectory,
        evidencePath: result.evidencePath,
        mission,
        persona,
      })
    : undefined;

  return {
    missionId: mission.id,
    personaId: persona.id,
    viewport,
    outputRoot: options.outputRoot,
    ...(prompt ? { reviewPromptPath: prompt.promptPath } : {}),
    ...result,
  };
}

export async function runNormalizeReview(options: NormalizeReviewOptions) {
  const { mission, persona } = resolveAuditContracts(
    options.missionFilter,
    options.personaOverride
  );
  const evidencePath = join(
    options.runDirectory,
    'missions',
    mission.id,
    'evidence.json'
  );
  const evidence = await readJson<MissionEvidence>(evidencePath);
  const runMetadata = await readOptionalJson<ReviewPromptRunMetadata>(
    join(options.runDirectory, 'run.json')
  );
  const reviewerOutput = options.reviewerOutputPath
    ? await readFile(options.reviewerOutputPath, 'utf8')
    : options.reviewerOutput ?? '';
  const report = await writeNormalizedReviewReport({
    runDirectory: options.runDirectory,
    runMetadata,
    mission,
    persona,
    evidence,
    reviewerOutput,
  });

  return {
    missionId: mission.id,
    personaId: persona.id,
    runDirectory: options.runDirectory,
    markdownPath: report.markdownPath,
    jsonPath: report.jsonPath,
    reviewStatus: report.summary.reviewStatus,
    findings: report.summary.findings.length,
    nextActionTickets: report.summary.nextActionTickets.length,
  };
}

async function main() {
  const args = process.argv.slice(2);
  if (args[0] === 'normalize-review') {
    const result = await runNormalizeReview(
      parseNormalizeReviewOptions(args.slice(1))
    );
    console.log(JSON.stringify(result, null, 2));
    return;
  }

  const options = parseRunnerOptions(args);
  const result = await runAudit(options);
  console.log(JSON.stringify(result, null, 2));
}

function parseViewport(value: string): ViewportPreset {
  if (!allowedViewports.has(value as ViewportPreset)) {
    throw new Error(`Unsupported viewport: ${value}`);
  }

  return value as ViewportPreset;
}

function parseHeadless(value: string): boolean {
  const normalized = value.trim().toLowerCase();
  if (normalized === 'true') {
    return true;
  }

  if (normalized === 'false') {
    return false;
  }

  throw new Error(`Invalid --headless: ${value}. Expected true or false.`);
}

export function resolveAuditContracts(missionId: string, personaId?: string) {
  const mission = missionCatalog.find((entry) => entry.id === missionId);
  if (!mission) {
    throw new Error(`Unknown mission: ${missionId}`);
  }

  const resolvedPersonaId = personaId ?? mission.personaId;
  const persona = personaCatalog[resolvedPersonaId];
  if (!persona) {
    throw new Error(`Unknown persona: ${resolvedPersonaId}`);
  }

  return { mission, persona };
}

function validateUrl(value: string) {
  try {
    new URL(value);
  } catch {
    throw new Error(`Invalid --base-url: ${value}`);
  }
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

function isMainModule() {
  const entryPoint = process.argv[1];
  return Boolean(entryPoint && import.meta.url === pathToFileURL(entryPoint).href);
}

if (isMainModule()) {
  main().catch((error: unknown) => {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  });
}
