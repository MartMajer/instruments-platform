import { localeState } from './locale.svelte';

const dateFormats = {
	en: new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }),
	hr: new Intl.DateTimeFormat('hr-HR', { day: 'numeric', month: 'short', year: 'numeric' })
};

const dateTimeFormats = {
	en: new Intl.DateTimeFormat('en-GB', {
		day: 'numeric',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	}),
	hr: new Intl.DateTimeFormat('hr-HR', {
		day: 'numeric',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	})
};

const numberFormats = {
	en: new Intl.NumberFormat('en-GB'),
	hr: new Intl.NumberFormat('hr-HR')
};

export function formatDate(iso: string | null | undefined): string {
	if (!iso) {
		return '—';
	}

	const parsed = new Date(iso);
	return Number.isNaN(parsed.getTime()) ? '—' : dateFormats[localeState.current].format(parsed);
}

export function formatDateTime(iso: string | null | undefined): string {
	if (!iso) {
		return '—';
	}

	const parsed = new Date(iso);
	return Number.isNaN(parsed.getTime()) ? '—' : dateTimeFormats[localeState.current].format(parsed);
}

export function formatCount(value: number | null | undefined): string {
	return value == null ? '—' : numberFormats[localeState.current].format(value);
}

/** Sentence-case a backend status token like `awaiting_launch` or `AwaitingLaunch`. */
export function humanizeToken(token: string | null | undefined): string {
	if (!token) {
		return '—';
	}

	const spaced = token
		.replace(/[._-]+/g, ' ')
		.replace(/([a-z])([A-Z])/g, '$1 $2')
		.toLowerCase()
		.trim();

	return spaced.length === 0 ? '—' : spaced[0].toUpperCase() + spaced.slice(1);
}
