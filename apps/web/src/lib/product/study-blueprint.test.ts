import { describe, expect, it } from 'vitest';
import {
	buildStudyNamePlaceholder,
	defaultStudyBlueprintId,
	getStudyBlueprintOption,
	listStudyBlueprintOptions
} from './study-blueprint';

describe('study blueprint model', () => {
	it('starts from a custom research study by default', () => {
		const options = listStudyBlueprintOptions();

		expect(defaultStudyBlueprintId).toBe('custom_research_study');
		expect(options[0]).toMatchObject({
			id: 'custom_research_study',
			title: 'Custom research study'
		});
	});

	it('keeps blueprint copy generic and free from named-instrument claims', () => {
		const serialized = JSON.stringify(listStudyBlueprintOptions());

		expect(serialized).not.toMatch(/\b(OLBI|COPSOQ|MBI|PHQ-9|UWES)\b/i);
		expect(serialized).not.toMatch(/\b(norm|benchmark|official threshold|validated scale included)\b/i);
	});

	it('offers an OSH ergonomics study blueprint without canonical claims', () => {
		const option = getStudyBlueprintOption('osh_ergonomics_study');

		expect(option).toMatchObject({
			id: 'osh_ergonomics_study',
			title: 'OSH / ergonomics study',
			namePlaceholder: 'e.g. Warehouse strain and recovery pulse'
		});
		expect(option.highlights).toContain('Mixed answer formats');
		expect(option.nextSteps).toContainEqual(
			expect.objectContaining({ description: 'Map task exposure and recipient groups.' })
		);
		expect(buildStudyNamePlaceholder('osh_ergonomics_study')).toBe(
			'e.g. Warehouse strain and recovery pulse'
		);
	});

	it('returns the default option for unknown ids', () => {
		expect(getStudyBlueprintOption('missing')).toEqual(
			getStudyBlueprintOption(defaultStudyBlueprintId)
		);
	});

	it('generates a researcher-facing name placeholder from the selected blueprint', () => {
		expect(buildStudyNamePlaceholder('custom_research_study')).toBe(
			'e.g. Workload and recovery study'
		);
		expect(buildStudyNamePlaceholder('team_pulse')).toBe('e.g. Q3 team pulse');
		expect(buildStudyNamePlaceholder('repeated_wave')).toBe('e.g. Follow-up wellbeing study');
		expect(buildStudyNamePlaceholder('osh_ergonomics_study')).toBe(
			'e.g. Warehouse strain and recovery pulse'
		);
	});
});
