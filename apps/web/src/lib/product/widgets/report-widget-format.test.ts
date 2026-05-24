import { describe, expect, it } from 'vitest';
import {
	formatBooleanLabel,
	formatCodeLabel,
	formatNullableDate,
	formatNullableNumber,
	formatWidgetLabel
} from './report-widget-format';

describe('report widget formatters', () => {
	it('localizes app-owned fallback labels and code values', () => {
		expect(formatCodeLabel(null, hrWidgetFormatCopy)).toBe('Nije dostupno');
		expect(formatCodeLabel('proof_only', hrWidgetFormatCopy)).toBe('pregled');
		expect(formatCodeLabel('not_validated_interpretation', hrWidgetFormatCopy)).toBe(
			'tumačenje nije validirano'
		);
		expect(formatNullableDate(null, hrWidgetFormatCopy)).toBe('Nije dostupno');
		expect(formatNullableNumber(undefined, hrWidgetFormatCopy)).toBe('Nije dostupno');
		expect(formatBooleanLabel(true, hrWidgetFormatCopy)).toBe('Da');
		expect(formatBooleanLabel(false, hrWidgetFormatCopy)).toBe('Ne');
		expect(formatWidgetLabel('submitted', hrWidgetFormatCopy)).toBe('Predano');
		expect(formatWidgetLabel('unknown_label', hrWidgetFormatCopy)).toBe('unknown_label');
	});
});

const hrWidgetFormatCopy = {
	notAvailable: 'Nije dostupno',
	yes: 'Da',
	no: 'Ne',
	labels: {
		submitted: 'Predano'
	},
	codeLabels: {
		proof_only: 'pregled',
		not_validated_interpretation: 'tumačenje nije validirano'
	}
};
