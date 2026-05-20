import { readFile } from 'node:fs/promises';

import type { PersonaActionProvider } from './persona-action-driver.ts';

export function buildPersonaActionFileProvider(lines: string[]): PersonaActionProvider {
  const actions = lines.map((line) => line.trim()).filter(Boolean);
  let index = 0;

  return {
    proposeAction() {
      const action = actions[index];
      index += 1;

      return action ?? '{"kind":"stop","reason":"persona action file exhausted"}';
    },
  };
}

export async function loadPersonaActionFileProvider(path: string) {
  const content = await readFile(path, 'utf8');
  const lines = content
    .split(/\r?\n/u)
    .map((line) => line.trim())
    .filter((line) => line.length > 0 && !line.startsWith('#'));

  return buildPersonaActionFileProvider(lines);
}
