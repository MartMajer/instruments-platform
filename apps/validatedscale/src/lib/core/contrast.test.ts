import { describe, expect, it } from 'vitest';
import {
	contrastRatio,
	contrastWithWhite,
	ensureLegible,
	ensureLegibleOnWhite,
	isLegibleOnWhite,
	readableTextOn
} from './contrast';

describe('contrast guard (client mirror of the backend)', () => {
	it('leaves already-legible accents unchanged', () => {
		for (const accent of ['#000000', '#4530a6', '#2563eb', '#1f4fd1']) {
			expect(isLegibleOnWhite(accent)).toBe(true);
			expect(ensureLegibleOnWhite(accent)).toBe(accent);
		}
	});

	it('darkens illegible accents until they pass AA against white', () => {
		for (const accent of ['#ffff00', '#ffffff', '#ffe100', '#00ffff']) {
			expect(isLegibleOnWhite(accent)).toBe(false);
			const corrected = ensureLegibleOnWhite(accent);
			expect(corrected).not.toBe(accent);
			expect(isLegibleOnWhite(corrected)).toBe(true);
			expect(corrected).toMatch(/^#[0-9a-f]{6}$/);
		}
	});

	it('preserves the dominant hue when darkening yellow', () => {
		const corrected = ensureLegibleOnWhite('#ffee00');
		const r = parseInt(corrected.slice(1, 3), 16);
		const g = parseInt(corrected.slice(3, 5), 16);
		const b = parseInt(corrected.slice(5, 7), 16);
		expect(r).toBeGreaterThan(b);
		expect(g).toBeGreaterThan(b);
	});

	it('matches known WCAG contrast anchors', () => {
		expect(contrastWithWhite('#000000')).toBeCloseTo(21, 0);
		expect(contrastWithWhite('#ffffff')).toBeCloseTo(1, 1);
	});

	it('ensureLegible raises contrast against any background', () => {
		for (const [fg, bg] of [
			['#333333', '#111111'],
			['#eeeeee', '#ffffff'],
			['#1f4fd1', '#151c25']
		]) {
			expect(contrastRatio(ensureLegible(fg, bg), bg)).toBeGreaterThanOrEqual(4.5);
		}
	});

	it('readableTextOn picks the higher-contrast option', () => {
		expect(readableTextOn('#111111')).toBe('#ffffff');
		expect(readableTextOn('#ffffff')).toBe('#141c25');
	});
});
