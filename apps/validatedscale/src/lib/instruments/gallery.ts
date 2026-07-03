import type {
	CreateQuestionScaleRequest,
	CreateTemplateQuestionRequest,
	CreateTemplateSectionRequest
} from '$lib/api/setup';

/**
 * Curated instrument gallery.
 *
 * Auto-load entries ship verbatim item text ONLY for instruments whose legal
 * status permits it (public domain, or free-standard with attribution) — the
 * per-instrument evidence lives in docs/v2/10-domain/instrument-gallery-evidence.md.
 * They import as tenant-provided content (rights attested, citation prefilled).
 * Rights-required entries are listed with guidance and never ship item text.
 */

export type GalleryTemplate = {
	sections: CreateTemplateSectionRequest[];
	scales: CreateQuestionScaleRequest[];
	questions: CreateTemplateQuestionRequest[];
};

export type GalleryInstrument = {
	code: string;
	version: string;
	name: string;
	measures: string;
	audience: string;
	itemCount: number;
	minutes: string;
	license: string;
	licenseBadge: 'public-domain' | 'free' | 'rights-required';
	citation: string;
	provenance: string;
	autoload: boolean;
	howToGet?: string;
	template?: GalleryTemplate;
	scoringDocument?: string;
	scoringProduces?: string;
	scoringKey?: string;
};

const FOUR_POINT_ANCHORS = JSON.stringify([
	{ value: 0, label: 'Not at all' },
	{ value: 1, label: 'Several days' },
	{ value: 2, label: 'More than half the days' },
	{ value: 3, label: 'Nearly every day' }
]);

const YES_NO = JSON.stringify({
	options: [
		{ code: 'yes', label: 'Yes' },
		{ code: 'no', label: 'No' }
	]
});

function likert(
	ordinal: number,
	code: string,
	text: string,
	scaleCode: string
): CreateTemplateQuestionRequest {
	return {
		ordinal,
		code,
		type: 'likert',
		textDefault: text,
		sectionCode: 'MAIN',
		scaleCode,
		required: true,
		reverseCoded: false,
		measurementLevel: null,
		payload: '{}',
		missingCodes: '[]'
	};
}

function yesNo(
	ordinal: number,
	code: string,
	text: string,
	sectionCode: string
): CreateTemplateQuestionRequest {
	return {
		ordinal,
		code,
		type: 'single',
		textDefault: text,
		sectionCode,
		scaleCode: null,
		required: true,
		reverseCoded: false,
		measurementLevel: null,
		payload: YES_NO,
		missingCodes: '[]'
	};
}

function sumDocument(ruleId: string, outputs: { code: string; items: string[] }[]): string {
	const inputs = outputs.map((output) => ({
		id: `${output.code}_items`,
		kind: 'answers',
		items: output.items
	}));
	const nodes = outputs.flatMap((output) => [
		{ id: `${output.code}_answers`, op: 'select_answers', input: `${output.code}_items` },
		{
			id: `${output.code}_score`,
			op: 'sum',
			input: `${output.code}_answers`,
			missing_data: { strategy: 'require_all' }
		}
	]);
	return JSON.stringify({
		rule_id: ruleId,
		rule_version: '1.0.0',
		schema_version: '1.0.0',
		engine_min_version: '1.0.0',
		scale_defaults: {},
		inputs,
		nodes,
		outputs: outputs.map((output) => ({ code: output.code, node: `${output.code}_score` })),
		missing_data: { defaults: { strategy: 'require_all' } }
	});
}

/* ---------- GAD-7 (public domain — Pfizer screeners) ---------- */

const GAD7_ITEMS = [
	'Feeling nervous, anxious, or on edge',
	'Not being able to stop or control worrying',
	'Worrying too much about different things',
	'Trouble relaxing',
	'Being so restless that it is hard to sit still',
	'Becoming easily annoyed or irritable',
	'Feeling afraid, as if something awful might happen'
];

/* ---------- PHQ-4 (public domain — Pfizer screeners) ---------- */

const PHQ4_ITEMS = [
	'Feeling nervous, anxious, or on edge',
	'Not being able to stop or control worrying',
	'Little interest or pleasure in doing things',
	'Feeling down, depressed, or hopeless'
];

/* ---------- NASA-TLX (public domain — NASA) ---------- */

