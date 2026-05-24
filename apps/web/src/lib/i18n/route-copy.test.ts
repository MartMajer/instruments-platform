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
		expect(copy.questionnaire.blueprintTitle).toBe('Pregled dizajna upitnika');
		expect(copy.questionnaire.addQuestion).toBe('Dodaj pitanje');
		expect(copy.scoring.resultsTitle).toBe('Izlazi rezultata');
		expect(copy.wave.responseMode.anonymousLongitudinalLabel).toBe(
			'Anonimno s ponovljenim sudjelovanjem'
		);
		expect(copy.recipients.audienceRules.externalEmailsLabel).toBe('Jednokratni uvoz e-pošte');
	});
	it('keeps English study setup language explicit about source, questionnaire, and starting points', () => {
		const copy = routePageCopy('en');

		expect(copy.portfolio.startBlueprint).toBe('Choose a study starting point');
		expect(copy.instruments.description).toBe(
			'Review reusable questionnaire sources that can seed a study. The study itself is built inside Setup.'
		);
		expect(copy.selectedStudy.setupBody.questionnaire.blueprintTitle).toBe(
			'Questionnaire design review'
		);
	});

	it('provides Croatian selected-study collection body copy', () => {
		const copy = routePageCopy('hr-HR').selectedStudy.operationsBody;

		expect(copy.progressTitle).toBe('Tijek prikupljanja');
		expect(copy.statusKicker).toBe('Status prikupljanja');
		expect(copy.emailSetup.title).toBe('Provjera slanja e-pošte prije slanja');
		expect(copy.shareAccess.privateInvitationsTitle).toBe('Privatne pozivnice su aktivne');
		expect(copy.simulation.simulateCollection).toBe('Simuliraj prikupljanje');
		expect(copy.navigation.goToResults).toBe('Idi na rezultate');
	});

	it('provides Croatian directory, team, and settings body copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.directory.csvFile).toBe('CSV datoteka');
		expect(copy.directory.previewCsv).toBe('Pregledaj CSV');
		expect(copy.team.memberEmail).toBe('E-pošta člana');
		expect(copy.team.copyLink).toBe('Kopiraj poveznicu');
		expect(copy.settings.directoryShortcut).toBe('Imenik');
		expect(copy.settings.exportsShortcut).toBe('Izvozi');
	});


	it('provides Croatian respondent runtime copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.respondent.loadingSurvey).toBe('Učitavanje upitnika');
		expect(copy.respondent.saveAndReview).toBe('Spremi i pregledaj');
		expect(copy.unsubscribe.button).toBe('Odjavi ovu adresu e-pošte');
	});

});
