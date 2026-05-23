import type { AppLocale } from '$lib/i18n/localization';

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

export function listStudyBlueprintOptions(locale: AppLocale = 'en') {
	return studyBlueprintOptions.map((option) => copyStudyBlueprintOption(localizeStudyBlueprintOption(option, locale)));
}

export function getStudyBlueprintOption(id: string | null | undefined, locale: AppLocale = 'en') {
	const option =
		studyBlueprintOptions.find((option) => option.id === id) ??
		studyBlueprintOptions.find((option) => option.id === defaultStudyBlueprintId) ??
		studyBlueprintOptions[0];
	return copyStudyBlueprintOption(localizeStudyBlueprintOption(option, locale));
}

export function buildStudyNamePlaceholder(id: string | null | undefined, locale: AppLocale = 'en') {
	return getStudyBlueprintOption(id, locale).namePlaceholder;
}

function copyStudyBlueprintOption(option: StudyBlueprintOption): StudyBlueprintOption {
	return {
		...option,
		highlights: [...option.highlights],
		nextSteps: option.nextSteps.map((step) => ({ ...step }))
	};
}

const croatianBlueprintCopy: Record<StudyBlueprintId, Omit<StudyBlueprintOption, 'id'>> = {
	custom_research_study: {
		eyebrow: 'Počnite od prazne strukture',
		title: 'Prilagođena istraživačka studija',
		summary: 'Koristite ovo kada želite definirati vlastite konstrukte, pitanja i izlazne rezultate.',
		bestFor: 'Akademski projekti, interna istraživanja i prilagođene workplace studije.',
		namePlaceholder: 'npr. Studija opterećenja i oporavka',
		highlights: ['Definirajte konstrukte', 'Napišite prilagođena pitanja', 'Odaberite rezultate'],
		nextStepsTitle: 'Izgradit ćete studiju iz svoje istraživačke ideje',
		nextSteps: [
			{
				label: 'Svrha',
				description: 'Imenujte čime se studija bavi i koju odluku rezultati trebaju podržati.'
			},
			{
				label: 'Upitnik',
				description: 'Napišite pitanja, odaberite formate odgovora i grupirajte ih u dimenzije.'
			},
			{
				label: 'Rezultati',
				description: 'Odaberite koji odgovori postaju skorovi ili sažeci prije početka prikupljanja.'
			}
		]
	},
	team_pulse: {
		eyebrow: 'Brza provjera tima',
		title: 'Timski puls',
		summary: 'Koristite ovo za kratku ponavljajuću provjeru tima, odjela ili kohorte.',
		bestFor: 'Kratki workplace pulsevi, provjere razreda i lagane ponavljajuće povratne informacije.',
		namePlaceholder: 'npr. Timski puls Q3',
		highlights: ['Kratki upitnik', 'Pogodno za anonimno prikupljanje', 'Brz pregled rezultata'],
		nextStepsTitle: 'Zadržat ćete studiju kratkom i lako ponovljivom',
		nextSteps: [
			{ label: 'Opseg', description: 'Definirajte grupu i mali skup tema koje puls treba pokriti.' },
			{ label: 'Pitanja', description: 'Izradite kompaktan upitnik koji se može brzo ispuniti.' },
			{ label: 'Primatelji', description: 'Odaberite ljude ili grupe za ovaj val prije pokretanja.' }
		]
	},
	repeated_wave: {
		eyebrow: 'Mjerite promjenu kroz vrijeme',
		title: 'Studija s ponovljenim valovima',
		summary: 'Koristite ovo kada se ista studija treba ponoviti kasnije i usporedba je važna.',
		bestFor: 'Bazne i follow-up studije, provjere intervencija i ponavljajuća mjerenja kohorte.',
		namePlaceholder: 'npr. Follow-up studija dobrobiti',
		highlights: ['Planiranje valova', 'Ponavljano sudjelovanje', 'Postavljanje spremno za usporedbu'],
		nextStepsTitle: 'Pripremit ćete prvi val s follow-upom na umu',
		nextSteps: [
			{ label: 'Plan vala', description: 'Počnite s Valom 1 i zadržite postavke spremne za kasniju usporedbu.' },
			{ label: 'Način povezivanja', description: 'Koristite postavku ponavljanog sudjelovanja kada se isti sudionik uspoređuje kroz vrijeme.' },
			{ label: 'Rezultati', description: 'Pregledajte trendove grupa i promjenu istih sudionika kada ima dovoljno podataka.' }
		]
	},
	osh_ergonomics_study: {
		eyebrow: 'Rizici rada i ergonomija',
		title: 'Studija zaštite na radu / ergonomije',
		summary:
			'Koristite ovo za praktičnu workplace studiju s izloženošću zadacima, nelagodom, oporavkom i follow-upom intervencija.',
		bestFor: 'OSH konzultanti, ergonomske procjene i provjere dobrobiti na radnom mjestu.',
		namePlaceholder: 'npr. Puls opterećenja i oporavka u skladištu',
		highlights: ['Mješoviti formati odgovora', 'Rezultati po dimenzijama', 'Grupe primatelja'],
		nextStepsTitle: 'Prilagodit ćete praktični početni model workplace rizika',
		nextSteps: [
			{ label: 'Izloženost zadacima', description: 'Mapirajte izloženost zadacima i grupe primatelja.' },
			{ label: 'Upitnik', description: 'Pregledajte početni upitnik i prilagodite formulacije radnom mjestu.' },
			{ label: 'Rezultati', description: 'Planirajte izlazne rezultate prije pokretanja prvog vala.' }
		]
	}
};

function localizeStudyBlueprintOption(option: StudyBlueprintOption, locale: AppLocale): StudyBlueprintOption {
	if (locale !== 'hr-HR') {
		return option;
	}

	return {
		id: option.id,
		...croatianBlueprintCopy[option.id]
	};
}