const TLX = [
	['Mental demand', 'How mentally demanding was the task?', 'Very low', 'Very high'],
	['Physical demand', 'How physically demanding was the task?', 'Very low', 'Very high'],
	['Temporal demand', 'How hurried or rushed was the pace of the task?', 'Very low', 'Very high'],
	[
		'Performance',
		'How successful were you in accomplishing what you were asked to do?',
		'Perfect',
		'Failure'
	],
	[
		'Effort',
		'How hard did you have to work to accomplish your level of performance?',
		'Very low',
		'Very high'
	],
	[
		'Frustration',
		'How insecure, discouraged, irritated, stressed, and annoyed were you?',
		'Very low',
		'Very high'
	]
] as const;

/* ---------- NMQ (free standard — Kuorinka et al. 1987) ---------- */

const NMQ_REGIONS = [
	['neck', 'Neck'],
	['shoulders', 'Shoulders'],
	['elbows', 'Elbows'],
	['wrists', 'Wrists/hands'],
	['upper_back', 'Upper back'],
	['lower_back', 'Lower back'],
	['hips', 'Hips/thighs'],
	['knees', 'Knees'],
	['ankles', 'Ankles/feet']
] as const;

function nmqTemplate(): GalleryTemplate {
	const sections = [
		{ ordinal: 1, code: 'M12', titleDefault: 'Trouble during the last 12 months' },
		{ ordinal: 2, code: 'PREV', titleDefault: 'Prevented from normal work during the last 12 months' },
		{ ordinal: 3, code: 'D7', titleDefault: 'Trouble during the last 7 days' }
	];
	const questions: CreateTemplateQuestionRequest[] = [];
	let ordinal = 1;
	for (const [code, label] of NMQ_REGIONS) {
		questions.push(
			yesNo(
				ordinal++,
				`M12_${code}`,
				`Have you at any time during the last 12 months had trouble (such as ache, pain, discomfort, numbness) in: ${label}?`,
				'M12'
			)
		);
	}
	for (const [code, label] of NMQ_REGIONS) {
		questions.push(
			yesNo(
				ordinal++,
				`PREV_${code}`,
				`Have you at any time during the last 12 months been prevented from doing your normal work (at home or away from home) because of the trouble in: ${label}?`,
				'PREV'
			)
		);
	}
	for (const [code, label] of NMQ_REGIONS) {
		questions.push(
			yesNo(
				ordinal++,
				`D7_${code}`,
				`Have you had trouble in: ${label} at any time during the last 7 days?`,
				'D7'
			)
		);
	}
	return { sections, scales: [], questions };
}

function nmqScoring(): string {
	const items = NMQ_REGIONS.map(([code]) => `M12_${code}`);
	return JSON.stringify({
		rule_id: 'nmq_regions_affected',
		rule_version: '1.0.0',
		schema_version: '1.0.0',
		engine_min_version: '1.0.0',
		scale_defaults: {},
		inputs: [{ id: 'regions_items', kind: 'answers', items }],
		nodes: [
			{
				id: 'regions_answers',
				op: 'map_choice_scores',
				input: 'regions_items',
				option_scores: Object.fromEntries(items.map((item) => [item, { yes: 1, no: 0 }]))
			},
			{
				id: 'regions_score',
				op: 'sum',
				input: 'regions_answers',
				missing_data: { strategy: 'require_all' }
			}
		],
		outputs: [{ code: 'regions_affected', node: 'regions_score' }],
		missing_data: { defaults: { strategy: 'require_all' } }
	});
}

