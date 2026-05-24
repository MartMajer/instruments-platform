import { describe, expect, it } from 'vitest';

import { getGoalPersonaProfile, goalPersonaProfiles } from './persona-goals.ts';

describe('goal-based UX persona profiles', () => {
  it('defines concrete app goals for every autonomous review persona', () => {
    expect(Object.keys(goalPersonaProfiles).sort()).toEqual([
      'busy-professor',
      'first-time-researcher',
      'osh-consultant',
      'work-ergonomics-specialist',
    ]);

    for (const profile of Object.values(goalPersonaProfiles)) {
      expect(profile.role.length).toBeGreaterThan(10);
      expect(profile.appGoal).toContain('/app');
      expect(profile.successCriteria.length).toBeGreaterThanOrEqual(4);
      expect(profile.confusionTriggers.length).toBeGreaterThanOrEqual(4);
      expect(profile.reviewerInstructions.join(' ')).toContain('Complain');
    }
  });

  it('gives each persona distinct domain expectations', () => {
    expect(getGoalPersonaProfile('first-time-researcher')).toEqual(
      expect.objectContaining({
        name: 'Dr. Ana Kovac',
        appGoal: expect.stringContaining('first study'),
      })
    );
    expect(getGoalPersonaProfile('osh-consultant')).toEqual(
      expect.objectContaining({
        name: 'Marko Horvat',
        appGoal: expect.stringContaining('client-ready'),
      })
    );
    expect(getGoalPersonaProfile('busy-professor')).toEqual(
      expect.objectContaining({
        name: 'Prof. Ivana Radic',
        appGoal: expect.stringContaining('Wave 1'),
      })
    );
    expect(getGoalPersonaProfile('work-ergonomics-specialist')).toEqual(
      expect.objectContaining({
        name: 'Nina Peric',
        appGoal: expect.stringContaining('ergonomics study'),
      })
    );
  });
});
