import type { CreateQuestionScaleRequest, CreateTemplateQuestionRequest } from '$lib/api/setup';

export type TemplateQuestionAnswerType =
	| 'likert'
	| 'single'
	| 'multi'
	| 'number'
	| 'text'
	| 'date'
	| 'nps'
	| 'ranking';

export type QuestionScalePreset =
	| 'agreement_5'
	| 'agreement_4'
	| 'frequency_5'
	| 'intensity_5'
	| 'discomfort_0_10'
	| 'custom';

export type StudyAuthoringPresetId = 'blank' | 'osh_ergonomics';

export type StudyAuthoringPresetOption = {
	id: StudyAuthoringPresetId;
	label: string;
	summary: string;
	detail: string;
	questionCount: number;
};

export type QuestionScalePresetOption = {
	value: QuestionScalePreset;
	label: string;
	detail: string;
};

export type TemplateQuestionAuthoringRow = {
	ordinal: number;
	code: string;
	dimensionLabel: string;
	type: TemplateQuestionAnswerType;
	textDefault: string;
	required: boolean;
	reverseCoded: boolean;
	scaleMin: number;
	scaleMax: number;
	scaleLowLabel: string;
	scaleHighLabel: string;
	scalePreset: QuestionScalePreset;
	choiceOptions: string[];
};

export type TemplateQuestionMoveDirection = 'up' | 'down';

export type MeanScoringDocumentOptions = {
	outputCode?: string;
	aggregation?: 'mean' | 'sum';
	includedQuestionCodes?: string[];
	missingStrategy?: 'require_all' | 'min_valid_count';
	minValidCount?: number;
};

export type ScoreCalculation = 'mean' | 'sum';
export type ScoreMissingStrategy = 'require_all' | 'min_valid_count';

export type ScoreOutputAuthoringRow = {
	localId: string;
	name: string;
	code: string;
	calculation: ScoreCalculation;
	missingStrategy: ScoreMissingStrategy;
	minValidCount: number;
	includedQuestionCodes: string[];
};

export type ScorePlanOutputSummary = {
	localId: string;
	code: string;
	name: string;
	includedQuestionCount: number;
	dimensionLabels: string[];
	reverseScoredQuestionCount: number;
	calculationLabel: string;
	missingPolicyLabel: string;
};

export type QuestionAuthoringCardSummary = {
	code: string;
	title: string;
	dimensionLabel: string;
	scaleLabel: string;
	requiredLabel: string;
	resultUsageLabel: string;
};

export type ScoreMissingDataStrategySummary = {
	label: string;
	detail: string;
};

export type ReverseScoringReviewSummary = {
	reverseScoredQuestionCount: number;
	reverseScoredQuestionLabels: string[];
	affectedResultLabels: string[];
};

export type CollectedContextQuestionSummary = {
	code: string;
	dimensionLabel: string;
	typeLabel: string;
	text: string;
};

export type RespondentQuestionPreviewSummary = {
	ordinal: number;
	positionLabel: string;
	dimensionLabel: string;
	text: string;
	requiredLabel: string;
	answerFormatLabel: string;
	answerFormatDetail: string;
	responsePreviewLabel: string;
};

export type AuthoringReadinessSummary = {
	dimensionCount: number;
	questionCount: number;
	scoredQuestionCount: number;
	contextQuestionCount: number;
	resultOutputCount: number;
	reverseScoredQuestionCount: number;
	label: string;
};

export type QuestionnaireBlueprintReviewItemId =
	| 'constructs'
	| 'answer_formats'
	| 'respondent_order'
	| 'requiredness'
	| 'results';

export type QuestionnaireBlueprintReviewItem = {
	id: QuestionnaireBlueprintReviewItemId;
	label: string;
	status: 'ready' | 'attention';
	detail: string;
};

export type QuestionnaireBlueprintReview = {
	label: string;
	items: QuestionnaireBlueprintReviewItem[];
};

export type ResultsBlueprintReviewItemId =
	| 'outputs'
	| 'coverage'
	| 'missing_answers'
	| 'scale_compatibility'
	| 'direction'
	| 'interpretation'
	| 'export_schema';

export type ResultsBlueprintReviewItem = {
	id: ResultsBlueprintReviewItemId;
	label: string;
	status: 'ready' | 'attention';
	detail: string;
};

export type ResultsBlueprintReview = {
	label: string;
	items: ResultsBlueprintReviewItem[];
};

export type QuestionScoringDirectionKind =
	| 'higher_increases_score'
	| 'higher_reversed_before_score'
	| 'number_increases_score'
	| 'not_scored';

export type QuestionScoringDirectionSummary = {
	kind: QuestionScoringDirectionKind;
	label: string;
	detail: string;
};

export type QuestionScaleIntentKind =
	| 'agreement'
	| 'frequency'
	| 'intensity'
	| 'recommendation'
	| 'number'
	| 'choice'
	| 'written'
	| 'date'
	| 'ranking'
	| 'rating';

export type QuestionScaleIntentSummary = {
	kind: QuestionScaleIntentKind;
	label: string;
	detail: string;
};

export type QuestionDimensionSummary = {
	code: string;
	label: string;
	questionCount: number;
};

const defaultQuestionText = [
	'Write the first question for this study.',
	'Write the second question for this study.',
	'Write the third question for this study.'
];

const defaultQuestionDimensions = ['Topic 1', 'Topic 2', 'Topic 3'];

const defaultChoiceOptions = ['Option 1', 'Option 2'];

export const questionScalePresetOptions: QuestionScalePresetOption[] = [
	{
		value: 'agreement_5',
		label: 'Agreement, 1-5',
		detail: 'Strongly disagree to strongly agree, with a middle option.'
	},
	{
		value: 'agreement_4',
		label: 'Agreement, 1-4',
		detail: 'Strongly disagree to strongly agree, without a middle option.'
	},
	{
		value: 'frequency_5',
		label: 'Frequency, 1-5',
		detail: 'Never to always.'
	},
	{
		value: 'intensity_5',
		label: 'Intensity, 1-5',
		detail: 'Very low to very high.'
	},
	{
		value: 'discomfort_0_10',
		label: 'Discomfort severity, 0-10',
		detail: 'No discomfort to worst imaginable discomfort.'
	},
	{
		value: 'custom',
		label: 'Custom',
		detail: 'Keep the current scale values and labels.'
	}
];

export function createDefaultTemplateQuestionRows(): TemplateQuestionAuthoringRow[] {
	return defaultQuestionText.map((textDefault, index) =>
		createQuestionRow({
			ordinal: index + 1,
			code: `q${String(index + 1).padStart(2, '0')}`,
			dimensionLabel: defaultQuestionDimensions[index] ?? 'Study measure',
			textDefault,
			reverseCoded: index === 2
		})
	);
}

export function listStudyAuthoringPresetOptions(): StudyAuthoringPresetOption[] {
	return [
		{
			id: 'blank',
			label: 'Blank study',
			summary: 'Start with three editable placeholder questions.',
			detail: 'Use this when the study structure is already clear and you want to write everything manually.',
			questionCount: 3
		},
		{
			id: 'osh_ergonomics',
			label: 'OSH / ergonomics starter',
			summary: 'Start from a practical workplace-risk questionnaire with mixed answer formats.',
			detail:
				'Includes task exposure, manual handling, posture frequency, discomfort severity, recovery, existing controls, priority ranking, and open context.',
			questionCount: 8
		}
	];
}

export function createTemplateQuestionRowsForStudyPreset(
	presetId: StudyAuthoringPresetId
): TemplateQuestionAuthoringRow[] {
	if (presetId === 'osh_ergonomics') {
		return createOshErgonomicsTemplateQuestionRows();
	}

	return createDefaultTemplateQuestionRows();
}

