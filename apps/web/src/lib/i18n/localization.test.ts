import { describe, expect, test } from 'vitest';
import {
	formatAppDateTime,
	formatAppNumber,
	localizedHref,
	normalizeAppLocale,
	preferredAppLocaleFromAcceptLanguage
} from './localization';

describe('app localization foundation', () => {
	test('normalizes supported app locales and falls back to English', () => {
		expect(normalizeAppLocale('hr')).toBe('hr-HR');
		expect(normalizeAppLocale('hr-HR')).toBe('hr-HR');
		expect(normalizeAppLocale('en')).toBe('en');
		expect(normalizeAppLocale('de')).toBe('en');
		expect(normalizeAppLocale(null)).toBe('en');
	});

	test('selects Croatian from browser accept-language when it is preferred', () => {
		expect(preferredAppLocaleFromAcceptLanguage('hr-HR,hr;q=0.9,en;q=0.7')).toBe('hr-HR');
		expect(preferredAppLocaleFromAcceptLanguage('de-DE,de;q=0.9,en;q=0.8')).toBe('en');
	});

	test('formats Croatian date-time with Croatian numeric order and 24-hour time', () => {
		expect(formatAppDateTime('2026-05-23T14:05:00Z', 'hr-HR')).toBe('23.05.2026. 16:05');
		expect(formatAppDateTime(null, 'hr-HR')).toBe('Not available');
	});

	test('formats Croatian numbers with comma decimal separator', () => {
		expect(formatAppNumber(1234.5, 'hr-HR')).toBe('1.234,5');
	});

	test('builds same-page locale links without dropping existing parameters', () => {
		expect(localizedHref('https://example.test/app?tenantId=abc#top', 'hr-HR')).toBe(
			'/app?tenantId=abc&locale=hr-HR#top'
		);
	});
});
