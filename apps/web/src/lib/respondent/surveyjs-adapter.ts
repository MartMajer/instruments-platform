import type { RespondentQuestionResponse } from '../api/setup';

export type RespondentSurveyJson = {
	showQuestionNumbers: 'off';
	questionTitleLocation: 'top';
	showNavigationButtons: false;
	showCompleteButton: false;
	elements: RespondentSurveyElement[];
};

export type RespondentSurveyElement = {
	type: 'text' | 'comment' | 'rating' | 'radiogroup' | 'checkbox' | 'ranking';
	name: string;
	title: string;
	isRequired: boolean;
	inputType?: 'number' | 'date';
	min?: number | string;
	max?: number | string;
	step?: number;
	maxLength?: number;
	maxSelectedChoices?: number;
	selectToRankEnabled?: boolean;
	showOtherItem?: boolean;
	otherText?: string;
	rateMin?: number;
	rateMax?: number;
	minRateDescription?: string;
	maxRateDescription?: string;
	choices?: RespondentSurveyChoice[];
};

export type RespondentSurveyChoice = {
	value: string;
	text: string;
	isExclusive?: boolean;
};

export function buildRespondentSurveyJson(
	questions: RespondentQuestionResponse[]
): RespondentSurveyJson {
	return {
		showQuestionNumbers: 'off',
		questionTitleLocation: 'top',
		showNavigationButtons: false,
		showCompleteButton: false,
		elements: questions
			.slice()
			.sort((left, right) => left.ordinal - right.ordinal)
			.map(toSurveyElement)
	};
}

export function toSurveyInitialData(
	questions: RespondentQuestionResponse[],
	answers: Record<string, string>
) {
	return Object.fromEntries(
		questions.map((question) => [question.id, toSurveyInitialValue(question, answers[question.id])])
	);
}

export function normalizeSurveyDataToAnswers(
	questions: RespondentQuestionResponse[],
	data: Record<string, unknown>
) {
	return Object.fromEntries(
		questions.map((question) => {
			const value = data[question.id];

			return [question.id, normalizeSurveyValueToAnswer(question, value)];
		})
	);
}

function toSurveyElement(question: RespondentQuestionResponse): RespondentSurveyElement {
	const element: RespondentSurveyElement = {
		type: 'text',
		name: question.id,
		title: question.textDefault,
		isRequired: question.required
	};

	if (question.type === 'likert' || question.type === 'nps') {
		const scale = scaleDefinition(question);
		return {
			...element,
			type: 'rating',
			rateMin: scale.min,
			rateMax: scale.max,
			minRateDescription: scale.lowLabel,
			maxRateDescription: scale.highLabel
		};
	}

	if (question.type === 'single') {
		const choice = readObject(parseObject(question.payload), 'choice');
		return {
			...element,
			type: 'radiogroup',
			choices: questionChoices(question),
			...surveyOtherChoiceOptions(choice)
		};
	}

	if (question.type === 'multi') {
		const choice = readObject(parseObject(question.payload), 'choice');
		return {
			...element,
			type: 'checkbox',
			choices: questionChoices(question),
			...surveyOtherChoiceOptions(choice)
		};
	}

	if (question.type === 'ranking') {
		const ranking = readObject(parseObject(question.payload), 'ranking');
		const topN = readNumber(ranking, 'topN');
		return {
			...element,
			type: 'ranking',
			choices: questionChoices(question),
			...(readString(ranking, 'mode') === 'top_n' && topN !== null
				? {
						selectToRankEnabled: true,
						maxSelectedChoices: topN
					}
				: {})
		};
	}

	if (question.type === 'number') {
		const validation = readObject(parseObject(question.payload), 'validation');
		element.inputType = 'number';
		const min = readNumber(validation, 'min');
		const max = readNumber(validation, 'max');
		if (min !== null) {
			element.min = min;
		}
		if (max !== null) {
			element.max = max;
		}
		if (readBoolean(validation, 'integerOnly')) {
			element.step = 1;
		}
	}

	if (question.type === 'date') {
		const validation = readObject(parseObject(question.payload), 'validation');
		element.inputType = 'date';
		const minDate = readString(validation, 'minDate');
		const maxDate = readString(validation, 'maxDate');
		if (minDate) {
			element.min = minDate;
		}
		if (maxDate) {
			element.max = maxDate;
		}
	}

	if (question.type === 'text') {
		const text = readObject(parseObject(question.payload), 'text');
		if (readBoolean(text, 'multiline')) {
			element.type = 'comment';
		}
		const maxLength = readNumber(text, 'maxLength');
		if (maxLength !== null) {
			element.maxLength = maxLength;
		}
	}

	return element;
}

