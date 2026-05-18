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

describe('respondent SurveyJS adapter', () => {
	it('builds minimal SurveyJS JSON keyed by backend question id', () => {
		const surveyJson = buildRespondentSurveyJson([scaleQuestion]);

		expect(surveyJson).toMatchObject({
			showQuestionNumbers: 'off',
			questionTitleLocation: 'top',
			showNavigationButtons: false,
			showCompleteButton: false,
			elements: [
				{
					type: 'text',
					name: scaleQuestion.id,
					title: scaleQuestion.textDefault,
					isRequired: true,
					inputType: 'number',
					min: 1,
					max: 5
				}
			]
		});
	});

	it('maps route answers into SurveyJS initial data', () => {
		expect(toSurveyInitialData([scaleQuestion], { [scaleQuestion.id]: '4' })).toEqual({
			[scaleQuestion.id]: '4'
		});
	});

	it('normalizes SurveyJS data back to route answer strings', () => {
		expect(normalizeSurveyDataToAnswers([scaleQuestion], { [scaleQuestion.id]: 5 })).toEqual({
			[scaleQuestion.id]: '5'
		});
		expect(normalizeSurveyDataToAnswers([scaleQuestion], {})).toEqual({
			[scaleQuestion.id]: ''
		});
	});
});
