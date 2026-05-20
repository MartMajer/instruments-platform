import { describe, expect, it } from 'vitest';

import {
  buildRealisticResponseSimulation,
  getRealisticAuditCase,
} from './realistic-cases.ts';

describe('realistic UX audit cases', () => {
  it('builds deterministic synthetic responses for the OSH warehouse case', () => {
    const auditCase = getRealisticAuditCase('osh-warehouse-workload-recovery-pulse');
    if (!auditCase) {
      throw new Error('missing OSH warehouse case');
    }

    const simulation = buildRealisticResponseSimulation(auditCase);

    expect(simulation).toEqual(
      expect.objectContaining({
        campaignName: 'Baseline warehouse pulse - May 2026',
        seedMode: 'simulated-local-evidence',
        respondentCount: 24,
        completedResponseCount: 21,
        omittedResponseCount: 3,
      })
    );
    expect(simulation.responses).toHaveLength(21);
    expect(simulation.responses[0]).toEqual(
      expect.objectContaining({
        respondentKey: 'sim-001',
        segment: 'Day shift pickers',
        completed: true,
      })
    );
    expect(Object.keys(simulation.responses[0]?.answers ?? {})).toHaveLength(10);
    expect(simulation.segmentSummaries).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ segment: 'Night shift pickers', completed: 7 }),
        expect.objectContaining({ segment: 'Forklift and loading team', completed: 5 }),
      ])
    );
    expect(simulation.dimensionRisk).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          dimension: 'Recovery',
          riskLevel: expect.stringMatching(/moderate|high/),
        }),
        expect.objectContaining({
          dimension: 'Musculoskeletal strain',
          riskLevel: expect.stringMatching(/moderate|high/),
        }),
      ])
    );
  });

  it('defines a deterministic repeated-wave academic professor case', () => {
    const auditCase = getRealisticAuditCase('academic-workload-recovery-followup');
    if (!auditCase) {
      throw new Error('missing academic repeated-wave case');
    }

    const simulation = buildRealisticResponseSimulation(auditCase);

    expect(auditCase.waveCampaignNames).toEqual([
      'Baseline academic workload survey - May 2026',
      'Follow-up academic workload survey - June 2026',
    ]);
    expect(auditCase.questions).toHaveLength(10);
    expect(simulation).toEqual(
      expect.objectContaining({
        campaignName: 'Baseline academic workload survey - May 2026',
        respondentCount: 18,
        completedResponseCount: 16,
        omittedResponseCount: 2,
      })
    );
    expect(simulation.dimensionRisk).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          dimension: 'Teaching load',
          riskLevel: expect.stringMatching(/moderate|high/),
        }),
        expect.objectContaining({
          dimension: 'Research continuity',
          riskLevel: expect.stringMatching(/moderate|high/),
        }),
      ])
    );
  });
});
