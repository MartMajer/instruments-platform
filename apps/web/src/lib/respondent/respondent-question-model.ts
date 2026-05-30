import type { RespondentQuestionResponse } from '../api/setup';

export type RespondentQuestionChoice = {
	value: string;
	label: string;
	isExclusive: boolean;
};

export type RespondentScaleChoice = {
	value: number;
	label: string;
	anchorLabel: string | null;
};

export type RespondentScaleOptions = {
	min: number;
	max: number;
	lowLabel: string | null;
	highLabel: string | null;
	options: RespondentScaleChoice[];
};

export type RespondentRankingOptions = {
	mode: 'all' | 'top_n';
	topN: number | null;
};

export type RespondentInputConstraints = {
	min: number | null;
	max: number | null;
	step: number | null;
	minDate: string | null;
	maxDate: string | null;
	maxLength: number | null;
	multiline: boolean;
	unit: string | null;
};

export function orderedRespondentQuestions(
	questions: RespondentQuestionResponse[]
): RespondentQuestionResponse[] {
	return questions.slice().sort((left, right) => left.ordinal - right.ordinal);
}

export function visibleRespondentQuestions(
	questions: RespondentQuestionResponse[],
	answers: Record<string, string>
): RespondentQuestionResponse[] {
	return orderedRespondentQuestions(questions).filter((question) =>
		isRespondentQuestionVisible(question, questions, answers)
	);
}

export function isRespondentQuestionVisible(
	question: RespondentQuestionResponse,
	questions: RespondentQuestionResponse[],
	answers: Record<string, string>
): boolean {
	const rule = displayLogicRule(question);
	if (!rule) {
		return true;
	}

	const source = questions.find(
		(candidate) => candidate.code.trim().toLowerCase() === rule.sourceQuestionCode.toLowerCase()
	);
	if (!source) {
		return true;
	}

	if (!isRespondentQuestionVisible(source, questions, answers)) {
		return false;
	}

	return scalarAnswerValue(answers[source.id]) === rule.value;
}

export function scaleQuestionOptions(question: RespondentQuestionResponse): RespondentScaleOptions {
	const fallbackMin = question.type === 'nps' ? 0 : 1;
	const fallbackMax = question.type === 'nps' ? 10 : 5;
	const min = question.scaleMinValue ?? fallbackMin;
	const max = question.scaleMaxValue ?? fallbackMax;
	const anchors = parseScaleAnchors(question.scaleAnchors);
	const lowAnchor = anchors.find((anchor) => anchor.value === min);
	const highAnchor = anchors.find((anchor) => anchor.value === max);

	return {
		min,
		max,
		lowLabel: lowAnchor?.label ?? null,
		highLabel: highAnchor?.label ?? null,
		options: Array.from({ length: Math.max(0, max - min + 1) }, (_entry, index) => {
			const value = min + index;
			return {
				value,
				label: String(value),
				anchorLabel: anchors.find((anchor) => anchor.value === value)?.label ?? null
			};
		})
	};
}

export function questionChoices(question: RespondentQuestionResponse): RespondentQuestionChoice[] {
	const payload = parseObject(question.payload);
	const options = Array.isArray(payload.options) ? payload.options : [];
	return options
		.map((option, index) => {
			const candidate = option && typeof option === 'object' ? option : {};
			const code = readString(candidate, 'code') || `o${String(index + 1).padStart(2, '0')}`;
			const label = readString(candidate, 'label') || code;
			return {
				value: code,
				label,
				isExclusive: readBoolean(candidate, 'exclusive')
			};
		})
		.filter((choice) => choice.value.trim() && choice.label.trim());
}

export function matrixQuestionChoices(question: RespondentQuestionResponse): {
	rows: RespondentQuestionChoice[];
	columns: RespondentQuestionChoice[];
} {
	const matrix = readObject(parseObject(question.payload), 'matrix');
	return {
		rows: matrixChoices(matrix, 'rows'),
		columns: matrixChoices(matrix, 'columns')
	};
}

