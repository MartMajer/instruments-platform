import type { MissionPageSnapshot } from './mission-executor.ts';

export type UXAgentAction =
  | {
      kind: 'goto';
      path: string;
      reason: string;
    }
  | {
      kind: 'click-link';
      text: string;
      path?: string;
      reason: string;
    }
  | {
      kind: 'click-button';
      text: string;
      reason: string;
    }
  | {
      kind: 'fill';
      label: string;
      value: string;
      reason: string;
    }
  | {
      kind: 'complain';
      severity: 'blocker' | 'confusion' | 'polish';
      surface: string;
      problem: string;
      suggestedFix: string;
      ticketReadyWording?: string;
    }
  | {
      kind: 'stop';
      reason: string;
    };

export interface AgentActionValidation {
  allowed: boolean;
  reason?: string;
}

export function validateAgentActionAgainstSnapshot(
  action: UXAgentAction,
  snapshot: MissionPageSnapshot
): AgentActionValidation {
  if (action.kind === 'complain' || action.kind === 'stop') {
    return { allowed: true };
  }

  if (action.kind === 'goto') {
    return validateLocalAppPath(action.path);
  }

  if (action.kind === 'click-link') {
    return visibleLinks(snapshot).some((link) => sameText(link.text, action.text))
      ? { allowed: true }
      : {
          allowed: false,
          reason: `Action requires a visible link named "${action.text}".`,
        };
  }

  if (action.kind === 'click-button') {
    const button = visibleButtons(snapshot).find((candidate) =>
      sameText(candidate.text, action.text)
    );
    if (!button) {
      return {
        allowed: false,
        reason: `Action requires a visible button named "${action.text}".`,
      };
    }

    if (button.disabled) {
      return {
        allowed: false,
        reason: `Button "${action.text}" is disabled and cannot be clicked.`,
      };
    }

    return { allowed: true };
  }

  const field = snapshot.richTranscript?.fields.find((candidate) =>
    sameText(candidate.label, action.label)
  );
  return field
    ? { allowed: true }
    : {
        allowed: false,
        reason: `Action requires a visible field named "${action.label}".`,
      };
}

export function validateLocalAppPath(path: string): AgentActionValidation {
  const value = (path ?? '').trim();

  if (
    !value.startsWith('/') ||
    value.startsWith('//') ||
    value.includes('\\') ||
    value.includes('..') ||
    /^https?:\/\//i.test(value)
  ) {
    return {
      allowed: false,
      reason: 'Navigation allows only local app-relative paths.',
    };
  }

  if (value.includes('?') || value.includes('#')) {
    return {
      allowed: false,
      reason: 'Navigation actions cannot include query strings or fragments.',
    };
  }

  if (
    value === '/app' ||
    value.startsWith('/app/') ||
    value === '/signin' ||
    value === '/register' ||
    value === '/respond' ||
    value.startsWith('/respond/') ||
    value.startsWith('/r/')
  ) {
    return { allowed: true };
  }

  return {
    allowed: false,
    reason: `Navigation path "${value}" is outside allowed local app routes.`,
  };
}

export function linkPathForAction(
  snapshot: MissionPageSnapshot,
  action: Extract<UXAgentAction, { kind: 'click-link' }>
) {
  return visibleLinks(snapshot).find((link) => sameText(link.text, action.text))?.path;
}

function visibleLinks(snapshot: MissionPageSnapshot) {
  const richLinks = snapshot.richTranscript?.links ?? [];
  return [...richLinks, ...snapshot.links].filter((link) => link.text || link.path);
}

function visibleButtons(snapshot: MissionPageSnapshot) {
  const richButtons = snapshot.richTranscript?.buttons ?? [];
  const fallbackButtons = snapshot.buttons.map((text) => ({ text, disabled: false }));
  return [...richButtons, ...fallbackButtons].filter((button) => button.text);
}

function sameText(left: string, right: string) {
  return normalizeControlText(left) === normalizeControlText(right);
}

function normalizeControlText(value: string) {
  return (value ?? '').replace(/\s+/g, ' ').trim().toLowerCase();
}
