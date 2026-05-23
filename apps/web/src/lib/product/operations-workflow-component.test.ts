import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

describe('selected-series operations workflow component', () => {
	it('does not render localized operations copy expressions as literal strings', () => {
		const source = readFileSync('src/lib/product/SelectedSeriesOperationsWorkflow.svelte', 'utf8');

		expect(source).not.toMatch(/['"]\{operationsBodyCopy\./);
	});

	it('does not expose the generic neutral badge on respondent access setup', () => {
		const source = readFileSync('src/lib/product/SelectedSeriesOperationsWorkflow.svelte', 'utf8');

		expect(source).toContain('operationsBodyCopy.shareAccess.openLinkNotCreated');
		expect(source).not.toContain('? operationsBodyCopy.shareAccess.openLinkActive\n\t\t\t\t\t\t\t\t\t\t\t: undefined');
	});
});
