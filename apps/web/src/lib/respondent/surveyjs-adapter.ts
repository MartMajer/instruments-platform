import type { RespondentQuestionResponse } from '../api/setup';

export type RespondentSurveyJson = {
	showQuestionNumbers: 'off';
	questionTitleLocation: 'top';
	showNavigationButtons: false;
	showCompleteButton: false;
	elements: RespondentSurveyElement[];
};

export type RespondentSurveyElement = {
	type: 'text';
	name: string;
	title: string;
	isRequired: boolean;
	inputType?: 'number';
	min?: number;
	max?: number;
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
	return Object.fromEntries(questions.map((question) => [question.id, answers[question.id] ?? '']));
}

export function normalizeSurveyDataToAnswers(
	questions: RespondentQuestionResponse[],
	data: Record<string, unknown>
) {
	return Object.fromEntries(
		questions.map((question) => {
			const value = data[question.id];

			return [question.id, value === undefined || value === null ? '' : String(value)];
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

	if (
		typeof question.scaleMinValue === 'number' &&
		typeof question.scaleMaxValue === 'number'
	) {
		element.inputType = 'number';
		element.min = question.scaleMinValue;
		element.max = question.scaleMaxValue;
	}

	return element;
}
