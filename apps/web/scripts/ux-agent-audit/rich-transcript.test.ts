import { describe, expect, it } from 'vitest';

import {
  buildRichTranscriptMarkdown,
  normalizeRichScreenSnapshot,
  type RawRichScreenSnapshot,
} from './rich-transcript.ts';

describe('local-full UX transcript capture', () => {
  it('normalizes full visible page structure for persona review', () => {
    const snapshot = normalizeRichScreenSnapshot(rawScreen());

    expect(snapshot).toEqual(
      expect.objectContaining({
        title: 'Study setup',
        url: 'http://127.0.0.1:5174/app/campaign-series/study-local-1/setup',
        headings: ['Prepare study', 'Questionnaire', 'Results setup'],
        buttons: [
          { text: 'Save questionnaire', disabled: false },
          { text: 'Launch collection', disabled: true },
        ],
        fields: [
          {
            label: 'Study name',
            placeholder: 'Wave 1',
            value: 'Tenant quarterly pulse',
            required: true,
          },
        ],
        statusMessages: ['Missing scoring rule'],
      })
    );
    expect(snapshot.visibleText).toContain('Create or edit questions');
    expect(snapshot.visibleText).toContain('Launch collection');
  });

  it('writes a readable page-by-page markdown transcript', () => {
    const markdown = buildRichTranscriptMarkdown([normalizeRichScreenSnapshot(rawScreen())]);

    expect(markdown).toContain('# Local UX full transcript');
    expect(markdown).toContain('## 1. Study setup');
    expect(markdown).toContain('### Headings');
    expect(markdown).toContain('- Prepare study');
    expect(markdown).toContain('Launch collection (disabled)');
    expect(markdown).toContain('Study name');
    expect(markdown).toContain('Create or edit questions');
  });
});

function rawScreen(): RawRichScreenSnapshot {
  return {
    label: 'study-setup',
    title: 'Study setup',
    url: 'http://127.0.0.1:5174/app/campaign-series/study-local-1/setup',
    visibleText:
      'Prepare study Questionnaire Create or edit questions Results setup Missing scoring rule Launch collection',
    headings: ['Prepare study', 'Questionnaire', 'Results setup'],
    buttons: [
      { text: 'Save questionnaire', disabled: false },
      { text: 'Launch collection', disabled: true },
    ],
    links: [{ text: 'Results', path: '/app/campaign-series/study-local-1/reports' }],
    fields: [
      {
        label: 'Study name',
        placeholder: 'Wave 1',
        value: 'Tenant quarterly pulse',
        required: true,
      },
    ],
    sections: ['Study preparation', 'Questionnaire builder'],
    statusMessages: ['Missing scoring rule'],
  };
}
