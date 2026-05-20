import { describe, expect, it } from 'vitest';

import {
  validateAgentActionAgainstSnapshot,
  type UXAgentAction,
} from './autonomous-actions.ts';
import type { MissionPageSnapshot } from './mission-executor.ts';

describe('autonomous UX agent safe actions', () => {
  it('allows actions only against visible local UI state', () => {
    const snapshot = screenSnapshot();

    expect(
      validateAgentActionAgainstSnapshot(
        { kind: 'click-link', text: 'Launch ready setup', reason: 'inspect setup' },
        snapshot
      )
    ).toEqual({ allowed: true });
    expect(
      validateAgentActionAgainstSnapshot(
        { kind: 'click-button', text: 'Save questionnaire', reason: 'try save' },
        snapshot
      )
    ).toEqual({ allowed: true });
    expect(
      validateAgentActionAgainstSnapshot(
        { kind: 'fill', label: 'Study name', value: 'Work stress pulse', reason: 'name study' },
        snapshot
      )
    ).toEqual({ allowed: true });
  });

  it('rejects unsafe navigation and unavailable controls', () => {
    const snapshot = screenSnapshot();
    const cases: Array<[UXAgentAction, string]> = [
      [
        { kind: 'goto', path: 'https://validatedscale-staging.croat.dev/app', reason: 'remote' },
        'only local app-relative paths',
      ],
      [
        { kind: 'goto', path: '/app?token=secret', reason: 'query state' },
        'query strings or fragments',
      ],
      [
        { kind: 'click-link', text: 'Missing link', reason: 'not visible' },
        'visible link',
      ],
      [
        { kind: 'click-button', text: 'Launch collection', reason: 'disabled' },
        'disabled',
      ],
      [
        { kind: 'fill', label: 'Password', value: 'secret', reason: 'unsafe field' },
        'visible field',
      ],
    ];

    for (const [action, reason] of cases) {
      expect(validateAgentActionAgainstSnapshot(action, snapshot)).toEqual({
        allowed: false,
        reason: expect.stringContaining(reason),
      });
    }
  });

  it('allows complaint and stop actions without a visible control', () => {
    const snapshot = screenSnapshot();

    expect(
      validateAgentActionAgainstSnapshot(
        {
          kind: 'complain',
          severity: 'confusion',
          surface: 'Setup',
          problem: 'I cannot tell what to do next.',
          suggestedFix: 'Add a concrete next action.',
        },
        snapshot
      )
    ).toEqual({ allowed: true });
    expect(
      validateAgentActionAgainstSnapshot({ kind: 'stop', reason: 'mission complete' }, snapshot)
    ).toEqual({ allowed: true });
  });
});

function screenSnapshot(): MissionPageSnapshot {
  return {
    label: 'setup-ready',
    title: 'Setup',
    url: 'http://127.0.0.1:5174/app/demo',
    visibleTextExcerpt: 'Launch ready Setup Study name',
    buttons: ['Save questionnaire', 'Launch collection'],
    links: [{ text: 'Launch ready setup', path: '/app/campaign-series/fixture-series-ready/setup' }],
    richTranscript: {
      label: 'setup-ready',
      title: 'Setup',
      url: 'http://127.0.0.1:5174/app/demo',
      visibleText: 'Launch ready Setup Study name',
      headings: ['Setup'],
      buttons: [
        { text: 'Save questionnaire', disabled: false },
        { text: 'Launch collection', disabled: true },
      ],
      links: [
        {
          text: 'Launch ready setup',
          path: '/app/campaign-series/fixture-series-ready/setup',
        },
      ],
      fields: [
        {
          label: 'Study name',
          placeholder: 'Wave 1',
          value: '',
          required: true,
        },
      ],
      sections: ['Study preparation'],
      statusMessages: [],
    },
  };
}
