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
	| 'custom';

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
	'After work, I need time to recover mentally.',
	'During demanding weeks, small interruptions feel harder to handle.',
	'I can usually regain focus after a short break.'
];

const defaultQuestionDimensions = ['Recovery need', 'Workload strain', 'Recovery capacity'];

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

function scoreCode(label: string): string {
	const normalized = label
		.trim()
		.toLowerCase()
		.replace(/[^a-z0-9]+/g, '_')
		.replace(/^_+|_+$/g, '');
	return normalized || 'total';
}