function createOshErgonomicsTemplateQuestionRows(): TemplateQuestionAuthoringRow[] {
	return renumberRows([
		createQuestionRow({
			ordinal: 1,
			code: 'primary_tasks',
			dimensionLabel: 'Task profile',
			type: 'multi',
			textDefault: 'Which tasks were part of your normal work during this period?',
			choiceOptions: [
				'Manual handling or lifting',
				'Repetitive hand or arm work',
				'Prolonged sitting or screen work',
				'Standing or walking for long periods',
				'Driving or mobile work',
				'Other task pattern'
			],
			reverseCoded: false
		}),
		createQuestionRow({
			ordinal: 2,
			code: 'manual_handling_exposure',
			dimensionLabel: 'Manual handling',
			type: 'single',
			textDefault: 'How much manual handling or forceful work did this period include?',
			choiceOptions: ['None or almost none', 'Occasional', 'Frequent', 'Most of the shift'],
			reverseCoded: false
		}),
		createQuestionRow({
			ordinal: 3,
			code: 'awkward_posture_frequency',
			dimensionLabel: 'Posture and repetition',
			textDefault:
				'I often worked in awkward postures, repetitive movements, or fixed positions.',
			scalePreset: 'frequency_5',
			scaleLowLabel: 'Never',
			scaleHighLabel: 'Always'
		}),
		createQuestionRow({
			ordinal: 4,
			code: 'discomfort_severity',
			dimensionLabel: 'Discomfort',
			type: 'number',
			textDefault:
				'What was your highest work-related physical discomfort level during this period?',
			scalePreset: 'discomfort_0_10',
			scaleMin: 0,
			scaleMax: 10,
			scaleLowLabel: 'No discomfort',
			scaleHighLabel: 'Worst imaginable discomfort'
		}),
		createQuestionRow({
			ordinal: 5,
			code: 'break_recovery',
			dimensionLabel: 'Recovery and control',
			textDefault: 'I had enough breaks, task variation, or recovery time to manage strain.',
			reverseCoded: true
		}),
		createQuestionRow({
			ordinal: 6,
			code: 'equipment_support',
			dimensionLabel: 'Existing controls',
			type: 'single',
			textDefault: 'Were suitable tools, workstation adjustments, or aids available when needed?',
			choiceOptions: ['Yes', 'Partly', 'No', 'Not applicable'],
			reverseCoded: false
		}),
		createQuestionRow({
			ordinal: 7,
			code: 'change_priority',
			dimensionLabel: 'Intervention priorities',
			type: 'ranking',
			textDefault: 'Rank the changes that would most reduce strain for this work.',
			choiceOptions: [
				'Task rotation or work pacing',
				'Equipment or workstation changes',
				'Staffing, scheduling, or workload change',
				'Training, instructions, or supervision'
			],
			reverseCoded: false
		}),
		createQuestionRow({
			ordinal: 8,
			code: 'worker_context',
			dimensionLabel: 'Worker context',
			type: 'text',
			textDefault:
				'What context should be understood before interpreting your answers or planning changes?',
			required: false,
			reverseCoded: false
		})
	]);
}

export function appendTemplateQuestionRow(
	rows: TemplateQuestionAuthoringRow[]
): TemplateQuestionAuthoringRow[] {
	const nextCode = nextQuestionCode(rows);
	return renumberRows([
		...rows,
		createQuestionRow({
			ordinal: rows.length + 1,
			code: nextCode,
			textDefault: 'New question text'
		})
	]);
}

export function duplicateTemplateQuestionRow(
	rows: TemplateQuestionAuthoringRow[],
	code: string
): TemplateQuestionAuthoringRow[] {
	const index = rows.findIndex((row) => row.code === code);
	if (index < 0) {
		return renumberRows(rows);
	}

	const source = rows[index];
	const duplicate: TemplateQuestionAuthoringRow = {
		...source,
		ordinal: source.ordinal + 1,
		code: nextQuestionCode(rows),
		textDefault: source.textDefault.trim()
			? `${source.textDefault.trim()} (copy)`
			: `${source.code} copy`,
		choiceOptions: [...source.choiceOptions]
	};

	return renumberRows([...rows.slice(0, index + 1), duplicate, ...rows.slice(index + 1)]);
}

export function removeTemplateQuestionRow(
	rows: TemplateQuestionAuthoringRow[],
	code: string
): TemplateQuestionAuthoringRow[] {
	if (rows.length <= 1) {
		return renumberRows(rows);
	}

	return renumberRows(rows.filter((row) => row.code !== code));
}

export function moveTemplateQuestionRow(
	rows: TemplateQuestionAuthoringRow[],
	code: string,
	direction: TemplateQuestionMoveDirection
): TemplateQuestionAuthoringRow[] {
	const index = rows.findIndex((row) => row.code === code);
	if (index < 0) {
		return renumberRows(rows);
	}

	const targetIndex = direction === 'up' ? index - 1 : index + 1;
	if (targetIndex < 0 || targetIndex >= rows.length) {
		return renumberRows(rows);
	}

	const nextRows = [...rows];
	const current = nextRows[index];
	nextRows[index] = nextRows[targetIndex];
	nextRows[targetIndex] = current;
	return renumberRows(nextRows);
}

export function validateTemplateQuestionRows(rows: TemplateQuestionAuthoringRow[]): string[] {
	const errors: string[] = [];
	const seenCodes = new Set<string>();
	const duplicateCodes = new Set<string>();

	rows.forEach((row, index) => {
		const code = row.code.trim();
		if (!code) {
			errors.push(`Question ${index + 1} needs an internal code.`);
		}

		if (!row.textDefault.trim()) {
			errors.push(`Question ${index + 1} needs question text.`);
		}

		if (isScaleBackedType(row.type)) {
			if (!Number.isFinite(row.scaleMin) || !Number.isFinite(row.scaleMax) || row.scaleMin >= row.scaleMax) {
				errors.push(`Question ${index + 1} needs a valid scale range.`);
			}

			if (!row.scaleLowLabel.trim() || !row.scaleHighLabel.trim()) {
				errors.push(`Question ${index + 1} needs labels for both ends of the scale.`);
			}
		}

		if (
			(row.type === 'single' || row.type === 'multi' || row.type === 'ranking') &&
			normalizedChoiceOptions(row).length < 2
		) {
			errors.push(`Question ${index + 1} needs at least two answer options.`);
		}
	});

	for (const row of rows) {
		const code = row.code.trim();
		if (!code) {
			continue;
		}

		const normalizedCode = code.toLowerCase();
		if (seenCodes.has(normalizedCode) && !duplicateCodes.has(normalizedCode)) {
			errors.push(`Question code ${code} is duplicated.`);
			duplicateCodes.add(normalizedCode);
		}

		seenCodes.add(normalizedCode);
	}

	return errors;
}

export function applyQuestionScalePreset(
	row: TemplateQuestionAuthoringRow,
	preset: QuestionScalePreset
): TemplateQuestionAuthoringRow {
	if (preset === 'custom') {
		return { ...row, scalePreset: 'custom' };
	}

	const presetValues: Record<
		Exclude<QuestionScalePreset, 'custom'>,
		Pick<TemplateQuestionAuthoringRow, 'scaleMin' | 'scaleMax' | 'scaleLowLabel' | 'scaleHighLabel'>
	> = {
		agreement_5: {
			scaleMin: 1,
			scaleMax: 5,
			scaleLowLabel: 'Strongly disagree',
			scaleHighLabel: 'Strongly agree'
		},
		agreement_4: {
			scaleMin: 1,
			scaleMax: 4,
			scaleLowLabel: 'Strongly disagree',
			scaleHighLabel: 'Strongly agree'
		},
		frequency_5: {
			scaleMin: 1,
			scaleMax: 5,
			scaleLowLabel: 'Never',
			scaleHighLabel: 'Always'
		},
		intensity_5: {
			scaleMin: 1,
			scaleMax: 5,
			scaleLowLabel: 'Very low',
			scaleHighLabel: 'Very high'
		},
		discomfort_0_10: {
			scaleMin: 0,
			scaleMax: 10,
			scaleLowLabel: 'No discomfort',
			scaleHighLabel: 'Worst imaginable discomfort'
		}
	};

	return {
		...row,
		scalePreset: preset,
		...presetValues[preset]
	};
}

export function toCreateQuestionScales(
	rows: TemplateQuestionAuthoringRow[]
): CreateQuestionScaleRequest[] {
	return renumberRows(rows)
		.filter((row) => isScaleBackedType(row.type))
		.map((row) => ({
			code: questionScaleCode(row),
			type: row.type === 'nps' ? 'nps' : 'likert',
			minValue: row.scaleMin,
			maxValue: row.scaleMax,
			step: 1,
			naAllowed: false,
			anchors: JSON.stringify([
				{ value: row.scaleMin, label: row.scaleLowLabel.trim() },
				{ value: row.scaleMax, label: row.scaleHighLabel.trim() }
			])
		}));
}

