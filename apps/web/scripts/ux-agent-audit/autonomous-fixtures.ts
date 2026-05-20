import { listFixtureScenarios } from '../../src/lib/product/fixtures.ts';

import { autonomousProductPaths } from './autonomous-product-read-models.ts';
import type { PersonaId } from './personas.ts';
import type { ViewportPreset } from './types.ts';

export { autonomousProductPaths };

export interface AutonomousFixtureMission {
  id: string;
  personaId: PersonaId;
  goal: string;
  viewport: ViewportPreset;
  maxSteps: number;
  entryPath: string;
  targetFixtureCatalogIds: string[];
  targetProductPaths: string[];
  reviewFocus: string[];
  fixtureProvenance: string;
  localOnly: true;
}

const fixtureScenarios = listFixtureScenarios();
const fixtureScenarioByKey = new Map(
  fixtureScenarios.map((scenario) => [`${scenario.groupId}:${scenario.id}`, scenario])
);

export const autonomousFixtureMissions = [
  mission({
    id: 'fixture-first-study-setup',
    personaId: 'first-time-researcher',
    goal: 'Autonomously inspect first-study setup states and complain when the next setup action is unclear.',
    viewport: 'desktop',
    maxSteps: 8,
    targetFixtureCatalogIds: ['setup:empty', 'setup:launch-ready', 'setup:blocked'],
    targetProductPaths: [
      autonomousProductPaths.ownStudySetup,
      autonomousProductPaths.setupSampleSetup,
      autonomousProductPaths.completedSampleSetup,
    ],
    reviewFocus: ['study setup', 'questionnaire setup', 'launch readiness blockers'],
  }),
  mission({
    id: 'fixture-wave-results-comparison',
    personaId: 'busy-professor',
    goal: 'Autonomously inspect repeated-wave, results, and export states for change-over-time comprehension.',
    viewport: 'desktop',
    maxSteps: 8,
    targetFixtureCatalogIds: [
      'waves:two-waves-linked',
      'reports:visible-aggregate',
      'exports:downloadable',
    ],
    targetProductPaths: [
      autonomousProductPaths.longitudinalSampleWaves,
      autonomousProductPaths.completedSampleResults,
      autonomousProductPaths.exports,
    ],
    reviewFocus: ['wave comparison', 'results readiness', 'downloadable export confidence'],
  }),
  mission({
    id: 'fixture-questionnaire-scoring',
    personaId: 'osh-consultant',
    goal: 'Autonomously inspect questionnaire and scoring readiness states for a consultant preparing a client study.',
    viewport: 'tablet',
    maxSteps: 8,
    targetFixtureCatalogIds: ['setup:empty', 'setup:blocked', 'reports:missing-scores'],
    targetProductPaths: [
      autonomousProductPaths.ownStudySetup,
      autonomousProductPaths.completedSampleSetup,
      autonomousProductPaths.completedSampleResults,
    ],
    reviewFocus: ['questionnaire authoring', 'scoring rule readiness', 'client-ready wording'],
  }),
] satisfies AutonomousFixtureMission[];

export function getAutonomousFixtureMission(id: string) {
  return autonomousFixtureMissions.find((mission) => mission.id === id);
}

export function resolveAutonomousMissionTargetPaths(missionId: string) {
  const mission = getAutonomousFixtureMission(missionId);
  if (!mission) {
    throw new Error(`Unknown autonomous fixture mission: ${missionId}`);
  }

  return mission.targetProductPaths;
}

function mission(options: {
  id: string;
  personaId: PersonaId;
  goal: string;
  viewport: ViewportPreset;
  maxSteps: number;
  targetFixtureCatalogIds: string[];
  targetProductPaths: string[];
  reviewFocus: string[];
}): AutonomousFixtureMission {
  const scenarios = options.targetFixtureCatalogIds.map((scenarioId) => {
    const scenario = fixtureScenarioByKey.get(scenarioId);
    if (!scenario) {
      throw new Error(`Unknown product fixture scenario: ${scenarioId}`);
    }

    return scenario;
  });

  return {
    ...options,
    entryPath: '/app',
    fixtureProvenance:
      `Seeded local product read model backed by ${scenarios.length} demo fixture catalog scenario(s).`,
    localOnly: true,
  };
}
