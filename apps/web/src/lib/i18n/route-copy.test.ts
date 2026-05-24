import { describe, expect, it } from 'vitest';
import { routePageCopy } from './route-copy';

describe('localized route body copy', () => {
	it('keeps the public entry copy buyer-facing instead of implementation-facing', () => {
		const copy = routePageCopy('en');
		const publicEntryText = Object.values(copy.publicEntry)
			.filter((value) => typeof value === 'string')
			.join(' ');

		expect(copy.publicEntry.heroTitle).toBe(
			'Run studies, response collection, and results without rebuilding the data stack.'
		);
		expect(copy.publicEntry.languageSwitchAria).toBe('Language');
		expect(copy.publicEntry.workflowRibbon).toBe(
			'Questionnaires, collection, results, waves, and exports'
		);
		expect(copy.publicEntry.productStageRibbon).toBe(
			'Download response datasets, data descriptions, and report packets for analysis.'
		);
		expect(publicEntryText).not.toMatch(/private beta|tenant|authenticated|provider|owner-controlled/i);
	});

	it('keeps public sign-in and registration copy out of provider and beta jargon', () => {
		const copy = routePageCopy('en');
		const authText = [copy.common.privateBeta, ...Object.values(copy.signIn), ...Object.values(copy.register)]
			.filter((value) => typeof value === 'string')
			.join(' ');

		expect(copy.common.privateBeta).toBe('Access note');
		expect(copy.register.betaAccessCode).toBe('Access code');
		expect(authText).not.toMatch(/private beta|sign-in provider|provider handles|owner-controlled/i);
	});

	it('provides Croatian public and auth page copy', () => {
		const copy = routePageCopy('hr-HR');
		const publicEntryText = Object.values(copy.publicEntry)
			.filter((value) => typeof value === 'string')
			.join(' ');

		expect(copy.publicEntry.metaDescription).toBe(
			'Platforma za upitnike, prikupljanje odgovora, rezultate, ponovljena mjerenja i izvoz podataka s opisom podataka.'
		);
		expect(copy.publicEntry.heroKicker).toBe('Platforma za istraživanja i wellbeing programe');
		expect(copy.publicEntry.heroTitle).toBe(
			'Vodite studije, prikupljanje odgovora i rezultate bez improviziranih tablica.'
		);
		expect(copy.publicEntry.languageSwitchAria).toBe('Jezik');
		expect(copy.publicEntry.productStage).toBe('Čist izvoz');
		expect(copy.publicEntry.productStageRibbon).toBe(
			'Preuzmite podatke odgovora, opis podataka i sažetke izvještaja za analizu.'
		);
		expect(copy.publicEntry.workflowRibbon).toBe(
			'Upitnik, prikupljanje, rezultati, mjerenja i izvoz'
		);
		expect(copy.publicEntry.workspaceOverview).toBe('Pregled studije');
		expect(copy.publicEntry.showcaseStudies).toBe('Studije');
		expect(publicEntryText).not.toMatch(/hostan|hosting|valov|šifrarnik/i);
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
		expect(copy.selectedStudy.surfaces.waves.title).toBe('Mjerenja');
		expect(copy.selectedStudy.surfaceChrome.collectionDetails.title).toBe('Operativni detalji');
	});

	it('provides Croatian selected-study setup body copy', () => {
		const copy = routePageCopy('hr-HR').selectedStudy.setupBody;

		expect(copy.progressTitle).toBe('Napredak postavljanja studije');
		expect(copy.questionnaire.paletteTitle).toBe('Odaberite uređivi skup pitanja');
		expect(copy.questionnaire.blueprintTitle).toBe('Provjera upitnika');
		expect(copy.questionnaire.addQuestion).toBe('Dodaj pitanje');
		expect(copy.scoring.resultsTitle).toBe('Izlazi rezultata');
		expect(copy.wave.responseMode.anonymousLongitudinalLabel).toBe(
			'Anonimno s ponovljenim sudjelovanjem'
		);
		expect(copy.recipients.audienceRules.externalEmailsLabel).toBe('Jednokratni uvoz e-pošte');
	});
	it('keeps English study setup language explicit about source, questionnaire, and starting points', () => {
		const copy = routePageCopy('en');

		expect(copy.portfolio.startBlueprint).toBe('Choose how to start the study');
		expect(copy.portfolio.studyModelTitle).toBe('Study, source, and questionnaire');
		expect(copy.portfolio.studyModelStudyBody).toContain('study container');
		expect(copy.portfolio.studyModelStartingPointBody).toContain('Provides source material');
		expect(copy.instruments.description).toBe(
			'Review reusable questionnaire sources that can seed a study. The study itself is built inside Setup.'
		);
		expect(copy.selectedStudy.setupBody.questionnaire.blueprintTitle).toBe(
			'Questionnaire check'
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
