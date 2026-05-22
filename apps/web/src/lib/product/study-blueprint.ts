export type StudyBlueprintId =
	| 'custom_research_study'
	| 'team_pulse'
	| 'repeated_wave'
	| 'osh_ergonomics_study';

export type StudyBlueprintStep = {
	label: string;
	description: string;
};

export type StudyBlueprintOption = {
	id: StudyBlueprintId;
	eyebrow: string;
	title: string;
	summary: string;
	bestFor: string;
	namePlaceholder: string;
	highlights: string[];
	nextStepsTitle: string;
	nextSteps: StudyBlueprintStep[];
};

export const defaultStudyBlueprintId: StudyBlueprintId = 'custom_research_study';

const studyBlueprintOptions: StudyBlueprintOption[] = [
	{
		id: 'custom_research_study',
		eyebrow: 'Start from blank structure',
		title: 'Custom research study',
		summary: 'Use this when you want to define your own constructs, questions, and result outputs.',
		bestFor: 'Academic projects, internal research, and custom workplace studies.',
		namePlaceholder: 'e.g. Workload and recovery study',
		highlights: ['Define constructs', 'Write custom questions', 'Choose result outputs'],
		nextStepsTitle: 'You will build the study from your research idea',
		nextSteps: [
			{
				label: 'Purpose',
				description: 'Name what the study is about and what decision the results should support.'
			},
			{
				label: 'Questionnaire',
				description: 'Write questions, choose answer formats, and group them into dimensions.'
			},
			{
				label: 'Results',
				description: 'Choose which answers become scores or summaries before collection starts.'
			}
		]
	},
	{
		id: 'team_pulse',
		eyebrow: 'Quick workplace check-in',
		title: 'Team pulse',
		summary: 'Use this when you want a short recurring check-in for a team, department, or cohort.',
		bestFor: 'Short workplace pulses, class check-ins, and lightweight repeated feedback.',
		namePlaceholder: 'e.g. Q3 team pulse',
		highlights: ['Short questionnaire', 'Anonymous-friendly collection', 'Fast results review'],
		nextStepsTitle: 'You will keep the study short and easy to repeat',
		nextSteps: [
			{
				label: 'Scope',
				description: 'Define the group and the small set of topics this pulse should cover.'
			},
			{
				label: 'Questions',
				description: 'Create a compact questionnaire that respondents can finish quickly.'
			},
			{
				label: 'Recipients',
				description: 'Choose the people or groups for this wave before launching collection.'
			}
		]
	},
	{
		id: 'repeated_wave',
		eyebrow: 'Measure change over time',
		title: 'Repeated-wave study',
		summary: 'Use this when the same study should run again later and comparison matters.',
		bestFor: 'Baseline/follow-up studies, intervention checks, and recurring cohort measurement.',
		namePlaceholder: 'e.g. Follow-up wellbeing study',
		highlights: ['Wave planning', 'Repeat participation', 'Comparison-ready setup'],
		nextStepsTitle: 'You will prepare the first wave with follow-up in mind',
		nextSteps: [
			{
				label: 'Wave plan',
				description: 'Start with Wave 1 and keep the setup ready for a later comparison wave.'
			},
			{
				label: 'Linking mode',
				description: 'Use repeat-participation setup when the same respondent should be compared over time.'
			},
			{
				label: 'Results',
				description: 'Review both group trends and same-respondent change when enough data exists.'
			}
		]
	},
	{
		id: 'osh_ergonomics_study',
		eyebrow: 'Workplace risk and ergonomics',
		title: 'OSH / ergonomics study',
		summary:
			'Use this when you need a practical workplace study with task exposure, discomfort, recovery, and intervention follow-up.',
		bestFor: 'OSH consultants, ergonomics reviews, and workplace wellbeing checks.',
		namePlaceholder: 'e.g. Warehouse strain and recovery pulse',
		highlights: ['Mixed answer formats', 'Dimension-based results', 'Recipient groups'],
		nextStepsTitle: 'You will adapt a practical workplace-risk starter',
		nextSteps: [
			{
				label: 'Task exposure',
				description: 'Map task exposure and recipient groups.'
			},
			{
				label: 'Questionnaire',
				description: 'Review the starter questionnaire and adjust wording to the workplace.'
			},
			{
				label: 'Results',
				description: 'Plan result outputs before launching the first wave.'
			}
		]
	}
];

export function listStudyBlueprintOptions() {
	return studyBlueprintOptions.map(copyStudyBlueprintOption);
}

export function getStudyBlueprintOption(id: string | null | undefined) {
	return (
		studyBlueprintOptions.find((option) => option.id === id) ??
		studyBlueprintOptions.find((option) => option.id === defaultStudyBlueprintId) ??
		studyBlueprintOptions[0]
	);
}

export function buildStudyNamePlaceholder(id: string | null | undefined) {
	return getStudyBlueprintOption(id).namePlaceholder;
}

function copyStudyBlueprintOption(option: StudyBlueprintOption): StudyBlueprintOption {
	return {
		...option,
		highlights: [...option.highlights],
		nextSteps: option.nextSteps.map((step) => ({ ...step }))
	};
}
