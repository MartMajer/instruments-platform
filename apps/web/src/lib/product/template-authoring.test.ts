import { describe, expect, it } from 'vitest';
import {
	appendTemplateQuestionRow,
	buildScoreProduces,
	buildScoringDocument,
	buildMeanScoringDocument,
	createDefaultScoreOutputRows,
	createDefaultTemplateQuestionRows,
	moveTemplateQuestionRow,
	removeTemplateQuestionRow,
	toCreateTemplateQuestions,
	validateTemplateQuestionRows
} from './template-authoring';

describe('template authoring helpers', () => {
	it('creates default editable likert rows', () => {
		const rows = createDefaultTemplateQuestionRows();

		expect(rows.map((row) => ({ ordinal: row.ordinal, code: row.code, type: row.type }))).toEqual([
			{ ordinal: 1, code: 'q01', type: 'likert' },
			{ ordinal: 2, code: 'q02', type: 'likert' },
			{ ordinal: 3, code: 'q03', type: 'likert' }
		]);
		expect(rows[2]?.reverseCoded).toBe(true);
	});

	it('appends the next question row and keeps existing rows unchanged', () => {
		const rows = createDefaultTemplateQuestionRows();
		const nextRows = appendTemplateQuestionRow(rows);

		expect(rows).toHaveLength(3);
		expect(nextRows.map((row) => row.code)).toEqual(['q01', 'q02', 'q03', 'q04']);
		expect(nextRows.at(-1)).toMatchObject({
			ordinal: 4,
			code: 'q04',
			type: 'likert',
			required: true,
			reverseCoded: false
		});
	});

	it('removes a question and renumbers ordinals', () => {
		const rows = removeTemplateQuestionRow(createDefaultTemplateQuestionRows(), 'q02');

		expect(rows.map((row) => ({ ordinal: row.ordinal, code: row.code }))).toEqual([
			{ ordinal: 1, code: 'q01' },
			{ ordinal: 2, code: 'q03' }
		]);
	});

	it('does not remove the last remaining question', () => {
		const rows = removeTemplateQuestionRow([createDefaultTemplateQuestionRows()[0]], 'q01');

		expect(rows.map((row) => row.code)).toEqual(['q01']);
	});

	it('moves questions up and down while renumbering ordinals', () => {
		const movedUp = moveTemplateQuestionRow(createDefaultTemplateQuestionRows(), 'q03', 'up');
		expect(movedUp.map((row) => ({ ordinal: row.ordinal, code: row.code }))).toEqual([
			{ ordinal: 1, code: 'q01' },
			{ ordinal: 2, code: 'q03' },
			{ ordinal: 3, code: 'q02' }
		]);

		const movedDown = moveTemplateQuestionRow(movedUp, 'q01', 'down');
		expect(movedDown.map((row) => ({ ordinal: row.ordinal, code: row.code }))).toEqual([
			{ ordinal: 1, code: 'q03' },
			{ ordinal: 2, code: 'q01' },
			{ ordinal: 3, code: 'q02' }
		]);
	});

	it('validates non-empty unique codes and text', () => {
		expect(
			validateTemplateQuestionRows([
				{ ...createDefaultTemplateQuestionRows()[0], code: '', textDefault: 'Valid text' },
				{ ...createDefaultTemplateQuestionRows()[1], code: 'q02', textDefault: '' },
				{ ...createDefaultTemplateQuestionRows()[2], code: 'q02', textDefault: 'Duplicate code' }
			])
		).toEqual([
			'Question 1 needs an internal code.',
			'Question 2 needs question text.',
			'Question code q02 is duplicated.'
		]);
	});

	it('maps authoring rows to the existing create-template request shape', () => {
		const rows = [
			{
				...createDefaultTemplateQuestionRows()[0],
				code: 'energy',
				textDefault: 'I have enough energy after work.',
				required: false
			}
		];

		expect(toCreateTemplateQuestions(rows)).toEqual([
			{
				ordinal: 1,
				code: 'energy',
				type: 'likert',
				textDefault: 'I have enough energy after work.',
				sectionCode: 'core',
				scaleCode: 'scale_energy',
				required: false,
				reverseCoded: false,
				measurementLevel: 'ordinal',
				payload: JSON.stringify({
					scale: {
						min: 1,
						max: 5,
						lowLabel: 'Strongly disagree',
						highLabel: 'Strongly agree'
					}
				}),
				missingCodes: '[]'
			}
		]);
	});

	it('builds default scoring from current question codes and reverse-coded flags', () => {
		const rows = appendTemplateQuestionRow(createDefaultTemplateQuestionRows()).map((row) =>
			row.code === 'q04' ? { ...row, reverseCoded: true } : row
		);
		const document = JSON.parse(buildMeanScoringDocument('tenant-rule.total', rows)) as {
			rule_id: string;
			inputs: Array<{ items: string[] }>;
			nodes: Array<{ id: string; explicit_reverse_items?: string[] }>;
		};

		expect(document.rule_id).toBe('tenant-rule.total');
		expect(document.inputs[0]?.items).toEqual(['q01', 'q02', 'q03', 'q04']);
		expect(document.nodes.find((node) => node.id === 'total_scored_answers')?.explicit_reverse_items).toEqual([
			'q03',
			'q04'
		]);
	});

	it('builds multiple scoring outputs with per-output question sets and missing policies', () => {
		const rows = appendTemplateQuestionRow(createDefaultTemplateQuestionRows()).map((row) =>
			row.code === 'q04'
				? { ...row, code: 'recovery', textDefault: 'I recover quickly.', reverseCoded: false }
				: row
		);
		const outputs = [
			{
				...createDefaultScoreOutputRows(rows)[0],
				name: 'Exhaustion',
				code: 'exhaustion',
				includedQuestionCodes: ['q01', 'q03'],
				missingStrategy: 'require_all' as const
			},
			{
				localId: 'score-recovery',
				name: 'Recovery',
				code: 'recovery',
				calculation: 'sum' as const,
				missingStrategy: 'min_valid_count' as const,
				minValidCount: 1,
				includedQuestionCodes: ['recovery']
			}
		];
		const document = JSON.parse(buildScoringDocument('tenant-rule.multi', rows, outputs)) as {
			inputs: Array<{ id: string; items: string[] }>;
			nodes: Array<{ id: string; op: string; input: string; missing_data?: Record<string, unknown> }>;
			outputs: Array<{ code: string; node: string }>;
		};

		expect(document.inputs).toEqual([
			{ id: 'exhaustion_items', kind: 'answers', items: ['q01', 'q03'] },
			{ id: 'recovery_items', kind: 'answers', items: ['recovery'] }
		]);
		expect(document.nodes.find((node) => node.id === 'exhaustion_score')).toMatchObject({
			op: 'mean',
			input: 'exhaustion_scored_answers',
			missing_data: { strategy: 'require_all' }
		});
		expect(document.nodes.find((node) => node.id === 'recovery_score')).toMatchObject({
			op: 'sum',
			input: 'recovery_answers',
			missing_data: { strategy: 'min_valid_count', min_valid_count: 1 }
		});
		expect(document.outputs).toEqual([
			{ code: 'exhaustion', node: 'exhaustion_score' },
			{ code: 'recovery', node: 'recovery_score' }
		]);
		expect(JSON.parse(buildScoreProduces(outputs))).toEqual({ scores: ['exhaustion', 'recovery'] });
	});
});
