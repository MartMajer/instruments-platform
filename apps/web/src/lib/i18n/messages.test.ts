import { describe, expect, it } from 'vitest';
import {
	appMessage,
	appMessageIds,
	formatCount,
	missingAppMessageIds,
	type AppMessageId
} from './messages';
import type { AppLocale } from './localization';

const supportedLocales: AppLocale[] = ['en', 'hr-HR'];

describe('app message catalog', () => {
	it('keeps every supported locale aligned with the English message id set', () => {
		const ids = appMessageIds();

		expect(ids.length).toBeGreaterThan(0);
		for (const locale of supportedLocales) {
			expect(missingAppMessageIds(locale)).toEqual([]);
		}
	});

	it('formats Croatian count nouns with plural-aware forms', () => {
		expect(formatCount('hr-HR', 1, 'row')).toBe('1 redak');
		expect(formatCount('hr-HR', 2, 'row')).toBe('2 retka');
		expect(formatCount('hr-HR', 12, 'row')).toBe('12 redaka');
		expect(formatCount('hr-HR', 2, 'answerVariable')).toBe('2 varijable odgovora');
	});

	it('formats Results workflow dynamic messages through stable ids', () => {
		expect(appMessage('hr-HR', 'results.packet.responses.collected', { count: 12 })).toBe(
			'12 prikupljenih odgovora'
		);
		expect(appMessage('hr-HR', 'results.packet.scores.visible', { count: 2 })).toBe(
			'2 vidljiva rezultata'
		);
		expect(appMessage('hr-HR', 'results.export.rowShape.responseRows', { count: 2 })).toBe(
			'2 retka; jedan redak po predanom odgovoru'
		);
		expect(
			appMessage('hr-HR', 'results.export.waveFields.includedForWaves', { count: 2 })
		).toBe('Polja mjerenja uključena su za 2 mjerenja');
	});

	it('formats Waves workflow dynamic messages through stable ids', () => {
		expect(appMessage('hr-HR', 'waves.review.waveSequence.multiple.summary', { count: 2 })).toBe(
			'Postoje 2 mjerenja'
		);
		expect(
			appMessage('hr-HR', 'waves.review.dataReadiness.linkedReady.summary', {
				linkedPairs: 2,
				visibleScores: 1
			})
		).toBe('2 povezana para, 1 vidljiv usporedni rezultat');
		expect(
			appMessage('hr-HR', 'waves.method.outputs.ready.summary', {
				count: 2,
				outputs: 'workload, recovery'
			})
		).toBe('2 uspoređena izlaza rezultata: workload, recovery');
	});

	it('formats Setup workflow dynamic messages through stable ids', () => {
		expect(
			appMessage('hr-HR', 'setup.launchState.savedSelections', {
				selectionCount: 2,
				pairCount: 5
			})
		).toBe('Spremljeno: 2 odabira, spremno: 5 parova pozivnica.');
		expect(
			appMessage('hr-HR', 'setup.designMap.questionnaireSaved', {
				name: 'Upitnik',
				questionCount: 5
			})
		).toBe('Upitnik spremljen je s 5 pitanja.');
		expect(
			appMessage('hr-HR', 'setup.waveContext.recipientBelongsUntilLaunch', {
				waveName: 'Mjerenje 1'
			})
		).toBe('Odabir primatelja pripada mjerenju Mjerenje 1 dok se to mjerenje ne pokrene.');
	});

	it('keeps message ids stable and non-empty', () => {
		const invalidIds = appMessageIds().filter(
			(id: AppMessageId) => !/^[a-z][A-Za-z0-9]*(\.[a-z][A-Za-z0-9]*)+$/u.test(id)
		);

		expect(invalidIds).toEqual([]);
	});
});
