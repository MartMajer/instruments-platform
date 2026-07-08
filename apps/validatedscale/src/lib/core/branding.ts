import { isHexColor } from './contrast';

/**
 * A fully-resolved, contrast-guarded tenant palette from the backend. Every value
 * is a concrete #rrggbb; the frontend only maps these onto the platform's own CSS
 * custom properties — no tenant-authored CSS ever reaches a style rule.
 */
export type AppBrandingTheme = {
	accent: string;
	onAccent: string;
	accentOnTopbar: string;
	topbar: string;
	topbarInk: string;
	background: string;
	surface: string;
	ink: string;
};

/**
 * Builds the inline CSS-custom-property string that themes a surface. Every token
 * value is hard-gated to a validated hex first; if any is malformed the whole
 * theme is dropped (default identity) rather than emitting a partial or unsafe
 * rule. Soft variants (ink-2/3, washes, lines) are derived here via color-mix so
 * the backend only needs to guard the anchors.
 */
export function themeStyle(theme: AppBrandingTheme | null | undefined): string {
	if (!theme) return '';

	const values = [
		theme.accent,
		theme.onAccent,
		theme.accentOnTopbar,
		theme.topbar,
		theme.topbarInk,
		theme.background,
		theme.surface,
		theme.ink
	];
	if (!values.every(isHexColor)) return '';

	const { accent, onAccent, accentOnTopbar, topbar, topbarInk, background, surface, ink } = theme;

	return (
		`--tenant-accent:${accent};` +
		`--tenant-on-accent:${onAccent};` +
		`--color-stain:${accent};` +
		`--color-stain-wash:color-mix(in oklab, ${accent} 12%, ${surface});` +
		`--color-stain-deep:color-mix(in oklab, ${accent} 78%, black);` +
		`--color-stain-line:color-mix(in oklab, ${accent} 30%, ${surface});` +
		`--color-stain-bright:${accentOnTopbar};` +
		`--color-ground:${background};` +
		`--color-sunk:color-mix(in oklab, ${background} 92%, ${ink});` +
		`--color-surface:${surface};` +
		`--color-ink:${ink};` +
		`--color-ink-2:color-mix(in oklab, ${ink} 80%, ${surface});` +
		`--color-ink-3:color-mix(in oklab, ${ink} 60%, ${surface});` +
		`--color-line:color-mix(in oklab, ${ink} 16%, ${surface});` +
		`--color-line-2:color-mix(in oklab, ${ink} 34%, ${surface});` +
		`--color-topbar:${topbar};` +
		`--color-topbar-ink:${topbarInk};`
	);
}

/**
 * Applies a themeStyle() string as CSS custom properties on an element (use the
 * document root so the full-bleed `html` background themes too, not just a
 * container). Returns a cleanup that removes exactly the properties it set, so a
 * theme lifts cleanly on navigation. Safe to call with an empty string.
 */
export function applyThemeVars(element: HTMLElement, style: string): () => void {
	const declarations = style
		.split(';')
		.map((part) => part.trim())
		.filter(Boolean)
		.map((part) => {
			const separator = part.indexOf(':');
			return [part.slice(0, separator).trim(), part.slice(separator + 1).trim()] as const;
		});

	for (const [name, value] of declarations) {
		element.style.setProperty(name, value);
	}

	return () => {
		for (const [name] of declarations) {
			element.style.removeProperty(name);
		}
	};
}