export function toCreateTemplateQuestions(
	rows: TemplateQuestionAuthoringRow[]
): CreateTemplateQuestionRequest[] {
	return renumberRows(rows).map((row) => ({
		ordinal: row.ordinal,
		code: row.code.trim(),
		type: row.type,
		textDefault: row.textDefault.trim(),
		sectionCode: dimensionCodeForLabel(row.dimensionLabel),
		scaleCode: isScaleBackedType(row.type) ? questionScaleCode(row) : null,
		required: row.required,
		reverseCoded: isScaleBackedType(row.type) ? row.reverseCoded : false,
		measurementLevel: measurementLevelFor(row),
		payload: questionPayload(row),
		missingCodes: '[]'
	}));
}

export function buildMeanScoringDocument(
	ruleId: string,
	rows: TemplateQuestionAuthoringRow[],
	options: MeanScoringDocumentOptions = {}
): string {
	return buildScoringDocument(ruleId, rows, [
		{
			localId: 'score-total',
			name: options.outputCode ?? 'Total score',
			code: scoreCode(options.outputCode ?? 'total'),
			calculation: options.aggregation ?? 'mean',
			missingStrategy: options.missingStrategy ?? 'require_all',
			minValidCount: Math.max(1, options.minValidCount ?? 1),
			includedQuestionCodes: options.includedQuestionCodes ?? rows.filter(isMeanScoreEligible).map((row) => row.code)
		}
	]);
}

export function createDefaultScoreOutputRows(
	rows: TemplateQuestionAuthoringRow[]
): ScoreOutputAuthoringRow[] {
	return [
		{
			localId: createScoreOutputLocalId(),
			name: 'Total score',
			code: 'total',
			calculation: 'mean',
			missingStrategy: 'require_all',
			minValidCount: 1,
			includedQuestionCodes: rows.filter(isMeanScoreEligible).map((row) => row.code)
		}
	];
}

export function createScoreOutputRowsForStudyPreset(
	presetId: StudyAuthoringPresetId,
	rows: TemplateQuestionAuthoringRow[]
): ScoreOutputAuthoringRow[] {
	if (presetId === 'osh_ergonomics') {
		return createOshErgonomicsScoreOutputRows(rows);
	}

	return [];
}

function createOshErgonomicsScoreOutputRows(
	rows: TemplateQuestionAuthoringRow[]
): ScoreOutputAuthoringRow[] {
	const definitions = [
		{
			name: 'Posture and repetition strain',
			code: 'posture_repetition_strain',
			includedQuestionCodes: ['awkward_posture_frequency']
		},
		{
			name: 'Discomfort severity',
			code: 'discomfort_severity',
			includedQuestionCodes: ['discomfort_severity']
		},
		{
			name: 'Recovery and control',
			code: 'recovery_control',
			includedQuestionCodes: ['break_recovery']
		}
	];
	const eligibleCodeSet = new Set(rows.filter(isMeanScoreEligible).map((row) => row.code));

	return definitions
		.map((definition) => ({
			localId: createScoreOutputLocalId(),
			name: definition.name,
			code: definition.code,
			calculation: 'mean' as const,
			missingStrategy: 'require_all' as const,
			minValidCount: 1,
			includedQuestionCodes: definition.includedQuestionCodes.filter((code) =>
				eligibleCodeSet.has(code)
			)
		}))
		.filter((output) => output.includedQuestionCodes.length > 0);
}

export function appendScoreOutputRow(
	outputs: ScoreOutputAuthoringRow[],
	rows: TemplateQuestionAuthoringRow[]
): ScoreOutputAuthoringRow[] {
	const index = outputs.length + 1;
	const usedOutputCodes = new Set(normalizeScoreCodes(outputs));
	const dimensions = summarizeQuestionDimensions(rows);
	const selectedDimension =
		dimensions.find((dimension) => !usedOutputCodes.has(dimension.code)) ?? dimensions[0];
	const selectedDimensionRows = selectedDimension
		? rows.filter(
				(row) =>
					isMeanScoreEligible(row) &&
					dimensionCodeForLabel(row.dimensionLabel) === selectedDimension.code
			)
		: [];
	const fallbackRows = rows.filter(isMeanScoreEligible);
	const includedRows = selectedDimensionRows.length ? selectedDimensionRows : fallbackRows;
	const defaultName = selectedDimension?.label ?? `Dimension ${index}`;
	const defaultCode = nextScoreCode(
		outputs,
		selectedDimension?.code ?? `dimension_${index}`
	);
	return [
		...outputs,
		{
			localId: createScoreOutputLocalId(),
			name: defaultName,
			code: defaultCode,
			calculation: 'mean',
			missingStrategy: 'require_all',
			minValidCount: 1,
			includedQuestionCodes: includedRows.map((row) => row.code)
		}
	];
}

export function removeScoreOutputRow(
	outputs: ScoreOutputAuthoringRow[],
	localId: string
): ScoreOutputAuthoringRow[] {
	if (outputs.length <= 1) {
		return outputs;
	}

	return outputs.filter((output) => output.localId !== localId);
}

export function syncScoreOutputQuestionCodes(
	outputs: ScoreOutputAuthoringRow[],
	rows: TemplateQuestionAuthoringRow[]
): ScoreOutputAuthoringRow[] {
	const eligibleCodes = rows.filter(isMeanScoreEligible).map((row) => row.code);
	const eligibleCodeSet = new Set(eligibleCodes.map((code) => code.trim().toLowerCase()));

	return outputs.map((output, index) => {
		const retainedCodes = output.includedQuestionCodes.filter((code) =>
			eligibleCodeSet.has(code.trim().toLowerCase())
		);
		const newCodes =
			index === 0
				? eligibleCodes.filter(
						(code) =>
							!retainedCodes.some(
								(candidate) => candidate.trim().toLowerCase() === code.trim().toLowerCase()
							)
					)
				: [];

		return {
			...output,
			includedQuestionCodes: [...retainedCodes, ...newCodes]
		};
	});
}

export function validateScoreOutputRows(
	outputs: ScoreOutputAuthoringRow[],
	rows: TemplateQuestionAuthoringRow[]
): string[] {
	const errors: string[] = [];
	const eligibleCodeSet = new Set(
		rows
			.filter(isMeanScoreEligible)
			.map((row) => row.code.trim().toLowerCase())
			.filter(Boolean)
	);
	const seenCodes = new Set<string>();

	if (!outputs.length) {
		errors.push('Add at least one result output.');
		return errors;
	}

	outputs.forEach((output, index) => {
		const label = output.name.trim() || `Result ${index + 1}`;
		const code = scoreCode(output.code || output.name);

		if (!output.name.trim()) {
			errors.push(`Result ${index + 1} needs a name.`);
		}

		if (!code) {
			errors.push(`${label} needs a result code.`);
		} else if (seenCodes.has(code)) {
			errors.push(`Result code ${code} is duplicated.`);
		} else {
			seenCodes.add(code);
		}

		const selectedCodes = output.includedQuestionCodes.filter((questionCode) =>
			eligibleCodeSet.has(questionCode.trim().toLowerCase())
		);
		if (!selectedCodes.length) {
			errors.push(`${label} needs at least one rating, recommendation, or number question.`);
		}

		const compatibilityWarning = scoreScaleCompatibilityWarning(output, rows);
		if (compatibilityWarning) {
			errors.push(compatibilityWarning);
		}

		if (
			output.missingStrategy === 'min_valid_count' &&
			(!Number.isFinite(output.minValidCount) || Math.trunc(output.minValidCount) < 1)
		) {
			errors.push(`${label} needs a positive minimum answered count.`);
		}
	});

	return errors;
}

