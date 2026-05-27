import { describe, expect, it } from 'vitest';
import { getReportWidgetComponent } from './widgets/report-widget-registry';
import ExportArtifactRegistryWidget from './widgets/ExportArtifactRegistryWidget.svelte';
import FinalityProvenanceSummaryWidget from './widgets/FinalityProvenanceSummaryWidget.svelte';
import ReportReadinessSummaryWidget from './widgets/ReportReadinessSummaryWidget.svelte';
import ResultsDashboardWidget from './widgets/ResultsDashboardWidget.svelte';
import ScoreCoverageSummaryWidget from './widgets/ScoreCoverageSummaryWidget.svelte';
import SelectedCampaignReportStateWidget from './widgets/SelectedCampaignReportStateWidget.svelte';
import UnsupportedReportWidget from './widgets/UnsupportedReportWidget.svelte';
import VisualAnalyticsEntryWidget from './widgets/VisualAnalyticsEntryWidget.svelte';

describe('report widget registry', () => {
	it('resolves known report widget kinds', () => {
		expect(getReportWidgetComponent('results-dashboard/v1')).toBe(ResultsDashboardWidget);
		expect(getReportWidgetComponent('report-readiness-summary/v1')).toBe(
			ReportReadinessSummaryWidget
		);
		expect(getReportWidgetComponent('score-coverage-summary/v1')).toBe(ScoreCoverageSummaryWidget);
		expect(getReportWidgetComponent('selected-campaign-report-state/v1')).toBe(
			SelectedCampaignReportStateWidget
		);
		expect(getReportWidgetComponent('export-artifact-registry/v1')).toBe(
			ExportArtifactRegistryWidget
		);
		expect(getReportWidgetComponent('visual-analytics-entry/v1')).toBe(VisualAnalyticsEntryWidget);
		expect(getReportWidgetComponent('finality-provenance-summary/v1')).toBe(
			FinalityProvenanceSummaryWidget
		);
	});

	it('returns unsupported widget for unknown kinds', () => {
		expect(getReportWidgetComponent('unknown-widget/v1')).toBe(UnsupportedReportWidget);
	});
});
