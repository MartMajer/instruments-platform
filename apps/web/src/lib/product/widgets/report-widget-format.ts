export function formatCodeLabel(value: string | null | undefined) {
	return value ? value.replaceAll('_', ' ') : 'Not available';
}

export function formatNullableDate(value: string | null | undefined) {
	return value ?? 'Not available';
}

export function formatNullableNumber(value: number | null | undefined) {
	return value === null || value === undefined ? 'Not available' : String(value);
}

export function formatBooleanLabel(value: boolean) {
	return value ? 'Yes' : 'No';
}

export function formatBytes(value: number) {
	if (value < 1000) {
		return `${value} B`;
	}

	if (value < 1_000_000) {
		return `${(value / 1000).toFixed(1)} KB`;
	}

	return `${(value / 1_000_000).toFixed(1)} MB`;
}