export function buildScoringDocument(
	ruleId: string,
	rows: TemplateQuestionAuthoringRow[],
	outputs: ScoreOutputAuthoringRow[]
): string {
	const normalizedOutputs = normalizeScoreOutputs(outputs, rows);
	const reverseCodedRows = rows.filter((row) => row.reverseCoded && isScaleBackedType(row.type));
	const reverseScale = reverseCodedRows[0] ?? rows.find((row) => isScaleBackedType(row.type));
	const inputs = [];
	const nodes = [];
	const outputDefinitions = [];

	for (const output of normalizedOutputs) {
		const inputId = `${output.code}_items`;
		const answersId = `${output.code}_answers`;
		const reverseId = `${output.code}_scored_answers`;
		const scoreNodeId = `${output.code}_score`;
		const outputRows = rows.filter((row) =>
			output.includedQuestionCodes.some(
				(code) => code.trim().toLowerCase() === row.code.trim().toLowerCase()
			)
		);
		const outputReverseRows = outputRows.filter((row) => row.reverseCoded && isScaleBackedType(row.type));
		const aggregateInput = reverseScale && outputReverseRows.length > 0 ? reverseId : answersId;

		inputs.push({ id: inputId, kind: 'answers', items: outputRows.map((row) => row.code.trim()) });
		nodes.push({ id: answersId, op: 'select_answers', input: inputId });

		if (reverseScale && outputReverseRows.length > 0) {
			nodes.push({
				id: reverseId,
				op: 'reverse_code',
				input: answersId,
				scale: 'default_rating',
				reverse_flag_source: 'explicit_list',
				explicit_reverse_items: outputReverseRows.map((row) => row.code.trim())
			});
		}

		nodes.push({
			id: scoreNodeId,
			op: output.calculation,
			input: aggregateInput,
			missing_data: missingPolicyDocument(output)
		});
		outputDefinitions.push({ code: output.code, node: scoreNodeId });
	}

	return JSON.stringify(
		{
			rule_id: ruleId,
			rule_version: '1.0.0',
			schema_version: '1.0.0',
			engine_min_version: '1.0.0',
			scale_defaults: reverseScale
				? {
						default_rating: { min: reverseScale.scaleMin, max: reverseScale.scaleMax }
					}
				: {},
			inputs,
			nodes,
			outputs: outputDefinitions,
			missing_data: {
				defaults: { strategy: 'require_all' }
			}
		},
		null,
		2
	);
}

export function buildScoreProduces(outputs: ScoreOutputAuthoringRow[]): string {
	return JSON.stringify(
		{
			scores: normalizeScoreCodes(outputs)
		},
		null,
		2
	);
}

export function describeQuestionScoringDirection(
	row: TemplateQuestionAuthoringRow
): QuestionScoringDirectionSummary {
	if (row.type === 'number') {
		return {
			kind: 'number_increases_score',
			label: 'Higher numbers increase included result scores',
			detail:
				'Number answers are used as entered in every result output that includes this question.'
		};
	}

	if (!isScaleBackedType(row.type)) {
		return {
			kind: 'not_scored',
			label: 'Collected but not scored',
			detail:
				'This answer format is collected for context, but it is not used in numeric result scores.'
		};
	}

	if (row.reverseCoded) {
		return {
			kind: 'higher_reversed_before_score',
			label: 'Higher answers are reversed before scoring',
			detail: `${row.scaleMax} (${row.scaleHighLabel.trim()}) is converted toward ${row.scaleMin}; ${row.scaleMin} (${row.scaleLowLabel.trim()}) is converted toward ${row.scaleMax}. Use this for protective wording when the result score should still point in one direction.`
		};
	}

	return {
		kind: 'higher_increases_score',
		label: 'Higher answers increase included result scores',
		detail: `${row.scaleMin} (${row.scaleLowLabel.trim()}) to ${row.scaleMax} (${row.scaleHighLabel.trim()}) is used as entered in every result output that includes this question.`
	};
}

export function describeQuestionResultUsage(
	row: TemplateQuestionAuthoringRow,
	outputs: ScoreOutputAuthoringRow[]
): string {
	if (!isMeanScoreEligible(row)) {
		return 'Not available for numeric result outputs.';
	}

	const labels = outputs
		.filter((output) =>
			output.includedQuestionCodes.some(
				(code) => code.trim().toLowerCase() === row.code.trim().toLowerCase()
			)
		)
		.map((output) => output.name.trim() || output.code.trim() || 'Untitled result');

	if (!labels.length) {
		return 'Not included in any result output yet.';
	}

	return `Used in: ${labels.join(', ')}.`;
}

export function describeQuestionScaleIntent(
	row: TemplateQuestionAuthoringRow
): QuestionScaleIntentSummary {
	if (row.type === 'number') {
		return {
			kind: 'number',
			label: 'Number entry',
			detail: 'Respondents enter a numeric value. Use this for counts, minutes, ratings with known units, or direct measurements.'
		};
	}

	if (row.type === 'text') {
		return {
			kind: 'written',
			label: 'Written response',
			detail: 'Respondents write free text. This is collected for review but is not included in numeric scoring.'
		};
	}

	if (row.type === 'date') {
		return {
			kind: 'date',
			label: 'Date',
			detail: 'Respondents provide a date. This is collected for context and is not included in numeric scoring.'
		};
	}

	if (row.type === 'ranking') {
		return {
			kind: 'ranking',
			label: 'Ranking',
			detail: 'Respondents order choices. Ranking can support descriptive review but is not included in current numeric result outputs.'
		};
	}

	if (row.type === 'single' || row.type === 'multi') {
		return {
			kind: 'choice',
			label: row.type === 'single' ? 'Single choice' : 'Multiple choice',
			detail: 'Respondents choose from defined options. This is collected for grouping or context, not current numeric result outputs.'
		};
	}

	if (row.type === 'nps') {
		return {
			kind: 'recommendation',
			label: 'Recommendation scale',
			detail: 'Respondents answer on a recommendation-style numeric scale. Higher values increase included result scores unless reversed.'
		};
	}

	const low = row.scaleLowLabel.trim().toLowerCase();
	const high = row.scaleHighLabel.trim().toLowerCase();

	if (low.includes('never') || high.includes('always')) {
		return {
			kind: 'frequency',
			label: 'Frequency scale',
			detail: 'Respondents report how often something happens. Higher answers increase included result scores unless reversed.'
		};
	}

	if (low.includes('disagree') || high.includes('agree')) {
		return {
			kind: 'agreement',
			label: 'Agreement scale',
			detail: 'Respondents report how much they agree with a statement. Higher answers increase included result scores unless reversed.'
		};
	}

	if (low.includes('low') || high.includes('high') || high.includes('extreme') || high.includes('very')) {
		return {
			kind: 'intensity',
			label: 'Intensity scale',
			detail: 'Respondents report strength or intensity. Higher answers increase included result scores unless reversed.'
		};
	}

	return {
		kind: 'rating',
		label: 'Rating scale',
		detail: 'Respondents answer on a numeric rating scale. Higher answers increase included result scores unless reversed.'
	};
}

export function summarizeQuestionDimensions(
	rows: TemplateQuestionAuthoringRow[]
): QuestionDimensionSummary[] {
	const summaries = new Map<string, QuestionDimensionSummary>();

	for (const row of rows) {
		const label = normalizeDimensionLabel(row.dimensionLabel);
		const code = dimensionCodeForLabel(label);
		const existing = summaries.get(code);

		if (existing) {
			existing.questionCount += 1;
		} else {
			summaries.set(code, { code, label, questionCount: 1 });
		}
	}

	return [...summaries.values()];
}

export function summarizeScorePlan(
	outputs: ScoreOutputAuthoringRow[],
	rows: TemplateQuestionAuthoringRow[]
): ScorePlanOutputSummary[] {
	return normalizeScoreOutputs(outputs, rows).map((output) => {
		const outputRows = rows.filter((row) =>
			output.includedQuestionCodes.some(
				(code) => code.trim().toLowerCase() === row.code.trim().toLowerCase()
			)
		);
		const dimensionLabels = [
			...new Set(outputRows.map((row) => normalizeDimensionLabel(row.dimensionLabel)))
		];

		return {
			localId: output.localId,
			code: output.code,
			name: output.name.trim() || output.code,
			includedQuestionCount: outputRows.length,
			dimensionLabels,
			reverseScoredQuestionCount: outputRows.filter(
				(row) => row.reverseCoded && isScaleBackedType(row.type)
			).length,
			calculationLabel: output.calculation === 'sum' ? 'Sum score' : 'Mean score',
			missingPolicyLabel:
				output.missingStrategy === 'min_valid_count'
					? `Requires at least ${Math.max(1, Math.trunc(output.minValidCount || 1))} selected questions`
					: 'Requires every selected question'
		};
	});
}

