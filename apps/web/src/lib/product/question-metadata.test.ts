import { describe, expect, test } from 'vitest';
import {
	createDefaultTemplateQuestionRows,
	toCreateTemplateQuestions,
	toDraftRespondentPreviewContract,
	toDraftRespondentQuestions,
	validateTemplateQuestionRows,
	type TemplateQuestionAuthoringRow
} from './template-authoring';

function row(
	overrides: Partial<TemplateQuestionAuthoringRow> &
		Pick<TemplateQuestionAuthoringRow, 'code' | 'type'>
): TemplateQuestionAuthoringRow {
	return {
		...createDefaultTemplateQuestionRows()[0],
		textDefault: `Question ${overrides.code}`,
		dimensionLabel: 'Metadata',
		...overrides
	} as TemplateQuestionAuthoringRow;
}

describe('advanced question metadata', () => {
	test('serializes existing answer-format metadata into create-template payloads', () => {
		const rows = [
			row({
				code: 'weekly_hours',
				type: 'number',
				numberMin: 0,
				numberMax: 80,
				numberUnit: 'hours/week',
				numberIntegerOnly: true
			}),
			row({
				code: 'context_note',
				type: 'text',
				textMultiline: true,
				textMaxLength: 500
			}),
			row({
				code: 'followup_date',
				type: 'date',
				dateEarliest: '2026-01-01',
				dateLatest: '2026-12-31'
			}),
			row({
				code: 'barriers',
				type: 'multi',
				choiceOptions: ['Workload', 'Equipment', 'None of these'],
				choiceAllowOther: true,
				choiceOtherLabel: 'Other barrier',
				choiceExclusiveOptionLabel: 'None of these'
			}),
			row({
				code: 'priorities',
				type: 'ranking',
				choiceOptions: ['Staffing', 'Tools', 'Training', 'Schedule'],
				rankingMode: 'top_n',
				rankingTopN: 2
			}),
			row({
				code: 'body_discomfort',
				type: 'matrix',
				matrixRows: ['Neck / shoulders', 'Lower back'],
				matrixColumns: ['None', 'Mild', 'Severe']
			})
		];

		const payloads = Object.fromEntries(
			toCreateTemplateQuestions(rows).map((question) => [
				question.code,
				JSON.parse(question.payload)
			])
		);

		expect(payloads.weekly_hours).toEqual({
			validation: { min: 0, max: 80, integerOnly: true },
			display: { unit: 'hours/week' }
		});
		expect(payloads.context_note).toEqual({
			text: { multiline: true, maxLength: 500 }
		});
		expect(payloads.followup_date).toEqual({
			validation: { minDate: '2026-01-01', maxDate: '2026-12-31' }
		});
		expect(payloads.barriers).toEqual({
			options: [
				{ code: 'o01', label: 'Workload' },
				{ code: 'o02', label: 'Equipment' },
				{ code: 'o03', label: 'None of these', exclusive: true },
				{ code: 'o04', label: 'Other barrier', isOther: true }
			],
			choice: {
				allowOther: true,
				otherLabel: 'Other barrier',
				exclusiveOptionCode: 'o03'
			}
		});
		expect(payloads.priorities).toEqual({
			options: [
				{ code: 'o01', label: 'Staffing' },
				{ code: 'o02', label: 'Tools' },
				{ code: 'o03', label: 'Training' },
				{ code: 'o04', label: 'Schedule' }
			],
			ranking: { mode: 'top_n', topN: 2 }
		});
		expect(payloads.body_discomfort).toEqual({
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
	});

	test('validates metadata that would make respondent answers ambiguous', () => {
		const errors = validateTemplateQuestionRows([
			row({ code: 'bad_number', type: 'number', numberMin: 10, numberMax: 5 }),
			row({
				code: 'bad_text',
				type: 'text',
				textMaxLength: 0
			}),
			row({
				code: 'bad_date',
				type: 'date',
				dateEarliest: '2026-12-31',
				dateLatest: '2026-01-01'
			}),
			row({
				code: 'bad_choice',
				type: 'multi',
				choiceOptions: ['Yes', 'No'],
				choiceExclusiveOptionLabel: 'Not applicable'
			}),
			row({
				code: 'bad_ranking',
				type: 'ranking',
				choiceOptions: ['A', 'B'],
				rankingMode: 'top_n',
				rankingTopN: 3
			}),
			row({
				code: 'bad_matrix',
				type: 'matrix',
				matrixRows: [],
				matrixColumns: ['Only one']
			})
		]);

		expect(errors).toEqual(
			expect.arrayContaining([
				'Question 1 number minimum must be less than or equal to the maximum.',
				'Question 2 text max length must be greater than zero.',
				'Question 3 earliest date must be on or before latest date.',
				'Question 4 exclusive option must match an answer option.',
				'Question 5 top-N ranking must be between 1 and the number of available options.',
				'Question 6 matrix needs at least one row.',
				'Question 6 matrix needs at least two column options.'
			])
		);
	});

	test('serializes and validates constrained follow-up display rules', () => {
		const source = row({
			code: 'has_barrier',
			type: 'single',
			choiceOptions: ['Yes', 'No']
		});
		const followUp = row({
			code: 'barrier_detail',
			type: 'text',
			displayLogicEnabled: true,
			displayLogicSourceQuestionCode: 'has_barrier',
			displayLogicSourceOptionCode: 'o01'
		});

		const payload = JSON.parse(
			toCreateTemplateQuestions([source, followUp]).find(
				(question) => question.code === 'barrier_detail'
			)?.payload ?? '{}'
		);

		expect(payload.displayLogic).toEqual({
			mode: 'show_when',
			sourceQuestionCode: 'has_barrier',
			operator: 'equals',
			value: 'o01',
			requiredWhenVisible: true
		});
		expect(validateTemplateQuestionRows([source, followUp])).toEqual([]);
		expect(validateTemplateQuestionRows([followUp])).toEqual(
			expect.arrayContaining(['Question 1 display rule needs an earlier source question.'])
		);
		expect(
			validateTemplateQuestionRows([
				source,
				{
					...followUp,
					displayLogicSourceOptionCode: 'o99'
				}
			])
		).toEqual(expect.arrayContaining(['Question 2 display rule needs one source answer.']));
	});

	test('shows metadata in respondent preview instead of generic missing-metadata warnings', () => {
		const preview = toDraftRespondentPreviewContract(
			[
				row({
					code: 'weekly_hours',
					type: 'number',
					numberMin: 0,
					numberMax: 80,
					numberUnit: 'hours/week',
					numberIntegerOnly: true
				}),
				row({
					code: 'context_note',
					type: 'text',
					textMultiline: true,
					textMaxLength: 500
				}),
				row({
					code: 'followup_date',
					type: 'date',
					dateEarliest: '2026-01-01',
					dateLatest: '2026-12-31'
				}),
				row({
					code: 'barriers',
					type: 'multi',
					choiceOptions: ['Workload', 'Equipment', 'None of these'],
					choiceAllowOther: true,
					choiceOtherLabel: 'Other barrier',
					choiceExclusiveOptionLabel: 'None of these'
				}),
				row({
					code: 'priorities',
					type: 'ranking',
					choiceOptions: ['Staffing', 'Tools', 'Training', 'Schedule'],
					rankingMode: 'top_n',
					rankingTopN: 2
				}),
				row({
					code: 'body_discomfort',
					type: 'matrix',
					matrixRows: ['Neck / shoulders', 'Lower back'],
					matrixColumns: ['None', 'Mild', 'Severe']
				})
			],
			[]
		);

		const numberQuestion = preview.questions.find((question) => question.code === 'weekly_hours');
		const textQuestion = preview.questions.find((question) => question.code === 'context_note');
		const dateQuestion = preview.questions.find((question) => question.code === 'followup_date');
		const choiceQuestion = preview.questions.find((question) => question.code === 'barriers');
		const rankingQuestion = preview.questions.find((question) => question.code === 'priorities');
		const matrixQuestion = preview.questions.find(
			(question) => question.code === 'body_discomfort'
		);

		expect(numberQuestion?.answerFormatDetail).toContain('0 to 80');
		expect(numberQuestion?.answerFormatDetail).toContain('hours/week');
		expect(numberQuestion?.answerFormatDetail).toContain('whole numbers');
		expect(numberQuestion?.warnings).not.toContain(
			'Number input has no min, max, unit, or decimal constraint yet.'
		);
		expect(textQuestion?.answerFormatDetail).toContain('Long text');
		expect(textQuestion?.answerFormatDetail).toContain('500 characters');
		expect(dateQuestion?.answerFormatDetail).toContain('2026-01-01 to 2026-12-31');
		expect(choiceQuestion?.answerFormatDetail).toContain('Other barrier');
		expect(choiceQuestion?.choices.map((choice) => choice.text)).toContain(
			'Other barrier (write-in)'
		);
		expect(choiceQuestion?.choices.map((choice) => choice.text)).toContain(
			'None of these (exclusive)'
		);
		expect(rankingQuestion?.answerFormatDetail).toContain('top 2');
		expect(matrixQuestion?.answerFormatLabel).toBe('Matrix / grid');
		expect(matrixQuestion?.answerFormatDetail).toContain('2 rows');
		expect(matrixQuestion?.matrixRows.map((row) => row.text)).toEqual([
			'Neck / shoulders',
			'Lower back'
		]);
		expect(matrixQuestion?.matrixColumns.map((column) => column.text)).toEqual([
			'None',
			'Mild',
			'Severe'
		]);
	});

	test('maps draft questions to the real respondent runner question contract', () => {
		const scale = row({
			code: 'strain',
			type: 'likert',
			scaleMin: 1,
			scaleMax: 5,
			scaleLowLabel: 'Strongly disagree',
			scaleHighLabel: 'Strongly agree'
		});
		const source = row({
			code: 'has_barrier',
			type: 'single',
			choiceOptions: ['Yes', 'No']
		});
		const followUp = row({
			code: 'barrier_detail',
			type: 'text',
			textMultiline: true,
			textMaxLength: 240,
			displayLogicEnabled: true,
			displayLogicSourceQuestionCode: 'has_barrier',
			displayLogicSourceOptionCode: 'o01'
		});

		const questions = toDraftRespondentQuestions([scale, source, followUp]);

		expect(questions).toHaveLength(3);
		expect(questions[0]).toMatchObject({
			id: 'draft-strain',
			ordinal: 1,
			code: 'strain',
			type: 'likert',
			textDefault: 'Question strain',
			required: true,
			scaleCode: 'scale_strain',
			scaleMinValue: 1,
			scaleMaxValue: 5
		});
		expect(JSON.parse(questions[0].scaleAnchors ?? '[]')).toEqual([
			{ value: 1, label: 'Strongly disagree' },
			{ value: 5, label: 'Strongly agree' }
		]);
		expect(JSON.parse(questions[1].payload ?? '{}').options).toEqual([
			{ code: 'o01', label: 'Yes' },
			{ code: 'o02', label: 'No' }
		]);
		expect(JSON.parse(questions[2].payload ?? '{}')).toEqual({
			text: { multiline: true, maxLength: 240 },
			displayLogic: {
				mode: 'show_when',
				sourceQuestionCode: 'has_barrier',
				operator: 'equals',
				value: 'o01',
				requiredWhenVisible: true
			}
		});
	});
});
