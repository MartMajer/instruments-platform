import { describe, expect, it } from 'vitest';
import { routePageCopy } from './route-copy';

const mojibakePattern =
	/[\u00c2\u00c3]|\u00c4[\u0080-\u00bf]|\u00c5[\u0080-\u00bf]|\u00e2[\u0080-\u2122]/u;
const replacementQuestionMarkPattern = /\p{L}\?\p{L}/u;
const unresolvedPlaceholderPattern = /\{[a-zA-Z0-9_.]+\}/u;

function collectStrings(value: unknown, path = '$', output: string[] = []) {
	if (typeof value === 'string') {
		output.push(`${path}: ${value}`);
		return output;
	}

	if (Array.isArray(value)) {
		value.forEach((item, index) => collectStrings(item, `${path}[${index}]`, output));
		return output;
	}

	if (value && typeof value === 'object') {
		for (const [key, entryValue] of Object.entries(value)) {
			collectStrings(entryValue, `${path}.${key}`, output);
		}
	}

	return output;
}

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
		expect(copy.publicEntry.productStage).toBe('Analysis handoff');
		expect(copy.publicEntry.productStageRibbon).toBe(
			'Keep datasets, data descriptions, and report context ready for analysis.'
		);
		expect(publicEntryText).not.toMatch(
			/private beta|tenant|authenticated|provider|owner-controlled/i
		);
	});

	it('keeps public sign-in and registration copy out of provider and beta jargon', () => {
		const copy = routePageCopy('en');
		const authText = [
			copy.common.privateBeta,
			...Object.values(copy.signIn),
			...Object.values(copy.register)
		]
			.filter((value) => typeof value === 'string')
			.join(' ');

		expect(copy.common.privateBeta).toBe('Access note');
		expect(copy.register.betaAccessCode).toBe('Access code');
		expect(authText).not.toMatch(
			/private beta|sign-in provider|provider handles|owner-controlled/i
		);
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
		expect(copy.publicEntry.productStage).toBe('Predaja za analizu');
		expect(copy.publicEntry.productStageRibbon).toBe(
			'Podaci, opis podataka i kontekst izvještaja ostaju spremni za analizu.'
		);
		expect(copy.publicEntry.workflowRibbon).toBe(
			'Upitnik, prikupljanje, rezultati, mjerenja i izvoz'
		);
		expect(copy.publicEntry.workspaceOverview).toBe('Pregled studije');
		expect(copy.publicEntry.showcaseStudies).toBe('Studije');
		expect(publicEntryText).not.toMatch(/hostan|hosting|valov|šifrarnik/i);
		expect(publicEntryText).not.toMatch(mojibakePattern);
		expect(copy.signIn.title).toBe('Prijavite se u svoj radni prostor.');
		expect(copy.register.workspaceName).toBe('Naziv radnog prostora');
	});

	it('provides Croatian workspace route copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.workspaceHome.title).toBe('Početna');
		expect(copy.workspaceHome.heroTitle).toBe(
			'Izradite studiju, prikupite odgovore, pregledajte rezultate i pripremite dokaze.'
		);
		expect(copy.workspaceHome.sampleStudies).toBe('Primjeri studija');
		expect(copy.workspaceHome.sampleReadOnlyNote).toContain('samo za čitanje');
		expect(copy.workspaceHome.sampleDemo.title).toBe('Pregledajte završene primjere studija.');
		expect(copy.workspaceHome.sampleDemo.workloadFiles[0]).toContain('opisom podataka');
		expect(copy.portfolio.title).toBe('Studije');
		expect(copy.instruments.title).toBe('Instrumenti');
		expect(copy.exports.title).toBe('Preuzimanje datoteka');
		expect(copy.directory.title).toBe('Ljudi i grupe');
		expect(copy.team.title).toBe('Pristup radnom prostoru');
		expect(copy.settings.title).toBe('Postavke radnog prostora');
		expect(copy.selectedStudy.surfaceChrome.missingStudy).toBe(
			'Odaberite studiju prije otvaranja ove površine.'
		);
		expect(copy.selectedStudy.surfaceChrome.surfaceUnavailableFallback).toBe(
			'Površina odabrane studije nije se mogla učitati.'
		);
	});

	it('does not contain mojibake, replacement question marks, or unresolved placeholders in Croatian route copy', () => {
		const copy = routePageCopy('hr-HR');
		const strings = collectStrings(copy);

		expect(strings.filter((entry) => mojibakePattern.test(entry))).toEqual([]);
		expect(strings.filter((entry) => replacementQuestionMarkPattern.test(entry))).toEqual([]);
		expect(strings.filter((entry) => unresolvedPlaceholderPattern.test(entry))).toEqual([]);
	});

	it('provides Croatian selected-study surface copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.selectedStudy.overview.title).toBe('Pregled');
		expect(copy.selectedStudy.surfaces.setup.title).toBe('Postavljanje studije');
		expect(copy.selectedStudy.surfaces.operations.title).toBe('Prikupljanje odgovora');
		expect(copy.selectedStudy.surfaces.reports.title).toBe('Pregled rezultata');
		expect(copy.selectedStudy.surfaces.waves.title).toBe('Mjerenja');
		expect(copy.selectedStudy.surfaceChrome.collectionDetails.title).toBe('Operativni detalji');
		expect(copy.selectedStudy.reportsWorkflow.surface.flowKicker).toBe('Tijek studije · Rezultati');
		expect(copy.selectedStudy.wavesWorkflow.surface.flowKicker).toBe('Tijek studije · Mjerenja');
		expect(copy.selectedStudy.wavesWorkflow.surface.title).toBe(
			'Ponovite studiju i usporedite mjerenja'
		);
		expect(copy.selectedStudy.wavesWorkflow.surface.scoreMethodLabel).toBe('Metoda rezultata');
		expect(copy.selectedStudy.reportsWorkflow.component.downloadAction).toBe('Radnja preuzimanja');
		expect(copy.selectedStudy.reportsWorkflow.component.currentPurpose.responseDataset).toBe(
			'CSV skupa odgovora i opis podataka'
		);
		expect(copy.selectedStudy.wavesWorkflow.component.whereWavesFit).toBe('Uloga mjerenja');
		expect(copy.selectedStudy.wavesWorkflow.component.currentTaskTitle).toBe(
			'Trenutni zadatak usporedbe'
		);
	});

	it('provides Croatian selected-study setup body copy', () => {
		const copy = routePageCopy('hr-HR').selectedStudy.setupBody;

		expect(copy.progressTitle).toBe('Napredak postavljanja studije');
		expect(copy.questionnaire.paletteTitle).toBe('Odaberite početni predložak upitnika');
		expect(copy.questionnaire.blueprintTitle).toBe('Provjera upitnika');
		expect(copy.questionnaire.addQuestion).toBe('Dodaj pitanje');
		expect(copy.scoring.resultsTitle).toBe('Postavljanje rezultata');
		expect(copy.wave.responseMode.anonymousLongitudinalLabel).toBe(
			'Usporedba iste osobe, anoniman izvještaj'
		);
		expect(copy.recipients.audienceRules.externalEmailsLabel).toBe('Jednokratni uvoz e-pošte');
	});
	it('keeps English study setup language explicit about source, questionnaire, and starting points', () => {
		const copy = routePageCopy('en');

		expect(copy.portfolio.startBlueprint).toBe('Choose a starting point');
		expect(copy.portfolio.studyModelTitle).toBe('How the pieces fit');
		expect(copy.portfolio.studyModelStudyBody).toContain('study container');
		expect(copy.portfolio.studyModelStartingPointBody).toContain('Provides source material');
		expect(copy.instruments.description).toBe(
			'Review reusable questionnaire sources that can seed a study. The study itself is built inside Setup.'
		);
		expect(copy.selectedStudy.setupBody.questionnaire.blueprintTitle).toBe('Questionnaire check');
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
		expect(copy.team.memberEmail).toBe('E-pošta operatera');
		expect(copy.team.copyLink).toBe('Kopiraj poveznicu za prijavu');
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