export function summarizeResultsBlueprintReview(
	rows: TemplateQuestionAuthoringRow[],
	outputs: ScoreOutputAuthoringRow[]
): ResultsBlueprintReview {
	const scoreableRows = rows.filter(isMeanScoreEligible);
	const normalizedOutputs = normalizeScoreOutputs(outputs, rows);
	const includedQuestionCodes = new Set(
		normalizedOutputs.flatMap((output) =>
			output.includedQuestionCodes.map((code) => code.trim().toLowerCase())
		)
	);
	const includedScoreableCount = scoreableRows.filter((row) =>
		includedQuestionCodes.has(row.code.trim().toLowerCase())
	).length;
	const reverseRows = rows.filter((row) => row.reverseCoded && isMeanScoreEligible(row));
	const affectedReverseCodes = new Set(reverseRows.map((row) => row.code.trim().toLowerCase()));
	const affectedReverseOutputLabels = normalizedOutputs
		.filter((output) =>
			output.includedQuestionCodes.some((code) => affectedReverseCodes.has(code.trim().toLowerCase()))
		)
		.map((output) => output.name.trim() || output.code);
	const allRequireEveryQuestion =
		normalizedOutputs.length > 0 &&
		normalizedOutputs.every((output) => output.missingStrategy === 'require_all');
	const scaleCompatibilityWarnings = normalizedOutputs
		.map((output) => scoreScaleCompatibilityWarning(output, rows))
		.filter((warning): warning is string => Boolean(warning));

	return {
		label: `${normalizedOutputs.length} result ${
			normalizedOutputs.length === 1 ? 'output' : 'outputs'
		}, ${includedScoreableCount} scored ${
			includedScoreableCount === 1 ? 'question' : 'questions'
		}, ${reverseRows.length} reversed`,
		items: [
			{
				id: 'outputs',
				label: 'Result outputs',
				status: normalizedOutputs.length > 0 ? 'ready' : 'attention',
				detail:
					normalizedOutputs.length > 0
						? `${normalizedOutputs.length} result ${
								normalizedOutputs.length === 1 ? 'output' : 'outputs'
							} will be saved: ${formatInlineList(
								normalizedOutputs.map((output) => output.name.trim() || output.code)
							)}.`
						: 'Add at least one result output.'
			},
			{
				id: 'coverage',
				label: 'Question coverage',
				status:
					scoreableRows.length > 0 && includedScoreableCount === scoreableRows.length
						? 'ready'
						: 'attention',
				detail:
					scoreableRows.length > 0
						? `${includedScoreableCount} of ${scoreableRows.length} scoreable ${
								scoreableRows.length === 1 ? 'question is' : 'questions are'
							} included in at least one result output.`
						: 'Add rating, recommendation, or number questions before saving numeric results.'
			},
			{
				id: 'missing_answers',
				label: 'Missing answers',
				status: normalizedOutputs.length > 0 ? 'ready' : 'attention',
				detail: allRequireEveryQuestion
					? 'All outputs require every selected question.'
					: 'At least one output uses a minimum-answered rule; review whether partial answers should still produce a result.'
			},
			{
				id: 'scale_compatibility',
				label: 'Scale compatibility',
				status:
					normalizedOutputs.length > 0 && scaleCompatibilityWarnings.length === 0
						? 'ready'
						: 'attention',
				detail:
					scaleCompatibilityWarnings[0] ??
					(normalizedOutputs.length > 0
						? 'Each result output uses one compatible answer-scale family.'
						: 'Add result outputs before reviewing scale compatibility.')
			},
			{
				id: 'direction',
				label: 'Score direction',
				status: reverseRows.length > 0 ? 'attention' : 'ready',
				detail:
					reverseRows.length > 0
						? `${reverseRows.length} reverse-scored ${
								reverseRows.length === 1 ? 'question affects' : 'questions affect'
							} ${formatInlineList(affectedReverseOutputLabels)}.`
						: 'No questions are reversed before scoring.'
			},
			{
				id: 'interpretation',
				label: 'Interpretation boundary',
				status: 'ready',
				detail:
					'These are custom study result outputs. They describe calculation, not official norms, benchmarks, or validated thresholds.'
			},
			{
				id: 'export_schema',
				label: 'Export schema',
				status: normalizedOutputs.length > 0 ? 'ready' : 'attention',
				detail:
					'CSV/report exports should preserve question codes, answer formats, score outputs, missing-answer rules, and visibility guardrails.'
			}
		]
	};
}

export function summarizeQuestionAuthoringCards(
	rows: TemplateQuestionAuthoringRow[],
	outputs: ScoreOutputAuthoringRow[]
): QuestionAuthoringCardSummary[] {
	return rows.map((row) => ({
		code: row.code,
		title: questionTitle(row),
		dimensionLabel: normalizeDimensionLabel(row.dimensionLabel),
		scaleLabel: describeQuestionScaleIntent(row).label,
		requiredLabel: row.required ? 'Required' : 'Optional',
		resultUsageLabel: describeQuestionResultUsage(row, outputs)
	}));
}

export function describeScoreMissingDataStrategy(
	output: ScoreOutputAuthoringRow
): ScoreMissingDataStrategySummary {
	if (output.missingStrategy === 'min_valid_count') {
		const count = Math.max(1, Math.trunc(output.minValidCount || 1));
		return {
			label: 'Minimum answered rule',
			detail: `A respondent needs at least ${count} selected questions answered for this result score.`
		};
	}

	return {
		label: 'Strict missing-data rule',
		detail: 'A respondent needs every selected question answered for this result score.'
	};
}

export function summarizeReverseScoringReview(
	rows: TemplateQuestionAuthoringRow[],
	outputs: ScoreOutputAuthoringRow[]
): ReverseScoringReviewSummary {
	const reverseRows = rows.filter((row) => row.reverseCoded && isScaleBackedType(row.type));
	const reverseCodeSet = new Set(reverseRows.map((row) => row.code.trim().toLowerCase()));
	const affectedResultLabels = outputs
		.filter((output) =>
			output.includedQuestionCodes.some((code) => reverseCodeSet.has(code.trim().toLowerCase()))
		)
		.map((output) => output.name.trim() || output.code.trim() || 'Untitled result');

	return {
		reverseScoredQuestionCount: reverseRows.length,
		reverseScoredQuestionLabels: reverseRows.map(questionTitle),
		affectedResultLabels
	};
}

export function summarizeCollectedContextQuestions(
	rows: TemplateQuestionAuthoringRow[]
): CollectedContextQuestionSummary[] {
	return rows.filter((row) => !isMeanScoreEligible(row)).map((row) => ({
		code: row.code,
		dimensionLabel: normalizeDimensionLabel(row.dimensionLabel),
		typeLabel: describeQuestionScaleIntent(row).label,
		text: questionTitle(row)
	}));
}

export function summarizeRespondentQuestionPreview(
	rows: TemplateQuestionAuthoringRow[]
): RespondentQuestionPreviewSummary[] {
	const orderedRows = renumberRows(rows);
	const total = orderedRows.length;
	return orderedRows.map((row) => ({
		ordinal: row.ordinal,
		positionLabel: `Question ${row.ordinal} of ${total}`,
		dimensionLabel: normalizeDimensionLabel(row.dimensionLabel),
		text: questionTitle(row),
		requiredLabel: row.required ? 'Required' : 'Optional',
		answerFormatLabel: describeQuestionScaleIntent(row).label,
		answerFormatDetail: answerFormatDetail(row),
		responsePreviewLabel: respondentResponsePreview(row)
	}));
}

export function summarizeAuthoringReadiness(
	rows: TemplateQuestionAuthoringRow[],
	outputs: ScoreOutputAuthoringRow[]
): AuthoringReadinessSummary {
	const dimensionCount = summarizeQuestionDimensions(rows).length;
	const scoredQuestionCount = rows.filter(isMeanScoreEligible).length;
	const contextQuestionCount = rows.length - scoredQuestionCount;
	const reverseScoredQuestionCount = rows.filter(
		(row) => row.reverseCoded && isScaleBackedType(row.type)
	).length;
	const dimensionLabel = `${dimensionCount} ${dimensionCount === 1 ? 'dimension' : 'dimensions'}`;
	const scoredLabel = `${scoredQuestionCount} scored ${scoredQuestionCount === 1 ? 'question' : 'questions'}`;
	const outputLabel = `${outputs.length} result ${outputs.length === 1 ? 'output' : 'outputs'}`;

	return {
		dimensionCount,
		questionCount: rows.length,
		scoredQuestionCount,
		contextQuestionCount,
		resultOutputCount: outputs.length,
		reverseScoredQuestionCount,
		label: `${dimensionLabel}, ${scoredLabel}, ${outputLabel}`
	};
}

