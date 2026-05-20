import { pathToFileURL } from 'node:url';

import { captureBrowserEvidence } from './browser.ts';
import { hasFixedMissionExecutor } from './mission-executor.ts';
import { missions } from './missions.ts';
import { personas } from './personas.ts';
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

const allowedFlags = new Set([
  '--base-url',
  '--headless',
  '--mission',
  '--output',
  '--persona',
  '--viewport',
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
    captureScreenshots: executeFixedMission,
    includeSanitizedVisibleText: executeFixedMission,
    executeFixedMission,
  });

  return {
    missionId: mission.id,
    personaId: persona.id,
    viewport,
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
