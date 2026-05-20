import { describe, expect, it } from 'vitest';

import type { AutonomousPersonaContext } from './autonomous-loop.ts';
import type { AutonomousFixtureMission } from './autonomous-fixtures.ts';
import { buildPersonaActionRequest, parsePersonaActionResponse } from './persona-action-driver.ts';
import { getGoalPersonaProfile } from './persona-goals.ts';

describe('persona action driver protocol', () => {
  it('builds a bounded action request from the current persona context', () => {
    const request = buildPersonaActionRequest(contextFixture());

    expect(request).toEqual(
      expect.objectContaining({
        schemaVersion: 1,
        mission: expect.objectContaining({
          id: 'fixture-first-study-setup',
          goal: expect.stringContaining('Inspect'),
        }),
        persona: expect.objectContaining({
          name: 'Dr. Ana Kovac',
        }),
        currentPage: expect.objectContaining({
          title: 'Setup',
          buttons: ['Save questionnaire'],
          links: [{ text: 'Results', path: '/app/campaign-series/study-1/reports' }],
          fields: [{ label: 'Study name', required: true }],
        }),
        allowedActions: [
          'click-link',
          'click-button',
          'fill',
          'complain',
          'stop',
        ],
      })
    );
    expect(JSON.stringify(request)).not.toContain('ana@example.test');
    expect(JSON.stringify(request)).not.toContain('token=secret');
  });

  it('parses raw or fenced JSON into a safe UX agent action', () => {
    expect(
      parsePersonaActionResponse(
        '{"kind":"click-button","text":"Save questionnaire","reason":"save setup progress"}'
      )
    ).toEqual({
      kind: 'click-button',
      text: 'Save questionnaire',
      reason: 'save setup progress',
    });

    expect(
      parsePersonaActionResponse([
        '```json',
        '{"kind":"complain","severity":"confusion","surface":"Setup","problem":"I cannot tell what is missing.","suggestedFix":"Show the missing setup item."}',
        '```',
      ].join('\n'))
    ).toEqual(
      expect.objectContaining({
        kind: 'complain',
        severity: 'confusion',
        surface: 'Setup',
      })
    );
  });

  it('rejects malformed or unsafe persona action output', () => {
    const unsafeOutputs = [
      'not json',
      '{"kind":"eval","code":"alert(1)"}',
      '{"kind":"goto","path":"https://validatedscale-staging.croat.dev/app","reason":"remote"}',
      '{"kind":"click-button","text":"","reason":"missing text"}',
      '{"kind":"fill","label":"Password","value":"secret","reason":"credential"}',
      '{"kind":"complain","severity":"critical","surface":"Setup","problem":"bad","suggestedFix":"fix"}',
    ];

    for (const output of unsafeOutputs) {
      expect(() => parsePersonaActionResponse(output)).toThrow();
    }
  });
});

function contextFixture(): AutonomousPersonaContext {
  const mission: AutonomousFixtureMission = {
    id: 'fixture-first-study-setup',
    personaId: 'first-time-researcher',
    personaProfile: getGoalPersonaProfile('first-time-researcher'),
    goal: 'Inspect first study setup.',
    viewport: 'desktop',
    maxSteps: 4,
    entryPath: '/app',
    targetFixtureCatalogIds: ['setup:empty'],
    targetProductPaths: ['/app/campaign-series/study-1/setup'],
    reviewFocus: ['setup'],
    fixtureProvenance: 'local product read model',
    supportedDataModes: ['fixture'],
    localOnly: true,
  };

  return {
    mission,
    currentSnapshot: {
      label: 'setup',
      title: 'Setup',
      url: 'http://127.0.0.1:5174/app/campaign-series/study-1/setup?token=secret',
      visibleTextExcerpt: 'Setup ana@example.test Save questionnaire',
      buttons: ['Save questionnaire'],
      links: [{ text: 'Results', path: '/app/campaign-series/study-1/reports' }],
      richTranscript: {
        label: 'setup',
        title: 'Setup',
        url: 'http://127.0.0.1:5174/app/campaign-series/study-1/setup?token=secret',
        visibleText: 'Setup Save questionnaire Study name',
        headings: ['Setup'],
        buttons: [{ text: 'Save questionnaire', disabled: false }],
        links: [{ text: 'Results', path: '/app/campaign-series/study-1/reports' }],
        fields: [{ label: 'Study name', placeholder: '', value: '', required: true }],
        sections: ['Study setup'],
        statusMessages: [],
      },
    },
    visitedProductPaths: ['/app/campaign-series/study-1/setup'],
    steps: [{ index: 1, action: 'Opened setup.', url: '/app/campaign-series/study-1/setup' }],
    findings: [],
  };
}