export function summarizeQuestionnaireBlueprintReview(
	rows: TemplateQuestionAuthoringRow[],
	outputs: ScoreOutputAuthoringRow[]
): QuestionnaireBlueprintReview {
	const orderedRows = renumberRows(rows);
	const dimensions = summarizeQuestionDimensions(orderedRows);
	const requiredCount = orderedRows.filter((row) => row.required).length;
	const optionalCount = orderedRows.length - requiredCount;
	const scoredQuestionCount = orderedRows.filter(isMeanScoreEligible).length;
	const outputQuestionCodes = new Set(
		outputs.flatMap((output) =>
			output.includedQuestionCodes.map((code) => code.trim().toLowerCase()).filter(Boolean)
		)
	);
	const scoredQuestionsInResults = orderedRows.filter(
		(row) => isMeanScoreEligible(row) && outputQuestionCodes.has(row.code.trim().toLowerCase())
	).length;
	const answerFormatLabels = [
		...new Set(orderedRows.map((row) => describeQuestionScaleIntent(row).label))
	];
	const answerFormatDetail =
		answerFormatLabels.length > 1
			? `Uses ${formatInlineList(answerFormatLabels)}. Review whether each format matches the evidence you need.`
			: `Uses ${
					answerFormatLabels[0] ?? 'one answer format'
				} only. Add choice, number, ranking, or written context questions when the study needs richer evidence.`;

	return {
		label: `${dimensions.length} ${dimensions.length === 1 ? 'construct' : 'constructs'}, ${
			orderedRows.length
		} ${orderedRows.length === 1 ? 'question' : 'questions'}, ${requiredCount} required`,
		items: [
			{
				id: 'constructs',
				label: 'Construct plan',
				status: dimensions.length > 0 ? 'ready' : 'attention',
				detail:
					dimensions.length > 0
						? `Questions are grouped into ${formatInlineList(
								dimensions.map((dimension) => dimension.label)
							)}.`
						: 'Add at least one construct or dimension label before saving.'
			},
			{
				id: 'answer_formats',
				label: 'Answer formats',
				status: answerFormatLabels.length > 1 ? 'ready' : 'attention',
				detail: answerFormatDetail
			},
			{
				id: 'respondent_order',
				label: 'Respondent order',
				status: orderedRows.length > 0 ? 'ready' : 'attention',
				detail:
					orderedRows.length > 0
						? `Respondents answer ${orderedRows.length} ${
								orderedRows.length === 1 ? 'question' : 'questions'
							} in order, from "${questionTitle(orderedRows[0])}" to "${questionTitle(
								orderedRows[orderedRows.length - 1]
							)}"`
						: 'Add questions before reviewing respondent order.'
			},
			{
				id: 'requiredness',
				label: 'Required answers',
				status: orderedRows.length > 0 ? 'ready' : 'attention',
				detail: `${requiredCount} required, ${optionalCount} optional.`
			},
			{
				id: 'results',
				label: 'Results coverage',
				status: scoredQuestionCount > 0 && outputs.length > 0 ? 'ready' : 'attention',
				detail:
					scoredQuestionCount > 0 && outputs.length > 0
						? `${scoredQuestionsInResults} scored ${
								scoredQuestionsInResults === 1 ? 'question feeds' : 'questions feed'
							} ${outputs.length} result ${outputs.length === 1 ? 'output' : 'outputs'}.`
						: 'Add at least one rating, recommendation, or number question for numeric results.'
			}
		]
	};
}

export function dimensionCodeForLabel(label: string): string {
	return (
		label
			.trim()
			.toLowerCase()
			.replace(/[^a-z0-9]+/g, '_')
			.replace(/^_+|_+$/g, '') || 'study_measure'
	);
}

function normalizeScoreOutputs(
	outputs: ScoreOutputAuthoringRow[],
	rows: TemplateQuestionAuthoringRow[]
): Array<ScoreOutputAuthoringRow & { code: string }> {
	const eligibleRows = rows.filter(isMeanScoreEligible);
	const eligibleCodeSet = new Set(eligibleRows.map((row) => row.code.trim().toLowerCase()));

	return outputs.map((output) => ({
		...output,
		code: scoreCode(output.code || output.name),
		minValidCount: Math.max(1, Math.trunc(output.minValidCount || 1)),
		includedQuestionCodes: output.includedQuestionCodes.filter((code) =>
			eligibleCodeSet.has(code.trim().toLowerCase())
		)
	}));
}

function normalizeScoreCodes(outputs: ScoreOutputAuthoringRow[]) {
	return outputs.map((output) => scoreCode(output.code || output.name)).filter(Boolean);
}

function scoreScaleCompatibilityWarning(
	output: Pick<ScoreOutputAuthoringRow, 'name' | 'code' | 'includedQuestionCodes'>,
	rows: TemplateQuestionAuthoringRow[]
): string | null {
	const rowByCode = new Map(rows.map((row) => [row.code.trim().toLowerCase(), row]));
	const families = new Map<string, string>();

	for (const code of output.includedQuestionCodes) {
		const row = rowByCode.get(code.trim().toLowerCase());
		if (!row || !isMeanScoreEligible(row)) {
			continue;
		}

		const family = scoreScaleFamilyForQuestion(row);
		families.set(family.id, family.label);
	}

	if (families.size <= 1) {
		return null;
	}

	const label = output.name.trim() || output.code.trim() || 'Result output';
	return `${label} mixes incompatible answer scales: ${formatInlineList([
		...families.values()
	])}. Create separate result outputs or normalize outside this release.`;
}

function scoreScaleFamilyForQuestion(row: TemplateQuestionAuthoringRow): { id: string; label: string } {
	if (row.type === 'number') {
		if (
			row.scalePreset === 'discomfort_0_10' ||
			(row.scaleMin === 0 &&
				row.scaleMax === 10 &&
				row.scaleLowLabel.toLowerCase().includes('discomfort'))
		) {
			return { id: 'discomfort_0_10', label: 'Discomfort severity, 0-10' };
		}

		return { id: 'number', label: 'Number entry' };
	}

	if (row.type === 'nps') {
		return { id: 'recommendation', label: 'Recommendation scale' };
	}

	if (row.scalePreset === 'frequency_5') {
		return { id: 'frequency', label: 'Frequency scale' };
	}

	if (row.scalePreset === 'intensity_5') {
		return { id: 'intensity', label: 'Intensity scale' };
	}

	if (row.scalePreset === 'discomfort_0_10') {
		return { id: 'discomfort_0_10', label: 'Discomfort severity, 0-10' };
	}

	const low = row.scaleLowLabel.trim().toLowerCase();
	const high = row.scaleHighLabel.trim().toLowerCase();

	if (low.includes('never') || high.includes('always')) {
		return { id: 'frequency', label: 'Frequency scale' };
	}

	if (low.includes('disagree') || high.includes('agree')) {
		return { id: 'agreement', label: 'Agreement scale' };
	}

	if (low.includes('low') || high.includes('high')) {
		return { id: 'intensity', label: 'Intensity scale' };
	}

	return { id: 'rating', label: 'Rating scale' };
}

function missingPolicyDocument(output: ScoreOutputAuthoringRow) {
	if (output.missingStrategy === 'min_valid_count') {
		return {
			strategy: 'min_valid_count',
			min_valid_count: Math.max(1, Math.trunc(output.minValidCount || 1))
		};
	}

	return { strategy: 'require_all' };
}

function createScoreOutputLocalId() {
	if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
		return crypto.randomUUID();
	}

	return `score-${Math.random().toString(36).slice(2, 10)}`;
}

