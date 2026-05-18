import type { CreateTemplateQuestionRequest } from '$lib/api/setup';

export type TemplateQuestionAuthoringRow = {
	ordinal: number;
	code: string;
	type: CreateTemplateQuestionRequest['type'];
	textDefault: string;
	required: boolean;
	reverseCoded: boolean;
};

export type TemplateQuestionMoveDirection = 'up' | 'down';

const defaultQuestionText = [
	'After work, I need time to recover mentally.',
	'During demanding weeks, small interruptions feel harder to handle.',
	'I can usually regain focus after a short break.'
];

export function createDefaultTemplateQuestionRows(): TemplateQuestionAuthoringRow[] {
	return defaultQuestionText.map((textDefault, index) => ({
		ordinal: index + 1,
		code: `q${String(index + 1).padStart(2, '0')}`,
		type: 'likert',
		textDefault,
		required: true,
		reverseCoded: index === 2
	}));
}

export function appendTemplateQuestionRow(
	rows: TemplateQuestionAuthoringRow[]
): TemplateQuestionAuthoringRow[] {
	const nextCode = nextQuestionCode(rows);
	return renumberRows([
		...rows,
		{
			ordinal: rows.length + 1,
			code: nextCode,
			type: 'likert',
			textDefault: 'New question text',
			required: true,
			reverseCoded: false
		}
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
			errors.push(`Question ${index + 1} needs a code.`);
		}

		if (!row.textDefault.trim()) {
			errors.push(`Question ${index + 1} needs question text.`);
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

export function toCreateTemplateQuestions(
	rows: TemplateQuestionAuthoringRow[]
): CreateTemplateQuestionRequest[] {
	return renumberRows(rows).map((row) => ({
		ordinal: row.ordinal,
		code: row.code.trim(),
		type: row.type,
		textDefault: row.textDefault.trim(),
		sectionCode: 'core',
		scaleCode: row.type === 'likert' ? 'agreement' : null,
		required: row.required,
		reverseCoded: row.reverseCoded,
		measurementLevel: row.type === 'likert' ? 'ordinal' : null,
		payload: '{}',
		missingCodes: '[]'
	}));
}

export function buildMeanScoringDocument(
	ruleId: string,
	rows: TemplateQuestionAuthoringRow[]
): string {
	const eligibleRows = rows.filter((row) => isMeanScoreEligible(row) && row.code.trim());
	const itemCodes = eligibleRows.map((row) => row.code.trim());
	const reverseCodedItems = eligibleRows
		.filter((row) => row.reverseCoded)
		.map((row) => row.code.trim());

	return JSON.stringify(
		{
			rule_id: ruleId,
			rule_version: '1.0.0',
			schema_version: '1.0.0',
			engine_min_version: '1.0.0',
			scale_defaults: {
				agreement: { min: 1, max: 5 }
			},
			inputs: [{ id: 'core_items', kind: 'answers', items: itemCodes }],
			nodes: [
				{ id: 'core_answers', op: 'select_answers', input: 'core_items' },
				{
					id: 'scored_answers',
					op: 'reverse_code',
					input: 'core_answers',
					scale: 'agreement',
					reverse_flag_source: 'explicit_list',
					explicit_reverse_items: reverseCodedItems
				},
				{ id: 'total', op: 'mean', input: 'scored_answers' }
			],
			outputs: [{ code: 'total', node: 'total' }],
			missing_data: {
				defaults: { strategy: 'require_all' }
			}
		},
		null,
		2
	);
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

function isMeanScoreEligible(row: TemplateQuestionAuthoringRow): boolean {
	return row.type === 'likert';
}
