import type { PersonaDefinition } from './types';

export const personas = {
  'workspace-owner': {
    id: 'workspace-owner',
    displayName: 'Workspace owner',
    defaultViewport: 'desktop',
    severityFocus: 'critical',
    reviewGuidance: [
      'Prioritize failures that block workspace access, governance, billing confidence, or team setup.',
    ],
  },
  'first-time-researcher': {
    id: 'first-time-researcher',
    displayName: 'First-time researcher',
    defaultViewport: 'desktop',
    severityFocus: 'high',
    reviewGuidance: [
      'Look for unclear language, missing guidance, and steps that assume prior survey tooling experience.',
    ],
  },
  'busy-professor': {
    id: 'busy-professor',
    displayName: 'Busy professor',
    defaultViewport: 'desktop',
    severityFocus: 'medium',
    reviewGuidance: [
      'Flag slow paths, hidden status, excessive confirmation work, and weak shortcuts for recurring studies.',
    ],
  },
  'osh-consultant': {
    id: 'osh-consultant',
    displayName: 'OSH consultant',
    defaultViewport: 'tablet',
    severityFocus: 'high',
    reviewGuidance: [
      'Emphasize client-ready wording, auditability, export confidence, and field-work handoff clarity.',
    ],
  },
  'mobile-respondent': {
    id: 'mobile-respondent',
    displayName: 'Mobile respondent',
    defaultViewport: 'mobile',
    severityFocus: 'critical',
    reviewGuidance: [
      'Focus on small-screen comprehension, fatigue, privacy confidence, and completion without account context.',
    ],
  },
} satisfies Record<string, PersonaDefinition>;

export type PersonaId = keyof typeof personas;