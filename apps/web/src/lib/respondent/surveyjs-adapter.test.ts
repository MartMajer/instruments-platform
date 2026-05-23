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

const matrixPayload = JSON.stringify({
	matrix: {
		mode: 'single',
		rows: [
			{ code: 'r01', label: 'Neck / shoulders' },
			{ code: 'r02', label: 'Lower back' }
		],
		columns: [
			{ code: 'c01', label: 'None' },
			{ code: 'c02', label: 'Mild' },
			{ code: 'c03', label: 'Severe' }
		]
	}
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
			question({ type: 'matrix', payload: matrixPayload }),
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
			{
				type: 'matrix',
				name: 'question-matrix',
				rows: [
					{ value: 'r01', text: 'Neck / shoulders' },
					{ value: 'r02', text: 'Lower back' }
				],
				columns: [
					{ value: 'c01', text: 'None' },
					{ value: 'c02', text: 'Mild' },
					{ value: 'c03', text: 'Severe' }
				]
			},
			{ type: 'rating', name: 'question-nps', rateMin: 0, rateMax: 10 }
		]);
	});

	it('carries question metadata into SurveyJS controls where the runtime supports it', () => {
		const surveyJson = buildRespondentSurveyJson([
			question({
				type: 'number',
				payload: JSON.stringify({
					validation: { min: 0, max: 80, integerOnly: true },
					display: { unit: 'hours/week' }
				})
			}),
			question({
				type: 'date',
				payload: JSON.stringify({
					validation: { minDate: '2026-01-01', maxDate: '2026-12-31' }
				})
			}),
			question({
				type: 'text',
				payload: JSON.stringify({
					text: { multiline: true, maxLength: 500 }
				})
			}),
			question({
				type: 'multi',
				payload: JSON.stringify({
					options: [
						{ code: 'o01', label: 'Workload' },
						{ code: 'o02', label: 'None of these', exclusive: true }
					],
					choice: { allowOther: true, otherLabel: 'Other barrier' }
				})
			}),
			question({
				type: 'ranking',
				payload: JSON.stringify({
					options: [
						{ code: 'o01', label: 'Staffing' },
						{ code: 'o02', label: 'Tools' },
						{ code: 'o03', label: 'Training' }
					],
					ranking: { mode: 'top_n', topN: 2 }
				})
			})
		]);

		expect(surveyJson.elements).toMatchObject([
			{ type: 'text', inputType: 'number', min: 0, max: 80, step: 1 },
			{ type: 'text', inputType: 'date', min: '2026-01-01', max: '2026-12-31' },
			{ type: 'comment', maxLength: 500 },
			{
				type: 'checkbox',
				showOtherItem: true,
				otherText: 'Other barrier',
				choices: [
					{ value: 'o01', text: 'Workload' },
					{ value: 'o02', text: 'None of these', isExclusive: true }
				]
			},
			{
				type: 'ranking',
				selectToRankEnabled: true,
				maxSelectedChoices: 2
			}
		]);
	});

	it('maps route answers into SurveyJS initial data', () => {
		const multiQuestion = question({ type: 'multi', payload: choicePayload });
		const matrixQuestion = question({ type: 'matrix', payload: matrixPayload });

		expect(
			toSurveyInitialData([scaleQuestion, multiQuestion, matrixQuestion], {
				[scaleQuestion.id]: '4',
				[multiQuestion.id]: '["o01","o03"]',
				[matrixQuestion.id]: '{"r01":"c02","r02":"c03","unknown":"c01"}'
			})
		).toEqual({
			[scaleQuestion.id]: 4,
			[multiQuestion.id]: ['o01', 'o03'],
			[matrixQuestion.id]: { r01: 'c02', r02: 'c03' }
		});
	});

	it('normalizes SurveyJS data back to route answer strings', () => {
		const multiQuestion = question({ type: 'multi', payload: choicePayload });
		const matrixQuestion = question({ type: 'matrix', payload: matrixPayload });

		expect(
			normalizeSurveyDataToAnswers([scaleQuestion, multiQuestion, matrixQuestion], {
				[scaleQuestion.id]: 5,
				[multiQuestion.id]: ['o01', 'o03'],
				[matrixQuestion.id]: { r01: 'c02', r02: 'c03', unknown: 'c01' }
			})
		).toEqual({
			[scaleQuestion.id]: '5',
			[multiQuestion.id]: '["o01","o03"]',
			[matrixQuestion.id]: '{"r01":"c02","r02":"c03"}'
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
