import { describe, expect, it } from 'vitest';

import {
  autonomousProductPaths,
  autonomousFixtureMissions,
  getAutonomousFixtureMission,
  resolveAutonomousMissionForDataMode,
  resolveAutonomousMissionTargetPaths,
} from './autonomous-fixtures.ts';

describe('autonomous fixture-backed UX missions', () => {
  it('defines the minimum persona mission set for autonomous local review', () => {
    expect(autonomousFixtureMissions.map((mission) => mission.id)).toEqual([
      'fixture-first-study-setup',
      'fixture-wave-results-comparison',
      'fixture-questionnaire-scoring',
      'fullstack-workspace-inspection',
      'fullstack-create-study',
      'fullstack-osh-warehouse-pulse',
      'fullstack-academic-repeated-wave-review',
    ]);
    expect(getAutonomousFixtureMission('fixture-first-study-setup')).toEqual(
      expect.objectContaining({
        personaId: 'first-time-researcher',
        entryPath: '/app',
        viewport: 'desktop',
      })
    );
  });

  it('resolves target paths through normal app product surfaces', () => {
    expect(resolveAutonomousMissionTargetPaths('fixture-first-study-setup')).toEqual([
      autonomousProductPaths.ownStudySetup,
      autonomousProductPaths.setupSampleSetup,
      autonomousProductPaths.completedSampleSetup,
    ]);
    expect(resolveAutonomousMissionTargetPaths('fixture-wave-results-comparison')).toEqual([
      autonomousProductPaths.longitudinalSampleWaves,
      autonomousProductPaths.longitudinalSampleResults,
      autonomousProductPaths.exports,
    ]);
    expect(resolveAutonomousMissionTargetPaths('fixture-questionnaire-scoring')).toEqual([
      autonomousProductPaths.completedSampleSetup,
      autonomousProductPaths.completedSampleCollect,
      autonomousProductPaths.completedSampleResults,
    ]);
  });

  it('keeps autonomous missions local-only while avoiding demo as the primary target', () => {
    for (const mission of autonomousFixtureMissions) {
      expect(mission.localOnly).toBe(true);
      expect(mission.entryPath).toBe('/app');
      expect(mission.personaProfile.id).toBe(mission.personaId);
      expect(mission.personaProfile.appGoal).toContain('/app');
      expect(mission.personaProfile.successCriteria.length).toBeGreaterThanOrEqual(4);
      expect(mission.targetProductPaths.length).toBeGreaterThan(0);
      expect(resolveAutonomousMissionTargetPaths(mission.id)).not.toContain('/app/demo');
      expect(resolveAutonomousMissionTargetPaths(mission.id)).not.toContain('');
    }

    for (const mission of autonomousFixtureMissions.filter((entry) =>
      entry.id.startsWith('fixture-')
    )) {
      expect(mission.supportedDataModes).toContain('fixture');
      expect(mission.fixtureProvenance).toContain('local product read model');
      expect(mission.targetFixtureCatalogIds.length).toBeGreaterThan(0);
    }
  });

  it('fails closed when a fixture-only mission is requested in fullstack mode', () => {
    expect(() =>
      resolveAutonomousMissionForDataMode('fixture-first-study-setup', 'fullstack')
    ).toThrow('does not support fullstack data mode');
    expect(resolveAutonomousMissionForDataMode('fixture-first-study-setup', 'fixture')).toEqual(
      expect.objectContaining({ id: 'fixture-first-study-setup' })
    );
  });

  it('defines a local fullstack mission that refuses fixture data mode', () => {
    expect(resolveAutonomousMissionForDataMode('fullstack-workspace-inspection', 'fullstack')).toEqual(
      expect.objectContaining({
        id: 'fullstack-workspace-inspection',
        supportedDataModes: ['fullstack'],
        fixtureProvenance: expect.stringContaining('no product read-model mocks'),
      })
    );
    expect(() =>
      resolveAutonomousMissionForDataMode('fullstack-workspace-inspection', 'fixture')
    ).toThrow('does not support fixture data mode');
  });

  it('defines a local fullstack mutation mission for creating a study through the UI', () => {
    expect(resolveAutonomousMissionForDataMode('fullstack-create-study', 'fullstack')).toEqual(
      expect.objectContaining({
        id: 'fullstack-create-study',
        supportedDataModes: ['fullstack'],
        targetProductPaths: ['/app/campaign-series'],
        mutationPlan: expect.objectContaining({
          kind: 'create-study',
          fieldLabel: 'Study name',
          buttonText: 'Create study',
        }),
      })
    );
    expect(() =>
      resolveAutonomousMissionForDataMode('fullstack-create-study', 'fixture')
    ).toThrow('does not support fixture data mode');
  });

  it('defines a realistic OSH warehouse fullstack mission with case evidence', () => {
    const mission = resolveAutonomousMissionForDataMode(
      'fullstack-osh-warehouse-pulse',
      'fullstack'
    );

    expect(mission).toEqual(
      expect.objectContaining({
        id: 'fullstack-osh-warehouse-pulse',
        personaId: 'osh-consultant',
        supportedDataModes: ['fullstack'],
        realisticCase: expect.objectContaining({
          id: 'osh-warehouse-workload-recovery-pulse',
          studyName: 'Warehouse workload and recovery pulse',
          instrumentName: 'Warehouse workload and recovery instrument',
          campaignName: 'Baseline warehouse pulse - May 2026',
        }),
        mutationPlan: expect.objectContaining({
          kind: 'create-study',
          studyName: 'Warehouse workload and recovery pulse',
          setupInstrument: expect.objectContaining({
            fieldLabel: 'Instrument name',
            value: 'Warehouse workload and recovery instrument',
            buttonText: 'Save instrument',
          }),
        }),
        fullstackSeedPlan: expect.objectContaining({
          kind: 'seed-realistic-campaign-results',
        }),
      })
    );
    expect(mission.realisticCase?.questions).toHaveLength(10);
    expect(mission.realisticCase?.syntheticResponses).toEqual(
      expect.objectContaining({
        respondentCount: 24,
        completionCount: 21,
      })
    );
    expect(() =>
      resolveAutonomousMissionForDataMode('fullstack-osh-warehouse-pulse', 'fixture')
    ).toThrow('does not support fixture data mode');
  });

  it('defines a realistic repeated-wave busy-professor fullstack mission', () => {
    const mission = resolveAutonomousMissionForDataMode(
      'fullstack-academic-repeated-wave-review',
      'fullstack'
    );

    expect(mission).toEqual(
      expect.objectContaining({
        id: 'fullstack-academic-repeated-wave-review',
        personaId: 'busy-professor',
        supportedDataModes: ['fullstack'],
        realisticCase: expect.objectContaining({
          id: 'academic-workload-recovery-followup',
          studyName: 'Academic workload and recovery follow-up',
          campaignName: 'Baseline academic workload survey - May 2026',
          waveCampaignNames: [
            'Baseline academic workload survey - May 2026',
            'Follow-up academic workload survey - June 2026',
          ],
        }),
        mutationPlan: expect.objectContaining({
          kind: 'create-study',
          studyName: 'Academic workload and recovery follow-up',
        }),
        fullstackSeedPlan: expect.objectContaining({
          kind: 'seed-realistic-repeated-wave-results',
        }),
      })
    );
    expect(mission.reviewFocus).toContain('change-over-time comparison readiness');
    expect(() =>
      resolveAutonomousMissionForDataMode('fullstack-academic-repeated-wave-review', 'fixture')
    ).toThrow('does not support fixture data mode');
  });
});
