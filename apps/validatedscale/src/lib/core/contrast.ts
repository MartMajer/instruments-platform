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
const MAX_ITERATIONS = 128;

type Rgb = [number, number, number];

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
