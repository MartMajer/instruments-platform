import { describe, expect, it } from 'vitest';

import {
  buildScriptedFixturePersonaActor,
  runAutonomousFixtureMission,
  type AutonomousPageAdapter,
} from './autonomous-loop.ts';
import type { AutonomousFixtureMission } from './autonomous-fixtures.ts';
import type { MissionPageSnapshot } from './mission-executor.ts';
import { getGoalPersonaProfile } from './persona-goals.ts';

describe('autonomous UX persona loop', () => {
  it('drives visible fixture links without owner clicks', async () => {
    const mission = missionFixture({
      targetProductPaths: ['/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup'],
    });
    const adapter = fakeAdapter([
      cockpitSnapshot('New team study', '/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup'),
      pageSnapshot('/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup', 'Study setup', 'Ready to launch'),
    ]);

    const result = await runAutonomousFixtureMission(
      adapter,
      mission,
      buildScriptedFixturePersonaActor(mission)
    );

    expect(result.status).toBe('completed');
    expect(result.steps.map((step) => step.action)).toEqual([
      'Opened autonomous product mission entry route /app.',
      'Clicked visible link "New team study".',
      'Stopped autonomous product mission: inspected all target product paths.',
    ]);
    expect(result.observations).toEqual(
      expect.objectContaining({
        autonomousMode: true,
        localOnly: true,
        personaGoal: expect.objectContaining({
          name: 'Dr. Ana Kovac',
          appGoal: expect.stringContaining('first study'),
        }),
        personaGoalAssessment: expect.objectContaining({
          status: 'completed',
          checkedCriteriaCount: 5,
        }),
        visitedProductPaths: ['/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup'],
      })
    );
    expect(result.reviewerOutput).toContain('"personaGoal"');
    expect(result.reviewerOutput).toContain('"successCriteria"');
  });

  it('grades persona criteria as observed or unclear instead of claiming route visitation proves success', async () => {
    const mission = missionFixture({
      personaProfile: getGoalPersonaProfile('osh-consultant'),
      personaId: 'osh-consultant',
      targetProductPaths: [
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
        '/app/campaign-series/019ad5b6-7f00-7000-8a00-000000000102/operations',
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/reports',
      ],
    });
    const adapter = fakeAdapter([
      cockpitSnapshot('Quarterly pulse setup', '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup'),
      pageSnapshot(
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
        'Setup progress',
        'Setup progress 5 of 5 required steps complete. Recipients are configured.'
      ),
      pageSnapshot(
        '/app/campaign-series/019ad5b6-7f00-7000-8a00-000000000102/operations',
        'Collection',
        'Collection is live. Recipient roster is ready. Waiting for responses.'
      ),
      pageSnapshot(
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/reports',
        'Results',
        'Client export is ready to download.'
      ),
    ]);

    const result = await runAutonomousFixtureMission(
      adapter,
      mission,
      buildScriptedFixturePersonaActor(mission)
    );
    const assessment = result.observations.personaGoalAssessment as {
      successCriteria: Array<{ criterion: string; status: string; evidence: string }>;
    };

    expect(assessment.successCriteria).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          criterion: expect.stringContaining('Collection state'),
          status: 'observed',
          evidence: expect.stringContaining('Collection'),
        }),
      ])
    );
    for (const criterion of assessment.successCriteria) {
      expect(['observed', 'unclear', 'not_observed']).toContain(criterion.status);
      expect(criterion.evidence).not.toContain('Mission visited all configured product surfaces');
    }
  });

  it('prefers route-specific transcript sections over generic workspace chrome for criterion evidence', async () => {
    const mission = missionFixture({
      personaProfile: getGoalPersonaProfile('osh-consultant'),
      personaId: 'osh-consultant',
      targetProductPaths: [
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/operations',
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/reports',
      ],
    });
    const adapter = fakeAdapter([
      cockpitSnapshot(
        'Quarterly pulse setup',
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
        'setup progress recipients collection state draft live closed waiting responses results export client handoff'
      ),
      pageSnapshot(
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
        'Setup route',
        'Setup progress shows remaining work and recipient readiness.'
      ),
      pageSnapshot(
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/operations',
        'Collection route',
        'Collection state is live and waiting for responses.'
      ),
      pageSnapshot(
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/reports',
        'Reports route',
        'Results export is ready for client handoff.'
      ),
    ]);

    const result = await runAutonomousFixtureMission(
      adapter,
      mission,
      buildScriptedFixturePersonaActor(mission)
    );
    const assessment = result.observations.personaGoalAssessment as {
      successCriteria: Array<{ criterion: string; status: string; evidence: string }>;
    };
    const collectionCriterion = assessment.successCriteria.find((entry) =>
      entry.criterion.includes('Collection state')
    );
    const exportCriterion = assessment.successCriteria.find((entry) =>
      entry.criterion.includes('Results/export')
    );

    expect(collectionCriterion).toEqual(
      expect.objectContaining({
        status: 'observed',
        evidence: expect.stringContaining('Collection route'),
      })
    );
    expect(exportCriterion).toEqual(
      expect.objectContaining({
        status: 'observed',
        evidence: expect.stringContaining('Reports route'),
      })
    );
    expect(collectionCriterion?.evidence).not.toContain('Study cockpit');
    expect(exportCriterion?.evidence).not.toContain('Study cockpit');
  });

  it('does not cite instrument-only setup text as proof of questionnaire and scoring guidance', async () => {
    const mission = missionFixture({
      targetProductPaths: ['/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup'],
    });
    const adapter = fakeAdapter([
      cockpitSnapshot('New team study', '/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup'),
      pageSnapshot(
        '/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup',
        'Setup route',
        'Study setup',
        [
          'Current setup step Instrument requirements before building the questionnaire in researcher language.',
          'Questionnaire requirements are explained. Results setup explains how answers become study results.',
        ]
      ),
    ]);

    const result = await runAutonomousFixtureMission(
      adapter,
      mission,
      buildScriptedFixturePersonaActor(mission)
    );
    const assessment = result.observations.personaGoalAssessment as {
      successCriteria: Array<{ criterion: string; status: string; evidence: string }>;
    };
    const questionnaireCriterion = assessment.successCriteria.find((entry) =>
      entry.criterion.includes('Questionnaire and scoring')
    );

    expect(questionnaireCriterion).toEqual(
      expect.objectContaining({
        status: 'observed',
        evidence: expect.stringContaining('Results setup explains'),
      })
    );
    expect(questionnaireCriterion?.evidence).not.toContain('Instrument requirements');
  });

  it('continues through normal product prerequisite states instead of filing false tickets', async () => {
    const mission = missionFixture({
      targetProductPaths: [
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/reports',
      ],
    });
    const adapter = fakeAdapter([
      cockpitSnapshot('Quarterly pulse setup', '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup'),
      pageSnapshot(
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
        'Blocked setup',
        'Missing scoring rule. Launch readiness is blocked.'
      ),
      pageSnapshot(
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/reports',
        'Results',
        'Results overview Export file ready.'
      ),
    ]);

    const result = await runAutonomousFixtureMission(
      adapter,
      mission,
      buildScriptedFixturePersonaActor(mission)
    );

    expect(result.status).toBe('completed');
    expect(result.personaFindings).toEqual([]);
    expect(result.observations).toEqual(
      expect.objectContaining({
        visitedProductPaths: [
          '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
          '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/reports',
        ],
      })
    );
  });

  it('complains on hard product route failures that block UX review', async () => {
    const mission = missionFixture({
      targetProductPaths: ['/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup'],
    });
    const adapter = fakeAdapter([
      cockpitSnapshot('Quarterly pulse setup', '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup'),
      pageSnapshot(
        '/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup',
        'Unavailable',
        'Campaign series unavailable. API request failed with status 404.'
      ),
    ]);

    const result = await runAutonomousFixtureMission(
      adapter,
      mission,
      buildScriptedFixturePersonaActor(mission)
    );

    expect(result.status).toBe('blocked');
    expect(result.personaFindings[0]).toEqual(
      expect.objectContaining({
        severity: 'blocker',
        observedConfusion: expect.stringContaining('Campaign series unavailable'),
      })
    );
  });

  it('complains when the app product shell never leaves workspace access loading', async () => {
    const mission = missionFixture({
      targetProductPaths: ['/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup'],
    });
    const adapter = fakeAdapter([
      pageSnapshot(
        '/app',
        'Workspace',
        'Workspace access Checking workspace access Confirming your signed-in account and workspace membership.'
      ),
      pageSnapshot(
        '/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup',
        'Workspace',
        'Workspace access Checking workspace access Confirming your signed-in account and workspace membership.'
      ),
    ]);

    const result = await runAutonomousFixtureMission(
      adapter,
      mission,
      buildScriptedFixturePersonaActor(mission)
    );

    expect(result.status).toBe('blocked');
    expect(result.personaFindings[0]).toEqual(
      expect.objectContaining({
        severity: 'blocker',
        observedConfusion: expect.stringContaining('workspace access'),
        ticketReadyWording: expect.stringContaining('local product app auth'),
      })
    );
  });
});

