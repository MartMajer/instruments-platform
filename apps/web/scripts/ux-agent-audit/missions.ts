import type { PersonaId } from './personas';
import type { MissionDefinition } from './types';

export const missions = [
  {
    id: 'auth-enter-workspace',
    personaId: 'workspace-owner',
    goal: 'Enter the correct workspace from the authentication flow and understand the next required action.',
    maxSteps: 8,
    viewport: 'desktop',
    successCriteria: ['Workspace context is visible before the user proceeds.'],
  },
  {
    id: 'create-first-study',
    personaId: 'first-time-researcher',
    goal: 'Find how a first-time researcher would create or open a first study and understand setup, collection, and results without unsafe persisted writes.',
    maxSteps: 12,
    viewport: 'desktop',
    successCriteria: [
      'Sign-in blockers are recorded as UX observations instead of failing the audit run.',
      'When local app access exists, study creation/opening and setup, collection, results, and waves navigation are inspected without submitting mutating actions.',
    ],
  },
  {
    id: 'configure-study',
    personaId: 'first-time-researcher',
    goal: 'Configure the study basics and questionnaire settings with enough confidence to continue setup.',
    maxSteps: 14,
    viewport: 'desktop',
    successCriteria: ['Required configuration is saved with clear validation feedback.'],
  },
  {
    id: 'prepare-audience',
    personaId: 'osh-consultant',
    goal: 'Prepare the intended audience and invitation details for a client or organization study.',
    maxSteps: 12,
    viewport: 'tablet',
    successCriteria: ['Audience readiness and unresolved blockers are understandable.'],
  },
  {
    id: 'launch-wave',
    personaId: 'busy-professor',
    goal: 'Launch a collection wave while understanding timing, recipient scope, and irreversible consequences.',
    maxSteps: 10,
    viewport: 'desktop',
    successCriteria: ['The wave is ready to send or blocked by explicit fixable issues.'],
  },
  {
    id: 'respondent-submit-mobile',
    personaId: 'mobile-respondent',
    goal: 'Complete and submit a questionnaire on a phone with clear progress and privacy expectations.',
    maxSteps: 18,
    viewport: 'mobile',
    successCriteria: ['The respondent reaches a submitted confirmation state.'],
  },
  {
    id: 'review-results-export',
    personaId: 'osh-consultant',
    goal: 'Review collected results and export useful evidence for a client-ready reporting workflow.',
    maxSteps: 14,
    viewport: 'desktop',
    successCriteria: ['An export action is discoverable and result interpretation is clear.'],
  },
  {
    id: 'review-waves',
    personaId: 'busy-professor',
    goal: 'Review wave history and current collection status to decide the next operational action.',
    maxSteps: 10,
    viewport: 'desktop',
    successCriteria: ['Wave state, response progress, and next action are visible.'],
  },
] satisfies MissionDefinition<PersonaId>[];