export const gallery: GalleryInstrument[] = [
	{
		code: 'GAD-7',
		version: '1.0',
		name: 'Generalized Anxiety Disorder scale (GAD-7)',
		measures:
			'Anxiety severity over the past two weeks. Seven statements rated 0–3; total 0–21 with established cut-offs (5 mild, 10 moderate, 15 severe).',
		audience: 'Wellbeing monitoring, occupational health, student wellbeing',
		itemCount: 7,
		minutes: '2',
		license: 'Public domain — developed with Pfizer support; no permission required',
		licenseBadge: 'public-domain',
		citation:
			'Spitzer RL, Kroenke K, Williams JBW, Löwe B. A brief measure for assessing generalized anxiety disorder: the GAD-7. Arch Intern Med. 2006;166(10):1092–1097.',
		provenance:
			'Public-domain screener (phqscreeners.com states no permission required). Item text reproduced verbatim from the published instrument.',
		autoload: true,
		scoringKey: 'gad7_score',
		scoringDocument: sumDocument('gad7_score', [
			{ code: 'total', items: GAD7_ITEMS.map((_, i) => `Q${String(i + 1).padStart(2, '0')}`) }
		]),
		scoringProduces: JSON.stringify({ scores: ['total'] }),
		template: {
			sections: [
				{
					ordinal: 1,
					code: 'MAIN',
					titleDefault:
						'Over the last 2 weeks, how often have you been bothered by the following problems?'
				}
			],
			scales: [
				{
					code: 'S4',
					type: 'likert',
					minValue: 0,
					maxValue: 3,
					step: 1,
					naAllowed: false,
					anchors: FOUR_POINT_ANCHORS
				}
			],
			questions: GAD7_ITEMS.map((text, i) =>
				likert(i + 1, `Q${String(i + 1).padStart(2, '0')}`, text, 'S4')
			)
		}
	},
	{
		code: 'PHQ-4',
		version: '1.0',
		name: 'Patient Health Questionnaire-4 (PHQ-4)',
		measures:
			'Ultra-brief anxiety + depression screen. Four statements rated 0–3; anxiety and depression subscores (0–6 each, cut-off 3) and a total (0–12).',
		audience: 'Pulse checks where every item counts',
		itemCount: 4,
		minutes: '1',
		license: 'Public domain — developed with Pfizer support; no permission required',
		licenseBadge: 'public-domain',
		citation:
			'Kroenke K, Spitzer RL, Williams JBW, Löwe B. An ultra-brief screening scale for anxiety and depression: the PHQ-4. Psychosomatics. 2009;50(6):613–621.',
		provenance:
			'Public-domain screener (phqscreeners.com states no permission required). Item text reproduced verbatim from the published instrument.',
		autoload: true,
		scoringKey: 'phq4_score',
		scoringDocument: sumDocument('phq4_score', [
			{ code: 'anxiety', items: ['Q01', 'Q02'] },
			{ code: 'depression', items: ['Q03', 'Q04'] },
			{ code: 'total', items: ['Q01', 'Q02', 'Q03', 'Q04'] }
		]),
		scoringProduces: JSON.stringify({ scores: ['anxiety', 'depression', 'total'] }),
		template: {
			sections: [
				{
					ordinal: 1,
					code: 'MAIN',
					titleDefault:
						'Over the last 2 weeks, how often have you been bothered by the following problems?'
				}
			],
			scales: [
				{
					code: 'S4',
					type: 'likert',
					minValue: 0,
					maxValue: 3,
					step: 1,
					naAllowed: false,
					anchors: FOUR_POINT_ANCHORS
				}
			],
			questions: PHQ4_ITEMS.map((text, i) =>
				likert(i + 1, `Q${String(i + 1).padStart(2, '0')}`, text, 'S4')
			)
		}
	},
	{
		code: 'NASA-TLX',
		version: '1.0',
		name: 'NASA Task Load Index (raw TLX)',
		measures:
			'Perceived workload across six dimensions — mental, physical and temporal demand, performance, effort, frustration. Raw-TLX scoring: mean of the six ratings.',
		audience: 'Ergonomics and workload studies — the standard workload instrument',
		itemCount: 6,
		minutes: '2',
		license: 'Public domain — developed at NASA Ames Research Center (US government work)',
		licenseBadge: 'public-domain',
		citation:
			'Hart SG, Staveland LE. Development of NASA-TLX: results of empirical and theoretical research. In: Hancock PA, Meshkati N, eds. Human Mental Workload. 1988:139–183.',
		provenance:
			'US government work, public domain. Rating-scale definitions per the NASA TLX manual; administered here as 0–20 ratings (raw TLX, unweighted).',
		autoload: true,
		scoringKey: 'nasa_tlx_raw',
		scoringDocument: JSON.stringify({
			rule_id: 'nasa_tlx_raw',
			rule_version: '1.0.0',
			schema_version: '1.0.0',
			engine_min_version: '1.0.0',
			scale_defaults: {},
			inputs: [
				{ id: 'total_items', kind: 'answers', items: TLX.map((_, i) => `Q${String(i + 1).padStart(2, '0')}`) }
			],
			nodes: [
				{ id: 'total_answers', op: 'select_answers', input: 'total_items' },
				{
					id: 'total_score',
					op: 'mean',
					input: 'total_answers',
					missing_data: { strategy: 'require_all' }
				}
			],
			outputs: [{ code: 'total', node: 'total_score' }],
			missing_data: { defaults: { strategy: 'require_all' } }
		}),
		scoringProduces: JSON.stringify({ scores: ['total'] }),
		template: {
			sections: [{ ordinal: 1, code: 'MAIN', titleDefault: 'Task load' }],
			scales: TLX.map((dimension, i) => ({
				code: `S${String(i + 1).padStart(2, '0')}`,
				type: 'likert' as const,
				minValue: 0,
				maxValue: 20,
				step: 1,
				naAllowed: false,
				anchors: JSON.stringify([
					{ value: 0, label: dimension[2] },
					{ value: 20, label: dimension[3] }
				])
			})),
			questions: TLX.map((dimension, i) =>
				likert(
					i + 1,
					`Q${String(i + 1).padStart(2, '0')}`,
					`${dimension[0]} — ${dimension[1]}`,
					`S${String(i + 1).padStart(2, '0')}`
				)
			)
		}
	},
	{
		code: 'NMQ',
		version: '1.0',
		name: 'Nordic Musculoskeletal Questionnaire (general)',
		measures:
			'Musculoskeletal trouble in nine body regions — last 12 months, work prevention, last 7 days. Score: number of regions affected in the last 12 months (0–9).',
		audience: 'Occupational ergonomics — the standard MSK symptom survey',
		itemCount: 27,
		minutes: '5',
		license: 'Free standard instrument; cite Kuorinka et al. 1987',
		licenseBadge: 'free',
		citation:
			'Kuorinka I, Jonsson B, Kilbom A, et al. Standardised Nordic questionnaires for the analysis of musculoskeletal symptoms. Appl Ergon. 1987;18(3):233–237.',
		provenance:
			'Standardised Nordic questionnaire, freely used and reproduced across occupational-health research since 1987. General-section structure per the original publication.',
		autoload: true,
		scoringKey: 'nmq_regions_affected',
		scoringDocument: nmqScoring(),
		scoringProduces: JSON.stringify({ scores: ['regions_affected'] }),
		template: nmqTemplate()
	},
	{
		code: 'COPSOQ-III',
		version: '—',
		name: 'Copenhagen Psychosocial Questionnaire III',
		measures: 'Psychosocial work environment — the EU standard for psychosocial risk assessment.',
		audience: 'OSH consultants, workplace risk assessment',
		itemCount: 32,
		minutes: '10',
		license: 'Free, but requires registration with the COPSOQ International Network and acceptance of its terms',
		licenseBadge: 'rights-required',
		citation: 'Burr H, et al. The third version of the Copenhagen Psychosocial Questionnaire. Saf Health Work. 2019;10(4):482–503.',
		provenance: '',
		autoload: false,
		howToGet:
			'Register at copsoq-network.org, download the questionnaire under their terms, then import it above.'
	},
	{
		code: 'UWES-9',
		version: '—',
		name: 'Utrecht Work Engagement Scale (UWES-9)',
		measures: 'Work engagement — vigor, dedication, absorption.',
		audience: 'Engagement research',
		itemCount: 9,
		minutes: '3',
		license: 'Free for non-commercial research; commercial use requires permission from the authors',
		licenseBadge: 'rights-required',
		citation: 'Schaufeli WB, Bakker AB, Salanova M. Educ Psychol Meas. 2006;66(4):701–716.',
		provenance: '',
		autoload: false,
		howToGet:
			'Free for research from wilmarschaufeli.nl; confirm your use qualifies, then import it above.'
	},
	{
		code: 'PSS-10',
		version: '—',
		name: 'Perceived Stress Scale (PSS-10)',
		measures: 'Perceived stress over the last month.',
		audience: 'Stress research',
		itemCount: 10,
		minutes: '3',
		license: 'Free for nonprofit academic research; other use requires permission',
		licenseBadge: 'rights-required',
		citation: 'Cohen S, Kamarck T, Mermelstein R. J Health Soc Behav. 1983;24(4):385–396.',
		provenance: '',
		autoload: false,
		howToGet:
			'Confirm your use is nonprofit research (or obtain permission), then import the published items above.'
	},
	{
		code: 'MBI',
		version: '—',
		name: 'Maslach Burnout Inventory (MBI)',
		measures: 'Burnout — emotional exhaustion, depersonalization, personal accomplishment.',
		audience: 'Burnout research (licensed)',
		itemCount: 22,
		minutes: '5',
		license: 'Commercial — licensed by Mind Garden; never usable without a purchased license',
		licenseBadge: 'rights-required',
		citation: 'Maslach C, Jackson SE. J Occup Behav. 1981;2(2):99–113.',
		provenance: '',
		autoload: false,
		howToGet: 'Purchase a license from Mind Garden, then import it above under that license.'
	}
];
