const dateFormat = new Intl.DateTimeFormat('en-GB', {
	day: 'numeric',
	month: 'short',
	year: 'numeric'
});

const dateTimeFormat = new Intl.DateTimeFormat('en-GB', {
	day: 'numeric',
	month: 'short',
	hour: '2-digit',
	minute: '2-digit'
});

export function formatDate(iso: string | null | undefined): string {
	if (!iso) {
		return '—';
	}

	const parsed = new Date(iso);
	return Number.isNaN(parsed.getTime()) ? '—' : dateFormat.format(parsed);
}

export function formatDateTime(iso: string | null | undefined): string {
	if (!iso) {
		return '—';
	}

	const parsed = new Date(iso);
	return Number.isNaN(parsed.getTime()) ? '—' : dateTimeFormat.format(parsed);
}

export function formatCount(value: number | null | undefined): string {
	return value == null ? '—' : new Intl.NumberFormat('en-GB').format(value);
}

/** Sentence-case a backend status token like `awaiting_launch` or `AwaitingLaunch`. */
export function humanizeToken(token: string | null | undefined): string {
	if (!token) {
		return '—';
	}

	const spaced = token
		.replace(/[_-]+/g, ' ')
		.replace(/([a-z])([A-Z])/g, '$1 $2')
		.toLowerCase()
		.trim();

	return spaced.length === 0 ? '—' : spaced[0].toUpperCase() + spaced.slice(1);
}
