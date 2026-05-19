import { describe, expect, it } from 'vitest';
import type { RespondentQuestionResponse } from '../api/setup';
import {
	buildRespondentSurveyJson,
	normalizeSurveyDataToAnswers,
	toSurveyInitialData
} from './surveyjs-adapter';

const scaleQuestion = {
	id: '07d19d0a-8417-41d7-8b36-5147d96ad7f8',
	ordinal: 1,
	code: 'q01',
	type: 'likert',
	textDefault: 'After work, I need time to recover mentally.',
	required: true,
	scaleMinValue: 1,
	scaleMaxValue: 5
} satisfies RespondentQuestionResponse;

const choicePayload = JSON.stringify({
	options: [
		{ code: 'o01', label: 'Morning' },
		{ code: 'o02', label: 'Afternoon' },
		{ code: 'o03', label: 'Evening' }
	]
});

function question(
	overrides: Partial<RespondentQuestionResponse> & Pick<RespondentQuestionResponse, 'type'>
): RespondentQuestionResponse {
	return {
		id: `question-${overrides.type}`,
		ordinal: 1,
		code: `q_${overrides.type}`,
		textDefault: `Question ${overrides.type}`,
		required: false,
		...overrides
	};
}

describe('respondent SurveyJS adapter', () => {
	it('renders rating scales as bounded rating controls keyed by backend question id', () => {
		const surveyJson = buildRespondentSurveyJson([scaleQuestion]);

		expect(surveyJson).toMatchObject({
			showQuestionNumbers: 'off',
			questionTitleLocation: 'top',
			showNavigationButtons: false,
			showCompleteButton: false,
			elements: [
				{
					type: 'rating',
					name: scaleQuestion.id,
					title: scaleQuestion.textDefault,
					isRequired: true,
					rateMin: 1,
					rateMax: 5
				}
			]
		});
	});

	it('renders supported researcher-authored respondent formats', () => {
		const surveyJson = buildRespondentSurveyJson([
			question({ type: 'text' }),
			question({ type: 'number' }),
			question({ type: 'date' }),
			question({ type: 'single', payload: choicePayload }),
			question({ type: 'multi', payload: choicePayload }),
			question({ type: 'ranking', payload: choicePayload }),
			question({ type: 'nps', scaleMinValue: 0, scaleMaxValue: 10 })
		]);

		expect(surveyJson.elements).toMatchObject([
			{ type: 'text', name: 'question-text' },
			{ type: 'text', name: 'question-number', inputType: 'number' },
			{ type: 'text', name: 'question-date', inputType: 'date' },
			{
				type: 'radiogroup',
				name: 'question-single',
				choices: [
					{ value: 'o01', text: 'Morning' },
					{ value: 'o02', text: 'Afternoon' },
					{ value: 'o03', text: 'Evening' }
				]
			},
			{
				type: 'checkbox',
				name: 'question-multi',
				choices: [
					{ value: 'o01', text: 'Morning' },
					{ value: 'o02', text: 'Afternoon' },
					{ value: 'o03', text: 'Evening' }
				]
			},
			{
				type: 'ranking',
				name: 'question-ranking',
				choices: [
					{ value: 'o01', text: 'Morning' },
					{ value: 'o02', text: 'Afternoon' },
					{ value: 'o03', text: 'Evening' }
				]
			},
			{ type: 'rating', name: 'question-nps', rateMin: 0, rateMax: 10 }
		]);
	});

	it('maps route answers into SurveyJS initial data', () => {
		const multiQuestion = question({ type: 'multi', payload: choicePayload });

		expect(
			toSurveyInitialData([scaleQuestion, multiQuestion], {
				[scaleQuestion.id]: '4',
				[multiQuestion.id]: '["o01","o03"]'
			})
		).toEqual({
			[scaleQuestion.id]: 4,
			[multiQuestion.id]: ['o01', 'o03']
		});
	});

	it('normalizes SurveyJS data back to route answer strings', () => {
		const multiQuestion = question({ type: 'multi', payload: choicePayload });

		expect(
			normalizeSurveyDataToAnswers([scaleQuestion, multiQuestion], {
				[scaleQuestion.id]: 5,
				[multiQuestion.id]: ['o01', 'o03']
			})
		).toEqual({
			[scaleQuestion.id]: '5',
			[multiQuestion.id]: '["o01","o03"]'
		});
		expect(normalizeSurveyDataToAnswers([scaleQuestion], {})).toEqual({
			[scaleQuestion.id]: ''
		});
	});

	it('normalizes single changed values with the question answer semantics', () => {
		const rankingQuestion = question({ type: 'ranking', payload: choicePayload });

		expect(normalizeSurveyDataToAnswers([rankingQuestion], { [rankingQuestion.id]: ['o02', 'o01'] })).toEqual({
			[rankingQuestion.id]: '["o02","o01"]'
		});
		expect(normalizeSurveyDataToAnswers([scaleQuestion], { [scaleQuestion.id]: 5 })).toEqual({
			[scaleQuestion.id]: '5'
		});
	});
});
