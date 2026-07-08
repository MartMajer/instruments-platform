/**
 * Client-side mirror of the backend AccentContrastGuard, used only to power the
 * Settings live preview as the picker moves. The backend stays the source of
 * truth for what respondents actually receive — this produces the same
 * deterministic result so the preview matches the served accent.
 *
 * A tenant accent lands as a button background carrying white label text and as
 * accent-colored marks on white; both reduce to "enough WCAG contrast against
 * white". A too-light accent is darkened toward black (hue-preserving) until it
 * meets AA 4.5:1, rather than rejected.
 */

const MINIMUM_CONTRAST_RATIO = 4.5;
const DARKEN_STEP = 0.96;
const NUDGE_STEP = 0.04;
const MAX_ITERATIONS = 160;

type Rgb = [number, number, number];

const WHITE: Rgb = [255, 255, 255];
const BLACK: Rgb = [0, 0, 0];
const DARK_INK: Rgb = [20, 28, 37]; // #141c25

export function isHexColor(value: string | null | undefined): value is string {
	return typeof value === 'string' && /^#[0-9a-fA-F]{6}$/.test(value.trim());
}

export function contrastWithWhite(hex: string): number {
	const rgb = parseHex(hex);
	if (!rgb) return 1;
	return 1.05 / (relativeLuminance(rgb) + 0.05);
}

export function isLegibleOnWhite(hex: string): boolean {
	return contrastWithWhite(hex) >= MINIMUM_CONTRAST_RATIO;
}

export function ensureLegibleOnWhite(hex: string): string {
	const rgb = parseHex(hex);
	if (!rgb) return hex;

	if (relativeLuminanceContrast(rgb) >= MINIMUM_CONTRAST_RATIO) {
		return toHex(rgb);
	}

	let current = rgb;
	for (let i = 0; i < MAX_ITERATIONS; i++) {
		current = [
			Math.round(current[0] * DARKEN_STEP),
			Math.round(current[1] * DARKEN_STEP),
			Math.round(current[2] * DARKEN_STEP)
		];
		if (relativeLuminanceContrast(current) >= MINIMUM_CONTRAST_RATIO) {
			return toHex(current);
		}
		if (current[0] === 0 && current[1] === 0 && current[2] === 0) {
			break;
		}
	}

	return '#000000';
}

/** Contrast ratio between two hex colors (1–21). Mirrors the backend engine. */
export function contrastRatio(aHex: string, bHex: string): number {
	const a = parseHex(aHex);
	const b = parseHex(bHex);
	if (!a || !b) return 1;
	return contrast(a, b);
}

/** Nudge a foreground toward black/white until it meets AA against a background. */
export function ensureLegible(foregroundHex: string, backgroundHex: string): string {
	const fg = parseHex(foregroundHex);
	const bg = parseHex(backgroundHex);
	if (!fg || !bg) return foregroundHex;
	if (contrast(fg, bg) >= MINIMUM_CONTRAST_RATIO) return toHex(fg);

	const target = contrast(BLACK, bg) >= contrast(WHITE, bg) ? BLACK : WHITE;
	let current = fg;
	for (let i = 0; i < MAX_ITERATIONS; i++) {
		current = [
			Math.round(current[0] + (target[0] - current[0]) * NUDGE_STEP),
			Math.round(current[1] + (target[1] - current[1]) * NUDGE_STEP),
			Math.round(current[2] + (target[2] - current[2]) * NUDGE_STEP)
		];
		if (contrast(current, bg) >= MINIMUM_CONTRAST_RATIO) return toHex(current);
		if (current[0] === target[0] && current[1] === target[1] && current[2] === target[2]) break;
	}
	return toHex(target);
}

/** White or the app's dark ink, whichever reads better on the background. */
export function readableTextOn(backgroundHex: string): string {
	const bg = parseHex(backgroundHex);
	if (!bg) return '#ffffff';
	return contrast(WHITE, bg) >= contrast(DARK_INK, bg) ? '#ffffff' : toHex(DARK_INK);
}

function contrast(a: Rgb, b: Rgb): number {
	const la = relativeLuminance(a);
	const lb = relativeLuminance(b);
	return (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
}

function relativeLuminanceContrast(rgb: Rgb): number {
	return 1.05 / (relativeLuminance(rgb) + 0.05);
}

function relativeLuminance([r, g, b]: Rgb): number {
	return 0.2126 * linear(r) + 0.7152 * linear(g) + 0.0722 * linear(b);
}

function linear(channel: number): number {
	const c = channel / 255;
	return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
}

function parseHex(value: string): Rgb | null {
	if (!isHexColor(value)) return null;
	const hex = value.trim();
	return [
		parseInt(hex.slice(1, 3), 16),
		parseInt(hex.slice(3, 5), 16),
		parseInt(hex.slice(5, 7), 16)
	];
}

function toHex([r, g, b]: Rgb): string {
	const clamp = (n: number) => Math.max(0, Math.min(255, n)).toString(16).padStart(2, '0');
	return `#${clamp(r)}${clamp(g)}${clamp(b)}`;
}
