import { validateLocalAppPath, type UXAgentAction } from './autonomous-actions.ts';
import type { AutonomousPersonaActor, AutonomousPersonaContext } from './autonomous-loop.ts';
import type { JsonObject, JsonValue } from './evidence.ts';

export interface PersonaActionRequest {
  schemaVersion: 1;
  mission: {
    id: string;
    goal: string;
    reviewFocus: string[];
    targetProductPaths: string[];
    visitedProductPaths: string[];
  };
  persona: {
    name: string;
    role: string;
    appGoal: string;
    successCriteria: string[];
    confusionTriggers: string[];
    hardFailureTriggers: string[];
    reviewerInstructions: string[];
  };
  currentPage: {
    label: string;
    title: string;
    path: string;
    visibleTextExcerpt: string;
    headings: string[];
    buttons: string[];
    links: Array<{ text: string; path?: string }>;
    fields: Array<{ label: string; required: boolean }>;
    statusMessages: string[];
  };
  progress: {
    stepCount: number;
    unresolvedFindingCount: number;
  };
  allowedActions: Array<'click-link' | 'click-button' | 'fill' | 'complain' | 'stop'>;
}

export interface PersonaActionProvider {
  proposeAction(request: PersonaActionRequest): string | Promise<string>;
}

const allowedActionKinds = new Set([
  'click-link',
  'click-button',
  'fill',
  'complain',
  'stop',
]);
const allowedComplaintSeverities = new Set(['blocker', 'confusion', 'polish']);

export function buildPersonaActionRequest(
  context: AutonomousPersonaContext
): PersonaActionRequest {
  const transcript = context.currentSnapshot.richTranscript;

  return {
    schemaVersion: 1,
    mission: {
      id: cleanText(context.mission.id),
      goal: cleanText(context.mission.goal),
      reviewFocus: context.mission.reviewFocus.map(cleanText),
      targetProductPaths: context.mission.targetProductPaths.map(cleanPath),
      visitedProductPaths: context.visitedProductPaths.map(cleanPath),
    },
    persona: {
      name: cleanText(context.mission.personaProfile.name),
      role: cleanText(context.mission.personaProfile.role),
      appGoal: cleanText(context.mission.personaProfile.appGoal),
      successCriteria: context.mission.personaProfile.successCriteria.map(cleanText),
      confusionTriggers: context.mission.personaProfile.confusionTriggers.map(cleanText),
      hardFailureTriggers: context.mission.personaProfile.hardFailureTriggers.map(cleanText),
      reviewerInstructions: context.mission.personaProfile.reviewerInstructions.map(cleanText),
    },
    currentPage: {
      label: cleanText(context.currentSnapshot.label),
      title: cleanText(context.currentSnapshot.title),
      path: cleanPath(context.currentSnapshot.url),
      visibleTextExcerpt: cleanText(context.currentSnapshot.visibleTextExcerpt),
      headings: (transcript?.headings ?? []).map(cleanText),
      buttons: (transcript?.buttons ?? context.currentSnapshot.buttons.map((text) => ({ text })))
        .filter((button) => !('disabled' in button) || button.disabled !== true)
        .map((button) => cleanText(button.text)),
      links: (transcript?.links ?? context.currentSnapshot.links).map((link) => ({
        text: cleanText(link.text),
        ...(link.path ? { path: cleanPath(link.path) } : {}),
      })),
      fields: (transcript?.fields ?? []).map((field) => ({
        label: cleanText(field.label),
        required: field.required === true,
      })),
      statusMessages: (transcript?.statusMessages ?? []).map(cleanText),
    },
    progress: {
      stepCount: context.steps.length,
      unresolvedFindingCount: context.findings.length,
    },
    allowedActions: ['click-link', 'click-button', 'fill', 'complain', 'stop'],
  };
}

