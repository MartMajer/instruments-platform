import { describe, expect, it, vi } from 'vitest';

import type { PersonaActionRequest } from './persona-action-driver.ts';
import {
  buildPersonaActionHttpProvider,
  validatePersonaActionHttpUrl,
} from './persona-action-http-provider.ts';

const request: PersonaActionRequest = {
  schemaVersion: 1,
  mission: {
    id: 'fixture-first-study-setup',
    goal: 'Prepare a first study',
    reviewFocus: ['setup'],
    targetProductPaths: ['/app'],
    visitedProductPaths: ['/app'],
  },
  persona: {
    name: 'Dr. Ana Kovac',
    role: 'First-time researcher',
    appGoal: 'Create and prepare a study',
    successCriteria: ['Understands next step'],
    confusionTriggers: ['unclear setup'],
    hardFailureTriggers: ['cannot continue'],
    reviewerInstructions: ['Act like a researcher'],
  },
  currentPage: {
    label: 'initial',
    title: 'ValidatedScale',
    path: '/app',
    visibleTextExcerpt: 'Studies Plan studies',
    headings: ['Workspace'],
    buttons: [],
    links: [{ text: 'Studies Plan studies', path: '/app/campaign-series' }],
    fields: [],
    statusMessages: [],
  },
  progress: {
    stepCount: 0,
    unresolvedFindingCount: 0,
  },
  allowedActions: ['click-link', 'click-button', 'fill', 'complain', 'stop'],
};

describe('persona action HTTP provider', () => {
  it('posts the bounded persona action request to a local provider', async () => {
    const fetchImpl = vi.fn(async () => {
      return new Response('{"kind":"stop","reason":"done"}', {
        status: 200,
        headers: { 'content-type': 'application/json' },
      });
    }) satisfies typeof fetch;
    const provider = buildPersonaActionHttpProvider({
      url: 'http://127.0.0.1:8765/persona-action',
      fetchImpl,
    });

    await expect(provider.proposeAction(request)).resolves.toBe(
      '{"kind":"stop","reason":"done"}'
    );

    expect(fetchImpl).toHaveBeenCalledWith(
      'http://127.0.0.1:8765/persona-action',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({
          accept: 'application/json, text/plain',
          'content-type': 'application/json',
        }),
        body: expect.stringContaining('"schemaVersion":1'),
      })
    );
  });

  it('rejects non-loopback provider URLs', () => {
    expect(() =>
      validatePersonaActionHttpUrl('https://validatedscale-staging.croat.dev/act')
    ).toThrow('local loopback');
  });

  it('surfaces provider HTTP failures as provider errors', async () => {
    const provider = buildPersonaActionHttpProvider({
      url: 'http://localhost:8765/persona-action',
      fetchImpl: vi.fn(async () => new Response('broken', { status: 503 })),
    });

    await expect(provider.proposeAction(request)).rejects.toThrow(
      'Persona action HTTP provider returned 503'
    );
  });
});
