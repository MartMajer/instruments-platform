import { describe, expect, it } from 'vitest';
import {
	appendTemplateQuestionRow,
	appendScoreOutputRow,
	applyQuestionScalePreset,
	buildScoreProduces,
	buildScoringDocument,
	buildMeanScoringDocument,
	createDefaultScoreOutputRows,
	createDefaultTemplateQuestionRows,
	createScoreOutputRowsForStudyPreset,
	createTemplateQuestionRowsForStudyPreset,
	describeQuestionResultUsage,
	describeQuestionScaleIntent,
	describeQuestionScoringDirection,
	describeScoreMissingDataStrategy,
	duplicateTemplateQuestionRow,
	listStudyAuthoringPresetOptions,
	moveTemplateQuestionRow,
	questionScalePresetOptions,
	removeTemplateQuestionRow,
	summarizeAuthoringReadiness,
	summarizeCollectedContextQuestions,
	summarizeQuestionAuthoringCards,
	summarizeQuestionnaireBlueprintReview,
	summarizeRespondentQuestionPreview,
	summarizeReverseScoringReview,
	summarizeResultsBlueprintReview,
	summarizeScorePlan,
	summarizeQuestionDimensions,
	toCreateTemplateQuestions,
	validateScoreOutputRows,
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

	it('lists study authoring presets for credible first-study starts', () => {
		expect(listStudyAuthoringPresetOptions()).toEqual(
			expect.arrayContaining([
				expect.objectContaining({
					id: 'blank',
					label: 'Blank study',
					questionCount: 3
				}),
				expect.objectContaining({
					id: 'osh_ergonomics',
					label: 'OSH / ergonomics starter',
					questionCount: 8
				})
			])
		);
	});

	it('creates an OSH ergonomics starter with mixed answer formats and no named-instrument claims', () => {
		const rows = createTemplateQuestionRowsForStudyPreset('osh_ergonomics');

		expect(rows).toHaveLength(8);
		expect(rows.map((row) => row.code)).toEqual([
			'primary_tasks',
			'manual_handling_exposure',
			'awkward_posture_frequency',
			'discomfort_severity',
			'break_recovery',
			'equipment_support',
			'change_priority',
			'worker_context'
		]);
		expect([...new Set(rows.map((row) => row.type))].sort()).toEqual([
			'likert',
			'multi',
			'number',
			'ranking',
			'single',
			'text'
		]);
		expect(rows.find((row) => row.code === 'discomfort_severity')).toMatchObject({
			dimensionLabel: 'Discomfort',
			type: 'number',
			scaleMin: 0,
			scaleMax: 10,
			scaleLowLabel: 'No discomfort',
			scaleHighLabel: 'Worst imaginable discomfort'
		});

		const combinedText = rows.map((row) => `${row.dimensionLabel} ${row.textDefault}`).join(' ');
		expect(combinedText).not.toMatch(/OLBI|COPSOQ|MBI|PHQ-9|UWES|\bnorms?\b|\bbenchmarks?\b/i);
	});

	it('does not invent a default score for blank custom studies', () => {
		const rows = createTemplateQuestionRowsForStudyPreset('blank');

		expect(createScoreOutputRowsForStudyPreset('blank', rows)).toEqual([]);
	});

	it('creates separate OSH ergonomics result outputs instead of a mixed total score', () => {
		const rows = createTemplateQuestionRowsForStudyPreset('osh_ergonomics');
		const outputs = createScoreOutputRowsForStudyPreset('osh_ergonomics', rows);

		expect(outputs.map((output) => ({ name: output.name, code: output.code, includedQuestionCodes: output.includedQuestionCodes }))).toEqual([
			{
				name: 'Posture and repetition strain',
				code: 'posture_repetition_strain',
				includedQuestionCodes: ['awkward_posture_frequency']
			},
			{
				name: 'Discomfort severity',
				code: 'discomfort_severity',
				includedQuestionCodes: ['discomfort_severity']
			},
			{
				name: 'Recovery and control',
				code: 'recovery_control',
				includedQuestionCodes: ['break_recovery']
			}
		]);
		expect(validateScoreOutputRows(outputs, rows)).toEqual([]);
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

	it('duplicates a question after the source row and gives it a new code', () => {
		const rows = createDefaultTemplateQuestionRows();
		rows[1].type = 'single';
		rows[1].choiceOptions = ['First option', 'Second option'];

		const nextRows = duplicateTemplateQuestionRow(rows, 'q02');

		expect(nextRows.map((row) => ({ ordinal: row.ordinal, code: row.code }))).toEqual([
			{ ordinal: 1, code: 'q01' },
			{ ordinal: 2, code: 'q02' },
			{ ordinal: 3, code: 'q04' },
			{ ordinal: 4, code: 'q03' }
		]);
		expect(nextRows[2]).toMatchObject({
			code: 'q04',
			type: 'single',
			textDefault: `${rows[1].textDefault} (copy)`,
			choiceOptions: ['First option', 'Second option']
		});
		expect(nextRows[2]?.choiceOptions).not.toBe(rows[1].choiceOptions);
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
				sectionCode: 'topic_1',
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

	it('describes scoring direction and result usage in researcher-facing language', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = [
			{
				...createDefaultScoreOutputRows(rows)[0],
				name: 'Recovery risk',
				code: 'recovery_risk',
				includedQuestionCodes: ['q01', 'q03']
			}
		];

		expect(describeQuestionScoringDirection(rows[0])).toEqual({
			kind: 'higher_increases_score',
			label: 'Higher answers increase included result scores',
			detail:
				'1 (Strongly disagree) to 5 (Strongly agree) is used as entered in every result output that includes this question.'
		});
		expect(describeQuestionScoringDirection(rows[2])).toEqual({
			kind: 'higher_reversed_before_score',
			label: 'Higher answers are reversed before scoring',
			detail:
				'5 (Strongly agree) is converted toward 1; 1 (Strongly disagree) is converted toward 5. Use this for protective wording when the result score should still point in one direction.'
		});
		expect(
			describeQuestionScoringDirection({
				...rows[0],
				type: 'text'
			})
		).toMatchObject({
			kind: 'not_scored',
			label: 'Collected but not scored'
		});
		expect(describeQuestionResultUsage(rows[0], outputs)).toBe('Used in: Recovery risk.');
		expect(describeQuestionResultUsage(rows[1], outputs)).toBe(
			'Not included in any result output yet.'
		);
	});
});

describe('questionnaire dimension and scale intent authoring', () => {
	it('creates default rows with researcher-facing dimension labels', () => {
		const rows = createDefaultTemplateQuestionRows();

		expect(rows.map((row) => row.dimensionLabel)).toEqual([
			'Topic 1',
			'Topic 2',
			'Topic 3'
		]);
	});

	it('uses dimension labels as template section codes', () => {
		const rows = createDefaultTemplateQuestionRows();
		rows[0].dimensionLabel = 'Psychological demands';
		rows[1].dimensionLabel = 'Psychological demands';
		rows[2].dimensionLabel = 'Recovery capacity';

		const questions = toCreateTemplateQuestions(rows);

		expect(questions.map((question) => question.sectionCode)).toEqual([
			'psychological_demands',
			'psychological_demands',
			'recovery_capacity'
		]);
	});

	it('summarizes question dimensions with question counts', () => {
		const rows = createDefaultTemplateQuestionRows();
		rows[0].dimensionLabel = 'Workload';
		rows[1].dimensionLabel = 'Workload';
		rows[2].dimensionLabel = 'Recovery';

		expect(summarizeQuestionDimensions(rows)).toEqual([
			{ code: 'workload', label: 'Workload', questionCount: 2 },
			{ code: 'recovery', label: 'Recovery', questionCount: 1 }
		]);
	});

	it('explains scale intent in research language', () => {
		const rows = createDefaultTemplateQuestionRows();
		rows[0].scaleLowLabel = 'Never';
		rows[0].scaleHighLabel = 'Always';
		rows[1].type = 'number';
		rows[2].type = 'text';

		expect(describeQuestionScaleIntent(rows[0]).label).toBe('Frequency scale');
		expect(describeQuestionScaleIntent(rows[1]).label).toBe('Number entry');
		expect(describeQuestionScaleIntent(rows[2]).label).toBe('Written response');
	});
});

describe('scoring plan summaries', () => {
	it('summarizes result outputs by dimensions, included questions, reverse scoring, and missing data', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = createDefaultScoreOutputRows(rows);

		expect(summarizeScorePlan(outputs, rows)).toEqual([
			{
				localId: outputs[0].localId,
				code: 'total',
				name: 'Total score',
				includedQuestionCount: 3,
				dimensionLabels: ['Topic 1', 'Topic 2', 'Topic 3'],
				reverseScoredQuestionCount: 1,
				calculationLabel: 'Mean score',
				missingPolicyLabel: 'Requires every selected question'
			}
		]);
	});

	it('summarizes minimum-answered missing-data rules', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = createDefaultScoreOutputRows(rows).map((output) => ({
			...output,
			missingStrategy: 'min_valid_count' as const,
			minValidCount: 2
		}));

		expect(summarizeScorePlan(outputs, rows)[0]?.missingPolicyLabel).toBe(
			'Requires at least 2 selected questions'
		);
	});

	it('summarizes results blueprint review with coverage, missing answers, direction, and boundaries', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = createDefaultScoreOutputRows(rows);

		expect(summarizeResultsBlueprintReview(rows, outputs)).toEqual({
			label: '1 result output, 3 scored questions, 1 reversed',
			items: [
				{
					id: 'outputs',
					label: 'Result outputs',
					status: 'ready',
					detail: '1 result output will be saved: Total score.'
				},
				{
					id: 'coverage',
					label: 'Question coverage',
					status: 'ready',
					detail: '3 of 3 scoreable questions are included in at least one result output.'
				},
				{
					id: 'missing_answers',
					label: 'Missing answers',
					status: 'ready',
					detail: 'All outputs require every selected question.'
				},
				{
					id: 'scale_compatibility',
					label: 'Scale compatibility',
					status: 'ready',
					detail: 'Each result output uses one compatible answer-scale family.'
				},
				{
					id: 'direction',
					label: 'Score direction',
					status: 'attention',
					detail: '1 reverse-scored question affects Total score.'
				},
				{
					id: 'interpretation',
					label: 'Interpretation boundary',
					status: 'ready',
					detail:
						'These are custom study result outputs. They describe calculation, not official norms, benchmarks, or validated thresholds.'
				},
				{
					id: 'export_schema',
					label: 'Export schema',
					status: 'ready',
					detail:
						'CSV/report exports should preserve question codes, answer formats, score outputs, missing-answer rules, and visibility guardrails.'
				}
			]
		});
	});

	it('blocks result outputs that mix incompatible answer-scale families', () => {
		const rows = createTemplateQuestionRowsForStudyPreset('osh_ergonomics');
		const outputs = [
			{
				localId: 'score-mixed',
				name: 'Unsafe total score',
				code: 'unsafe_total_score',
				calculation: 'mean' as const,
				missingStrategy: 'require_all' as const,
				minValidCount: 1,
				includedQuestionCodes: ['awkward_posture_frequency', 'discomfort_severity', 'break_recovery']
			}
		];

		expect(validateScoreOutputRows(outputs, rows)).toContain(
			'Unsafe total score mixes incompatible answer scales: Frequency scale, Discomfort severity, 0-10, and Agreement scale. Create separate result outputs or normalize outside this release.'
		);
		expect(summarizeResultsBlueprintReview(rows, outputs).items).toContainEqual({
			id: 'scale_compatibility',
			label: 'Scale compatibility',
			status: 'attention',
			detail:
				'Unsafe total score mixes incompatible answer scales: Frequency scale, Discomfort severity, 0-10, and Agreement scale. Create separate result outputs or normalize outside this release.'
		});
	});
});

