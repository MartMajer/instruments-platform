import { listFixtureScenarios } from '../../src/lib/product/fixtures.ts';

import { autonomousProductPaths } from './autonomous-product-read-models.ts';
import { getGoalPersonaProfile, type GoalPersonaProfile } from './persona-goals.ts';
import type { PersonaId } from './personas.ts';
import { getRealisticAuditCase, type RealisticAuditCase } from './realistic-cases.ts';
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
  realisticCase?: RealisticAuditCase;
  mutationPlan?: AutonomousMutationPlan;
  fullstackSeedPlan?: AutonomousFullstackSeedPlan;
  localOnly: true;
}

export type AutonomousMutationPlan = {
  kind: 'create-study';
  fieldLabel: string;
  buttonText: string;
  buttonTextAlternatives?: string[];
  studyNamePrefix?: string;
  studyName?: string;
  setupInstrument?: {
    fieldLabel: string;
    fieldLabelAlternatives?: string[];
    value: string;
    buttonText: string;
    buttonTextAlternatives?: string[];
    versionFieldLabel?: string;
    versionValue?: string;
  };
};

export type AutonomousFullstackSeedPlan = {
  kind: 'seed-realistic-campaign-results' | 'seed-realistic-repeated-wave-results';
};

const fixtureScenarios = listFixtureScenarios();
const fixtureScenarioByKey = new Map(
  fixtureScenarios.map((scenario) => [`${scenario.groupId}:${scenario.id}`, scenario])
);
const oshWarehouseCase = requireRealisticAuditCase('osh-warehouse-workload-recovery-pulse');
const academicRepeatedWaveCase = requireRealisticAuditCase('academic-workload-recovery-followup');

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
  mission({
    id: 'fullstack-osh-warehouse-pulse',
    personaId: 'osh-consultant',
    goal:
      'Autonomously create a realistic local OSH warehouse workload study and continue into the first setup action with believable case content.',
    viewport: 'desktop',
    maxSteps: 8,
    targetFixtureCatalogIds: [],
    targetProductPaths: ['/app/campaign-series'],
    reviewFocus: [
      'realistic custom study creation',
      'workplace OSH instrument setup',
      'synthetic response plan readiness',
      'consultant-ready wording',
    ],
    supportedDataModes: ['fullstack'],
    realisticCaseId: oshWarehouseCase.id,
    mutationPlan: {
      kind: 'create-study',
      fieldLabel: 'Study name',
      buttonText: 'Create study',
      studyName: oshWarehouseCase.studyName,
      setupInstrument: {
        fieldLabel: 'Instrument name',
        value: oshWarehouseCase.instrumentName,
        buttonText: 'Save instrument',
      },
    },
    fullstackSeedPlan: {
      kind: 'seed-realistic-campaign-results',
    },
    fixtureProvenance:
      'Live local full-stack app/API/database mutation using a synthetic OSH warehouse case profile; no product read-model mocks are installed by the harness.',
  }),
  mission({
    id: 'fullstack-academic-repeated-wave-review',
    personaId: 'busy-professor',
    goal:
      'Autonomously create a realistic local academic workload follow-up study, seed two anonymous-longitudinal waves, and inspect whether Waves and Results explain comparison readiness.',
    viewport: 'desktop',
    maxSteps: 8,
    targetFixtureCatalogIds: [],
    targetProductPaths: ['/app/campaign-series'],
    reviewFocus: [
      'repeated-wave setup comprehension',
      'anonymous longitudinal participant-code expectations',
      'change-over-time comparison readiness',
      'export and analysis readiness after two closed waves',
    ],
    supportedDataModes: ['fullstack'],
    realisticCaseId: academicRepeatedWaveCase.id,
    mutationPlan: {
      kind: 'create-study',
      fieldLabel: 'Study name',
      buttonText: 'Create study',
      studyName: academicRepeatedWaveCase.studyName,
      setupInstrument: {
        fieldLabel: 'Instrument name',
        value: academicRepeatedWaveCase.instrumentName,
        buttonText: 'Save instrument',
      },
    },
    fullstackSeedPlan: {
      kind: 'seed-realistic-repeated-wave-results',
    },
    fixtureProvenance:
      'Live local full-stack app/API/database mutation using a synthetic busy-professor repeated-wave case profile; no product read-model mocks are installed by the harness.',
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
  realisticCaseId?: string;
  mutationPlan?: AutonomousMutationPlan;
  fullstackSeedPlan?: AutonomousFullstackSeedPlan;
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
    realisticCase: options.realisticCaseId
      ? requireRealisticAuditCase(options.realisticCaseId)
      : undefined,
    entryPath: '/app',
    fixtureProvenance:
      options.fixtureProvenance ??
      `Seeded local product read model backed by ${scenarios.length} demo fixture catalog scenario(s).`,
    supportedDataModes: options.supportedDataModes ?? ['fixture'],
    localOnly: true,
  };
}

function requireRealisticAuditCase(id: string) {
  const auditCase = getRealisticAuditCase(id);
  if (!auditCase) {
    throw new Error(`Unknown realistic audit case: ${id}`);
  }

  return auditCase;
}
