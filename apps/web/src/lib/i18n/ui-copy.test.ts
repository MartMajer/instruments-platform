import { describe, expect, test } from 'vitest';
import { appShellCopy, respondentReceiptCopy, surfaceNavCopy } from './ui-copy';

describe('localized UI copy', () => {
	test('provides Croatian app shell navigation labels', () => {
		const copy = appShellCopy('hr-HR');

		expect(copy.nav.home).toBe('Početna');
		expect(copy.nav.studies).toBe('Studije');
		expect(copy.nav.directory).toBe('Imenik');
		expect(copy.actions.signOut).toBe('Odjava');
		expect(copy.language.label).toBe('Jezik');
	});

	test('provides Croatian product sidebar labels', () => {
		const copy = surfaceNavCopy('hr-HR');

		expect(copy.sections.studies).toBe('Radni prostor');
		expect(copy.sections.selectedStudy).toBe('Aktivna studija');
		expect(copy.surfaces.exports).toBe('Datoteke');
		expect(copy.surfaces.setup).toBe('Postavljanje');
		expect(copy.descriptions.collect).toBe('Provedi prikupljanje');
		expect(copy.descriptions.selectStudyFirst).toBe('Najprije odaberite ili izradite studiju');
	});

	test('provides Croatian respondent receipt labels and guidance', () => {
		const copy = respondentReceiptCopy('hr-HR');

		expect(copy.title).toBe('Odgovor je poslan');
		expect(copy.metrics.study).toBe('Studija');
		expect(copy.guidance.close).toBe('Možete zatvoriti ovu stranicu.');
	});
});
