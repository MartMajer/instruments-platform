import { describe, expect, it } from 'vitest';

import {
  buildScriptedFixturePersonaActor,
  runAutonomousFixtureMission,
  type AutonomousPageAdapter,
} from './autonomous-loop.ts';
import type { AutonomousFixtureMission } from './autonomous-fixtures.ts';
import type { MissionPageSnapshot } from './mission-executor.ts';

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
        visitedProductPaths: ['/app/campaign-series/6a82f6e0-4712-4c3e-9d20-53715d5c96f3/setup'],
      })
    );
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

function cockpitSnapshot(linkText: string, path: string): MissionPageSnapshot {
  return {
    label: 'product-entry',
    title: 'Study cockpit',
    url: 'http://127.0.0.1:5174/app',
    visibleTextExcerpt: `${linkText} Study cockpit`,
    buttons: [],
    links: [{ text: linkText, path }],
    richTranscript: {
      label: 'product-entry',
      title: 'Study cockpit',
      url: 'http://127.0.0.1:5174/app',
      visibleText: `${linkText} Study cockpit`,
      headings: ['Study cockpit'],
      buttons: [],
      links: [{ text: linkText, path }],
      fields: [],
      sections: ['Sample studies', 'Your studies'],
      statusMessages: [],
    },
  };
}

function pageSnapshot(path: string, title: string, visibleText: string): MissionPageSnapshot {
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
      sections: [visibleText],
      statusMessages: visibleText.includes('Missing') ? ['Missing scoring rule'] : [],
    },
  };
}

function extractPath(url: string) {
  return new URL(url).pathname;
}
