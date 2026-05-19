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

export type TemplateQuestionAuthoringRow = {
	ordinal: number;
	code: string;
	type: TemplateQuestionAnswerType;
	textDefault: string;
	required: boolean;
	reverseCoded: boolean;
	scaleMin: number;
	scaleMax: number;
	scaleLowLabel: string;
	scaleHighLabel: string;
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

const defaultQuestionText = [
	'After work, I need time to recover mentally.',
	'During demanding weeks, small interruptions feel harder to handle.',
	'I can usually regain focus after a short break.'
];

const defaultChoiceOptions = ['Option 1', 'Option 2'];

export function createDefaultTemplateQuestionRows(): TemplateQuestionAuthoringRow[] {
	return defaultQuestionText.map((textDefault, index) =>
		createQuestionRow({
			ordinal: index + 1,
			code: `q${String(index + 1).padStart(2, '0')}`,
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
		sectionCode: 'core',
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
	const includedCodes = new Set(
		options.includedQuestionCodes?.map((code) => code.trim().toLowerCase()).filter(Boolean)
	);
	const eligibleRows = rows.filter(
		(row) =>
			isMeanScoreEligible(row) &&
			row.code.trim() &&
			(includedCodes.size === 0 || includedCodes.has(row.code.trim().toLowerCase()))
	);
	const itemCodes = eligibleRows.map((row) => row.code.trim());
	const reverseCodedRows = eligibleRows.filter((row) => row.reverseCoded && isScaleBackedType(row.type));
	const reverseScale = reverseCodedRows[0] ?? eligibleRows.find((row) => isScaleBackedType(row.type));
	const outputCode = scoreCode(options.outputCode ?? 'total');
	const aggregation = options.aggregation ?? 'mean';
	const missingDefaults =
		options.missingStrategy === 'min_valid_count'
			? { strategy: 'min_valid_count', min_valid_count: Math.max(1, options.minValidCount ?? 1) }
			: { strategy: 'require_all' };
	const nodes = reverseScale
		? [
				{ id: 'core_answers', op: 'select_answers', input: 'core_items' },
				{
					id: 'scored_answers',
					op: 'reverse_code',
					input: 'core_answers',
					scale: 'default_rating',
					reverse_flag_source: 'explicit_list',
					explicit_reverse_items: reverseCodedRows.map((row) => row.code.trim())
				},
				{ id: 'total', op: aggregation, input: 'scored_answers' }
			]
		: [
				{ id: 'core_answers', op: 'select_answers', input: 'core_items' },
				{ id: 'total', op: aggregation, input: 'core_answers' }
			];

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
			inputs: [{ id: 'core_items', kind: 'answers', items: itemCodes }],
			nodes,
			outputs: [{ code: outputCode, node: 'total' }],
			missing_data: {
				defaults: missingDefaults
			}
		},
		null,
		2
	);
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
		type: row.type ?? 'likert',
		textDefault: row.textDefault,
		required: row.required ?? true,
		reverseCoded: row.reverseCoded ?? false,
		scaleMin: row.scaleMin ?? 1,
		scaleMax: row.scaleMax ?? 5,
		scaleLowLabel: row.scaleLowLabel ?? 'Strongly disagree',
		scaleHighLabel: row.scaleHighLabel ?? 'Strongly agree',
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

function scoreCode(label: string): string {
	const normalized = label
		.trim()
		.toLowerCase()
		.replace(/[^a-z0-9]+/g, '_')
		.replace(/^_+|_+$/g, '');
	return normalized || 'total';
}
