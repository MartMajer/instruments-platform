import { describe, expect, it } from 'vitest';

import { parseRunnerOptions } from './run';

describe('UX audit runner option parsing', () => {
  it('parses local audit runner CLI options', () => {
    const options = parseRunnerOptions([
      '--base-url',
      'http://127.0.0.1:5174',
      '--mission',
      'create-first-study',
      '--persona',
      'first-time-researcher',
      '--viewport',
      'desktop',
      '--output',
      '../../artifacts/ux-agent-runs/test',
    ]);

    expect(options).toEqual({
      baseUrl: 'http://127.0.0.1:5174',
      missionFilter: 'create-first-study',
      personaOverride: 'first-time-researcher',
      viewportOverride: 'desktop',
      outputRoot: '../../artifacts/ux-agent-runs/test',
    });
  });
});
