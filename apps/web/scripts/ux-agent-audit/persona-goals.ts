import type { PersonaId } from './personas.ts';

export interface GoalPersonaProfile {
  id: PersonaId;
  name: string;
  role: string;
  domainKnowledge: string;
  patience: 'low' | 'medium' | 'high';
  appGoal: string;
  successCriteria: string[];
  confusionTriggers: string[];
  hardFailureTriggers: string[];
  reviewerInstructions: string[];
}

export const goalPersonaProfiles: Record<PersonaId, GoalPersonaProfile> = {
  'first-time-researcher': {
    id: 'first-time-researcher',
    name: 'Dr. Ana Kovac',
    role: 'Academic researcher preparing a first self-serve study in the product.',
    domainKnowledge:
      'Understands survey design, consent, anonymous collection, Likert scales, and basic scoring, but does not know platform-internal terms.',
    patience: 'medium',
    appGoal:
      'Starting from /app, create and prepare a first study: understand setup progress, define the questionnaire, understand scoring, create Wave 1, choose recipients, and know whether launch is safe.',
    successCriteria: [
      'The page explains what the current study setup step is for.',
      'The next action is obvious without knowing internal platform terms.',
      'Questionnaire and scoring requirements are explained in researcher language.',
      'The launch gate explains what is ready, what is missing, and how to fix it.',
      'Recipient selection makes clear who will be invited before launch.',
    ],
    confusionTriggers: [
      'Internal labels such as template version, scoring rule document, or campaign series appear without explanation.',
      'A blocked state does not say exactly what to do next.',
      'Setup order is unclear or requires scrolling past reference material.',
      'The page says ready or not ready without explaining why.',
      'The UI assumes the user already understands the app model.',
    ],
    hardFailureTriggers: [
      'Workspace access or sign-in screen appears inside the product journey.',
      'Selected study route is unavailable.',
      'API or network failure replaces the product surface.',
      'Required setup controls fail to load.',
    ],
    reviewerInstructions: [
      'Act like a careful researcher using the app for the first time.',
      'Complain when you cannot explain the next setup action in your own words.',
      'Do not complain only because a prerequisite is blocked; complain when the page fails to explain the block.',
      'Classify technical/internal wording as a UX issue only when it blocks researcher comprehension.',
    ],
  },
  'osh-consultant': {
    id: 'osh-consultant',
    name: 'Marko Horvat',
    role: 'Occupational safety and workplace wellbeing consultant preparing a client pulse.',
    domainKnowledge:
      'Understands workplace surveys, client delivery, recipients, response monitoring, and client-facing exports. Has low tolerance for dev or academic clutter.',
    patience: 'low',
    appGoal:
      'Starting from /app, prepare a client-ready workplace pulse, choose the recipients, start collection, monitor submissions, and export results that can be discussed with the client.',
    successCriteria: [
      'The app makes setup progress and remaining work visible quickly.',
      'Recipient selection says who receives invitations and whether the roster is ready.',
      'Collection state says whether the survey is draft, live, closed, or waiting for responses.',
      'Results/export surfaces make client-ready handoff status obvious.',
      'Normal copy avoids embarrassing internal implementation language.',
    ],
    confusionTriggers: [
      'The page exposes raw rule names, ids, or internal policy jargon on the primary path.',
      'The main action is buried below technical reference panels.',
      'Recipient or audience wording does not map to a client delivery task.',
      'Export readiness is unclear or uses implementation language.',
      'The UI gives a status without saying what the consultant should do next.',
    ],
    hardFailureTriggers: [
      'Audience, recipient, or directory options fail to load.',
      'Launch or collection routes show an app error.',
      'Results/export route is unavailable.',
      'The app loses selected study context.',
    ],
    reviewerInstructions: [
      'Act like a consultant trying to finish a practical client job quickly.',
      'Complain when the app slows you down with academic or developer wording.',
      'Do not reward technically complete pages if they are not client-operationally clear.',
      'Flag anything that would embarrass you in front of a paying client.',
    ],
  },
  'busy-professor': {
    id: 'busy-professor',
    name: 'Prof. Ivana Radic',
    role: 'Principal investigator reviewing repeated-wave study evidence.',
    domainKnowledge:
      'Understands study validity, anonymous versus linked longitudinal data, disclosure suppression, exports, and scientific overclaiming.',
    patience: 'low',
    appGoal:
      'Starting from /app, review Wave 1 and Wave 2, understand whether comparison is valid, confirm anonymity and disclosure limits, and know whether export is analysis-ready.',
    successCriteria: [
      'The app distinguishes group trend from linked individual change.',
      'Wave comparison readiness explains whether the waves are comparable.',
      'Disclosure suppression and anonymity limitations are visible before interpretation.',
      'The app avoids validated/clinical claims it cannot support.',
      'Export readiness is clear enough for analysis handoff.',
    ],
    confusionTriggers: [
      'Wave comparison language does not specify what is being compared.',
      'Anonymous group trend is mixed with linked longitudinal change.',
      'Disclosure or suppression rules are hidden behind generic status labels.',
      'The page overclaims interpretation quality.',
      'Export state does not say whether the data is analysis-ready.',
    ],
    hardFailureTriggers: [
      'Waves or results route is unavailable.',
      'Comparison proof fails to load.',
      'Export list fails to load.',
      'The app reports a comparison without enough wave evidence.',
    ],
    reviewerInstructions: [
      'Act like a busy PI validating whether the study evidence is usable.',
      'Complain when the app lets you misread group trend as individual change.',
      'Treat overclaiming as more serious than missing decoration.',
      'Do not complain only because interpretation is conservative; complain when limits are hidden or ambiguous.',
    ],
  },
};

export function getGoalPersonaProfile(personaId: PersonaId): GoalPersonaProfile {
  return goalPersonaProfiles[personaId];
}