export function parsePersonaActionResponse(rawOutput: string): UXAgentAction {
  const value = parseJsonLike(rawOutput);
  if (!isRecord(value)) {
    throw new Error('Persona action output must be a JSON object.');
  }

  const kind = stringField(value, 'kind');
  if (!allowedActionKinds.has(kind)) {
    throw new Error(`Unsupported persona action kind: ${kind || 'missing'}`);
  }

  if (kind === 'click-link') {
    return {
      kind,
      text: requiredString(value, 'text'),
      ...(optionalString(value, 'path') ? { path: validateOptionalPath(value.path) } : {}),
      reason: requiredString(value, 'reason'),
    };
  }

  if (kind === 'click-button') {
    return {
      kind,
      text: requiredString(value, 'text'),
      reason: requiredString(value, 'reason'),
    };
  }

  if (kind === 'fill') {
    const label = requiredString(value, 'label');
    if (/password|secret|token|cookie|authorization/i.test(label)) {
      throw new Error('Persona fill actions cannot target credential or secret fields.');
    }

    return {
      kind,
      label,
      value: requiredString(value, 'value'),
      reason: requiredString(value, 'reason'),
    };
  }

  if (kind === 'complain') {
    const severity = requiredString(value, 'severity');
    if (!allowedComplaintSeverities.has(severity)) {
      throw new Error(`Unsupported complaint severity: ${severity}`);
    }

    return {
      kind,
      severity: severity as 'blocker' | 'confusion' | 'polish',
      surface: requiredString(value, 'surface'),
      problem: requiredString(value, 'problem'),
      suggestedFix: requiredString(value, 'suggestedFix'),
      ...(optionalString(value, 'ticketReadyWording')
        ? { ticketReadyWording: cleanText(value.ticketReadyWording) }
        : {}),
    };
  }

  return {
    kind: 'stop',
    reason: requiredString(value, 'reason'),
  };
}

export function buildProviderPersonaActionActor(
  provider: PersonaActionProvider
): AutonomousPersonaActor {
  return {
    async decide(context) {
      const request = buildPersonaActionRequest(context);

      try {
        return parsePersonaActionResponse(await provider.proposeAction(request));
      } catch (error) {
        const detail = error instanceof Error ? error.message : 'Unknown provider failure.';
        return {
          kind: 'complain',
          severity: 'blocker',
          surface: 'Persona action provider',
          problem: `The persona action provider returned an invalid action: ${cleanText(detail)}`,
          suggestedFix:
            'Fix the provider bridge to return one allowed JSON action for the current visible UI state.',
          ticketReadyWording:
            'Fix UXA02 persona action provider output: every provider response must be one allowed JSON action validated against visible local UI.',
        };
      }
    },
  };
}

function parseJsonLike(text: string) {
  const candidates = [text.trim()];
  const fenced = /```(?:json)?\s*([\s\S]*?)```/i.exec(text);
  if (fenced?.[1]) {
    candidates.unshift(fenced[1].trim());
  }

  for (const candidate of candidates) {
    try {
      return JSON.parse(candidate) as unknown;
    } catch {
      continue;
    }
  }

  throw new Error('Persona action output must be valid JSON.');
}

function validateOptionalPath(value: unknown) {
  const path = requiredValue(value, 'path');
  const validation = validateLocalAppPath(path);
  if (!validation.allowed) {
    throw new Error(validation.reason ?? 'Invalid local path.');
  }

  return path;
}

function requiredString(source: Record<string, unknown>, field: string) {
  const value = stringField(source, field);
  if (!value) {
    throw new Error(`Persona action output missing required string field: ${field}`);
  }

  return value;
}

function optionalString(source: Record<string, unknown>, field: string) {
  return typeof source[field] === 'string' && source[field].trim().length > 0;
}

function stringField(source: Record<string, unknown>, field: string) {
  return typeof source[field] === 'string' ? cleanText(source[field]) : '';
}

function requiredValue(value: unknown, field: string) {
  if (typeof value !== 'string' || !value.trim()) {
    throw new Error(`Persona action output missing required string field: ${field}`);
  }

  return cleanText(value);
}

function cleanPath(value: string) {
  try {
    const parsed = new URL(value);
    return parsed.pathname;
  } catch {
    return value.split(/[?#]/)[0] ?? '';
  }
}

function cleanText(value: unknown) {
  return String(value ?? '')
    .replace(/\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/gi, '[redacted-email]')
    .replace(/\b[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{3,}\.[A-Za-z0-9_-]{3,}\b/g, '[redacted-token]')
    .replace(/[?#][^\s]*/g, '')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 1000);
}

function isRecord(value: unknown): value is Record<string, JsonValue> {
  return Boolean(value) && typeof value === 'object' && !Array.isArray(value);
}
