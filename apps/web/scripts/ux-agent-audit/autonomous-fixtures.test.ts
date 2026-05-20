import { describe, expect, it } from 'vitest';

import {
  autonomousProductPaths,
  autonomousFixtureMissions,
  getAutonomousFixtureMission,
  resolveAutonomousMissionTargetPaths,
} from './autonomous-fixtures.ts';

describe('autonomous fixture-backed UX missions', () => {
  it('defines the minimum persona mission set for autonomous local review', () => {
    expect(autonomousFixtureMissions.map((mission) => mission.id)).toEqual([
      'fixture-first-study-setup',
      'fixture-wave-results-comparison',
      'fixture-questionnaire-scoring',
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
      expect(mission.fixtureProvenance).toContain('local product read model');
      expect(mission.entryPath).toBe('/app');
      expect(mission.personaProfile.id).toBe(mission.personaId);
      expect(mission.personaProfile.appGoal).toContain('/app');
      expect(mission.personaProfile.successCriteria.length).toBeGreaterThanOrEqual(4);
      expect(mission.targetFixtureCatalogIds.length).toBeGreaterThan(0);
      expect(mission.targetProductPaths.length).toBeGreaterThan(0);
      expect(resolveAutonomousMissionTargetPaths(mission.id)).not.toContain('/app/demo');
      expect(resolveAutonomousMissionTargetPaths(mission.id)).not.toContain('');
    }
  });
});
