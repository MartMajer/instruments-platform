import { describe, expect, test } from 'vitest';
import {
	createScoreOutputRowsForQuestionnairePalette,
	createTemplateQuestionRowsForQuestionnairePalette,
	listQuestionnairePaletteOptions,
	validateScoreOutputRows,
	type QuestionnairePaletteId
} from './template-authoring';

describe('questionnaire palette', () => {
	test('offers several original editable questionnaire palettes without named-instrument claims', () => {
		const options = listQuestionnairePaletteOptions();
		const ids = options.map((option) => option.id);
		const visibleText = options
			.map((option) => `${option.label} ${option.summary} ${option.detail}`)
			.join(' ')
			.toLowerCase();

		expect(ids).toEqual([
			'blank',
			'workload_recovery',
			'osh_ergonomics',
			'office_ergonomics',
			'academic_workload',
			'team_climate',
			'healthcare_staff_strain'
		]);
		expect(visibleText).toContain('original editable');
		expect(visibleText).not.toContain('olbi');
		expect(visibleText).not.toContain('mbi');
		expect(visibleText).not.toContain('validated instrument');
	});

	test('creates workload and recovery starter with safe score outputs', () => {
		const rows = createTemplateQuestionRowsForQuestionnairePalette('workload_recovery');
		const outputs = createScoreOutputRowsForQuestionnairePalette('workload_recovery', rows);
		const rowCodes = new Set(rows.map((row) => row.code));
		const itemText = rows.map((row) => row.textDefault).join(' ').toLowerCase();

		expect(rows).toHaveLength(9);
		expect(outputs.map((output) => output.code)).toEqual([
			'workload_strain',
			'recovery_capacity',
			'work_climate_context'
		]);
		expect(itemText).toContain('mentally drained');
		expect(itemText).not.toContain('olbi');
		expect(itemText).not.toContain('validated');
		expect(outputs.every((output) => output.includedQuestionCodes.every((code) => rowCodes.has(code)))).toBe(
			true
		);
		expect(validateScoreOutputRows(outputs, rows)).toEqual([]);
	});

	test.each<QuestionnairePaletteId>([
		'workload_recovery',
		'osh_ergonomics',
		'office_ergonomics',
		'academic_workload',
		'team_climate',
		'healthcare_staff_strain'
	])('keeps %s suggested outputs compatible with current scoring rules', (paletteId) => {
		const rows = createTemplateQuestionRowsForQuestionnairePalette(paletteId);
		const outputs = createScoreOutputRowsForQuestionnairePalette(paletteId, rows);

		expect(rows.length).toBeGreaterThanOrEqual(6);
		expect(outputs.length).toBeGreaterThanOrEqual(1);
		expect(validateScoreOutputRows(outputs, rows)).toEqual([]);
	});
});
