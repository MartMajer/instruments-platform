import { pathToFileURL } from 'node:url';

import { captureBrowserEvidence } from './browser.ts';
import type { ViewportPreset } from './types.ts';

export interface RunnerOptions {
  baseUrl: string;
  missionFilter: string;
  personaOverride: string;
  viewportOverride: ViewportPreset;
  outputRoot: string;
}

const allowedViewports = new Set<ViewportPreset>(['desktop', 'tablet', 'mobile']);
const defaultMissionId = 'local-page-snapshot';
const defaultPersonaId = 'first-time-researcher';
const defaultViewport: ViewportPreset = 'desktop';
const defaultOutputRoot = '../../artifacts/ux-agent-runs/local';

export function parseRunnerOptions(args: string[]): RunnerOptions {
  const values = new Map<string, string>();

  for (let index = 0; index < args.length; index += 1) {
    const flag = args[index];
    if (!flag.startsWith('--')) {
      throw new Error(`Unexpected argument: ${flag}`);
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

  const viewportOverride = parseViewport(values.get('--viewport') ?? defaultViewport);

  return {
    baseUrl,
    missionFilter: values.get('--mission') ?? defaultMissionId,
    personaOverride: values.get('--persona') ?? defaultPersonaId,
    viewportOverride,
    outputRoot: values.get('--output') ?? defaultOutputRoot,
  };
}

export async function runAudit(options: RunnerOptions) {
  const result = await captureBrowserEvidence({
    baseUrl: options.baseUrl,
    missionId: options.missionFilter,
    personaId: options.personaOverride,
    missionGoal: 'Capture an initial browser evidence snapshot for a UX audit mission.',
    viewport: options.viewportOverride,
    outputRoot: options.outputRoot,
  });

  return {
    missionId: options.missionFilter,
    personaId: options.personaOverride,
    viewport: options.viewportOverride,
    outputRoot: options.outputRoot,
    ...result,
  };
}

async function main() {
  const options = parseRunnerOptions(process.argv.slice(2));
  const result = await runAudit(options);
  console.log(JSON.stringify(result, null, 2));
}

function parseViewport(value: string): ViewportPreset {
  if (!allowedViewports.has(value as ViewportPreset)) {
    throw new Error(`Unsupported viewport: ${value}`);
  }

  return value as ViewportPreset;
}

function validateUrl(value: string) {
  try {
    new URL(value);
  } catch {
    throw new Error(`Invalid --base-url: ${value}`);
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