function missionFixture(
  overrides: Partial<AutonomousFixtureMission> = {}
): AutonomousFixtureMission {
  return {
    id: 'fixture-first-study-setup',
    personaId: 'first-time-researcher',
    personaProfile: getGoalPersonaProfile('first-time-researcher'),
    goal: 'Inspect setup fixture states.',
    viewport: 'desktop',
    maxSteps: 6,
    entryPath: '/app',
    targetFixtureCatalogIds: ['setup:blocked'],
    targetProductPaths: ['/app/campaign-series/2f2f819f-f6eb-486a-9e0f-872ac30af3d4/setup'],
    reviewFocus: ['setup clarity'],
    fixtureProvenance: 'Seeded local product read model backed by demo fixture scenarios',
    localOnly: true,
    ...overrides,
  };
}

function fakeAdapter(snapshots: MissionPageSnapshot[]): AutonomousPageAdapter {
  const byPath = new Map<string, MissionPageSnapshot>();
  for (const snapshot of snapshots) {
    byPath.set(extractPath(snapshot.url), snapshot);
  }
  let current = snapshots[0];

  return {
    async gotoPath(path: string) {
      current = byPath.get(path) ?? current;
      return current;
    },
    async clickLink(text: string) {
      const link = current.links.find((candidate) => candidate.text === text);
      if (!link?.path) {
        throw new Error(`Missing fake link ${text}`);
      }
      current = byPath.get(link.path) ?? current;
      return current;
    },
    async clickButton() {
      return current;
    },
    async fillField() {
      return current;
    },
    async captureSnapshot() {
      return current;
    },
  };
}

