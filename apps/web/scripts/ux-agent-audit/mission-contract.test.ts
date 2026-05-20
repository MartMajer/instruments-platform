import { describe, expect, it } from 'vitest';

import { missions } from './missions';
import { personas } from './personas';

describe('UX audit mission contracts', () => {
  it('defines missions with executable audit contracts', () => {
    expect(missions.length).toBeGreaterThan(0);

    const missionIds = missions.map((mission) => mission.id);
    expect(new Set(missionIds).size).toBe(missionIds.length);

    for (const mission of missions) {
      expect(mission.id).toMatch(/^[a-z0-9-]+$/);
      expect(mission.goal.length).toBeGreaterThan(20);
      expect(mission.maxSteps).toBeGreaterThan(0);
      expect(personas[mission.personaId]).toBeDefined();
      expect(mission.successCriteria.length).toBeGreaterThan(0);

      for (const criterion of mission.successCriteria) {
        expect(criterion.trim().length).toBeGreaterThan(0);
      }
    }
  });

  it('defines personas keyed by id with review guidance', () => {
    expect(Object.keys(personas).length).toBeGreaterThan(0);

    for (const [personaId, persona] of Object.entries(personas)) {
      expect(persona.id).toBe(personaId);
      expect(persona.displayName.trim().length).toBeGreaterThan(0);
      expect(persona.reviewGuidance.length).toBeGreaterThan(0);

      for (const guidance of persona.reviewGuidance) {
        expect(guidance.trim().length).toBeGreaterThan(0);
      }
    }
  });
});