describe('dimension-based result output defaults', () => {
	it('adds a result output for the first authored dimension after the total score', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = createDefaultScoreOutputRows(rows);

		const nextOutputs = appendScoreOutputRow(outputs, rows);

		expect(nextOutputs[1]).toMatchObject({
			name: 'Topic 1',
			code: 'topic_1',
			includedQuestionCodes: ['q01']
		});
	});

	it('walks through uncovered dimensions when adding repeated result outputs', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = appendScoreOutputRow(createDefaultScoreOutputRows(rows), rows);

		const nextOutputs = appendScoreOutputRow(outputs, rows);

		expect(nextOutputs[2]).toMatchObject({
			name: 'Topic 2',
			code: 'topic_2',
			includedQuestionCodes: ['q02']
		});
	});
});

describe('authoring density and review summaries', () => {
	it('summarizes question cards with dimension, scale, requiredness, and score usage', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = createDefaultScoreOutputRows(rows);

		expect(summarizeQuestionAuthoringCards(rows, outputs)[0]).toMatchObject({
			code: 'q01',
			title: 'Write the first question for this study.',
			dimensionLabel: 'Topic 1',
			scaleLabel: 'Agreement scale',
			requiredLabel: 'Required',
			resultUsageLabel: 'Used in: Total score.'
		});
	});

	it('describes missing-data strategy tradeoffs in researcher language', () => {
		const [output] = createDefaultScoreOutputRows(createDefaultTemplateQuestionRows());

		expect(describeScoreMissingDataStrategy(output)).toMatchObject({
			label: 'Strict missing-data rule',
			detail: 'A respondent needs every selected question answered for this result score.'
		});
		expect(describeScoreMissingDataStrategy({ ...output, missingStrategy: 'min_valid_count', minValidCount: 2 })).toMatchObject({
			label: 'Minimum answered rule',
			detail: 'A respondent needs at least 2 selected questions answered for this result score.'
		});
	});

	it('summarizes reverse-scoring review items', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = createDefaultScoreOutputRows(rows);

		expect(summarizeReverseScoringReview(rows, outputs)).toMatchObject({
			reverseScoredQuestionCount: 1,
			reverseScoredQuestionLabels: ['Write the third question for this study.'],
			affectedResultLabels: ['Total score']
		});
	});

	it('summarizes collected context questions separately from scored questions', () => {
		const rows = createDefaultTemplateQuestionRows();
		rows[1].type = 'text';
		rows[1].dimensionLabel = 'Open context';

		expect(summarizeCollectedContextQuestions(rows)).toEqual([
			{
				code: 'q02',
				dimensionLabel: 'Open context',
				typeLabel: 'Written response',
				text: 'Write the second question for this study.'
			}
		]);
	});

	it('summarizes respondent preview cards with dimension and answer format', () => {
		const rows = createDefaultTemplateQuestionRows();

		expect(summarizeRespondentQuestionPreview(rows)[0]).toMatchObject({
			ordinal: 1,
			positionLabel: 'Question 1 of 3',
			dimensionLabel: 'Topic 1',
			requiredLabel: 'Required',
			answerFormatLabel: 'Agreement scale',
			answerFormatDetail: '1 to 5: Strongly disagree -> Strongly agree',
			responsePreviewLabel: '1 Strongly disagree ... 5 Strongly agree'
		});
	});

	it('summarizes authoring readiness across questionnaire and scoring plan', () => {
		const rows = createDefaultTemplateQuestionRows();
		const outputs = createDefaultScoreOutputRows(rows);

		expect(summarizeAuthoringReadiness(rows, outputs)).toEqual({
			dimensionCount: 3,
			questionCount: 3,
			scoredQuestionCount: 3,
			contextQuestionCount: 0,
			resultOutputCount: 1,
			reverseScoredQuestionCount: 1,
			label: '3 dimensions, 3 scored questions, 1 result output'
		});
	});

	it('summarizes questionnaire blueprint review for constructs, order, requiredness, and results', () => {
		const rows = createDefaultTemplateQuestionRows();
		rows[0].dimensionLabel = 'Workload';
		rows[1].dimensionLabel = 'Workload';
		rows[2].dimensionLabel = 'Recovery';
		rows[2].required = false;
		const outputs = createDefaultScoreOutputRows(rows);

		expect(summarizeQuestionnaireBlueprintReview(rows, outputs)).toEqual({
			label: '2 constructs, 3 questions, 2 required',
			items: [
				{
					id: 'constructs',
					label: 'Construct plan',
					status: 'ready',
					detail: 'Questions are grouped into Workload and Recovery.'
				},
				{
					id: 'answer_formats',
					label: 'Answer formats',
					status: 'attention',
					detail:
						'Uses Agreement scale only. Add choice, number, ranking, or written context questions when the study needs richer evidence.'
				},
				{
					id: 'respondent_order',
					label: 'Respondent order',
					status: 'ready',
					detail:
						'Respondents answer 3 questions in order, from "Write the first question for this study." to "Write the third question for this study."'
				},
				{
					id: 'requiredness',
					label: 'Required answers',
					status: 'ready',
					detail: '2 required, 1 optional.'
				},
				{
					id: 'results',
					label: 'Results coverage',
					status: 'ready',
					detail: '3 scored questions feed 1 result output.'
				}
			]
		});
	});
});

