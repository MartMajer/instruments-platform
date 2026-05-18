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
		label: 'Instrument',
		description: 'Tenant-provided source, provenance, and rights attestation.',
		status: 'ready'
	},
	{
		href: '/#template',
		label: 'Template',
		description: 'Sections, questions, scale choices, and version metadata.',
		status: 'ready'
	},
	{
		href: '/#scoring',
		label: 'Scoring',
		description: 'Draft scoring rule document bound to the template version.',
		status: 'ready'
	},
	{
		href: '/#campaign',
		label: 'Campaign',
		description: 'Series, wave, identity mode, audience, and draft launch window.',
		status: 'ready'
	},
	{
		href: '/#launch-readiness',
		label: 'Launch readiness',
		description: 'Diagnostics before a tenant-provided campaign can move toward launch.',
		status: 'ready'
	}
];

export const setupStageStatusLabels: Record<SetupStageStatus, string> = {
	ready: 'Ready',
	next: 'Next',
	blocked: 'Blocked'
};
