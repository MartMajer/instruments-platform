import { describe, expect, it } from 'vitest';
import type { RespondentQuestionResponse } from '../api/setup';
import {
	answerToInputValue,
	inputValueToAnswer,
	isQuestionAnswered,
	matrixQuestionChoices,
	questionInputConstraints,
	questionChoices,
	rankingQuestionOptions,
	scaleQuestionOptions,
	visibleRespondentQuestions
} from './respondent-question-model';

const scaleQuestion = {
	id: '07d19d0a-8417-41d7-8b36-5147d96ad7f8',
	ordinal: 1,
	code: 'q01',
	type: 'likert',
	textDefault: 'After work, I need time to recover mentally.',
	required: true,
	scaleMinValue: 1,
	scaleMaxValue: 5,
	scaleAnchors: JSON.stringify([
		{ value: 1, label: 'Strongly disagree' },
		{ value: 5, label: 'Strongly agree' }
	])
} satisfies RespondentQuestionResponse;

const choicePayload = JSON.stringify({
	options: [
		{ code: 'o01', label: 'Morning' },
		{ code: 'o02', label: 'Afternoon' },
		{ code: 'o03', label: 'Evening' }
	],
	choice: { allowOther: true, otherLabel: 'Another time' }
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

describe('respondent question model', () => {
	it('sorts respondent questions and applies constrained display logic', () => {
		const source = question({
			id: 'source-id',
			ordinal: 2,
			code: 'q_single',
			type: 'single',
			payload: choicePayload
		});
		const followUp = question({
			id: 'follow-up-id',
			ordinal: 3,
			code: 'q_followup',
			type: 'text',
			payload: JSON.stringify({
				displayLogic: {
					mode: 'show_when',
					sourceQuestionCode: 'q_single',
					operator: 'equals',
					value: 'o02'
				}
			})
		});
		const first = question({ id: 'first-id', ordinal: 1, code: 'q_first', type: 'number' });

		expect(visibleRespondentQuestions([followUp, source, first], {})).toEqual([first, source]);
		expect(
			visibleRespondentQuestions([followUp, source, first], { 'source-id': '"o02"' }).map(
				(visible) => visible.id
			)
		).toEqual(['first-id', 'source-id', 'follow-up-id']);
		expect(
			visibleRespondentQuestions([followUp, source, first], { 'source-id': '"o01"' }).map(
				(visible) => visible.id
			)
		).toEqual(['first-id', 'source-id']);
	});

	it('builds first-party control options for scale, choice, ranking, and matrix questions', () => {
		expect(scaleQuestionOptions(scaleQuestion)).toEqual({
			min: 1,
			max: 5,
			lowLabel: 'Strongly disagree',
			highLabel: 'Strongly agree',
			options: [
				{ value: 1, label: '1', anchorLabel: 'Strongly disagree' },
				{ value: 2, label: '2', anchorLabel: null },
				{ value: 3, label: '3', anchorLabel: null },
				{ value: 4, label: '4', anchorLabel: null },
				{ value: 5, label: '5', anchorLabel: 'Strongly agree' }
			]
		});
		expect(questionChoices(question({ type: 'single', payload: choicePayload }))).toEqual([
			{ value: 'o01', label: 'Morning', isExclusive: false },
			{ value: 'o02', label: 'Afternoon', isExclusive: false },
			{ value: 'o03', label: 'Evening', isExclusive: false }
		]);
		expect(
			rankingQuestionOptions(
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
			)
		).toEqual({ mode: 'top_n', topN: 2 });
		expect(matrixQuestionChoices(question({ type: 'matrix', payload: matrixPayload }))).toEqual({
			rows: [
				{ value: 'r01', label: 'Neck / shoulders', isExclusive: false },
				{ value: 'r02', label: 'Lower back', isExclusive: false }
			],
			columns: [
				{ value: 'c01', label: 'None', isExclusive: false },
				{ value: 'c02', label: 'Mild', isExclusive: false },
				{ value: 'c03', label: 'Severe', isExclusive: false }
			]
		});
	});

	it('preserves existing route answer serialization semantics', () => {
		const multiQuestion = question({ type: 'multi', payload: choicePayload });
		const matrixQuestion = question({ type: 'matrix', payload: matrixPayload });
		const singleQuestion = question({ type: 'single', payload: choicePayload });
		const textQuestion = question({ type: 'text' });

		expect(answerToInputValue(scaleQuestion, '4')).toBe(4);
		expect(answerToInputValue(multiQuestion, '["o01","o03"]')).toEqual(['o01', 'o03']);
		expect(answerToInputValue(matrixQuestion, '{"r01":"c02","r02":"c03","unknown":"c01"}')).toEqual({
			r01: 'c02',
			r02: 'c03'
		});
		expect(answerToInputValue(singleQuestion, '"o02"')).toBe('o02');
		expect(answerToInputValue(textQuestion, '"Needs follow-up"')).toBe('Needs follow-up');

		expect(inputValueToAnswer(scaleQuestion, 5)).toBe('5');
		expect(inputValueToAnswer(multiQuestion, ['o01', 'o03'])).toBe('["o01","o03"]');
		expect(
			inputValueToAnswer(matrixQuestion, { r01: 'c02', r02: 'c03', unknown: 'c01' })
		).toBe('{"r01":"c02","r02":"c03"}');
		expect(inputValueToAnswer(singleQuestion, 'o02')).toBe('"o02"');
		expect(inputValueToAnswer(textQuestion, 'Needs follow-up')).toBe('"Needs follow-up"');
		expect(inputValueToAnswer(textQuestion, '')).toBe('');
	});

	it('detects complete and incomplete answers by question shape', () => {
		const multiQuestion = question({ type: 'multi', payload: choicePayload, required: true });
		const matrixQuestion = question({ type: 'matrix', payload: matrixPayload, required: true });
		const requiredTextQuestion = question({ type: 'text', required: true });
		const optionalTextQuestion = question({ type: 'text', required: false });

		expect(isQuestionAnswered(scaleQuestion, '3')).toBe(true);
		expect(isQuestionAnswered(scaleQuestion, '')).toBe(false);
		expect(isQuestionAnswered(multiQuestion, '[]')).toBe(false);
		expect(isQuestionAnswered(multiQuestion, '["o02"]')).toBe(true);
		expect(isQuestionAnswered(matrixQuestion, '{"r01":"c02"}')).toBe(false);
		expect(isQuestionAnswered(matrixQuestion, '{"r01":"c02","r02":"c03"}')).toBe(true);
		expect(isQuestionAnswered(requiredTextQuestion, '"   "')).toBe(false);
		expect(isQuestionAnswered(requiredTextQuestion, '"Needs follow-up"')).toBe(true);
		expect(isQuestionAnswered(optionalTextQuestion, '')).toBe(true);
	});

	it('extracts input constraints for number, date, and text questions', () => {
		expect(
			questionInputConstraints(
				question({
					type: 'number',
					payload: JSON.stringify({
						validation: { min: 0, max: 80, integerOnly: true },
						display: { unit: 'hours/week' }
					})
				})
			)
		).toEqual({
			min: 0,
			max: 80,
			step: 1,
			minDate: null,
			maxDate: null,
			maxLength: null,
			multiline: false,
			unit: 'hours/week'
		});
		expect(
			questionInputConstraints(
				question({
					type: 'date',
					payload: JSON.stringify({ validation: { minDate: '2026-01-01', maxDate: '2026-12-31' } })
				})
			)
		).toMatchObject({ minDate: '2026-01-01', maxDate: '2026-12-31' });
		expect(
			questionInputConstraints(
				question({
					type: 'text',
					payload: JSON.stringify({ text: { multiline: true, maxLength: 500 } })
				})
			)
		).toMatchObject({ multiline: true, maxLength: 500 });
	});
});