describe('answer scale presets', () => {
	it('defaults scale-backed questions to the five-point agreement preset', () => {
		const rows = createDefaultTemplateQuestionRows();

		expect(rows.map((row) => row.scalePreset)).toEqual([
			'agreement_5',
			'agreement_5',
			'agreement_5'
		]);
	});

	it('applies frequency and four-point agreement presets', () => {
		const [row] = createDefaultTemplateQuestionRows();

		expect(applyQuestionScalePreset(row, 'frequency_5')).toMatchObject({
			scalePreset: 'frequency_5',
			scaleMin: 1,
			scaleMax: 5,
			scaleLowLabel: 'Never',
			scaleHighLabel: 'Always'
		});
		expect(applyQuestionScalePreset(row, 'agreement_4')).toMatchObject({
			scalePreset: 'agreement_4',
			scaleMin: 1,
			scaleMax: 4,
			scaleLowLabel: 'Strongly disagree',
			scaleHighLabel: 'Strongly agree'
		});
		expect(applyQuestionScalePreset(row, 'discomfort_0_10')).toMatchObject({
			scalePreset: 'discomfort_0_10',
			scaleMin: 0,
			scaleMax: 10,
			scaleLowLabel: 'No discomfort',
			scaleHighLabel: 'Worst imaginable discomfort'
		});
	});

	it('keeps current scale values for the custom preset', () => {
		const [row] = createDefaultTemplateQuestionRows();
		const customRow = { ...row, scaleMin: 0, scaleMax: 10, scaleLowLabel: 'Low', scaleHighLabel: 'High' };

		expect(applyQuestionScalePreset(customRow, 'custom')).toMatchObject({
			scalePreset: 'custom',
			scaleMin: 0,
			scaleMax: 10,
			scaleLowLabel: 'Low',
			scaleHighLabel: 'High'
		});
	});

	it('offers the supported scale presets in researcher language', () => {
		expect(questionScalePresetOptions.map((option) => option.value)).toEqual([
			'agreement_5',
			'agreement_4',
			'frequency_5',
			'intensity_5',
			'discomfort_0_10',
			'custom'
		]);
	});
});
