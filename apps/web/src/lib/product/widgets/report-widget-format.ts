export type ReportWidgetFormatCopy = {
	notAvailable: string;
	yes: string;
	no: string;
	codeLabels: Record<string, string>;
};

const defaultReportWidgetFormatCopy: ReportWidgetFormatCopy = {
	notAvailable: 'Not available',
	yes: 'Yes',
	no: 'No',
	codeLabels: {
		proof_only: 'preview'
	}
};

export function formatCodeLabel(
	value: string | null | undefined,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	if (!value) {
		return copy.notAvailable;
	}

	const mapped = copy.codeLabels[value];
	if (mapped) {
		return mapped;
	}

	return value.replaceAll('_', ' ');
}

export function formatNullableDate(
	value: string | null | undefined,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	if (!value) {
		return copy.notAvailable;
	}

	const date = new Date(value);
	if (Number.isNaN(date.getTime())) {
		return value;
	}

	return new Intl.DateTimeFormat('hr-HR', {
		day: '2-digit',
		month: '2-digit',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		hour12: false
	}).format(date);
}

export function formatNullableNumber(
	value: number | null | undefined,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	return value === null || value === undefined ? copy.notAvailable : String(value);
}

export function formatBooleanLabel(
	value: boolean,
	copy: ReportWidgetFormatCopy = defaultReportWidgetFormatCopy
) {
	return value ? copy.yes : copy.no;
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

export function formatProductCopy(value: string | null | undefined) {
	if (!value) {
		return '';
	}

	return value
		.replace(/\bExport artifacts\b/g, 'Export files')
		.replace(/\bexport artifacts\b/g, 'export files')
		.replace(/\bExport artifact\b/g, 'Export file')
		.replace(/\bexport artifact\b/g, 'export file')
		.replace(/\breport proof export\b/g, 'results export')
		.replace(/\bReport proof export\b/g, 'Results export')
		.replace(/\breport proof\b/g, 'report preview')
		.replace(/\bReport proof\b/g, 'Report preview')
		.replace(/\bproof only\b/g, 'preview')
		.replace(/\bProof only\b/g, 'Preview');
}
