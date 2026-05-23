import { describe, expect, it } from 'vitest';
import { routePageCopy } from './route-copy';

describe('localized route body copy', () => {
	it('provides Croatian public and auth page copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.publicEntry.heroTitle).toContain('istraživačke studije');
		expect(copy.signIn.title).toBe('Prijavite se u svoj radni prostor.');
		expect(copy.register.workspaceName).toBe('Naziv radnog prostora');
	});

	it('provides Croatian workspace route copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.workspaceHome.title).toBe('Početna');
		expect(copy.portfolio.title).toBe('Studije');
		expect(copy.instruments.title).toBe('Instrumenti');
		expect(copy.exports.title).toBe('Preuzimanje datoteka');
		expect(copy.directory.title).toBe('Ljudi i grupe');
		expect(copy.team.title).toBe('Tim');
		expect(copy.settings.title).toBe('Postavke radnog prostora');
	});

	it('provides Croatian selected-study surface copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.selectedStudy.overview.title).toBe('Pregled');
		expect(copy.selectedStudy.surfaces.setup.title).toBe('Postavljanje studije');
		expect(copy.selectedStudy.surfaces.operations.title).toBe('Prikupljanje odgovora');
		expect(copy.selectedStudy.surfaces.reports.title).toBe('Pregled rezultata');
		expect(copy.selectedStudy.surfaces.waves.title).toBe('Valovi');
		expect(copy.selectedStudy.surfaceChrome.collectionDetails.title).toBe('Operativni detalji');
	});

	it('provides Croatian selected-study setup body copy', () => {
		const copy = routePageCopy('hr-HR').selectedStudy.setupBody;

		expect(copy.progressTitle).toBe('Napredak postavljanja studije');
		expect(copy.questionnaire.paletteTitle).toBe('Odaberite uređivi skup pitanja');
		expect(copy.questionnaire.addQuestion).toBe('Dodaj pitanje');
		expect(copy.scoring.resultsTitle).toBe('Izlazi rezultata');
		expect(copy.wave.responseMode.anonymousLongitudinalLabel).toBe(
			'Anonimno s ponovljenim sudjelovanjem'
		);
		expect(copy.recipients.audienceRules.externalEmailsLabel).toBe('Jednokratni uvoz e-pošte');
	});
});
