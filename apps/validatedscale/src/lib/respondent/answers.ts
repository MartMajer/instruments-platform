import type { RespondentQuestionResponse } from '$lib/api/setup';

export type ScaleAnchor = { value: number; label: string };
export type ChoiceOption = { code: string; label: string };

/**
 * Answer wire format (matches the platform contract exactly):
 * - likert / nps / number → bare numeric string ("4")
 * - multi → JSON array of option codes
 * - single / text / comment / date → JSON-quoted string ("\"o01\"")
 */
export function serializeAnswer(question: RespondentQuestionResponse, value: unknown): string {
	if (value === undefined || value === null || value === '') {
		return '';
	}

	if (question.type === 'multi' || question.type === 'ranking') {
		return JSON.stringify(Array.isArray(value) ? value.map(String) : [String(value)]);
	}

	if (question.type === 'matrix') {
		const record = value && typeof value === 'object' && !Array.isArray(value) ? value : {};
		return JSON.stringify(record);
	}

	if (question.type === 'likert' || question.type === 'nps' || question.type === 'number') {
		return String(value);
	}

	return JSON.stringify(String(value));
}

export function deserializeAnswer(question: RespondentQuestionResponse, raw: string | null): unknown {
	const emptyFor = () =>
		question.type === 'multi' || question.type === 'ranking'
			? []
			: question.type === 'matrix'
				? {}
				: '';

	if (raw == null || raw === '') {
		return emptyFor();
	}

	if (question.type === 'likert' || question.type === 'nps' || question.type === 'number') {
		return raw;
	}

	try {
		const parsed = JSON.parse(raw);
		if (question.type === 'multi' || question.type === 'ranking') {
			return Array.isArray(parsed) ? parsed.map(String) : [];
		}
		if (question.type === 'matrix') {
			return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
		}
		return typeof parsed === 'string' ? parsed : String(parsed);
	} catch {
		return emptyFor();
	}
}

export function parseAnchors(question: RespondentQuestionResponse): ScaleAnchor[] {
	try {
		const parsed = JSON.parse(question.scaleAnchors ?? '[]');
		if (!Array.isArray(parsed)) return [];
		return parsed
			.map((anchor) => {
				const candidate = anchor && typeof anchor === 'object' ? (anchor as Record<string, unknown>) : {};
				const value = typeof candidate.value === 'number' ? candidate.value : null;
				const label = typeof candidate.label === 'string' ? candidate.label : '';
				return value === null || !label ? null : { value, label };
			})
			.filter((anchor): anchor is ScaleAnchor => anchor !== null);
	} catch {
		return [];
	}
}

export function parseOptions(question: RespondentQuestionResponse): ChoiceOption[] {
	try {
		const payload = JSON.parse(question.payload ?? '{}');
		const options = Array.isArray(payload?.options) ? payload.options : [];
		return options
			.map((option: unknown, index: number) => {
				const candidate = option && typeof option === 'object' ? (option as Record<string, unknown>) : {};
				const code =
					(typeof candidate.code === 'string' && candidate.code) ||
					`o${String(index + 1).padStart(2, '0')}`;
				const label = (typeof candidate.label === 'string' && candidate.label) || code;
				return { code, label };
			})
			.filter((option: ChoiceOption) => option.code.trim() !== '' && option.label.trim() !== '');
	} catch {
		return [];
	}
}

export function scaleRange(question: RespondentQuestionResponse): number[] {
	const fallbackMin = question.type === 'nps' ? 0 : 1;
	const fallbackMax = question.type === 'nps' ? 10 : 5;
	const min = question.scaleMinValue ?? fallbackMin;
	const max = question.scaleMaxValue ?? fallbackMax;
	const values: number[] = [];
	for (let v = min; v <= max; v += 1) values.push(v);
	return values;
}

export type MatrixChoice = { code: string; label: string };

export function parseMatrix(question: RespondentQuestionResponse): {
	rows: MatrixChoice[];
	columns: MatrixChoice[];
} {
	try {
		const payload = JSON.parse(question.payload ?? '{}');
		const matrix = payload?.matrix && typeof payload.matrix === 'object' ? payload.matrix : {};
		const read = (key: 'rows' | 'columns', prefix: string): MatrixChoice[] => {
			const entries = Array.isArray(matrix[key]) ? matrix[key] : [];
			return entries.map((entry: unknown, index: number) => {
				const candidate =
					entry && typeof entry === 'object' ? (entry as Record<string, unknown>) : {};
				const code =
					(typeof candidate.code === 'string' && candidate.code) ||
					`${prefix}${String(index + 1).padStart(2, '0')}`;
				const label = (typeof candidate.label === 'string' && candidate.label) || code;
				return { code, label };
			});
		};
		return { rows: read('rows', 'r'), columns: read('columns', 'c') };
	} catch {
		return { rows: [], columns: [] };
	}
}

export function isAnswered(question: RespondentQuestionResponse, value: unknown): boolean {
	if (question.type === 'multi' || question.type === 'ranking') {
		return Array.isArray(value) && value.length > 0;
	}
	if (question.type === 'matrix') {
		const rows = parseMatrix(question).rows;
		const record = value && typeof value === 'object' ? (value as Record<string, unknown>) : {};
		return rows.length > 0 && rows.every((row) => typeof record[row.code] === 'string');
	}
	return value !== undefined && value !== null && value !== '';
}