function nextScoreCode(outputs: ScoreOutputAuthoringRow[], fallback: string) {
	const usedCodes = new Set(normalizeScoreCodes(outputs));
	const baseCode = scoreCode(fallback);
	if (!usedCodes.has(baseCode)) {
		return baseCode;
	}

	let index = 2;
	while (usedCodes.has(`${baseCode}_${index}`)) {
		index += 1;
	}

	return `${baseCode}_${index}`;
}

export function isMeanScoreEligible(row: TemplateQuestionAuthoringRow): boolean {
	return row.type === 'likert' || row.type === 'nps' || row.type === 'number';
}

function createQuestionRow(
	row: Pick<TemplateQuestionAuthoringRow, 'ordinal' | 'code' | 'textDefault'> &
		Partial<Omit<TemplateQuestionAuthoringRow, 'ordinal' | 'code' | 'textDefault'>>
): TemplateQuestionAuthoringRow {
	return {
		ordinal: row.ordinal,
		code: row.code,
		dimensionLabel: normalizeDimensionLabel(row.dimensionLabel ?? 'Study measure'),
		type: row.type ?? 'likert',
		textDefault: row.textDefault,
		required: row.required ?? true,
		reverseCoded: row.reverseCoded ?? false,
		scaleMin: row.scaleMin ?? 1,
		scaleMax: row.scaleMax ?? 5,
		scaleLowLabel: row.scaleLowLabel ?? 'Strongly disagree',
		scaleHighLabel: row.scaleHighLabel ?? 'Strongly agree',
		scalePreset: row.scalePreset ?? 'agreement_5',
		choiceOptions: row.choiceOptions ?? defaultChoiceOptions
	};
}

function renumberRows(rows: TemplateQuestionAuthoringRow[]): TemplateQuestionAuthoringRow[] {
	return rows.map((row, index) => ({ ...row, ordinal: index + 1 }));
}

function nextQuestionCode(rows: TemplateQuestionAuthoringRow[]): string {
	const maxSuffix = rows.reduce((max, row) => {
		const match = /^q(\d+)$/i.exec(row.code.trim());
		if (!match) {
			return max;
		}

		return Math.max(max, Number.parseInt(match[1], 10));
	}, 0);
	return `q${String(maxSuffix + 1).padStart(2, '0')}`;
}

function isScaleBackedType(type: TemplateQuestionAnswerType): boolean {
	return type === 'likert' || type === 'nps';
}

function questionScaleCode(row: TemplateQuestionAuthoringRow): string {
	return `scale_${row.code.trim().toLowerCase()}`;
}

function measurementLevelFor(row: TemplateQuestionAuthoringRow): string | null {
	if (row.type === 'likert' || row.type === 'nps') {
		return 'ordinal';
	}

	if (row.type === 'number') {
		return 'numeric';
	}

	if (row.type === 'ranking') {
		return 'ordinal';
	}

	if (row.type === 'single' || row.type === 'multi') {
		return 'nominal';
	}

	return null;
}

function questionPayload(row: TemplateQuestionAuthoringRow): string {
	if (isScaleBackedType(row.type)) {
		return JSON.stringify({
			scale: {
				min: row.scaleMin,
				max: row.scaleMax,
				lowLabel: row.scaleLowLabel.trim(),
				highLabel: row.scaleHighLabel.trim()
			}
		});
	}

	if (row.type === 'single' || row.type === 'multi' || row.type === 'ranking') {
		return JSON.stringify({
			options: normalizedChoiceOptions(row).map((label, index) => ({
				code: `o${String(index + 1).padStart(2, '0')}`,
				label
			}))
		});
	}

	if (row.type === 'text') {
		return JSON.stringify({ multiline: false });
	}

	return '{}';
}

function normalizedChoiceOptions(row: TemplateQuestionAuthoringRow): string[] {
	return row.choiceOptions.map((option) => option.trim()).filter(Boolean);
}

function normalizeDimensionLabel(label: string): string {
	return label.trim() || 'Study measure';
}

function questionTitle(row: TemplateQuestionAuthoringRow): string {
	return row.textDefault.trim() || row.code.trim() || 'Untitled question';
}

function answerFormatDetail(row: TemplateQuestionAuthoringRow): string {
	if (isScaleBackedType(row.type)) {
		return `${row.scaleMin} to ${row.scaleMax}: ${row.scaleLowLabel.trim()} -> ${row.scaleHighLabel.trim()}`;
	}

	if (row.type === 'single') {
		return `Choose one: ${normalizedChoiceOptions(row).join(', ')}`;
	}

	if (row.type === 'multi') {
		return `Choose any: ${normalizedChoiceOptions(row).join(', ')}`;
	}

	if (row.type === 'ranking') {
		return `Rank: ${normalizedChoiceOptions(row).join(', ')}`;
	}

	if (row.type === 'number') {
		return 'Number entry';
	}

	if (row.type === 'date') {
		return 'Date entry';
	}

	return 'Text response';
}

function respondentResponsePreview(row: TemplateQuestionAuthoringRow): string {
	const options = normalizedChoiceOptions(row);

	if (isScaleBackedType(row.type)) {
		return `${row.scaleMin} ${row.scaleLowLabel.trim()} ... ${row.scaleMax} ${row.scaleHighLabel.trim()}`;
	}

	if (row.type === 'single') {
		return options.length
			? `Radio choices: ${options.join(', ')}`
			: 'Radio choices appear here after options are added.';
	}

	if (row.type === 'multi') {
		return options.length
			? `Checkbox choices: ${options.join(', ')}`
			: 'Checkbox choices appear here after options are added.';
	}

	if (row.type === 'ranking') {
		return options.length
			? `Drag or number-rank: ${options.join(', ')}`
			: 'Ranking choices appear here after options are added.';
	}

	if (row.type === 'number') {
		return 'Numeric input field';
	}

	if (row.type === 'date') {
		return 'Date input field';
	}

	return 'Short written response field';
}

function formatInlineList(values: string[]) {
	if (values.length === 0) {
		return 'no constructs';
	}

	if (values.length === 1) {
		return values[0];
	}

	if (values.length === 2) {
		return `${values[0]} and ${values[1]}`;
	}

	return `${values.slice(0, -1).join(', ')}, and ${values[values.length - 1]}`;
}

function scoreCode(label: string): string {
	const normalized = label
		.trim()
		.toLowerCase()
		.replace(/[^a-z0-9]+/g, '_')
		.replace(/^_+|_+$/g, '');
	return normalized || 'total';
}

export type DraftRespondentRuntimeControl =
	| 'rating'
	| 'radio'
	| 'checkbox'
	| 'ranking'
	| 'number'
	| 'date'
	| 'text'
	| 'unsupported';

export type DraftRespondentPreviewChoice = {
	value: string;
	text: string;
};

export type DraftRespondentPreviewQuestion = {
	ordinal: number;
	code: string;
	positionLabel: string;
	dimensionLabel: string;
	text: string;
	required: boolean;
	requiredLabel: string;
	controlType: DraftRespondentRuntimeControl;
	runtimeElementType: string;
	inputType: string | null;
	answerFormatLabel: string;
	answerFormatDetail: string;
	responsePreviewLabel: string;
	choices: DraftRespondentPreviewChoice[];
	scaleMin: number | null;
	scaleMax: number | null;
	scaleLowLabel: string | null;
	scaleHighLabel: string | null;
	scoreEligibilityLabel: string;
	scoreDirectionLabel: string;
	scoreDirectionDetail: string;
	resultUsageLabel: string;
	warnings: string[];
};

export type DraftRespondentPreviewControlSummary = {
	label: string;
	count: number;
};

export type DraftRespondentPreviewContract = {
	label: string;
	detail: string;
	questionCount: number;
	warningCount: number;
	unsupportedCount: number;
	controls: DraftRespondentPreviewControlSummary[];
	questions: DraftRespondentPreviewQuestion[];
};

