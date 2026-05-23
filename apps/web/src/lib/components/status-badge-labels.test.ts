import { describe, expect, test } from 'vitest';
import { productStatusLabel } from './status-badge-labels';

describe('productStatusLabel', () => {
	test('localizes generic fallback status labels', () => {
		expect(productStatusLabel('neutral', 'en')).toBe('Neutral');
		expect(productStatusLabel('neutral', 'hr-HR')).toBe('Neutralno');
		expect(productStatusLabel('ready', 'hr-HR')).toBe('Spremno');
		expect(productStatusLabel('blocked', 'hr-HR')).toBe('Blokirano');
		expect(productStatusLabel('pending', 'hr-HR')).toBe('Na čekanju');
		expect(productStatusLabel('not_available', 'hr-HR')).toBe('Nije dostupno');
	});
});