export function normalizeSurveyValueToAnswer(
	question: RespondentQuestionResponse,
	value: unknown
): string {
	if (value === undefined || value === null || value === '') {
		return '';
	}

	if (usesArrayAnswer(question)) {
		return JSON.stringify(Array.isArray(value) ? value.map(String) : [String(value)]);
	}

	return String(value);
}

function toSurveyInitialValue(question: RespondentQuestionResponse, value: string | undefined) {
	if (value === undefined || value === '') {
		return usesArrayAnswer(question) ? [] : '';
	}

	if (!usesArrayAnswer(question)) {
		if (question.type === 'likert' || question.type === 'nps' || question.type === 'number') {
			const numericValue = Number(value);
			return Number.isFinite(numericValue) ? numericValue : '';
		}

		return value;
	}

	try {
		const parsed = JSON.parse(value);
		return Array.isArray(parsed) ? parsed.map(String) : [];
	} catch {
		return [];
	}
}

function usesArrayAnswer(question: RespondentQuestionResponse): boolean {
	return question.type === 'multi' || question.type === 'ranking';
}

function scaleDefinition(question: RespondentQuestionResponse) {
	const fallbackMin = question.type === 'nps' ? 0 : 1;
	const fallbackMax = question.type === 'nps' ? 10 : 5;
	const anchors = parseScaleAnchors(question.scaleAnchors);
	const lowAnchor = anchors.find((anchor) => anchor.value === (question.scaleMinValue ?? fallbackMin));
	const highAnchor = anchors.find((anchor) => anchor.value === (question.scaleMaxValue ?? fallbackMax));

	return {
		min: question.scaleMinValue ?? fallbackMin,
		max: question.scaleMaxValue ?? fallbackMax,
		lowLabel: lowAnchor?.label,
		highLabel: highAnchor?.label
	};
}

function questionChoices(question: RespondentQuestionResponse): RespondentSurveyChoice[] {
	const payload = parseObject(question.payload);
	const options = Array.isArray(payload.options) ? payload.options : [];
	return options
		.map((option, index) => {
			const candidate = option && typeof option === 'object' ? option : {};
			const code = readString(candidate, 'code') || `o${String(index + 1).padStart(2, '0')}`;
			const label = readString(candidate, 'label') || code;
			return {
				value: code,
				text: label,
				isExclusive: readBoolean(candidate, 'exclusive') || undefined
			};
		})
		.filter((choice) => choice.value.trim() && choice.text.trim());
}

function surveyOtherChoiceOptions(choice: Record<string, unknown>): Partial<RespondentSurveyElement> {
	if (!readBoolean(choice, 'allowOther')) {
		return {};
	}

	const otherText = readString(choice, 'otherLabel');
	return {
		showOtherItem: true,
		...(otherText ? { otherText } : {})
	};
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

function parseObject(value: string | null | undefined): Record<string, unknown> {
	try {
		const parsed = JSON.parse(value ?? '{}');
		return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
			? (parsed as Record<string, unknown>)
			: {};
	} catch {
		return {};
	}
}

function readString(source: object, key: string): string | null {
	const value = (source as Record<string, unknown>)[key];
	return typeof value === 'string' ? value : null;
}

function readNumber(source: object, key: string): number | null {
	const value = (source as Record<string, unknown>)[key];
	return typeof value === 'number' ? value : null;
}

function readBoolean(source: object, key: string): boolean {
	const value = (source as Record<string, unknown>)[key];
	return value === true;
}

function readObject(source: object, key: string): Record<string, unknown> {
	const value = (source as Record<string, unknown>)[key];
	return value && typeof value === 'object' && !Array.isArray(value)
		? (value as Record<string, unknown>)
		: {};
}