export function toDraftRespondentPreviewContract(
	rows: TemplateQuestionAuthoringRow[],
	scoreOutputs: ScoreOutputAuthoringRow[]
): DraftRespondentPreviewContract {
	const questions = rows
		.slice()
		.sort((left, right) => left.ordinal - right.ordinal)
		.map((row, index) => toDraftRespondentPreviewQuestion(row, index, scoreOutputs));
	const warningCount = questions.reduce((sum, question) => sum + question.warnings.length, 0);
	const unsupportedCount = questions.filter((question) => question.controlType === 'unsupported').length;
	const controls = Array.from(
		questions.reduce((map, question) => {
			const current = map.get(question.answerFormatLabel) ?? 0;
			map.set(question.answerFormatLabel, current + 1);
			return map;
		}, new Map<string, number>())
	).map(([label, count]) => ({ label, count }));

	return {
		label:
			unsupportedCount > 0
				? 'Some questions cannot be previewed'
				: warningCount > 0
					? 'Runtime preview needs review'
					: 'Runtime preview ready',
		detail:
			unsupportedCount > 0
				? 'At least one draft question uses a format the respondent runtime cannot render safely.'
				: warningCount > 0
					? 'The respondent controls can be rendered, but some answer-format limitations should be reviewed before launch.'
					: 'Every draft question maps to a respondent control supported by the current runtime.',
		questionCount: questions.length,
		warningCount,
		unsupportedCount,
		controls,
		questions
	};
}

function toDraftRespondentPreviewQuestion(
	question: TemplateQuestionAuthoringRow,
	index: number,
	scoreOutputs: ScoreOutputAuthoringRow[]
): DraftRespondentPreviewQuestion {
	const runtime = draftRuntimeControl(question);
	const scoringDirection = describeQuestionScoringDirection(question);
	const warnings = draftRuntimeWarnings(question, runtime.controlType);

	return {
		ordinal: question.ordinal,
		code: question.code,
		positionLabel: `Question ${index + 1}`,
		dimensionLabel: question.dimensionLabel.trim() || 'No dimension',
		text: question.textDefault.trim() || 'Untitled question',
		required: question.required,
		requiredLabel: question.required ? 'Required' : 'Optional',
		controlType: runtime.controlType,
		runtimeElementType: runtime.runtimeElementType,
		inputType: runtime.inputType,
		answerFormatLabel: runtime.answerFormatLabel,
		answerFormatDetail: runtime.answerFormatDetail,
		responsePreviewLabel: runtime.responsePreviewLabel,
		choices: draftRuntimeChoices(question),
		scaleMin: runtime.scaleMin,
		scaleMax: runtime.scaleMax,
		scaleLowLabel: runtime.scaleLowLabel,
		scaleHighLabel: runtime.scaleHighLabel,
		scoreEligibilityLabel: isMeanScoreEligible(question)
			? 'Available for result scores'
			: 'Collected for context/export only',
		scoreDirectionLabel: scoringDirection.label,
		scoreDirectionDetail: scoringDirection.detail,
		resultUsageLabel: describeQuestionResultUsage(question, scoreOutputs),
		warnings
	};
}

function draftRuntimeControl(question: TemplateQuestionAuthoringRow) {
	if (question.type === 'likert' || question.type === 'nps') {
		return {
			controlType: 'rating' as const,
			runtimeElementType: 'rating',
			inputType: null,
			answerFormatLabel: question.type === 'nps' ? '0-10 rating' : 'Rating scale',
			answerFormatDetail: `Respondents choose one value from ${question.scaleMin} to ${question.scaleMax}.`,
			responsePreviewLabel: 'SurveyJS rating control',
			scaleMin: question.scaleMin,
			scaleMax: question.scaleMax,
			scaleLowLabel: question.scaleLowLabel,
			scaleHighLabel: question.scaleHighLabel
		};
	}

	if (question.type === 'single') {
		return {
			controlType: 'radio' as const,
			runtimeElementType: 'radiogroup',
			inputType: null,
			answerFormatLabel: 'Single choice',
			answerFormatDetail: 'Respondents choose one option. The stored answer is the selected option code.',
			responsePreviewLabel: 'SurveyJS radio group',
			scaleMin: null,
			scaleMax: null,
			scaleLowLabel: null,
			scaleHighLabel: null
		};
	}

	if (question.type === 'multi') {
		return {
			controlType: 'checkbox' as const,
			runtimeElementType: 'checkbox',
			inputType: null,
			answerFormatLabel: 'Multiple choice',
			answerFormatDetail: 'Respondents can choose multiple options. The stored answer is an array of option codes.',
			responsePreviewLabel: 'SurveyJS checkbox group',
			scaleMin: null,
			scaleMax: null,
			scaleLowLabel: null,
			scaleHighLabel: null
		};
	}

	if (question.type === 'ranking') {
		return {
			controlType: 'ranking' as const,
			runtimeElementType: 'ranking',
			inputType: null,
			answerFormatLabel: 'Ranking',
			answerFormatDetail: 'Respondents order the options. The stored answer is an ordered array of option codes.',
			responsePreviewLabel: 'SurveyJS ranking control',
			scaleMin: null,
			scaleMax: null,
			scaleLowLabel: null,
			scaleHighLabel: null
		};
	}

	if (question.type === 'number') {
		return {
			controlType: 'number' as const,
			runtimeElementType: 'text',
			inputType: 'number',
			answerFormatLabel: 'Number input',
			answerFormatDetail: 'Respondents enter a number. Min, max, unit, and decimal constraints are not modeled yet.',
			responsePreviewLabel: 'SurveyJS text control with number input',
			scaleMin: null,
			scaleMax: null,
			scaleLowLabel: null,
			scaleHighLabel: null
		};
	}

	if (question.type === 'date') {
		return {
			controlType: 'date' as const,
			runtimeElementType: 'text',
			inputType: 'date',
			answerFormatLabel: 'Date input',
			answerFormatDetail: 'Respondents enter a date. Earliest/latest date constraints are not modeled yet.',
			responsePreviewLabel: 'SurveyJS text control with date input',
			scaleMin: null,
			scaleMax: null,
			scaleLowLabel: null,
			scaleHighLabel: null
		};
	}

	if (question.type === 'text') {
		return {
			controlType: 'text' as const,
			runtimeElementType: 'text',
			inputType: null,
			answerFormatLabel: 'Text input',
			answerFormatDetail: 'Respondents enter free text. Long text and max length are not modeled yet.',
			responsePreviewLabel: 'SurveyJS text control',
			scaleMin: null,
			scaleMax: null,
			scaleLowLabel: null,
			scaleHighLabel: null
		};
	}

	return {
		controlType: 'unsupported' as const,
		runtimeElementType: 'unsupported',
		inputType: null,
		answerFormatLabel: 'Unsupported format',
		answerFormatDetail: 'This draft question does not map to a supported respondent control.',
		responsePreviewLabel: 'Unsupported by current respondent runtime',
		scaleMin: null,
		scaleMax: null,
		scaleLowLabel: null,
		scaleHighLabel: null
	};
}

function draftRuntimeChoices(question: TemplateQuestionAuthoringRow): DraftRespondentPreviewChoice[] {
	if (question.type !== 'single' && question.type !== 'multi' && question.type !== 'ranking') {
		return [];
	}

	return question.choiceOptions.map((option, index) => ({
		value: `o${String(index + 1).padStart(2, '0')}`,
		text: option.trim() || `Option ${index + 1}`
	}));
}

function draftRuntimeWarnings(
	question: TemplateQuestionAuthoringRow,
	controlType: DraftRespondentRuntimeControl
): string[] {
	const warnings: string[] = [];

	if (!question.textDefault.trim()) {
		warnings.push('Question text is empty; respondents will see an untitled question.');
	}

	if (controlType === 'unsupported') {
		warnings.push('This answer format cannot be rendered by the current respondent runtime.');
	}

	if ((question.type === 'likert' || question.type === 'nps') && question.scaleMin >= question.scaleMax) {
		warnings.push('Rating scale minimum must be lower than the maximum.');
	}

	if ((question.type === 'likert' || question.type === 'nps') && (!question.scaleLowLabel.trim() || !question.scaleHighLabel.trim())) {
		warnings.push('Rating endpoint labels should be visible before respondents answer.');
	}

	if ((question.type === 'single' || question.type === 'multi' || question.type === 'ranking') && question.choiceOptions.length < 2) {
		warnings.push('Choice and ranking questions need at least two options.');
	}

	if (question.type === 'number') {
		warnings.push('Number input has no min, max, unit, or decimal constraint yet.');
	}

	if (question.type === 'date') {
		warnings.push('Date input has no earliest/latest date constraint yet.');
	}

	if (question.type === 'text') {
		warnings.push('Text input has no long-text or max-length setting yet.');
	}

	return warnings;
}
