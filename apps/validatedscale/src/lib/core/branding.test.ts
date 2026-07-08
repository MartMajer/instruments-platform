import { describe, expect, it } from 'vitest';
import { resolveTheme, themeStyle, type AppBrandingTheme } from './branding';
import { contrastRatio } from './contrast';

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

describe('resolveTheme (client mirror of the backend)', () => {
	it('falls back to defaults for unset anchors', () => {
		const resolved = resolveTheme({});
		expect(resolved.background).toBe('#f2f4f8');
		expect(resolved.surface).toBe('#ffffff');
		expect(resolved.topbar).toBe('#151c25');
		expect(resolved.topbarInk).toBe('#ffffff');
	});

	it('keeps every foreground legible on its surface for a dark theme', () => {
		const resolved = resolveTheme({
			accent: '#12b3a6',
			topbar: '#0b1e3f',
			background: '#0d1117',
			surface: '#111827',
			ink: '#e6edf3'
		});
		expect(contrastRatio(resolved.ink, resolved.surface)).toBeGreaterThanOrEqual(4.5);
		expect(contrastRatio(resolved.accent, resolved.surface)).toBeGreaterThanOrEqual(4.5);
		expect(contrastRatio(resolved.topbarInk, resolved.topbar)).toBeGreaterThanOrEqual(4.5);
		expect(contrastRatio(resolved.onAccent, resolved.accent)).toBeGreaterThanOrEqual(4.5);
	});
});
