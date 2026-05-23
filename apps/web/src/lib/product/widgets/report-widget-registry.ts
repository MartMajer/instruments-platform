import type { Component } from 'svelte';
import type { ReportWidget } from '$lib/api/product';
import type { ReportWidgetFormatCopy } from './report-widget-format';
import ExportArtifactRegistryWidget from './ExportArtifactRegistryWidget.svelte';
import FinalityProvenanceSummaryWidget from './FinalityProvenanceSummaryWidget.svelte';
import ReportReadinessSummaryWidget from './ReportReadinessSummaryWidget.svelte';
import ScoreCoverageSummaryWidget from './ScoreCoverageSummaryWidget.svelte';
import SelectedCampaignReportStateWidget from './SelectedCampaignReportStateWidget.svelte';
import UnsupportedReportWidget from './UnsupportedReportWidget.svelte';
import VisualAnalyticsEntryWidget from './VisualAnalyticsEntryWidget.svelte';

export type ReportWidgetComponent = Component<{
	widget: ReportWidget;
	copy?: ReportWidgetFormatCopy;
}>;

const registry: Record<string, ReportWidgetComponent> = {
	'report-readiness-summary/v1': ReportReadinessSummaryWidget,
	'score-coverage-summary/v1': ScoreCoverageSummaryWidget,
	'selected-campaign-report-state/v1': SelectedCampaignReportStateWidget,
	'export-artifact-registry/v1': ExportArtifactRegistryWidget,
	'visual-analytics-entry/v1': VisualAnalyticsEntryWidget,
	'finality-provenance-summary/v1': FinalityProvenanceSummaryWidget
};

export function getReportWidgetComponent(kind: string): ReportWidgetComponent {
	return registry[kind] ?? UnsupportedReportWidget;
}