function cockpitSnapshot(linkText: string, path: string, extraVisibleText = ''): MissionPageSnapshot {
  return {
    label: 'product-entry',
    title: 'Study cockpit',
    url: 'http://127.0.0.1:5174/app',
    visibleTextExcerpt: `${linkText} Study cockpit ${extraVisibleText}`.trim(),
    buttons: [],
    links: [{ text: linkText, path }],
    richTranscript: {
      label: 'product-entry',
      title: 'Study cockpit',
      url: 'http://127.0.0.1:5174/app',
      visibleText: `${linkText} Study cockpit ${extraVisibleText}`.trim(),
      headings: ['Study cockpit'],
      buttons: [],
      links: [{ text: linkText, path }],
      fields: [],
      sections: ['Sample studies', 'Your studies'],
      statusMessages: [],
    },
  };
}

function pageSnapshot(
  path: string,
  title: string,
  visibleText: string,
  sections = [visibleText]
): MissionPageSnapshot {
  return {
    label: title.toLowerCase().replace(/\s+/g, '-'),
    title,
    url: `http://127.0.0.1:5174${path}`,
    visibleTextExcerpt: visibleText,
    buttons: ['Next'],
    links: [],
    richTranscript: {
      label: title,
      title,
      url: `http://127.0.0.1:5174${path}`,
      visibleText,
      headings: [title],
      buttons: [{ text: 'Next', disabled: false }],
      links: [],
      fields: [],
      sections,
      statusMessages: visibleText.includes('Missing') ? ['Missing scoring rule'] : [],
    },
  };
}

function extractPath(url: string) {
  return new URL(url).pathname;
}