export function rankingQuestionOptions(question: RespondentQuestionResponse): RespondentRankingOptions {
	const ranking = readObject(parseObject(question.payload), 'ranking');
	const mode = readString(ranking, 'mode') === 'top_n' ? 'top_n' : 'all';
	const topN = readNumber(ranking, 'topN');
	return {
		mode,
		topN: mode === 'top_n' ? topN : null
	};
}

export function questionInputConstraints(
	question: RespondentQuestionResponse
): RespondentInputConstraints {
	const payload = parseObject(question.payload);
	const validation = readObject(payload, 'validation');
	const display = readObject(payload, 'display');
	const text = readObject(payload, 'text');

	return {
		min: readNumber(validation, 'min'),
		max: readNumber(validation, 'max'),
		step: readBoolean(validation, 'integerOnly') ? 1 : null,
		minDate: readString(validation, 'minDate'),
		maxDate: readString(validation, 'maxDate'),
		maxLength: readNumber(text, 'maxLength'),
		multiline: readBoolean(text, 'multiline'),
		unit: readString(display, 'unit')
	};
}

export function answerToInputValue(
	question: RespondentQuestionResponse,
	value: string | undefined
): unknown {
	if (value === undefined || value === '') {
		if (usesObjectAnswer(question)) {
			return {};
		}

		return usesArrayAnswer(question) ? [] : '';
	}

	if (usesObjectAnswer(question)) {
		try {
			return normalizeMatrixAnswer(question, JSON.parse(value));
		} catch {
			return {};
		}
	}

	if (!usesArrayAnswer(question)) {
		const parsed = parseStoredAnswerValue(value);
		if (question.type === 'likert' || question.type === 'nps' || question.type === 'number') {
			const numericValue = Number(parsed);
			return Number.isFinite(numericValue) ? numericValue : '';
		}

		return typeof parsed === 'string' ? parsed : value;
	}

	try {
		const parsed = JSON.parse(value);
		return Array.isArray(parsed) ? parsed.map(String) : [];
	} catch {
		return [];
	}
}

export function inputValueToAnswer(question: RespondentQuestionResponse, value: unknown): string {
	if (value === undefined || value === null || value === '') {
		return '';
	}

	if (usesArrayAnswer(question)) {
		return JSON.stringify(Array.isArray(value) ? value.map(String) : [String(value)]);
	}

	if (usesObjectAnswer(question)) {
		return JSON.stringify(normalizeMatrixAnswer(question, value));
	}

	if (question.type === 'likert' || question.type === 'nps' || question.type === 'number') {
		return String(value);
	}

	return JSON.stringify(String(value));
}

export function isQuestionAnswered(question: RespondentQuestionResponse, value: string | undefined): boolean {
	if (!question.required) {
		return true;
	}

	if (value === undefined || value.trim() === '') {
		return false;
	}

	if (usesArrayAnswer(question)) {
		const parsed = answerToInputValue(question, value);
		return Array.isArray(parsed) && parsed.length > 0;
	}

	if (usesObjectAnswer(question)) {
		const parsed = answerToInputValue(question, value);
		if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
			return false;
		}

		const { rows } = matrixQuestionChoices(question);
		const answer = parsed as Record<string, string>;
		return rows.length > 0 && rows.every((row) => typeof answer[row.value] === 'string');
	}

	const parsed = parseStoredAnswerValue(value);
	if (typeof parsed === 'string') {
		return parsed.trim().length > 0;
	}

	return true;
}

export function parseStoredAnswerValue(value: string | undefined): unknown {
	if (value === undefined || value.trim() === '') {
		return value ?? '';
	}

	try {
		return JSON.parse(value);
	} catch {
		return value;
	}
}

