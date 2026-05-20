import { listFixtureScenarios } from '../../src/lib/product/fixtures.ts';

import { autonomousProductPaths } from './autonomous-product-read-models.ts';
import { getGoalPersonaProfile, type GoalPersonaProfile } from './persona-goals.ts';
import type { PersonaId } from './personas.ts';
import type { AutonomousDataMode, ViewportPreset } from './types.ts';

export { autonomousProductPaths };

export interface AutonomousFixtureMission {
  id: string;
  personaId: PersonaId;
  personaProfile: GoalPersonaProfile;
  goal: string;
  viewport: ViewportPreset;
  maxSteps: number;
  entryPath: string;
  targetFixtureCatalogIds: string[];
  targetProductPaths: string[];
  reviewFocus: string[];
  fixtureProvenance: string;
  supportedDataModes: AutonomousDataMode[];
  mutationPlan?: AutonomousMutationPlan;
  localOnly: true;
}

export type AutonomousMutationPlan = {
  kind: 'create-study';
  fieldLabel: string;
  buttonText: string;
  studyNamePrefix: string;
};

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
      autonomousProductPaths.longitudinalSampleResults,
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
    targetFixtureCatalogIds: [
      'setup:launch-ready',
      'operations:launched-anonymous',
      'reports:visible-aggregate',
    ],
    targetProductPaths: [
      autonomousProductPaths.completedSampleSetup,
      autonomousProductPaths.completedSampleCollect,
      autonomousProductPaths.completedSampleResults,
    ],
    reviewFocus: ['questionnaire authoring', 'scoring rule readiness', 'client-ready wording'],
  }),
  mission({
    id: 'fullstack-workspace-inspection',
    personaId: 'first-time-researcher',
    goal: 'Autonomously inspect the real local full-stack workspace entry and studies list without product read-model mocks.',
    viewport: 'desktop',
    maxSteps: 4,
    targetFixtureCatalogIds: [],
    targetProductPaths: ['/app/campaign-series'],
    reviewFocus: ['full-stack local auth/data readiness', 'studies entry clarity'],
    supportedDataModes: ['fullstack'],
    fixtureProvenance:
      'Live local full-stack app/API/database state; no product read-model mocks are installed by the harness.',
  }),
  mission({
    id: 'fullstack-create-study',
    personaId: 'first-time-researcher',
    goal: 'Autonomously create one real local full-stack study through the visible Studies page controls.',
    viewport: 'desktop',
    maxSteps: 6,
    targetFixtureCatalogIds: [],
    targetProductPaths: ['/app/campaign-series'],
    reviewFocus: ['full-stack local mutation', 'study creation clarity'],
    supportedDataModes: ['fullstack'],
    mutationPlan: {
      kind: 'create-study',
      fieldLabel: 'Study name',
      buttonText: 'Create study',
      studyNamePrefix: 'UXA full-stack mutation',
    },
    fixtureProvenance:
      'Live local full-stack app/API/database mutation; no product read-model mocks are installed by the harness.',
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

export function resolveAutonomousMissionForDataMode(
  missionId: string,
  dataMode: AutonomousDataMode
) {
  const mission = getAutonomousFixtureMission(missionId);
  if (!mission) {
    throw new Error(`Unknown autonomous fixture mission: ${missionId}`);
  }

  if (!mission.supportedDataModes.includes(dataMode)) {
    throw new Error(
      `Autonomous mission ${missionId} does not support ${dataMode} data mode.`
    );
  }

  return mission;
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
  supportedDataModes?: AutonomousDataMode[];
  mutationPlan?: AutonomousMutationPlan;
  fixtureProvenance?: string;
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
    personaProfile: getGoalPersonaProfile(options.personaId),
    entryPath: '/app',
    fixtureProvenance:
      options.fixtureProvenance ??
      `Seeded local product read model backed by ${scenarios.length} demo fixture catalog scenario(s).`,
    supportedDataModes: options.supportedDataModes ?? ['fixture'],
    localOnly: true,
  };
}
