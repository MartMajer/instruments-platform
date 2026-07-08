import { describe, expect, it } from 'vitest';
import { themeStyle, type AppBrandingTheme } from './branding';

const theme: AppBrandingTheme = {
	accent: '#12b3a6',
	onAccent: '#141c25',
	accentOnTopbar: '#5fd8cc',
	topbar: '#0b1e3f',
	topbarInk: '#ffffff',
	background: '#0d1117',
	surface: '#111827',
	ink: '#e6edf3'
};

describe('themeStyle', () => {
	it('maps the resolved palette onto the platform CSS custom properties', () => {
		const style = themeStyle(theme);
		expect(style).toContain('--color-ground:#0d1117;');
		expect(style).toContain('--color-surface:#111827;');
		expect(style).toContain('--color-ink:#e6edf3;');
		expect(style).toContain('--color-topbar:#0b1e3f;');
		expect(style).toContain('--color-topbar-ink:#ffffff;');
		expect(style).toContain('--tenant-accent:#12b3a6;');
		expect(style).toContain('--tenant-on-accent:#141c25;');
		expect(style).toContain('--color-stain-bright:#5fd8cc;');
	});

	it('returns empty for a missing theme', () => {
		expect(themeStyle(null)).toBe('');
		expect(themeStyle(undefined)).toBe('');
	});

	it('drops the whole theme if any value is not a validated hex (no injection)', () => {
		const poisoned = { ...theme, ink: 'red; } body { display:none' } as AppBrandingTheme;
		expect(themeStyle(poisoned)).toBe('');
	});
});
