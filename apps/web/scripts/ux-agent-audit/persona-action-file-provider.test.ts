import { describe, expect, it } from 'vitest';

import { buildPersonaActionFileProvider } from './persona-action-file-provider.ts';
import type { PersonaActionRequest } from './persona-action-driver.ts';

describe('persona action file provider', () => {
  it('returns one local action line per provider request', async () => {
    const provider = buildPersonaActionFileProvider([
      '{"kind":"click-link","text":"Studies Plan studies","path":"/app/campaign-series","reason":"open studies"}',
      '{"kind":"stop","reason":"done"}',
    ]);

    expect(await provider.proposeAction(requestFixture())).toContain('click-link');
    expect(await provider.proposeAction(requestFixture())).toContain('"stop"');
  });

  it('stops safely when the action file has no more actions', async () => {
    const provider = buildPersonaActionFileProvider([]);

    expect(await provider.proposeAction(requestFixture())).toBe(
      '{"kind":"stop","reason":"persona action file exhausted"}'
    );
  });
});

function requestFixture(): PersonaActionRequest {
  return {
    schemaVersion: 1,
    mission: {
      id: 'fixture-first-study-setup',
      goal: 'Inspect setup.',
      reviewFocus: ['setup'],
      targetProductPaths: ['/app/campaign-series'],
      visitedProductPaths: [],
    },
    persona: {
      name: 'Dr. Ana Kovac',
      role: 'Researcher',
      appGoal: 'Create study.',
      successCriteria: ['Can open studies'],
      confusionTriggers: [],
      hardFailureTriggers: [],
      reviewerInstructions: [],
    },
    currentPage: {
      label: 'home',
      title: 'Workspace',
      path: '/app',
      visibleTextExcerpt: 'Studies',
      headings: ['Workspace'],
      buttons: [],
      links: [{ text: 'Studies Plan studies', path: '/app/campaign-series' }],
      fields: [],
      statusMessages: [],
    },
    progress: { stepCount: 1, unresolvedFindingCount: 0 },
    allowedActions: ['click-link', 'click-button', 'fill', 'complain', 'stop'],
  };
}