export function displayLogicRule(question: RespondentQuestionResponse): {
	sourceQuestionCode: string;
	value: string;
} | null {
	const displayLogic = readObject(parseObject(question.payload), 'displayLogic');
	if (readString(displayLogic, 'mode') !== 'show_when') {
		return null;
	}

	if (readString(displayLogic, 'operator') !== 'equals') {
		return null;
	}

	const sourceQuestionCode = readString(displayLogic, 'sourceQuestionCode');
	const value = readString(displayLogic, 'value');
	if (!sourceQuestionCode || !value) {
		return null;
	}

	return { sourceQuestionCode, value };
}

export function parseObject(value: string | null | undefined): Record<string, unknown> {
	try {
		const parsed = JSON.parse(value ?? '{}');
		return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
			? (parsed as Record<string, unknown>)
			: {};
	} catch {
		return {};
	}
}

export function readString(source: object, key: string): string | null {
	const value = (source as Record<string, unknown>)[key];
	return typeof value === 'string' ? value : null;
}

export function readNumber(source: object, key: string): number | null {
	const value = (source as Record<string, unknown>)[key];
	return typeof value === 'number' ? value : null;
}

export function readBoolean(source: object, key: string): boolean {
	const value = (source as Record<string, unknown>)[key];
	return value === true;
}

export function readObject(source: object, key: string): Record<string, unknown> {
	const value = (source as Record<string, unknown>)[key];
	return value && typeof value === 'object' && !Array.isArray(value)
		? (value as Record<string, unknown>)
		: {};
}

function usesArrayAnswer(question: RespondentQuestionResponse): boolean {
	return question.type === 'multi' || question.type === 'ranking';
}

function usesObjectAnswer(question: RespondentQuestionResponse): boolean {
	return question.type === 'matrix';
}

function matrixChoices(source: Record<string, unknown>, key: 'rows' | 'columns'): RespondentQuestionChoice[] {
	const entries = Array.isArray(source[key]) ? source[key] : [];
	return entries
		.map((entry, index) => {
			const candidate = entry && typeof entry === 'object' ? entry : {};
			const fallbackPrefix = key === 'rows' ? 'r' : 'c';
			const code =
				readString(candidate, 'code') || `${fallbackPrefix}${String(index + 1).padStart(2, '0')}`;
			const label = readString(candidate, 'label') || code;
			return {
				value: code,
				label,
				isExclusive: false
			};
		})
		.filter((choice) => choice.value.trim() && choice.label.trim());
}

function normalizeMatrixAnswer(
	question: RespondentQuestionResponse,
	value: unknown
): Record<string, string> {
	if (!value || typeof value !== 'object' || Array.isArray(value)) {
		return {};
	}

	const { rows, columns } = matrixQuestionChoices(question);
	const columnCodes = new Set(columns.map((column) => column.value));
	const source = value as Record<string, unknown>;
	const answer: Record<string, string> = {};

	for (const row of rows) {
		const candidate = source[row.value];
		if (typeof candidate === 'string' && columnCodes.has(candidate)) {
			answer[row.value] = candidate;
		}
	}

	return answer;
}

function scalarAnswerValue(value: string | undefined): string | null {
	if (value === undefined || value.trim() === '') {
		return null;
	}

	const parsed = parseStoredAnswerValue(value);
	if (typeof parsed === 'string') {
		return parsed;
	}

	if (typeof parsed === 'number' || typeof parsed === 'boolean') {
		return String(parsed);
	}

	return null;
}

function parseScaleAnchors(value: string | null | undefined) {
	try {
		const parsed = JSON.parse(value ?? '[]');
		if (!Array.isArray(parsed)) {
			return [];
		}

		return parsed
			.map((anchor) => {
				const candidate = anchor && typeof anchor === 'object' ? anchor : {};
				const rawValue = readNumber(candidate, 'value');
				const label = readString(candidate, 'label');
				return rawValue === null || !label ? null : { value: rawValue, label };
			})
			.filter((anchor): anchor is { value: number; label: string } => anchor !== null);
	} catch {
		return [];
	}
}
