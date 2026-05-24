export type SetupStageStatus = 'ready' | 'next' | 'blocked';
export type SetupStageHref =
	| '/#instrument'
	| '/#template'
	| '/#scoring'
	| '/#campaign'
	| '/#launch-readiness';

export type SetupStage = {
	href: SetupStageHref;
	label: string;
	description: string;
	status: SetupStageStatus;
};

export const setupStages: SetupStage[] = [
	{
		href: '/#instrument',
		label: 'Questionnaire source',
		description: 'Reusable or imported source material that seeds the questionnaire.',
		status: 'ready'
	},
	{
		href: '/#template',
		label: 'Questionnaire',
		description: 'Sections, questions, answer formats, and respondent wording.',
		status: 'ready'
	},
	{
		href: '/#scoring',
		label: 'Result outputs',
		description: 'Scores, dimensions, export columns, and missing-answer rules.',
		status: 'ready'
	},
	{
		href: '/#campaign',
		label: 'Wave',
		description: 'One collection round with response mode, recipients, and launch window.',
		status: 'ready'
	},
	{
		href: '/#launch-readiness',
		label: 'Launch check',
		description: 'Diagnostics before the wave can move into collection.',
		status: 'ready'
	}
];

export const setupStageStatusLabels: Record<SetupStageStatus, string> = {
	ready: 'Ready',
	next: 'Next',
	blocked: 'Blocked'
};
