export type AppLocale = 'en' | 'hr-HR';

export type AppLocaleOption = {
	id: AppLocale;
	label: string;
	nativeLabel: string;
	htmlLang: string;
};

export const localeCookieName = 'instruments-platform.locale';

export const supportedAppLocales: AppLocaleOption[] = [
	{ id: 'en', label: 'English', nativeLabel: 'English', htmlLang: 'en' },
	{ id: 'hr-HR', label: 'Croatian', nativeLabel: 'Hrvatski', htmlLang: 'hr-HR' }
];

export function normalizeAppLocale(value: unknown): AppLocale {
	if (typeof value !== 'string') {
		return 'en';
	}

	const normalized = value.trim().toLowerCase().replace('_', '-');
	if (normalized === 'hr' || normalized === 'hr-hr') {
		return 'hr-HR';
	}

	if (normalized === 'en' || normalized === 'en-us' || normalized === 'en-gb') {
		return 'en';
	}

	return 'en';
}

export function preferredAppLocaleFromAcceptLanguage(value: string | null | undefined): AppLocale {
	if (!value) {
		return 'en';
	}

	const parsed = value
		.split(',')
		.map((part) => {
			const [localePart, qualityPart] = part.trim().split(';');
			const quality = qualityPart?.startsWith('q=')
				? Number.parseFloat(qualityPart.slice(2))
				: 1;
			return {
				locale: normalizeAppLocale(localePart),
				quality: Number.isFinite(quality) ? quality : 0
			};
		})
		.sort((left, right) => right.quality - left.quality);

	return parsed[0]?.locale ?? 'en';
}

export function appLocaleFromPageData(data: Record<string, unknown> | null | undefined): AppLocale {
	return normalizeAppLocale(data?.appLocale);
}

export function isCroatianLocale(locale: AppLocale): boolean {
	return locale === 'hr-HR';
}

export function htmlLangForLocale(locale: AppLocale): string {
	return supportedAppLocales.find((option) => option.id === locale)?.htmlLang ?? 'en';
}

export function localizedHref(currentHref: string | URL, locale: AppLocale): string {
	const url = currentHref instanceof URL ? new URL(currentHref) : new URL(currentHref);
	url.searchParams.set('locale', locale);
	return `${url.pathname}${url.search}${url.hash}`;
}

export function formatAppDateTime(
	value: string | Date | null | undefined,
	locale: AppLocale,
	options: { fallback?: string; timeZone?: string } = {}
): string {
	if (!value) {
		return options.fallback ?? 'Not available';
	}

	const date = value instanceof Date ? value : new Date(value);
	if (Number.isNaN(date.getTime())) {
		return options.fallback ?? 'Not available';
	}

	if (isCroatianLocale(locale)) {
		return formatCroatianDateTime(date, options.timeZone ?? 'Europe/Zagreb');
	}

	return new Intl.DateTimeFormat('en-US', {
		dateStyle: 'medium',
		timeStyle: 'short',
		timeZone: options.timeZone ?? 'Europe/Zagreb'
	}).format(date);
}

export function formatAppNumber(value: number, locale: AppLocale): string {
	return new Intl.NumberFormat(isCroatianLocale(locale) ? 'hr-HR' : 'en-US').format(value);
}

function formatCroatianDateTime(date: Date, timeZone: string): string {
	const parts = new Intl.DateTimeFormat('hr-HR', {
		timeZone,
		day: '2-digit',
		month: '2-digit',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		hourCycle: 'h23'
	}).formatToParts(date);

	return `${part(parts, 'day')}.${part(parts, 'month')}.${part(parts, 'year')}. ${part(
		parts,
		'hour'
	)}:${part(parts, 'minute')}`;
}

function part(parts: Intl.DateTimeFormatPart[], type: Intl.DateTimeFormatPartTypes): string {
	return parts.find((item) => item.type === type)?.value ?? '';
}
