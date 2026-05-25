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

	it('keeps message ids stable and non-empty', () => {
		const invalidIds = appMessageIds().filter(
			(id: AppMessageId) => !/^[a-z][A-Za-z0-9]*(\.[a-z][A-Za-z0-9]*)+$/u.test(id)
		);

		expect(invalidIds).toEqual([]);
	});
});
