import type {
  PersonaActionProvider,
  PersonaActionRequest,
} from './persona-action-driver.ts';

export interface PersonaActionHttpProviderOptions {
  url: string;
  timeoutMs?: number;
  fetchImpl?: typeof fetch;
}

const defaultTimeoutMs = 15000;
const maxTimeoutMs = 60000;

export function buildPersonaActionHttpProvider(
  options: PersonaActionHttpProviderOptions
): PersonaActionProvider {
  const url = validatePersonaActionHttpUrl(options.url);
  const timeoutMs = normalizeTimeoutMs(options.timeoutMs);
  const fetchImpl = options.fetchImpl ?? globalThis.fetch;
  if (typeof fetchImpl !== 'function') {
    throw new Error('Persona action HTTP provider requires fetch support.');
  }

  return {
    async proposeAction(request: PersonaActionRequest) {
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), timeoutMs);

      try {
        const response = await fetchImpl(url, {
          method: 'POST',
          headers: {
            accept: 'application/json, text/plain',
            'content-type': 'application/json',
          },
          body: JSON.stringify(request),
          signal: controller.signal,
        });

        if (!response.ok) {
          throw new Error(
            `Persona action HTTP provider returned ${response.status}`
          );
        }

        return await response.text();
      } finally {
        clearTimeout(timeout);
      }
    },
  };
}

export function validatePersonaActionHttpUrl(rawUrl: string) {
  const value = rawUrl.trim();
  if (!value) {
    throw new Error('Missing required option: --persona-action-url');
  }

  let parsed: URL;
  try {
    parsed = new URL(value);
  } catch {
    throw new Error('--persona-action-url must be a valid local loopback HTTP URL.');
  }

  if (parsed.protocol !== 'http:') {
    throw new Error('--persona-action-url must be a local loopback HTTP URL.');
  }

  if (parsed.username || parsed.password) {
    throw new Error('--persona-action-url must not include credentials.');
  }

  if (!isLoopbackHost(parsed.hostname)) {
    throw new Error('--persona-action-url must be a local loopback HTTP URL.');
  }

  return parsed.toString();
}

function normalizeTimeoutMs(value: number | undefined) {
  if (!Number.isFinite(value)) {
    return defaultTimeoutMs;
  }

  return Math.max(1, Math.min(maxTimeoutMs, Math.floor(value as number)));
}

function isLoopbackHost(hostname: string) {
  const normalized = hostname.toLowerCase().replace(/^\[|\]$/g, '');

  return (
    normalized === 'localhost' ||
    normalized === '127.0.0.1' ||
    normalized === '::1' ||
    normalized.endsWith('.localhost')
  );
}
