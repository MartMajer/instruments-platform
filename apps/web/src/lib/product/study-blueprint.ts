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

export type StudyBriefDesignType =
	| 'single_wave'
	| 'repeated_group_trend'
	| 'repeated_linked_change';

export type StudyBriefIntendedUse =
	| 'internal_review'
	| 'research_analysis'
	| 'client_report';

export type StudyBriefDefaults = {
	purpose: string;
	audience: string;
	designType: StudyBriefDesignType;
	intendedUse: StudyBriefIntendedUse;
	interpretationBoundary: string;
	ownerNotes: string;
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
	briefDefaults: StudyBriefDefaults;
};

export const defaultStudyBlueprintId: StudyBlueprintId = 'custom_research_study';

const studyBlueprintOptions: StudyBlueprintOption[] = [
	{
		id: 'custom_research_study',
		eyebrow: 'Blank study',
		title: 'Build from scratch',
		summary: 'Start with an empty study when you want to define your own topics, questions, and result outputs.',
		bestFor: 'Academic projects, internal research, and custom organization studies.',
		namePlaceholder: 'e.g. Workload and recovery study',
		highlights: ['Define constructs', 'Write custom questions', 'Choose result outputs'],
		briefDefaults: {
			purpose: 'Define a custom study question and the decision this study should support.',
			audience: 'Participants selected by the workspace team for this study.',
			designType: 'single_wave',
			intendedUse: 'research_analysis',
			interpretationBoundary:
				'Use results as custom-study evidence with method notes; do not present them as externally validated norms unless separately reviewed.',
			ownerNotes: ''
		},
		nextStepsTitle: 'Next you will turn the idea into a questionnaire',
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
		eyebrow: 'Short check-in',
		title: 'Team check-in',
		summary: 'Start with a compact study for a team, department, class, or cohort.',
		bestFor: 'Short internal check-ins, class reviews, and lightweight repeated feedback.',
		namePlaceholder: 'e.g. Q3 team pulse',
		highlights: ['Short questionnaire', 'Anonymous-friendly collection', 'Fast results review'],
		briefDefaults: {
			purpose: 'Run a short check-in to understand current group conditions and decide what needs follow-up.',
			audience: 'A team, department, class, or cohort invited to one short collection round.',
			designType: 'repeated_group_trend',
			intendedUse: 'internal_review',
			interpretationBoundary:
				'Use results for internal review and follow-up planning. Avoid individual-level conclusions.',
			ownerNotes: ''
		},
		nextStepsTitle: 'Next you will keep the questionnaire short and repeatable',
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
				description: 'Choose the people or groups for this measurement before launching collection.'
			}
		]
	},
	{
		id: 'repeated_wave',
		eyebrow: 'Compare measurements',
		title: 'Repeated measurement study',
		summary: 'Start here when you want to run the same study again later and compare change over time.',
		bestFor: 'Baseline and later checks, intervention reviews, and recurring cohort measurement.',
		namePlaceholder: 'e.g. Follow-up wellbeing study',
		highlights: ['Measurement planning', 'Repeat participation', 'Comparison-ready setup'],
		briefDefaults: {
			purpose: 'Measure change between an initial collection round and a later collection round.',
			audience: 'The same respondent group repeated across collection rounds where possible.',
			designType: 'repeated_linked_change',
			intendedUse: 'research_analysis',
			interpretationBoundary:
				'Compare change only where the questionnaire, scoring, and same-person comparison setup remain comparable.',
			ownerNotes: ''
		},
		nextStepsTitle: 'Next you will prepare the first measurement with comparison in mind',
		nextSteps: [
			{
				label: 'Measurement plan',
				description: 'Start with Measurement 1 and keep the setup ready for a later comparison.'
			},
			{
				label: 'Linking mode',
				description: 'Use same-person comparison setup when the same respondent should be compared over time.'
			},
			{
				label: 'Results',
				description: 'Review both group trends and same-respondent change when enough data exists.'
			}
		]
	},
	{
		id: 'osh_ergonomics_study',
		eyebrow: 'Work and ergonomics',
		title: 'Workplace health review',
		summary:
			'Start with a practical study covering task exposure, discomfort, recovery, and follow-up actions.',
		bestFor: 'Ergonomics reviews, occupational-health work, and wellbeing checks.',
		namePlaceholder: 'e.g. Warehouse strain and recovery pulse',
		highlights: ['Mixed answer formats', 'Dimension-based results', 'Recipient groups'],
		briefDefaults: {
			purpose: 'Assess task exposure, strain, recovery, and practical follow-up needs.',
			audience: 'Workers or teams selected for the workplace health or ergonomics review.',
			designType: 'repeated_group_trend',
			intendedUse: 'client_report',
			interpretationBoundary:
				'Use results as practical review input. Keep method limits and follow-up context with any stakeholder summary.',
			ownerNotes: ''
		},
		nextStepsTitle: 'Next you will adapt a practical work-and-ergonomics starter',
		nextSteps: [
			{
				label: 'Task exposure',
				description: 'Map task exposure and recipient groups.'
			},
			{
				label: 'Questionnaire',
				description: 'Review the starter questionnaire and adjust wording to the organization.'
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
		nextSteps: option.nextSteps.map((step) => ({ ...step })),
		briefDefaults: { ...option.briefDefaults }
	};
}

const croatianBlueprintCopy: Record<StudyBlueprintId, Omit<StudyBlueprintOption, 'id'>> = {
	custom_research_study: {
		eyebrow: 'Prazna studija',
		title: 'Izradite od početka',
		summary: 'Počnite s praznom studijom kada želite sami definirati teme, pitanja i rezultate.',
		bestFor: 'Akademski projekti, interna istraživanja i prilagođene organizacijske studije.',
		namePlaceholder: 'npr. Studija opterećenja i oporavka',
		highlights: ['Definirajte konstrukte', 'Napišite prilagođena pitanja', 'Odaberite rezultate'],
		briefDefaults: {
			purpose: 'Definirajte vlastito istraživačko pitanje i odluku koju rezultati trebaju podržati.',
			audience: 'Sudionici koje tim radnog prostora odabere za ovu studiju.',
			designType: 'single_wave',
			intendedUse: 'research_analysis',
			interpretationBoundary:
				'Koristite rezultate kao dokaz prilagođene studije s metodološkim bilješkama; nemojte ih prikazivati kao vanjski validirane norme bez zasebnog pregleda.',
			ownerNotes: ''
		},		nextStepsTitle: 'Zatim ćete ideju pretvoriti u upitnik',
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
		eyebrow: 'Kratka provjera',
		title: 'Provjera tima',
		summary: 'Počnite s kompaktnom studijom za tim, odjel, razred ili kohortu.',
		bestFor: 'Kratke interne provjere, provjere razreda i lagane ponavljajuće povratne informacije.',
		namePlaceholder: 'npr. Timski puls Q3',
		highlights: ['Kratki upitnik', 'Pogodno za anonimno prikupljanje', 'Brz pregled rezultata'],
		briefDefaults: {
			purpose: 'Provedite kratku provjeru kako biste razumjeli trenutno stanje grupe i odlučili što treba pratiti.',
			audience: 'Tim, odjel, razred ili kohorta pozvana u jedno kratko prikupljanje.',
			designType: 'repeated_group_trend',
			intendedUse: 'internal_review',
			interpretationBoundary:
				'Koristite rezultate za interni pregled i planiranje praćenja. Izbjegavajte zaključke o pojedincima.',
			ownerNotes: ''
		},		nextStepsTitle: 'Zatim ćete upitnik zadržati kratkim i lako ponovljivim',
		nextSteps: [
			{ label: 'Opseg', description: 'Definirajte grupu i mali skup tema koje puls treba pokriti.' },
			{ label: 'Pitanja', description: 'Izradite kompaktan upitnik koji se može brzo ispuniti.' },
			{ label: 'Primatelji', description: 'Odaberite ljude ili grupe za ovo mjerenje prije pokretanja.' }
		]
	},
	repeated_wave: {
		eyebrow: 'Usporedite mjerenja',
		title: 'Studija s ponovljenim mjerenjima',
		summary: 'Počnite ovdje kada istu studiju želite ponoviti kasnije i usporediti promjenu kroz vrijeme.',
		bestFor: 'Početna i kasnija mjerenja, provjere intervencija i ponavljajuća mjerenja kohorte.',
		namePlaceholder: 'npr. Follow-up studija dobrobiti',
		highlights: ['Planiranje mjerenja', 'Ponavljano sudjelovanje', 'Postavljanje spremno za usporedbu'],
		briefDefaults: {
			purpose: 'Mjerite promjenu između početnog i naknadnog prikupljanja.',
			audience: 'Ista grupa ispitanika kroz ponovljena prikupljanja gdje je to moguće.',
			designType: 'repeated_linked_change',
			intendedUse: 'research_analysis',
			interpretationBoundary:
				'Uspoređujte promjenu samo kada upitnik, bodovanje i postavke ponovljenog sudjelovanja ostanu usporedivi.',
			ownerNotes: ''
		},		nextStepsTitle: 'Zatim ćete prvo mjerenje pripremiti s usporedbom na umu',
		nextSteps: [
			{ label: 'Plan mjerenja', description: 'Počnite s Mjerenjem 1 i zadržite postavke spremne za kasniju usporedbu.' },
			{ label: 'Način povezivanja', description: 'Koristite postavku ponavljanog sudjelovanja kada se isti sudionik uspoređuje kroz vrijeme.' },
			{ label: 'Rezultati', description: 'Pregledajte trendove grupa i promjenu istih sudionika kada ima dovoljno podataka.' }
		]
	},
	osh_ergonomics_study: {
		eyebrow: 'Rad i ergonomija',
		title: 'Pregled rada i ergonomije',
		summary:
			'Počnite s praktičnom studijom o izloženosti zadacima, nelagodi, oporavku i daljnjim koracima.',
		bestFor: 'Ergonomske procjene, zaštitu zdravlja na radu i provjere dobrobiti.',
		namePlaceholder: 'npr. Puls opterećenja i oporavka u skladištu',
		highlights: ['Mješoviti formati odgovora', 'Rezultati po dimenzijama', 'Grupe primatelja'],
		briefDefaults: {
			purpose: 'Procijenite radnu izloženost, opterećenje, oporavak i praktične potrebe za praćenje.',
			audience: 'Radnici ili timovi odabrani za pregled rada i ergonomije.',
			designType: 'repeated_group_trend',
			intendedUse: 'client_report',
			interpretationBoundary:
				'Koristite rezultate kao ulaz za praktični pregled. Uz svaki sažetak za dionike zadržite metodološke granice i kontekst praćenja.',
			ownerNotes: ''
		},		nextStepsTitle: 'Zatim ćete prilagoditi praktični početni model rada i ergonomije',
		nextSteps: [
			{ label: 'Izloženost zadacima', description: 'Mapirajte izloženost zadacima i grupe primatelja.' },
			{ label: 'Upitnik', description: 'Pregledajte početni upitnik i prilagodite formulacije radnom mjestu.' },
			{ label: 'Rezultati', description: 'Planirajte izlazne rezultate prije pokretanja prvog mjerenja.' }
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
