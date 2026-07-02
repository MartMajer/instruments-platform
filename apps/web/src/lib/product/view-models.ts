import { ApiError } from '../api/client';
import type {
	CampaignSeriesHubResponse,
	CampaignSeriesListItemResponse,
	CampaignSeriesOwnershipMetadata,
	CampaignSeriesOperationsCampaignResponse,
	CampaignSeriesOperationsLaunchSnapshotResponse,
	CampaignSeriesOperationsWorkspaceResponse,
	CampaignSeriesReportsCampaignResponse,
	CampaignSeriesReportsWorkspaceResponse,
	CampaignSeriesScoreCoverageResponse,
	CampaignSeriesSetupCampaignResponse,
	CampaignSeriesSetupPolicyResponse,
	CampaignSeriesStudyBriefResponse,
	CampaignSeriesSetupWorkspaceResponse,
	CampaignSeriesWavesComparisonResponse,
	CampaignSeriesWavesWaveResponse,
	CampaignSeriesWavesWorkspaceResponse,
	CampaignSeriesPortfolioQuery,
	CampaignSeriesListResponse,
	ExportArtifactLibraryResponse,
	TenantSettingsWorkspaceResponse,
	WorkspaceOverviewResponse
} from '../api/product';
import type {
	AuthSessionResponse,
	CampaignReportProofResponse,
	CampaignSeriesWaveComparisonProofResponse,
	ExportArtifactDownloadResponse,
	InstrumentSummaryResponse,
	LaunchReadinessResponse,
	ReportProofExportArtifactResponse,
	ScoreInterpretationResponse
} from '../api/setup';
import type { AppLocale } from '../i18n/localization';
import {
	appMessage,
	formatCount as formatLocalizedCount,
	type AppMessageId,
	type AppMessageValues
} from '../i18n/messages';

export type DisclosureDisplayState = 'visible' | 'suppressed' | 'pending' | 'not_applicable';

export type DisplayRow = {
	label: string;
	value: string;
	mono?: boolean;
};

export type SelectedSeriesSurfaceId = 'setup' | 'operations' | 'reports' | 'waves';

export type ProductReadModelBadgeStatus =
	| 'ready'
	| 'archived'
	| 'blocked'
	| 'demo'
	| 'proof_only'
	| 'draft'
	| 'scheduled'
	| 'live'
	| 'closed'
	| 'cancelled'
	| 'pending'
	| 'failed'
	| 'empty'
	| 'neutral'
	| 'not_available'
	| 'not_configured';

export type CampaignSeriesOwnershipView = {
	label: string;
	badgeStatus: ProductReadModelBadgeStatus;
	isSample: boolean;
	sampleScenario: string | null;
	readOnlyReason: string | null;
	readOnlyMessage: string | null;
};

export type SetupPreparationChecklistItem = {
	id: 'instrument_template' | 'scoring' | 'policies' | 'campaign' | 'launch_readiness';
	label: string;
	status: ProductReadModelBadgeStatus;
	badgeLabel: string;
	summary: string;
	guidance: string;
	detailRows: DisplayRow[];
};

export type OperationsCollectionOverviewItem = {
	id: 'collection_state' | 'respondent_access' | 'response_progress' | 'score_readiness';
	label: string;
	status: ProductReadModelBadgeStatus;
	badgeLabel: string;
	summary: string;
	guidance: string;
	detailRows: DisplayRow[];
};

export type ReportsResultsOverviewItem = {
	id: 'result_availability' | 'coverage_visibility' | 'limitations_finality' | 'export_next_use';
	label: string;
	status: ProductReadModelBadgeStatus;
	badgeLabel: string;
	summary: string;
	guidance: string;
	detailRows: DisplayRow[];
};

export type SelectedSeriesStudyModelItem = {
	id:
		| 'study_brief'
		| 'study_container'
		| 'questionnaire_results'
		| 'collection_waves'
		| 'evidence_outputs'
		| 'current_state'
		| 'next_action';
	label: string;
	status: ProductReadModelBadgeStatus;
	badgeLabel: string;
	summary: string;
	guidance: string;
	detailRows: (DisplayRow & { status?: ProductReadModelBadgeStatus })[];
};

export type SelectedSeriesStudyModel = {
	title: string;
	description: string;
	items: SelectedSeriesStudyModelItem[];
};

export type SelectedSeriesOverviewCommand = {
	title: string;
	summary: string;
	status: ProductReadModelBadgeStatus;
	badgeLabel: string;
	actionLabel: string | null;
	href: string | null;
};

export type SelectedSeriesOverviewAttentionItem = {
	id: string;
	label: string;
	status: ProductReadModelBadgeStatus;
	badgeLabel: string;
	summary: string;
	href: string;
	actionLabel: string;
};

export type ExportArtifactLibraryOverviewItem = {
	id: 'ready_downloads' | 'attention_needed' | 'artifact_purpose' | 'study_context';
	label: string;
	status: ProductReadModelBadgeStatus;
	badgeLabel: string;
	summary: string;
	guidance: string;
	detailRows: DisplayRow[];
};

export function toLaunchReadinessView(readiness: LaunchReadinessResponse) {
	return {
		campaignId: readiness.campaignId,
		state: readiness.ready ? 'ready' : 'blocked',
		statusLabel: readiness.ready ? 'Ready' : 'Blocked',
		rows: readiness.issues.map((issue) => ({
			code: issue.code,
			label: toProductDisplayCopy(labelFromCode(issue.code)),
			severity: issue.severity,
			state: 'blocked',
			message: toProductDisplayCopy(issue.message)
		}))
	};
}

export function toWorkspaceOverviewView(overview: WorkspaceOverviewResponse, locale: AppLocale = 'en') {
	return localizeLegacyApiReadModelChrome({
		tenantId: overview.tenantId,
		lifecycleSteps: workspaceLifecycleSteps(locale),
		sampleStudies: overview.studyCollections.sampleStudies.map((item) =>
			toWorkspaceStudyCard(item, locale)
		),
		ownStudies: overview.studyCollections.ownStudies.map((item) =>
			toWorkspaceStudyCard(item, locale)
		),
		commandItems: overview.commandCenter.items.map((item) => toWorkspaceCommandItem(item, locale)),
		totalRows: toWorkspaceTotalRows(overview.totals, locale),
		recentSeries: overview.recentSeries.map((item) => toCampaignSeriesCard(item, locale))
	}, locale);
}

export function toTenantSettingsView(
	settings: TenantSettingsWorkspaceResponse,
	locale: AppLocale = 'en'
) {
	return localizeLegacyApiReadModelChrome({
		title: settings.profile.name.trim() || appMessage(locale, 'settings.tenant.untitled'),
		status: (settings.profile.status === 'active'
			? 'ready'
			: toProductReadModelBadgeStatus(settings.profile.status)) as ProductReadModelBadgeStatus,
		profileRows: [
			{ label: appMessage(locale, 'settings.profile.slug'), value: settings.profile.slug, mono: true },
			{ label: appMessage(locale, 'settings.profile.region'), value: settings.profile.region.toUpperCase() },
			{ label: appMessage(locale, 'settings.profile.defaultLocale'), value: settings.profile.defaultLocale },
			{
				label: appMessage(locale, 'settings.profile.status'),
				value: sentenceCase(humanizeValue(settings.profile.status))
			},
			{ label: appMessage(locale, 'settings.profile.created'), value: formatDateTime(settings.profile.createdAt) },
			{ label: appMessage(locale, 'settings.profile.updated'), value: formatDateTime(settings.profile.updatedAt) }
		],
		metricRows: [
			{
				label: appMessage(locale, 'settings.metric.studies'),
				value: formatCount(settings.counts.campaignSeriesCount)
			},
			{ label: appMessage(locale, 'settings.metric.campaigns'), value: formatCount(settings.counts.campaignCount) },
			{
				label: appMessage(locale, 'settings.metric.liveCampaigns'),
				value: formatCount(settings.counts.liveCampaignCount)
			},
			{
				label: appMessage(locale, 'settings.metric.submittedResponses'),
				value: formatCount(settings.counts.submittedResponseCount)
			},
			{ label: appMessage(locale, 'settings.metric.subjects'), value: formatCount(settings.counts.subjectCount) },
			{
				label: appMessage(locale, 'settings.metric.subjectGroups'),
				value: formatCount(settings.counts.subjectGroupCount)
			},
			{
				label: appMessage(locale, 'settings.metric.tenantMembers'),
				value: formatCount(settings.counts.tenantMemberCount)
			},
			{
				label: appMessage(locale, 'settings.metric.tenantRoles'),
				value: formatCount(settings.counts.tenantRoleCount)
			},
			{
				label: appMessage(locale, 'settings.metric.exportFiles'),
				value: formatCount(settings.counts.exportArtifactCount)
			}
		],
		reportBranding: {
			title: appMessage(locale, 'settings.reportBranding.title'),
			description: appMessage(locale, 'settings.reportBranding.description'),
			rows: [
				{
					label: appMessage(locale, 'settings.reportBranding.organizationLabel'),
					value:
						settings.reportBranding.organizationLabel.trim() ||
						appMessage(locale, 'settings.tenant.untitled')
				},
				{
					label: appMessage(locale, 'settings.reportBranding.reportTitle'),
					value:
						settings.reportBranding.reportTitle.trim() ||
						appMessage(locale, 'settings.reportBranding.defaultReportTitle')
				},
				{
					label: appMessage(locale, 'settings.reportBranding.source'),
					value: sentenceCase(humanizeValue(settings.reportBranding.brandingSource))
				},
				{
					label: appMessage(locale, 'settings.reportBranding.logoMode'),
					value: sentenceCase(humanizeValue(settings.reportBranding.logoMode))
				},
				{
					label: appMessage(locale, 'settings.reportBranding.accent'),
					value: settings.reportBranding.accentColorHex
				},
				{
					label: appMessage(locale, 'settings.reportBranding.layout'),
					value: sentenceCase(humanizeValue(settings.reportBranding.layoutVariant))
				}
			],
			deferredTitle: appMessage(locale, 'settings.reportBranding.deferredTitle'),
			deferredItems: settings.reportBranding.deferredCustomizations.map((item) =>
				sentenceCase(humanizeValue(item))
			)
		},
		managementLinks: settings.managementLinks.map((link) => ({
			id: link.id,
			label: link.label,
			description: link.description,
			href: link.route
		}))
	}, locale);
}

export function toInstrumentLibraryView(
	instruments: InstrumentSummaryResponse[],
	locale: AppLocale = 'en'
) {
	const launchEligibleCount = instruments.filter(
		(instrument) => instrument.canStartNewCampaign
	).length;
	const launchBlockedCount = instruments.length - launchEligibleCount;

	return localizeLegacyApiReadModelChrome({
		metricRows: [
			{ label: appMessage(locale, 'instruments.metric.sources'), value: formatCount(instruments.length) },
			{ label: appMessage(locale, 'instruments.metric.launchEligible'), value: formatCount(launchEligibleCount) },
			{ label: appMessage(locale, 'instruments.metric.launchBlocked'), value: formatCount(launchBlockedCount) }
		],
		cards: instruments.map((instrument) => {
			const launchEligible = instrument.canStartNewCampaign;

			return {
				id: instrument.id,
				title: instrument.fullName.trim() || instrument.code,
				subtitle: `${instrument.code} ${instrument.version}`,
				status: (launchEligible ? 'ready' : 'blocked') as ProductReadModelBadgeStatus,
				statusLabel: launchEligible
					? appMessage(locale, 'instruments.status.launchEligible')
					: appMessage(locale, 'instruments.status.launchBlocked'),
				rows: [
					{
						label: appMessage(locale, 'instruments.row.rights'),
						value: sentenceCase(humanizeValue(instrument.rightsStatus))
					},
					{
						label: appMessage(locale, 'instruments.row.validity'),
						value: instrument.validityLabel
					}
				]
			};
		})
	}, locale);
}

export function toExportArtifactLibraryView(
	library: ExportArtifactLibraryResponse,
	locale: AppLocale = 'en'
) {
	return localizeLegacyApiReadModelChrome({
		surfaceTitle: appMessage(locale, 'exports.library.surface.title'),
		surfaceEyebrow: appMessage(locale, 'exports.library.surface.eyebrow'),
		surfaceDescription: appMessage(locale, 'exports.library.surface.description'),
		referenceTitle: appMessage(locale, 'exports.library.reference.title'),
		referenceDescription: appMessage(locale, 'exports.library.reference.description'),
		exportOverview: toExportArtifactLibraryOverview(library, locale),
		metricRows: [
			{
				label: appMessage(locale, 'exports.library.row.exportFiles'),
				value: formatCount(library.summary.totalCount)
			},
			{
				label: appMessage(locale, 'exports.library.row.downloadable'),
				value: formatCount(library.summary.downloadableCount)
			},
			{
				label: appMessage(locale, 'exports.library.row.failed'),
				value: formatCount(library.summary.failedCount)
			},
			{
				label: appMessage(locale, 'exports.library.row.pending'),
				value: formatCount(library.summary.pendingCount)
			}
		],
		cards: library.artifacts.map((artifact) => toExportArtifactLibraryCard(artifact, locale))
	}, locale);
}

export function toCampaignSeriesListView(
	list: CampaignSeriesListResponse,
	query: CampaignSeriesPortfolioQuery = {},
	locale: AppLocale = 'en'
) {
	const filtersActive = hasActiveCampaignSeriesFilters(query);
	const items = list.items.map((item) => toCampaignSeriesCard(item, locale));

	return localizeLegacyApiReadModelChrome({
		items,
		studySections: toCampaignSeriesStudySections(items, locale),
		filtersActive,
		statusOptions: campaignSeriesPortfolioStatusOptions(locale),
		sortOptions: campaignSeriesPortfolioSortOptions(locale),
		visibilityOptions: campaignSeriesPortfolioVisibilityOptions(locale),
		emptyState:
			list.items.length === 0
				? filtersActive
					? {
							title: appMessage(locale, 'portfolio.empty.noMatching.title'),
							message: appMessage(locale, 'portfolio.empty.noMatching.message')
						}
					: {
							title: appMessage(locale, 'portfolio.empty.noStudies.title'),
							message: appMessage(locale, 'portfolio.empty.noStudies.message')
						}
				: null
	}, locale);
}

export function toCampaignSeriesHubView(hub: CampaignSeriesHubResponse, locale: AppLocale = 'en') {
	const archiveState = toCampaignSeriesArchiveState(hub);
	const ownership = toCampaignSeriesOwnership(hub, locale);
	const rows: DisplayRow[] = [
		{ label: appMessage(locale, 'overview.row.created'), value: formatDateTime(hub.createdAt) },
		{ label: appMessage(locale, 'overview.row.updated'), value: formatDateTime(hub.updatedAt) }
	];
	if (archiveState.archived) {
		rows.push({
			label: appMessage(locale, 'overview.row.archived'),
			value: formatNullableDateTime(archiveState.archivedAt)
		});
		if (archiveState.reason) {
			rows.push({ label: appMessage(locale, 'overview.row.archiveReason'), value: archiveState.reason });
		}
	}

	return localizeLegacyApiReadModelChrome({
		id: hub.id,
		surfaceTitle: appMessage(locale, 'overview.surface.title'),
		surfaceDescription: appMessage(locale, 'overview.surface.description'),
		referenceTitle: appMessage(locale, 'overview.reference.title'),
		referenceDescription: appMessage(locale, 'overview.reference.description'),
		title: hub.name.trim() || appMessage(locale, 'overview.untitledSeries'),
		subtitle: appMessage(locale, 'overview.subtitle', {
			campaignCount: hub.totals.campaignCount,
			liveCount: hub.totals.liveCampaignCount
		}),
		rows,
		ownership,
		canMutate: !ownership.isSample,
		archiveState,
		overviewCommand: toCampaignSeriesHubOverviewCommand(hub, locale),
		overviewMetrics: toCampaignSeriesHubOverviewMetrics(hub, locale),
		overviewAttentionTitle: appMessage(locale, 'overview.attention.title'),
		overviewAttentionItems: toCampaignSeriesHubAttentionItems(hub, locale),
		totalRows: toCampaignSeriesHubTotalRows(hub.totals),
		governanceRows: [
			toGovernanceRow('overview.governance.consent', hub.governance.consentStatus, locale),
			toGovernanceRow('overview.governance.retention', hub.governance.retentionStatus, locale),
			toGovernanceRow('overview.governance.disclosure', hub.governance.disclosureStatus, locale),
			toGovernanceRow('overview.governance.scoring', hub.governance.scoringStatus, locale)
		],
		studyModel: toCampaignSeriesHubStudyModel(hub, locale),
		lifecycleMap: toCampaignSeriesHubLifecycleMap(hub, locale),
		lifecycleItems: hub.lifecycle.map((item) => ({
			id: item.id,
			label: toProductDisplayCopy(item.label),
			status: toProductReadModelBadgeStatus(item.status),
			guidance: toProductDisplayCopy(item.guidance),
			route: item.route,
			actionLabel: item.actionLabel
		})),
		campaignRows: hub.campaigns.map((campaign) => ({
			id: campaign.id,
			title: campaign.name.trim() || 'Untitled wave',
			status: toProductReadModelBadgeStatus(campaign.status),
			rows: [
				{
					label: appMessage(locale, 'overview.row.identityMode'),
					value: localizedIdentityModeLabel(campaign.responseIdentityMode, locale)
				},
				{ label: appMessage(locale, 'overview.row.locale'), value: campaign.defaultLocale },
				{
					label: appMessage(locale, 'overview.row.submittedResponses'),
					value: formatCount(campaign.submittedResponseCount)
				},
				{ label: appMessage(locale, 'overview.row.scores'), value: formatCount(campaign.scoreCount) },
				{
					label: appMessage(locale, 'overview.row.exportFiles'),
					value: formatCount(campaign.exportArtifactCount)
				}
			]
		}))
	}, locale);
}

function toCampaignSeriesHubOverviewCommand(
	hub: CampaignSeriesHubResponse,
	locale: AppLocale
): SelectedSeriesOverviewCommand {
	if (hub.archived) {
		return {
			title: appMessage(locale, 'overview.command.title.archived'),
			summary: appMessage(locale, 'overview.command.summary.archived'),
			status: 'archived',
			badgeLabel: appMessage(locale, 'overview.command.badge.archived'),
			actionLabel: null,
			href: null
		};
	}

	const setupLifecycle = findHubLifecycleItem(hub, 'setup');
	const setupStatus = toProductReadModelBadgeStatus(setupLifecycle?.status);
	const routeBase = `/app/campaign-series/${hub.id}`;

	if (!setupLifecycleReady(setupStatus) || hub.totals.campaignCount === 0) {
		return {
			title: appMessage(locale, 'overview.command.title.setup'),
			summary: appMessage(locale, 'overview.command.summary.setup'),
			status: setupStatus === 'ready' ? 'pending' : setupStatus,
			badgeLabel: appMessage(locale, 'overview.command.badge.setup'),
			actionLabel: appMessage(locale, 'overview.command.action.setup'),
			href: `${routeBase}/setup`
		};
	}

	if (hub.totals.liveCampaignCount > 0) {
		return {
			title: appMessage(locale, 'overview.command.title.collecting'),
			summary: appMessage(locale, 'overview.command.summary.collecting', {
				liveCount: hub.totals.liveCampaignCount,
				responseCount: hub.totals.submittedResponseCount
			}),
			status: 'live',
			badgeLabel: appMessage(locale, 'overview.command.badge.collecting'),
			actionLabel: appMessage(locale, 'overview.command.action.collect'),
			href: `${routeBase}/operations`
		};
	}

	if (
		hub.totals.submittedResponseCount > 0 ||
		hub.totals.scoreCount > 0 ||
		hub.totals.exportArtifactCount > 0
	) {
		return {
			title: appMessage(locale, 'overview.command.title.results'),
			summary: appMessage(locale, 'overview.command.summary.results', {
				responseCount: hub.totals.submittedResponseCount,
				scoreCount: hub.totals.scoreCount,
				exportCount: hub.totals.exportArtifactCount
			}),
			status: 'ready',
			badgeLabel: appMessage(locale, 'overview.command.badge.results'),
			actionLabel: appMessage(locale, 'overview.command.action.results'),
			href: `${routeBase}/reports`
		};
	}

	return {
		title: appMessage(locale, 'overview.command.title.collect'),
		summary: appMessage(locale, 'overview.command.summary.collect'),
		status: 'pending',
		badgeLabel: appMessage(locale, 'overview.command.badge.collect'),
		actionLabel: appMessage(locale, 'overview.command.action.collect'),
		href: `${routeBase}/operations`
	};
}

function toCampaignSeriesHubOverviewMetrics(
	hub: CampaignSeriesHubResponse,
	locale: AppLocale
): DisplayRow[] {
	return [
		{
			label: appMessage(locale, 'overview.metric.measurements'),
			value: formatLocalizedCount(locale, hub.totals.campaignCount, 'measurement')
		},
		{
			label: appMessage(locale, 'overview.metric.live'),
			value: formatCount(hub.totals.liveCampaignCount)
		},
		{
			label: appMessage(locale, 'overview.metric.responses'),
			value: formatLocalizedCount(locale, hub.totals.submittedResponseCount, 'response')
		},
		{
			label: appMessage(locale, 'overview.metric.scores'),
			value: formatLocalizedCount(locale, hub.totals.scoreCount, 'score')
		},
		{
			label: appMessage(locale, 'overview.metric.exports'),
			value: formatLocalizedCount(locale, hub.totals.exportArtifactCount, 'exportFile')
		}
	];
}

function toCampaignSeriesHubAttentionItems(
	hub: CampaignSeriesHubResponse,
	locale: AppLocale
): SelectedSeriesOverviewAttentionItem[] {
	const setupLifecycle = findHubLifecycleItem(hub, 'setup');
	const setupStatus = toProductReadModelBadgeStatus(setupLifecycle?.status);
	if (!setupLifecycleReady(setupStatus) || hub.totals.campaignCount === 0) {
		return lifecycleAttentionItems(hub, setupLifecycle, 'setup', locale);
	}

	const operationsLifecycle = findHubLifecycleItem(hub, 'operations');
	const operationsStatus = toProductReadModelBadgeStatus(operationsLifecycle?.status);
	if (
		hub.totals.submittedResponseCount === 0 &&
		hub.totals.liveCampaignCount === 0 &&
		lifecycleNeedsAttention(operationsStatus)
	) {
		return lifecycleAttentionItems(hub, operationsLifecycle, 'operations', locale);
	}

	const reportsLifecycle = findHubLifecycleItem(hub, 'reports');
	const reportsStatus = toProductReadModelBadgeStatus(reportsLifecycle?.status);
	if (hub.totals.submittedResponseCount > 0 && lifecycleNeedsAttention(reportsStatus)) {
		return lifecycleAttentionItems(hub, reportsLifecycle, 'reports', locale);
	}

	return [];
}

function lifecycleAttentionItems(
	hub: CampaignSeriesHubResponse,
	item: CampaignSeriesHubResponse['lifecycle'][number] | undefined,
	route: CampaignSeriesHubResponse['lifecycle'][number]['route'],
	locale: AppLocale
): SelectedSeriesOverviewAttentionItem[] {
	const status = toProductReadModelBadgeStatus(item?.status);
	const phase = campaignSeriesHubLifecyclePhase(route, locale);
	return [
		{
			id: route,
			label: phase.label,
			status,
			badgeLabel: toModelBadgeLabel(status, locale),
			summary: toProductDisplayCopy(item?.guidance ?? phase.description),
			href: `/app/campaign-series/${hub.id}/${route}`,
			actionLabel: toProductDisplayCopy(item?.actionLabel ?? phase.label)
		}
	];
}

function setupLifecycleReady(status: ProductReadModelBadgeStatus) {
	return status === 'ready' || status === 'live';
}

function lifecycleNeedsAttention(status: ProductReadModelBadgeStatus) {
	return status === 'blocked' || status === 'not_available' || status === 'not_configured';
}

function toCampaignSeriesHubLifecycleMap(hub: CampaignSeriesHubResponse, locale: AppLocale) {
	return {
		title: appMessage(locale, 'overview.lifecycle.title'),
		description: appMessage(locale, 'overview.lifecycle.description'),
		items: hub.lifecycle.map((item) => {
			const phase = campaignSeriesHubLifecyclePhase(item.id, locale);

			return {
				id: item.id,
				label: phase.label,
				status: toProductReadModelBadgeStatus(item.status),
				description: phase.description,
				guidance: toProductDisplayCopy(item.guidance),
				route: item.route,
				href: `/app/campaign-series/${hub.id}/${item.route}`,
				actionLabel: item.actionLabel
			};
		})
	};
}

function campaignSeriesHubLifecyclePhase(
	id: CampaignSeriesHubResponse['lifecycle'][number]['id'],
	locale: AppLocale
) {
	switch (id) {
		case 'setup':
			return {
				label: appMessage(locale, 'overview.lifecycle.setup.label'),
				description: appMessage(locale, 'overview.lifecycle.setup.description')
			};
		case 'operations':
			return {
				label: appMessage(locale, 'overview.lifecycle.operations.label'),
				description: appMessage(locale, 'overview.lifecycle.operations.description')
			};
		case 'reports':
			return {
				label: appMessage(locale, 'overview.lifecycle.reports.label'),
				description: appMessage(locale, 'overview.lifecycle.reports.description')
			};
		case 'waves':
			return {
				label: appMessage(locale, 'overview.lifecycle.waves.label'),
				description: appMessage(locale, 'overview.lifecycle.waves.description')
			};
	}
}

function toCampaignSeriesHubStudyModel(
	hub: CampaignSeriesHubResponse,
	locale: AppLocale
): SelectedSeriesStudyModel {
	return {
		title: appMessage(locale, 'overview.studyModel.title'),
		description: appMessage(locale, 'overview.studyModel.description'),
		items: [
			toStudyBriefModelItem(hub.studyBrief, locale),
			toStudyCurrentStateModelItem(hub, locale),
			toStudyNextActionModelItem(hub, locale)
		]
	};
}

function toStudyCurrentStateModelItem(
	hub: CampaignSeriesHubResponse,
	locale: AppLocale
): SelectedSeriesStudyModelItem {
	return {
		id: 'current_state',
		label: appMessage(locale, 'overview.studyModel.currentState.label'),
		status: toStudyCurrentStateStatus(hub),
		badgeLabel: toStudyCurrentStateBadgeLabel(hub, locale),
		summary: appMessage(locale, 'overview.studyModel.currentState.summary', {
			waveCount: hub.totals.campaignCount,
			submittedCount: hub.totals.submittedResponseCount,
			exportCount: hub.totals.exportArtifactCount
		}),
		guidance: toStudyCurrentStateGuidance(hub, locale),
		detailRows: [
			{
				label: appMessage(locale, 'overview.row.collectionRounds'),
				value: formatCount(hub.totals.campaignCount)
			},
			{
				label: appMessage(locale, 'overview.row.collectedResponses'),
				value: formatCount(hub.totals.submittedResponseCount)
			},
			{
				label: appMessage(locale, 'overview.row.exportFiles'),
				value: formatCount(hub.totals.exportArtifactCount)
			}
		]
	};
}

function toStudyCurrentStateStatus(hub: CampaignSeriesHubResponse): ProductReadModelBadgeStatus {
	if (hub.totals.liveCampaignCount > 0) {
		return 'live';
	}

	if (hub.totals.submittedResponseCount > 0 || hub.totals.exportArtifactCount > 0) {
		return 'ready';
	}

	if (hub.totals.campaignCount > 0) {
		return 'pending';
	}

	return 'not_available';
}

function toStudyCurrentStateBadgeLabel(hub: CampaignSeriesHubResponse, locale: AppLocale): string {
	if (hub.totals.liveCampaignCount > 0) {
		return appMessage(locale, 'overview.studyModel.currentState.badge.live');
	}

	if (hub.totals.submittedResponseCount > 0 || hub.totals.exportArtifactCount > 0) {
		return appMessage(locale, 'overview.studyModel.currentState.badge.ready');
	}

	if (hub.totals.campaignCount > 0) {
		return appMessage(locale, 'overview.studyModel.currentState.badge.pending');
	}

	return appMessage(locale, 'overview.studyModel.currentState.badge.notAvailable');
}

function toStudyCurrentStateGuidance(hub: CampaignSeriesHubResponse, locale: AppLocale): string {
	if (hub.totals.liveCampaignCount > 0) {
		return appMessage(locale, 'overview.studyModel.currentState.guidance.collecting');
	}

	if (hub.totals.submittedResponseCount > 0 || hub.totals.exportArtifactCount > 0) {
		return appMessage(locale, 'overview.studyModel.currentState.guidance.resultsReady');
	}

	if (hub.totals.campaignCount > 0) {
		return appMessage(locale, 'overview.studyModel.currentState.guidance.prepared');
	}

	return appMessage(locale, 'overview.studyModel.currentState.guidance.setup');
}

function toStudyNextActionModelItem(
	hub: CampaignSeriesHubResponse,
	locale: AppLocale
): SelectedSeriesStudyModelItem {
	const nextAction = toStudyNextAction(hub);
	const status =
		nextAction === 'collect' && hub.totals.liveCampaignCount > 0
			? 'live'
			: nextAction === 'setup'
				? 'pending'
				: 'ready';

	return {
		id: 'next_action',
		label: appMessage(locale, 'overview.studyModel.nextAction.label'),
		status,
		badgeLabel: toStudyNextActionBadgeLabel(nextAction, locale),
		summary: toStudyNextActionSummary(nextAction, locale),
		guidance: toStudyNextActionGuidance(nextAction, locale),
		detailRows: []
	};
}

function toStudyNextAction(hub: CampaignSeriesHubResponse): 'setup' | 'collect' | 'results' {
	if (hub.totals.submittedResponseCount > 0 || hub.totals.scoreCount > 0 || hub.totals.exportArtifactCount > 0) {
		return 'results';
	}

	if (hub.totals.campaignCount > 0 || hub.totals.liveCampaignCount > 0) {
		return 'collect';
	}

	return 'setup';
}

function toStudyNextActionBadgeLabel(nextAction: 'setup' | 'collect' | 'results', locale: AppLocale): string {
	switch (nextAction) {
		case 'setup':
			return appMessage(locale, 'overview.studyModel.nextAction.badge.setup');
		case 'collect':
			return appMessage(locale, 'overview.studyModel.nextAction.badge.collect');
		case 'results':
			return appMessage(locale, 'overview.studyModel.nextAction.badge.results');
	}
}

function toStudyNextActionSummary(nextAction: 'setup' | 'collect' | 'results', locale: AppLocale): string {
	switch (nextAction) {
		case 'setup':
			return appMessage(locale, 'overview.studyModel.nextAction.summary.setup');
		case 'collect':
			return appMessage(locale, 'overview.studyModel.nextAction.summary.collect');
		case 'results':
			return appMessage(locale, 'overview.studyModel.nextAction.summary.results');
	}
}

function toStudyNextActionGuidance(nextAction: 'setup' | 'collect' | 'results', locale: AppLocale): string {
	switch (nextAction) {
		case 'setup':
			return appMessage(locale, 'overview.studyModel.nextAction.guidance.setup');
		case 'collect':
			return appMessage(locale, 'overview.studyModel.nextAction.guidance.collect');
		case 'results':
			return appMessage(locale, 'overview.studyModel.nextAction.guidance.results');
	}
}

function toStudyBriefModelItem(
	brief: CampaignSeriesStudyBriefResponse | null | undefined,
	locale: AppLocale
): SelectedSeriesStudyModelItem {
	const hasBrief = hasStudyBrief(brief);
	const purpose = localizedStudyBriefText(brief?.purpose, locale);
	const rows: SelectedSeriesStudyModelItem['detailRows'] = [
		{
			label: appMessage(locale, 'overview.studyBrief.audience'),
			value: localizedStudyBriefText(brief?.audience, locale) || appMessage(locale, 'overview.studyBrief.notSet')
		},
		{
			label: appMessage(locale, 'overview.studyBrief.design'),
			value: localizedStudyDesignType(brief?.designType, locale)
		},
		{
			label: appMessage(locale, 'overview.studyBrief.intendedUse'),
			value: localizedStudyIntendedUse(brief?.intendedUse, locale)
		}
	];

	return {
		id: 'study_brief',
		label: appMessage(locale, 'overview.studyBrief.label'),
		status: hasBrief ? 'ready' : 'pending',
		badgeLabel: hasBrief
			? appMessage(locale, 'overview.studyBrief.badge.ready')
			: appMessage(locale, 'overview.studyBrief.badge.pending'),
		summary: purpose
			? appMessage(locale, 'overview.studyBrief.summary.ready', { purpose })
			: appMessage(locale, 'overview.studyBrief.summary.missing'),
		guidance: hasBrief
			? appMessage(locale, 'overview.studyBrief.guidance.ready')
			: appMessage(locale, 'overview.studyBrief.guidance.missing'),
		detailRows: rows
	};
}

function hasStudyBrief(brief: CampaignSeriesStudyBriefResponse | null | undefined) {
	return Boolean(
		brief?.purpose?.trim() ||
			brief?.audience?.trim() ||
			brief?.designType?.trim() ||
			brief?.intendedUse?.trim() ||
			brief?.interpretationBoundary?.trim() ||
			brief?.ownerNotes?.trim()
	);
}

function localizedStudyBriefText(value: string | null | undefined, locale: AppLocale) {
	const text = value?.trim();
	if (!text || locale !== 'hr-HR') {
		return text;
	}

	return (
		croatianStudyBriefDefaultText[text] ??
		text
	);
}

const croatianStudyBriefDefaultText: Record<string, string> = {
	'Define a custom study question and the decision this study should support.':
		'Definirajte vlastito istraživačko pitanje i odluku koju rezultati trebaju podržati.',
	'Participants selected by the workspace team for this study.':
		'Sudionici koje tim radnog prostora odabere za ovu studiju.',
	'Use results as custom-study evidence with method notes; do not present them as externally validated norms unless separately reviewed.':
		'Koristite rezultate kao dokaz prilagođene studije s metodološkim bilješkama; nemojte ih prikazivati kao vanjski validirane norme bez zasebnog pregleda.',
	'Run a short check-in to understand current group conditions and decide what needs follow-up.':
		'Provedite kratku provjeru kako biste razumjeli trenutno stanje grupe i odlučili što treba pratiti.',
	'A team, department, class, or cohort invited to one short collection round.':
		'Tim, odjel, razred ili kohorta pozvana u jedno kratko prikupljanje.',
	'Use results for internal review and follow-up planning. Avoid individual-level conclusions.':
		'Koristite rezultate za interni pregled i planiranje praćenja. Izbjegavajte zaključke o pojedincima.',
	'Measure change between an initial collection round and a later collection round.':
		'Mjerite promjenu između početnog i naknadnog prikupljanja.',
	'The same respondent group repeated across collection rounds where possible.':
		'Ista grupa ispitanika kroz ponovljena prikupljanja gdje je to moguće.',
	'Compare change only where the questionnaire, scoring, and same-person comparison setup remain comparable.':
		'Uspoređujte promjenu samo kada upitnik, bodovanje i postavke ponovljenog sudjelovanja ostanu usporedivi.',
	'Assess task exposure, strain, recovery, and practical follow-up needs.':
		'Procijenite radnu izloženost, opterećenje, oporavak i praktične potrebe za praćenje.',
	'Workers or teams selected for the workplace health or ergonomics review.':
		'Radnici ili timovi odabrani za pregled rada i ergonomije.',
	'Use results as practical review input. Keep method limits and follow-up context with any stakeholder summary.':
		'Koristite rezultate kao ulaz za praktični pregled. Uz svaki sažetak za dionike zadržite metodološke granice i kontekst praćenja.'
};

function toStudyBriefContext(brief: CampaignSeriesStudyBriefResponse | null | undefined, locale: AppLocale) {
	const item = toStudyBriefModelItem(brief, locale);

	return {
		title: item.label,
		status: item.status,
		badgeLabel: item.badgeLabel,
		summary: item.summary,
		guidance: item.guidance,
		rows: item.detailRows
	};
}

function localizedStudyDesignType(value: string | null | undefined, locale: AppLocale) {
	switch (value) {
		case 'single_wave':
			return appMessage(locale, 'overview.studyBrief.design.singleWave');
		case 'repeated_group_trend':
			return appMessage(locale, 'overview.studyBrief.design.repeatedGroupTrend');
		case 'repeated_linked_change':
			return appMessage(locale, 'overview.studyBrief.design.repeatedLinkedChange');
		default:
			return value?.trim() ? humanizeValue(value) : appMessage(locale, 'overview.studyBrief.notSet');
	}
}

function localizedStudyIntendedUse(value: string | null | undefined, locale: AppLocale) {
	switch (value) {
		case 'internal_review':
			return appMessage(locale, 'overview.studyBrief.intendedUse.internalReview');
		case 'research_analysis':
			return appMessage(locale, 'overview.studyBrief.intendedUse.researchAnalysis');
		case 'client_report':
			return appMessage(locale, 'overview.studyBrief.intendedUse.clientReport');
		default:
			return value?.trim() ? humanizeValue(value) : appMessage(locale, 'overview.studyBrief.notSet');
	}
}

function findHubLifecycleItem(
	hub: CampaignSeriesHubResponse,
	id: CampaignSeriesHubResponse['lifecycle'][number]['id']
) {
	return hub.lifecycle.find((item) => item.id === id);
}

function toCollectionWavesModelStatus(hub: CampaignSeriesHubResponse): ProductReadModelBadgeStatus {
	if (hub.totals.liveCampaignCount > 0) {
		return 'live';
	}

	if (hub.totals.campaignCount > 0) {
		const operationsLifecycle = findHubLifecycleItem(hub, 'operations');
		return operationsLifecycle ? toProductReadModelBadgeStatus(operationsLifecycle.status) : 'pending';
	}

	return 'not_configured';
}

function toCollectionWavesModelBadgeLabel(hub: CampaignSeriesHubResponse, locale: AppLocale) {
	if (hub.totals.liveCampaignCount > 0) {
		return appMessage(locale, 'overview.studyModel.collectionWaves.badge.live');
	}

	if (hub.totals.campaignCount > 0) {
		return appMessage(locale, 'overview.studyModel.collectionWaves.badge.prepared');
	}

	return appMessage(locale, 'overview.studyModel.collectionWaves.badge.none');
}

function toCollectionWavesModelSummary(hub: CampaignSeriesHubResponse, locale: AppLocale) {
	if (hub.totals.campaignCount === 0) {
		return appMessage(locale, 'overview.studyModel.collectionWaves.summary.none');
	}

	return appMessage(locale, 'overview.studyModel.collectionWaves.summary.existing', {
		campaignCount: hub.totals.campaignCount,
		liveCount: hub.totals.liveCampaignCount
	});
}

function toEvidenceOutputsModelStatus(hub: CampaignSeriesHubResponse): ProductReadModelBadgeStatus {
	if (hub.totals.exportArtifactCount > 0 || hub.totals.scoreCount > 0) {
		return 'ready';
	}

	if (hub.totals.submittedResponseCount > 0) {
		return 'pending';
	}

	return 'not_available';
}

function toEvidenceOutputsModelBadgeLabel(hub: CampaignSeriesHubResponse, locale: AppLocale) {
	if (hub.totals.exportArtifactCount > 0 || hub.totals.scoreCount > 0) {
		return appMessage(locale, 'overview.studyModel.evidenceOutputs.badge.ready');
	}

	if (hub.totals.submittedResponseCount > 0) {
		return appMessage(locale, 'overview.studyModel.evidenceOutputs.badge.needsScoring');
	}

	return appMessage(locale, 'overview.studyModel.evidenceOutputs.badge.none');
}

function toEvidenceOutputsModelSummary(hub: CampaignSeriesHubResponse, locale: AppLocale) {
	return appMessage(locale, 'overview.studyModel.evidenceOutputs.summary', {
		submittedCount: hub.totals.submittedResponseCount,
		scoreCount: hub.totals.scoreCount,
		exportCount: hub.totals.exportArtifactCount
	});
}

function toModelBadgeLabel(status: ProductReadModelBadgeStatus, locale: AppLocale) {
	switch (status) {
		case 'ready':
			return appMessage(locale, 'overview.badge.ready');
		case 'pending':
			return appMessage(locale, 'overview.badge.pending');
		case 'blocked':
			return appMessage(locale, 'overview.badge.blocked');
		case 'live':
			return appMessage(locale, 'overview.badge.live');
		case 'not_configured':
			return appMessage(locale, 'overview.badge.notConfigured');
		case 'not_available':
			return appMessage(locale, 'overview.badge.notAvailable');
		default:
			return humanizeValue(status);
	}
}

export function toSelectedSeriesSurfaceView(
	hub: CampaignSeriesHubResponse,
	surface: SelectedSeriesSurfaceId,
	locale: AppLocale = 'en'
) {
	const hubView = toCampaignSeriesHubView(hub, locale);
	const config = selectedSeriesSurfaceConfig(surface);

	return localizeLegacyApiReadModelChrome({
		id: hubView.id,
		title: hubView.title,
		subtitle: hubView.subtitle,
		ownership: hubView.ownership,
		canMutate: hubView.canMutate,
		readOnlyMessage: hubView.ownership.readOnlyMessage,
		surfaceLabel: config.label,
		surfaceEyebrow: config.eyebrow,
		summaryRows: toSelectedSeriesSummaryRows(hub, surface),
		governanceRows: surface === 'setup' ? hubView.governanceRows : [],
		campaignRows: hub.campaigns.map((campaign) => toSelectedSeriesCampaignRow(campaign, surface)),
		emptyState: hub.campaigns.length === 0 ? config.emptyState : null,
		proofActionTitle: config.proofActionTitle,
		proofActionDescription: config.proofActionDescription
	}, locale);
}

export function toCampaignSeriesSetupWorkspaceView(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	locale: AppLocale = 'en'
) {
	const ownership = toCampaignSeriesOwnership(workspace.series, locale);

	return localizeLegacyApiReadModelChrome({
		id: workspace.series.id,
		title: workspace.series.name.trim() || appMessage(locale, 'readModel.untitledWaveSeries'),
		subtitle: appMessage(locale, 'readModel.subtitle.campaignsLive', {
			campaignCount: workspace.summary.campaignCount,
			liveCount: workspace.summary.liveCampaignCount
		}),
		ownership,
		canMutate: !ownership.isSample,
		readOnlyMessage: ownership.readOnlyMessage,
		studyBriefContext: toStudyBriefContext(workspace.series.studyBrief, locale),
		surfaceLabel: appMessage(locale, 'readModel.surface.setup.label'),
		surfaceEyebrow: appMessage(locale, 'readModel.surface.setup.eyebrow'),
		surfaceDescription: appMessage(locale, 'readModel.surface.setup.description'),
		referenceTitle: appMessage(locale, 'readModel.surface.setup.referenceTitle'),
		referenceDescription: appMessage(locale, 'readModel.surface.setup.referenceDescription'),
		summaryRows: [
			{ label: appMessage(locale, 'readModel.row.campaigns'), value: formatCount(workspace.summary.campaignCount) },
			{ label: appMessage(locale, 'readModel.row.liveCampaigns'), value: formatCount(workspace.summary.liveCampaignCount) },
			{
				label: appMessage(locale, 'readModel.row.missingPrerequisites'),
				value: formatCount(workspace.summary.missingPrerequisiteCount)
			}
		],
		preparationChecklist: toSetupPreparationChecklist(workspace),
		readiness: toSetupReadinessRow(workspace),
		policyRows: [
			toSetupPolicyRow('Consent', workspace.policies.consent),
			toSetupPolicyRow('Retention', workspace.policies.retention),
			toSetupPolicyRow('Disclosure', workspace.policies.disclosure)
		],
		selectedCampaignRows: workspace.selectedCampaign
			? toSetupCampaignDetailRows(workspace.selectedCampaign)
			: [],
		templateRows: workspace.template
			? [
					{ label: 'Template', value: workspace.template.templateName },
					{ label: 'Status', value: humanizeValue(workspace.template.status) },
					{ label: 'Locale', value: workspace.template.defaultLocale },
					{ label: 'Questions', value: formatCount(workspace.template.questionCount) }
				]
			: [],
		scoringRows: workspace.scoring
			? [
					{ label: 'Rule', value: workspace.scoring.ruleKey, mono: true },
					{ label: 'Status', value: humanizeValue(workspace.scoring.status) },
					{ label: 'Source', value: humanizeValue(workspace.scoring.source) }
				]
			: [],
		missingPrerequisiteRows: workspace.missingPrerequisites.map((item) => ({
			code: item.code,
			label: toProductDisplayCopy(item.label),
			message: toProductDisplayCopy(item.message),
			severity: item.severity,
			status: 'blocked' as ProductReadModelBadgeStatus
		})),
		campaignRows: workspace.campaigns.map((campaign) => ({
			id: campaign.id,
			title: campaign.name.trim() || appMessage(locale, 'readModel.untitledWave'),
			status: toProductReadModelBadgeStatus(campaign.status),
			rows: toSetupCampaignSummaryRows(campaign)
		})),
		emptyState:
			workspace.campaigns.length === 0
				? {
						title: appMessage(locale, 'readModel.surface.setup.emptyTitle'),
						message: appMessage(locale, 'readModel.surface.setup.emptyMessage')
					}
				: null,
		proofActionTitle: appMessage(locale, 'readModel.surface.setup.proofTitle'),
		proofActionDescription: appMessage(locale, 'readModel.surface.setup.proofDescription')
	}, locale);
}

export function toCampaignSeriesOperationsWorkspaceView(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	locale: AppLocale = 'en'
) {
	const ownership = toCampaignSeriesOwnership(workspace.series, locale);
	const scoreCoverage = normalizeOperationsScoreCoverage(workspace.scoreCoverage);

	return localizeLegacyApiReadModelChrome({
		id: workspace.series.id,
		title: workspace.series.name.trim() || 'Untitled wave series',
		subtitle: `${workspace.summary.campaignCount} ${workspace.summary.campaignCount === 1 ? 'campaign' : 'campaigns'}, ${workspace.summary.liveCampaignCount} live`,
		ownership,
		canMutate: !ownership.isSample,
		readOnlyMessage: ownership.readOnlyMessage,
		studyBriefContext: toStudyBriefContext(workspace.series.studyBrief, locale),
		surfaceLabel: 'Collect responses',
		surfaceEyebrow: 'Study collection',
		surfaceDescription:
			'Start the selected wave, share respondent access, monitor submissions, and close collection when finished.',
		referenceTitle: 'Collection reference',
		referenceDescription:
			'Launch records, prerequisite checks, and selected wave details stay here for review.',
		summaryRows: [
			{ label: 'Measurements', value: formatCount(workspace.summary.campaignCount) },
			{ label: 'Live measurements', value: formatCount(workspace.summary.liveCampaignCount) },
			{
				label: 'Respondent links',
				value: formatCount(workspace.summary.openLinkAssignmentCount)
			},
			{ label: 'Queued emails', value: formatCount(workspace.summary.queuedInvitationCount) },
			{ label: 'Sent emails', value: formatCount(workspace.summary.sentInvitationCount) },
			{ label: 'Failed emails', value: formatCount(workspace.summary.failedInvitationCount) },
			{
				label: 'Suppressed emails',
				value: formatCount(workspace.summary.bouncedInvitationCount ?? 0)
			},
			{
				label: 'Started responses',
				value: formatCount(workspace.summary.startedResponseCount)
			},
			{
				label: 'Draft responses',
				value: formatCount(workspace.summary.draftResponseCount)
			},
			{
				label: 'Submitted responses',
				value: formatCount(workspace.summary.submittedResponseCount)
			},
			{
				label: 'Latest response activity',
				value: latestWorkspaceResponseActivity(workspace)
			},
			{
				label: 'Missing prerequisites',
				value: formatCount(workspace.summary.missingPrerequisiteCount)
			}
		],
		collectionMonitor: {
			title: operationsReadModelMessage(locale, 'operations.readModel.collectionMonitor.title'),
			status: workspace.summary.collectionStatus,
			reportVisibilityStatus: workspace.summary.reportVisibilityStatus,
			guidance: localizedCollectionGuidance(workspace.summary.collectionGuidance, locale),
			summaryRows: [
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.startedResponses'),
					value: formatCount(workspace.summary.startedResponseCount)
				},
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.draftResponses'),
					value: formatCount(workspace.summary.draftResponseCount)
				},
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.submittedResponses'),
					value: formatCount(workspace.summary.submittedResponseCount)
				},
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.latestStarted'),
					value: formatCollectionDateTime(workspace.summary.latestResponseStartedAt)
				},
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.latestSubmitted'),
					value: formatCollectionDateTime(workspace.summary.latestResponseSubmittedAt)
				}
			]
		},
		collectionOverview: toOperationsCollectionOverview(workspace, scoreCoverage, locale),
		scoreCoverageMonitor: toScoreCoverageMonitor(scoreCoverage, true, locale),
		selectedCampaignRows: workspace.selectedCampaign
			? toOperationsCampaignDetailRows(workspace.selectedCampaign)
			: [],
		launchSnapshotRows: workspace.selectedCampaign?.launchSnapshot
			? toOperationsLaunchSnapshotRows(workspace.selectedCampaign.launchSnapshot)
			: [],
		missingPrerequisiteRows: workspace.missingPrerequisites.map((item) => ({
			code: item.code,
			label: toProductDisplayCopy(item.label),
			message: toProductDisplayCopy(item.message),
			severity: item.severity,
			status: 'blocked' as ProductReadModelBadgeStatus
		})),
		campaignRows: workspace.campaigns.map((campaign) => ({
			id: campaign.id,
			title: campaign.name.trim() || 'Untitled wave',
			status: toProductReadModelBadgeStatus(campaign.status),
			rows: toOperationsCampaignSummaryRows(campaign)
		})),
		emptyState:
			workspace.campaigns.length === 0
				? {
						title: 'No collection wave yet',
						message: 'Create a wave draft in Setup, then start collection here.'
					}
				: null,
		proofActionTitle: 'Collection actions',
		proofActionDescription:
			'Run the pre-launch check, start collection, share respondent access, monitor submissions, and close the wave.'
	}, locale);
}

export function toCampaignSeriesReportsWorkspaceView(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	locale: AppLocale = 'en'
) {
	const ownership = toCampaignSeriesOwnership(workspace.series, locale);
	const scoreCoverage = normalizeOperationsScoreCoverage(workspace.scoreCoverage);

	return localizeLegacyApiReadModelChrome({
		id: workspace.series.id,
		title: workspace.series.name.trim() || 'Untitled wave series',
		subtitle: `${workspace.summary.campaignCount} ${workspace.summary.campaignCount === 1 ? 'campaign' : 'campaigns'}, ${workspace.summary.liveCampaignCount} live`,
		ownership,
		canMutate: !ownership.isSample,
		readOnlyMessage: ownership.readOnlyMessage,
		studyBriefContext: toStudyBriefContext(workspace.series.studyBrief, locale),
		surfaceLabel: 'Review results',
		surfaceEyebrow: 'Study results',
		surfaceDescription:
			'Review result availability, coverage, limitations, and export next use for the selected campaign.',
		referenceTitle: 'Results reference',
		referenceDescription:
			'Selected wave details, limitations, prerequisite checks, and export records stay here for review.',
		resultsOverview: toReportsResultsOverview(workspace, scoreCoverage),
		summaryRows: [
			{ label: 'Measurements', value: formatCount(workspace.summary.campaignCount) },
			{ label: 'Live measurements', value: formatCount(workspace.summary.liveCampaignCount) },
			{
				label: 'Reportable campaigns',
				value: formatCount(workspace.summary.reportableCampaignCount)
			},
			{
				label: 'Submitted responses',
				value: formatCount(workspace.summary.submittedResponseCount)
			},
			{ label: 'Scores', value: formatCount(workspace.summary.scoreCount) },
			{ label: 'Visible scores', value: formatCount(workspace.summary.visibleScoreCount) },
			{ label: 'Suppressed scores', value: formatCount(workspace.summary.suppressedScoreCount) },
			...optionalCountRow('Preliminary live reports', workspace.summary.preliminaryLiveReportCount),
			...optionalCountRow('Closed-wave reports', workspace.summary.closedWaveReportCount),
			{ label: 'Export files', value: formatCount(workspace.summary.exportArtifactCount) },
			{
				label: 'Missing prerequisites',
				value: formatCount(workspace.summary.missingPrerequisiteCount)
			}
		],
		scoreCoverageSignal: toScoreCoverageMonitor(scoreCoverage, false, locale),
		selectedCampaignRows: workspace.selectedCampaign
			? toReportsCampaignDetailRows(workspace.selectedCampaign)
			: [],
		provenanceRows: workspace.selectedCampaign
			? toReportsCampaignProvenanceRows(workspace.selectedCampaign)
			: [],
		missingPrerequisiteRows: workspace.missingPrerequisites.map((item) => ({
			code: item.code,
			label: toProductDisplayCopy(item.label),
			message: toProductDisplayCopy(item.message),
			severity: item.severity,
			status: (item.severity === 'advisory' ? 'pending' : 'blocked') as ProductReadModelBadgeStatus
		})),
		campaignRows: workspace.campaigns.map((campaign) => ({
			id: campaign.id,
			title: campaign.name.trim() || 'Untitled wave',
			status: toProductReadModelBadgeStatus(campaign.reportStatus),
			rows: toReportsCampaignSummaryRows(campaign)
		})),
		emptyState:
			workspace.campaigns.length === 0
				? {
						title: 'No reportable campaigns yet',
						message: 'Submit responses and compute scores before report previews are available.'
					}
				: null,
		proofActionTitle: 'Results actions',
		proofActionDescription:
			'Review aggregate results, create export files, and download files when they are ready.'
	}, locale);
}

export function toCampaignSeriesWavesWorkspaceView(
	workspace: CampaignSeriesWavesWorkspaceResponse,
	locale: AppLocale = 'en'
) {
	const ownership = toCampaignSeriesOwnership(workspace.series, locale);

	return localizeLegacyApiReadModelChrome({
		id: workspace.series.id,
		title: workspace.series.name.trim() || 'Untitled wave series',
		subtitle: `${workspace.summary.campaignCount} ${workspace.summary.campaignCount === 1 ? 'campaign' : 'campaigns'}, ${workspace.summary.liveCampaignCount} live`,
		ownership,
		canMutate: !ownership.isSample,
		readOnlyMessage: ownership.readOnlyMessage,
		studyBriefContext: toStudyBriefContext(workspace.series.studyBrief, locale),
		surfaceLabel: 'Compare rounds',
		surfaceEyebrow: 'Repeated-round comparison',
		summaryRows: [
			{ label: 'Measurements', value: formatCount(workspace.summary.campaignCount) },
			{ label: 'Live measurements', value: formatCount(workspace.summary.liveCampaignCount) },
			{
				label: 'Repeat-participation waves',
				value: formatCount(workspace.summary.longitudinalWaveCount)
			},
			{ label: 'Submitted waves', value: formatCount(workspace.summary.submittedWaveCount) },
			{
				label: 'Linked repeat responses',
				value: formatCount(workspace.summary.linkedTrajectoryCount)
			},
			{
				label: 'Complete repeat-response pairs',
				value: formatCount(workspace.summary.completeTrajectoryCount)
			},
			{ label: 'Comparable scores', value: formatCount(workspace.summary.comparableScoreCount) },
			{
				label: 'Visible comparisons',
				value: formatCount(workspace.summary.visibleComparisonCount)
			},
			{
				label: 'Suppressed comparisons',
				value: formatCount(workspace.summary.suppressedComparisonCount)
			},
			...optionalCountRow('Preliminary live waves', workspace.summary.preliminaryLiveWaveCount),
			...optionalCountRow('Closed waves', workspace.summary.closedWaveCount),
			{
				label: 'Blocked comparisons',
				value: formatCount(workspace.summary.blockedComparisonCount)
			},
			{
				label: 'Missing prerequisites',
				value: formatCount(workspace.summary.missingPrerequisiteCount)
			}
		],
		selectedWaveRows:
			workspace.selectedBaselineWave && workspace.selectedComparisonWave
				? toWavesSelectedRows(
						workspace.selectedBaselineWave,
						workspace.selectedComparisonWave,
						workspace.comparison
					)
				: [],
		provenanceRows:
			workspace.selectedBaselineWave && workspace.selectedComparisonWave
				? [
						...toWavesWaveProvenanceRows('Baseline', workspace.selectedBaselineWave),
						...toWavesWaveProvenanceRows('Comparison', workspace.selectedComparisonWave)
					]
				: [],
		missingPrerequisiteRows: workspace.missingPrerequisites.map((item) => ({
			code: item.code,
			label: toProductDisplayCopy(item.label),
			message: toProductDisplayCopy(item.message),
			severity: item.severity,
			status: (item.severity === 'advisory' ? 'pending' : 'blocked') as ProductReadModelBadgeStatus
		})),
		campaignRows: workspace.waves.map((wave) => ({
			id: wave.id,
			title: wave.name.trim() || 'Untitled wave',
			status: toProductReadModelBadgeStatus(wave.status),
			rows: toWavesWaveSummaryRows(wave)
		})),
		emptyState:
			workspace.waves.length === 0
				? {
						title: 'No repeated rounds yet',
						message: 'Create and launch at least two collection rounds before comparing results over time.'
					}
				: null,
		proofActionTitle: 'Comparison actions',
		proofActionDescription:
			'Check whether repeated rounds can be compared, then review safe change-over-time summaries.'
	}, locale);
}

export function toProductApiErrorMessage(error: unknown, fallback: string) {
	if (!(error instanceof ApiError)) {
		return fallback;
	}

	const detail = readProblemDetail(error.body);
	if (detail) {
		return detail;
	}

	return `API request failed with status ${error.status}.`;
}

export function toReportProofView(report: CampaignReportProofResponse) {
	return {
		campaignId: report.campaignId,
		campaignSeriesId: report.campaignSeriesId,
		campaignName: report.campaignName,
		proofStatus: report.proofStatus,
		summary: toReportProofSummary(report),
		provenance: [
			{ label: 'Launch snapshot', value: report.launchSnapshot.id, mono: true },
			{ label: 'Scoring rule', value: report.launchSnapshot.scoringRuleId, mono: true },
			{ label: 'Disclosure k', value: String(report.disclosurePolicy.kMin) },
			{ label: 'Interpretation', value: report.interpretationStatus },
			...optionalDateTimeRow('Closed at', report.closedAt),
			...optionalDataFinalityRow('Data finality', report.dataFinality)
		],
		scoreRows: report.scores.map((score) => {
			const disclosureState = toDisclosureDisplayState(score.disclosure);
			const isVisible = disclosureState === 'visible';
			const interpretation = isVisible ? score.interpretation : null;

			return {
				dimensionCode: score.dimensionCode,
				disclosureState,
				submittedResponseCount: score.submittedResponseCount,
				scoreCount: isVisible ? formatNullableCount(score.scoreCount) : 'Suppressed',
				scoreMetadata: isVisible
					? formatScoreOutputMetadata(
							score.nValidTotal,
							score.nExpectedTotal,
							score.missingPolicyStatusSummary
						)
					: null,
				mean: isVisible ? formatNullableNumber(score.mean) : 'Suppressed',
				range:
					isVisible && score.min !== null && score.max !== null
						? `${formatNumber(score.min)}-${formatNumber(score.max)}`
						: 'Suppressed',
				interpretationLabel: interpretation?.label ?? null,
				interpretationMeta: formatInterpretationMeta(interpretation),
				note: score.suppressionReason
			};
		})
	};
}

function toReportProofSummary(report: CampaignReportProofResponse) {
	const scoreStates = report.scores.map((score) => toDisclosureDisplayState(score.disclosure));
	const visibleScoreRowCount = scoreStates.filter((state) => state === 'visible').length;
	const suppressedScoreRowCount = scoreStates.length - visibleScoreRowCount;
	const submittedResponseCount = report.scores.reduce(
		(maximum, score) => Math.max(maximum, score.submittedResponseCount),
		0
	);
	const dataFinality = humanizeValue(report.dataFinality ?? 'preliminary_live');
	const interpretationPosture = humanizeValue(report.interpretationStatus);

	return {
		title: 'Preliminary aggregate summary',
		headline: toReportProofSummaryHeadline(
			scoreStates.length,
			visibleScoreRowCount,
			submittedResponseCount
		),
		detail: toReportProofSummaryDetail(
			visibleScoreRowCount,
			suppressedScoreRowCount,
			interpretationPosture
		),
		metrics: [
			{ label: 'Visible score rows', value: formatCount(visibleScoreRowCount) },
			{ label: 'Suppressed score rows', value: formatCount(suppressedScoreRowCount) },
			{ label: 'Submitted responses', value: formatCount(submittedResponseCount) },
			{ label: 'Data finality', value: dataFinality }
		],
		guardrails: [
			'Aggregate only',
			'Disclosure guardrails still apply',
			'Interpretation labels are tenant-attested or not reviewed unless explicitly approved.'
		]
	};
}

function toReportProofSummaryHeadline(
	scoreRowCount: number,
	visibleScoreRowCount: number,
	submittedResponseCount: number
) {
	if (scoreRowCount === 0) {
		return 'No governed score rows are available yet.';
	}

	if (visibleScoreRowCount === 0) {
		return `All ${formatCount(scoreRowCount)} ${pluralize(scoreRowCount, 'score row', 'score rows')} are suppressed by disclosure guardrails.`;
	}

	return `${formatCount(visibleScoreRowCount)} ${pluralize(visibleScoreRowCount, 'visible score row', 'visible score rows')} across up to ${formatCount(submittedResponseCount)} ${pluralize(submittedResponseCount, 'submitted response', 'submitted responses')}.`;
}

function toReportProofSummaryDetail(
	visibleScoreRowCount: number,
	suppressedScoreRowCount: number,
	interpretationPosture: string
) {
	if (visibleScoreRowCount === 0 && suppressedScoreRowCount > 0) {
		return `No visible aggregate score values are shown until disclosure requirements are met. Interpretation posture: ${interpretationPosture}.`;
	}

	if (suppressedScoreRowCount === 0) {
		return `No score rows are suppressed by disclosure guardrails. Interpretation posture: ${interpretationPosture}.`;
	}

	if (suppressedScoreRowCount === 1) {
		return `1 score row is suppressed by disclosure guardrails. Interpretation posture: ${interpretationPosture}.`;
	}

	return `${formatCount(suppressedScoreRowCount)} score rows are suppressed by disclosure guardrails. Interpretation posture: ${interpretationPosture}.`;
}

export function toExportArtifactView(
	artifact: ReportProofExportArtifactResponse,
	download?: ExportArtifactDownloadResponse | null
) {
	return {
		id: artifact.id,
		fileName: artifact.fileName,
		status: artifact.status,
		format: artifact.format,
		rowCount: `${artifact.rowCount} ${artifact.rowCount === 1 ? 'row' : 'rows'}`,
		byteSize: formatBytes(artifact.byteSize),
		checksum: artifact.checksumSha256
			? `${artifact.checksumSha256.slice(0, 12)}...`
			: 'Not available',
		download: download
			? {
					fileName: download.fileName ?? artifact.fileName,
					contentType: download.contentType,
					byteSize: formatBytes(download.byteSize)
				}
			: null
	};
}

export function toWaveComparisonView(comparison: CampaignSeriesWaveComparisonProofResponse) {
	return {
		campaignSeriesId: comparison.campaignSeriesId,
		proofStatus: comparison.proofStatus,
		summaryRows: [
			{ label: 'Baseline wave', value: comparison.baselineWave?.name ?? 'Missing' },
			{ label: 'Comparison wave', value: comparison.comparisonWave?.name ?? 'Missing' },
			{
				label: 'Disclosure k',
				value: String(comparison.disclosurePolicy?.kMin ?? 'Not configured')
			},
			{ label: 'Interpretation', value: comparison.interpretationStatus }
		],
		scoreRows: comparison.scores.map((score) => {
			const disclosureState = toDisclosureDisplayState(score.disclosure);
			const isVisible = disclosureState === 'visible';
			const baselineInterpretation = isVisible ? score.baselineInterpretation : null;
			const comparisonInterpretation = isVisible ? score.comparisonInterpretation : null;

			return {
				dimensionCode: score.dimensionCode,
				disclosureState,
				compatibilityStatus: score.compatibilityStatus,
				baselineMean: isVisible ? formatNullableNumber(score.baselineMean) : 'Suppressed',
				comparisonMean: isVisible ? formatNullableNumber(score.comparisonMean) : 'Suppressed',
				aggregateDelta: isVisible ? formatDelta(score.aggregateDelta) : 'Suppressed',
				pairedDeltaMean: isVisible ? formatDelta(score.pairedDeltaMean) : 'Suppressed',
				linkedPairCount: score.linkedPairCount,
				baselineScoreMetadata: isVisible
					? formatScoreOutputMetadata(
							score.baselineNValidTotal,
							score.baselineNExpectedTotal,
							score.baselineMissingPolicyStatusSummary
						)
					: null,
				comparisonScoreMetadata: isVisible
					? formatScoreOutputMetadata(
							score.comparisonNValidTotal,
							score.comparisonNExpectedTotal,
							score.comparisonMissingPolicyStatusSummary
						)
					: null,
				baselineInterpretationLabel: baselineInterpretation?.label ?? null,
				comparisonInterpretationLabel: comparisonInterpretation?.label ?? null,
				interpretationMeta: formatInterpretationMeta(
					baselineInterpretation ?? comparisonInterpretation
				),
				note: score.compatibilityReason ?? score.suppressionReason
			};
		})
	};
}

export function toSessionView(input: {
	session?: AuthSessionResponse | null;
	error?: unknown;
	checking?: boolean;
}, locale: AppLocale = 'en') {
	if (input.checking) {
		return {
			state: 'checking',
			title: appMessage(locale, 'session.checking.title'),
			message: appMessage(locale, 'session.checking.message'),
			tenantId: null,
			userId: null
		};
	}

	if (input.session) {
		return {
			state: 'authenticated',
			title: appMessage(locale, 'session.authenticated.title'),
			message: appMessage(locale, 'session.authenticated.message', {
				tenantId: input.session.tenantId
			}),
			tenantId: input.session.tenantId,
			userId: input.session.userId
		};
	}

	if (input.error instanceof ApiError && input.error.status === 401) {
		return {
			state: 'unauthenticated',
			title: appMessage(locale, 'session.unauthenticated.title'),
			message: appMessage(locale, 'session.unauthenticated.message'),
			tenantId: null,
			userId: null
		};
	}

	if (input.error instanceof ApiError && input.error.status === 403) {
		return {
			state: 'forbidden',
			title: appMessage(locale, 'session.forbidden.title'),
			message: appMessage(locale, 'session.forbidden.message'),
			tenantId: null,
			userId: null
		};
	}

	return {
		state: 'failed',
		title: appMessage(locale, 'session.failed.title'),
		message:
			input.error instanceof ApiError
				? appMessage(locale, 'session.failed.statusMessage', { status: input.error.status })
				: appMessage(locale, 'session.failed.message'),
		tenantId: null,
		userId: null
	};
}

function toDisclosureDisplayState(disclosure: string | null | undefined): DisclosureDisplayState {
	if (!disclosure) {
		return 'not_applicable';
	}

	if (disclosure === 'visible') {
		return 'visible';
	}

	if (disclosure === 'pending') {
		return 'pending';
	}

	return 'suppressed';
}

function toWorkspaceTotalRows(
	totals: WorkspaceOverviewResponse['totals'],
	locale: AppLocale
): DisplayRow[] {
	return [
		{ label: appMessage(locale, 'settings.metric.studies'), value: formatCount(totals.campaignSeriesCount) },
		{ label: appMessage(locale, 'settings.metric.campaigns'), value: formatCount(totals.campaignCount) },
		{ label: appMessage(locale, 'settings.metric.liveCampaigns'), value: formatCount(totals.liveCampaignCount) },
		{
			label: appMessage(locale, 'settings.metric.submittedResponses'),
			value: formatCount(totals.submittedResponseCount)
		},
		{ label: appMessage(locale, 'settings.metric.exportFiles'), value: formatCount(totals.exportArtifactCount) }
	];
}

function toWorkspaceCommandItem(
	item: WorkspaceOverviewResponse['commandCenter']['items'][number],
	locale: AppLocale
) {
	const surfaceLabel = toWorkspaceCommandSurfaceLabel(item.surface, locale);
	const rows: DisplayRow[] = [{ label: appMessage(locale, 'workspace.row.surface'), value: surfaceLabel }];
	const localized = localizeWorkspaceCommand(item, locale);

	return {
		id: item.id,
		title: localized?.title ?? (item.title.trim() || appMessage(locale, 'workspace.command.untitled')),
		description:
			localized?.description ??
			(item.description.trim() || appMessage(locale, 'workspace.command.defaultDescription')),
		href: item.route,
		actionLabel:
			localized?.actionLabel ??
			(item.actionLabel.trim() || appMessage(locale, 'workspace.command.defaultAction')),
		status: toProductReadModelBadgeStatus(item.state),
		priority: item.priority,
		surfaceLabel,
		rows
	};
}

/**
 * The command-center read model carries English display strings; presentation
 * vocabulary and hr localization are applied here by command id. Unknown ids
 * fall back to the backend strings unchanged.
 */
function localizeWorkspaceCommand(
	item: WorkspaceOverviewResponse['commandCenter']['items'][number],
	locale: AppLocale
): { title: string; description: string; actionLabel: string } | null {
	const nameFromTitle = / for (.+)$/.exec(item.title.trim())?.[1] ?? '';
	type MessageKey = Parameters<typeof appMessage>[1];
	const named = (key: MessageKey) => appMessage(locale, key, { name: nameFromTitle });
	const plain = (key: MessageKey) => appMessage(locale, key);

	if (item.id === 'campaign_series.create') {
		const empty = item.title.trim().toLowerCase().startsWith('no ');
		return empty
			? {
					title: plain('briefing.command.create.emptyTitle'),
					description: plain('briefing.command.create.emptyDescription'),
					actionLabel: plain('briefing.command.create.emptyAction')
				}
			: {
					title: plain('briefing.command.create.title'),
					description: plain('briefing.command.create.description'),
					actionLabel: plain('briefing.command.create.action')
				};
	}

	if (item.id === 'directory.setup') {
		return {
			title: plain('briefing.command.directory.title'),
			description: plain('briefing.command.directory.description'),
			actionLabel: plain('briefing.command.directory.action')
		};
	}

	if (item.id === 'team.pending_provider_links') {
		return {
			title: plain('briefing.command.team.title'),
			description: plain('briefing.command.team.description'),
			actionLabel: plain('briefing.command.team.action')
		};
	}

	if (item.id === 'workspace.review') {
		return {
			title: plain('briefing.command.review.title'),
			description: plain('briefing.command.review.description'),
			actionLabel: plain('briefing.command.review.action')
		};
	}

	const seriesCommand = /^series\.[0-9a-f]{32}\.([a-z_]+)$/.exec(item.id)?.[1];
	if (!seriesCommand || !nameFromTitle) {
		return null;
	}

	const familyKeys = {
		setup: [
			'briefing.command.protocol.title',
			'briefing.command.protocol.description',
			'briefing.command.protocol.action'
		],
		operations: [
			'briefing.command.field.title',
			'briefing.command.field.description',
			'briefing.command.field.action'
		],
		reports: [
			'briefing.command.evidence.title',
			'briefing.command.evidence.description',
			'briefing.command.evidence.action'
		],
		score_remediation: [
			'briefing.command.scoring.title',
			'briefing.command.scoring.description',
			'briefing.command.scoring.action'
		],
		exports: [
			'briefing.command.exports.title',
			'briefing.command.exports.description',
			'briefing.command.exports.action'
		],
		waves: [
			'briefing.command.rounds.title',
			'briefing.command.rounds.description',
			'briefing.command.rounds.action'
		]
	} as const;
	const keys = familyKeys[seriesCommand as keyof typeof familyKeys];
	if (!keys) {
		return null;
	}

	return {
		title: named(keys[0]),
		description: plain(keys[1]),
		actionLabel: plain(keys[2])
	};
}

function workspaceLifecycleSteps(locale: AppLocale) {
	return [
		{
			id: 'prepare',
			label: appMessage(locale, 'workspace.lifecycle.prepare.label'),
			description: appMessage(locale, 'workspace.lifecycle.prepare.description')
		},
		{
			id: 'collect',
			label: appMessage(locale, 'workspace.lifecycle.collect.label'),
			description: appMessage(locale, 'workspace.lifecycle.collect.description')
		},
		{
			id: 'review',
			label: appMessage(locale, 'workspace.lifecycle.review.label'),
			description: appMessage(locale, 'workspace.lifecycle.review.description')
		},
		{
			id: 'export',
			label: appMessage(locale, 'workspace.lifecycle.export.label'),
			description: appMessage(locale, 'workspace.lifecycle.export.description')
		}
	] as const;
}

function toWorkspaceStudyCard(item: CampaignSeriesListItemResponse, locale: AppLocale) {
	const card = toCampaignSeriesCard(item, locale);
	const action = toWorkspaceStudyAction(item, locale);

	return {
		...card,
		actionLabel: action.label,
		actionHref: action.href
	};
}

function toWorkspaceStudyAction(item: CampaignSeriesListItemResponse, locale: AppLocale) {
	const baseHref = `/app/campaign-series/${item.id}`;
	const isSample = item.isSample === true || item.studyKind === 'sample';

	if (isSample) {
		if (item.submittedResponseCount > 0) {
			return { label: appMessage(locale, 'workspace.action.reviewSampleResults'), href: `${baseHref}/reports` };
		}

		if (item.liveCampaignCount > 0) {
			return {
				label: appMessage(locale, 'workspace.action.inspectSampleCollection'),
				href: `${baseHref}/operations`
			};
		}

		if (item.campaignCount === 0 || item.readinessStatus === 'not_configured') {
			return { label: appMessage(locale, 'workspace.action.inspectSampleSetup'), href: `${baseHref}/setup` };
		}

		return { label: appMessage(locale, 'workspace.action.openStudy'), href: baseHref };
	}

	if (item.readinessStatus === 'not_configured') {
		return { label: appMessage(locale, 'workspace.action.continueSetup'), href: `${baseHref}/setup` };
	}

	if (item.liveCampaignCount > 0) {
		return { label: appMessage(locale, 'workspace.action.monitorCollection'), href: `${baseHref}/operations` };
	}

	if (item.submittedResponseCount > 0) {
		return { label: appMessage(locale, 'workspace.action.reviewResults'), href: `${baseHref}/reports` };
	}

	return { label: appMessage(locale, 'workspace.action.openStudy'), href: baseHref };
}

function toWorkspaceCommandSurfaceLabel(surface: string, locale: AppLocale) {
	const labels: Record<string, string> = {
		campaign_series: appMessage(locale, 'workspace.surface.campaignSeries'),
		directory: appMessage(locale, 'workspace.surface.directory'),
		operations: appMessage(locale, 'workspace.surface.operations'),
		reports: appMessage(locale, 'workspace.surface.reports'),
		setup: appMessage(locale, 'workspace.surface.setup'),
		team: appMessage(locale, 'workspace.surface.team'),
		waves: appMessage(locale, 'workspace.surface.waves'),
		workspace: appMessage(locale, 'workspace.surface.workspace')
	};

	return labels[surface] ?? labelFromCode(surface);
}

function campaignSeriesPortfolioStatusOptions(locale: AppLocale) {
	return [
		{ value: 'all', label: appMessage(locale, 'portfolio.filter.allReadiness') },
		{ value: 'not_configured', label: appMessage(locale, 'portfolio.filter.notConfigured') },
		{ value: 'pending', label: appMessage(locale, 'portfolio.filter.pending') },
		{ value: 'proof_only', label: appMessage(locale, 'portfolio.filter.preview') }
	] as const;
}

function campaignSeriesPortfolioSortOptions(locale: AppLocale) {
	return [
		{ value: 'activity_desc', label: appMessage(locale, 'portfolio.sort.latestActivity') },
		{ value: 'updated_desc', label: appMessage(locale, 'portfolio.sort.recentlyUpdated') },
		{ value: 'created_desc', label: appMessage(locale, 'portfolio.sort.recentlyCreated') },
		{ value: 'name_asc', label: appMessage(locale, 'portfolio.sort.nameAscending') }
	] as const;
}

function campaignSeriesPortfolioVisibilityOptions(locale: AppLocale) {
	return [
		{ value: 'active', label: appMessage(locale, 'portfolio.visibility.active') },
		{ value: 'archived', label: appMessage(locale, 'portfolio.visibility.archived') },
		{ value: 'all', label: appMessage(locale, 'portfolio.visibility.all') }
	] as const;
}

function hasActiveCampaignSeriesFilters(query: CampaignSeriesPortfolioQuery) {
	const search = query.search?.trim() ?? '';
	const status = query.status?.trim() ?? 'all';
	const visibility = query.visibility?.trim() ?? 'active';

	return (
		search.length > 0 ||
		(status.length > 0 && status !== 'all') ||
		(visibility.length > 0 && visibility !== 'active')
	);
}

function toCampaignSeriesHubTotalRows(totals: CampaignSeriesHubResponse['totals']): DisplayRow[] {
	return [
		{ label: 'Measurements', value: formatCount(totals.campaignCount) },
		{ label: 'Live measurements', value: formatCount(totals.liveCampaignCount) },
		{ label: 'Submitted responses', value: formatCount(totals.submittedResponseCount) },
		{ label: 'Scores', value: formatCount(totals.scoreCount) },
		{ label: 'Export files', value: formatCount(totals.exportArtifactCount) }
	];
}

function toSelectedSeriesSummaryRows(
	hub: CampaignSeriesHubResponse,
	surface: SelectedSeriesSurfaceId
): DisplayRow[] {
	switch (surface) {
		case 'setup':
			return [
				{ label: 'Measurements', value: formatCount(hub.totals.campaignCount) },
				{ label: 'Live measurements', value: formatCount(hub.totals.liveCampaignCount) }
			];
		case 'reports':
			return [
				{ label: 'Submitted responses', value: formatCount(hub.totals.submittedResponseCount) },
				{ label: 'Scores', value: formatCount(hub.totals.scoreCount) },
				{ label: 'Export files', value: formatCount(hub.totals.exportArtifactCount) }
			];
		case 'operations':
		case 'waves':
			return [
				{ label: 'Measurements', value: formatCount(hub.totals.campaignCount) },
				{ label: 'Live measurements', value: formatCount(hub.totals.liveCampaignCount) },
				{ label: 'Submitted responses', value: formatCount(hub.totals.submittedResponseCount) }
			];
	}
}

function toSelectedSeriesCampaignRow(
	campaign: CampaignSeriesHubResponse['campaigns'][number],
	surface: SelectedSeriesSurfaceId
) {
	return {
		id: campaign.id,
		title: campaign.name.trim() || 'Untitled wave',
		status: toProductReadModelBadgeStatus(campaign.status),
		rows: toSelectedSeriesCampaignRows(campaign, surface)
	};
}

function toSelectedSeriesCampaignRows(
	campaign: CampaignSeriesHubResponse['campaigns'][number],
	surface: SelectedSeriesSurfaceId
): DisplayRow[] {
	switch (surface) {
		case 'reports':
			return [
				{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
				{ label: 'Scores', value: formatCount(campaign.scoreCount) },
				{ label: 'Export files', value: formatCount(campaign.exportArtifactCount) }
			];
		case 'operations':
		case 'waves':
			return [
				{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
				{ label: 'Locale', value: campaign.defaultLocale },
				{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
				{ label: 'Latest launch', value: formatCollectionDateTime(campaign.latestLaunchAt) }
			];
		case 'setup':
			return [
				{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
				{ label: 'Locale', value: campaign.defaultLocale },
				{ label: 'Status', value: humanizeValue(campaign.status) }
			];
	}
}

function toSetupPreparationChecklist(
	workspace: CampaignSeriesSetupWorkspaceResponse
): SetupPreparationChecklistItem[] {
	const configuredPolicies = [
		workspace.policies.consent,
		workspace.policies.retention,
		workspace.policies.disclosure
	].filter((policy) => policy.status === 'configured').length;
	const policyStatus = configuredPolicies === 3 ? 'ready' : 'blocked';
	const campaign = workspace.selectedCampaign ?? workspace.campaigns[0] ?? null;
	const readinessStatus = toPreparationReadinessStatus(workspace);

	return [
		{
			id: 'instrument_template',
			label: 'Instrument and template',
			status: workspace.template ? 'ready' : 'blocked',
			badgeLabel: workspace.template ? 'Ready' : 'Blocked',
			summary: workspace.template
				? `${workspace.template.templateName} / ${humanizeValue(workspace.template.status)} / ${formatCount(workspace.template.questionCount)} ${pluralize(workspace.template.questionCount, 'question', 'questions')}`
				: 'Missing template',
			guidance: workspace.template
				? 'Questionnaire is available for wave drafts.'
				: 'Save the questionnaire before results setup or wave drafts.',
			detailRows: workspace.template
				? [
						{ label: 'Template', value: workspace.template.templateName },
						{ label: 'Questions', value: formatCount(workspace.template.questionCount) }
					]
				: []
		},
		{
			id: 'scoring',
			label: 'Scoring',
			status: workspace.scoring ? 'ready' : 'blocked',
			badgeLabel: workspace.scoring ? 'Ready' : 'Blocked',
			summary: workspace.scoring
				? `${workspace.scoring.ruleKey} / ${humanizeValue(workspace.scoring.status)}`
				: 'Missing results setup',
			guidance: workspace.scoring
				? 'Result outputs are available for launch-readiness checks.'
				: 'Save results setup before launch-readiness checks.',
			detailRows: workspace.scoring
				? [
						{ label: 'Rule', value: workspace.scoring.ruleKey, mono: true }
					]
				: []
		},
		{
			id: 'policies',
			label: 'Policies',
			status: policyStatus,
			badgeLabel: policyStatus === 'ready' ? 'Ready' : 'Blocked',
			summary: `${configuredPolicies} of 3 policies configured`,
			guidance:
				policyStatus === 'ready'
					? 'Consent, retention, and disclosure policies are configured.'
					: toMissingPrerequisiteGuidance(
							workspace,
							'policy',
							'Configure missing policies before launch.'
						),
			detailRows: [
				{ label: 'Consent', value: sentenceCase(humanizeValue(workspace.policies.consent.status)) },
				{ label: 'Retention', value: sentenceCase(humanizeValue(workspace.policies.retention.status)) },
				{ label: 'Disclosure', value: sentenceCase(humanizeValue(workspace.policies.disclosure.status)) }
			]
		},
		{
			id: 'campaign',
			label: 'Wave draft',
			status: campaign ? 'ready' : 'blocked',
			badgeLabel: campaign ? 'Ready' : 'Blocked',
			summary: campaign
				? `${campaign.name.trim() || 'Untitled wave'} / ${humanizeValue(campaign.status)} / ${humanizeValue(campaign.responseIdentityMode)}`
				: 'Missing wave draft',
			guidance: campaign
				? 'Wave draft is ready for recipient setup and launch checks.'
				: 'Create a wave draft before checking launch readiness.',
			detailRows: campaign
				? [
						{ label: 'Selected wave', value: campaign.name.trim() || 'Untitled wave' },
						{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
						{ label: 'Locale', value: campaign.defaultLocale }
					]
				: []
		},
		{
			id: 'launch_readiness',
			label: 'Launch readiness',
			status: readinessStatus,
			badgeLabel: toPreparationBadgeLabel(readinessStatus),
			summary: workspace.readiness.ready
				? 'Launch readiness is ready'
				: `Launch readiness is ${humanizeValue(workspace.readiness.status)}`,
			guidance: workspace.readiness.ready
				? 'This study can move to collection when the campaign is launched.'
				: toMissingPrerequisiteGuidance(
						workspace,
						null,
						workspace.readiness.campaignId
							? 'Resolve the blocked launch-readiness requirements.'
							: 'Create a wave draft before checking launch readiness.'
					),
			detailRows: [
				{ label: 'Readiness', value: humanizeValue(workspace.readiness.status) },
				{
					label: 'Missing prerequisites',
					value: formatCount(workspace.summary.missingPrerequisiteCount)
				}
			]
		}
	];
}

function toSetupReadinessRow(workspace: CampaignSeriesSetupWorkspaceResponse) {
	const status = workspace.readiness.ready
		? 'ready'
		: toProductReadModelBadgeStatus(workspace.readiness.status);

	return {
		label: humanizeValue(workspace.readiness.status),
		status,
		badgeLabel: sentenceCase(humanizeValue(workspace.readiness.status))
	};
}

function toPreparationReadinessStatus(
	workspace: CampaignSeriesSetupWorkspaceResponse
): ProductReadModelBadgeStatus {
	if (workspace.readiness.ready) {
		return 'ready';
	}

	if (workspace.readiness.status === 'proof_only') {
		return 'proof_only';
	}

	if (workspace.readiness.status === 'not_available') {
		return 'not_available';
	}

	return 'blocked';
}

function toPreparationBadgeLabel(status: ProductReadModelBadgeStatus) {
	if (status === 'ready') {
		return 'Ready';
	}

	if (status === 'blocked') {
		return 'Blocked';
	}

	return sentenceCase(humanizeValue(status));
}

function toMissingPrerequisiteGuidance(
	workspace: CampaignSeriesSetupWorkspaceResponse,
	codeFragment: string | null,
	fallback: string
) {
	const rows = workspace.missingPrerequisites.filter((item) =>
		codeFragment ? item.code.includes(codeFragment) : true
	);

	if (rows.length === 0) {
		return fallback;
	}

	return rows.map((item) => `${item.label}: ${item.message}`).join(' ');
}

function toSetupPolicyRow(label: string, policy: CampaignSeriesSetupPolicyResponse) {
	const configured = policy.status === 'configured';
	const status = configured ? 'ready' : toProductReadModelBadgeStatus(policy.status);

	return {
		label,
		value: configured ? 'Configured' : sentenceCase(humanizeValue(policy.status)),
		status,
		badgeLabel: configured ? 'Configured' : sentenceCase(humanizeValue(policy.status)),
		details: (policy.details ?? []).map((detail) => ({
			label: detail.label,
			value: detail.value
		}))
	};
}

function toSetupCampaignDetailRows(campaign: CampaignSeriesSetupCampaignResponse): DisplayRow[] {
	return [
		{ label: 'Selected wave', value: campaign.name.trim() || 'Untitled wave' },
		{ label: 'Status', value: humanizeValue(campaign.status) },
		{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
		{ label: 'Locale', value: campaign.defaultLocale },
		{ label: 'Latest launch', value: formatCollectionDateTime(campaign.latestLaunchAt) }
	];
}

function toSetupCampaignSummaryRows(campaign: CampaignSeriesSetupCampaignResponse): DisplayRow[] {
	return [
		{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
		{ label: 'Locale', value: campaign.defaultLocale },
		{ label: 'Latest launch', value: formatCollectionDateTime(campaign.latestLaunchAt) }
	];
}

function toOperationsCampaignDetailRows(
	campaign: CampaignSeriesOperationsCampaignResponse
): DisplayRow[] {
	return [
		{ label: 'Selected wave', value: campaign.name.trim() || 'Untitled wave' },
		{ label: 'Status', value: humanizeValue(campaign.status) },
		{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
		{ label: 'Locale', value: campaign.defaultLocale },
		{ label: 'Collection started', value: formatCollectionDateTime(campaign.latestLaunchAt) },
		{ label: 'Closed', value: formatCollectionDateTime(campaign.closedAt) },
		{ label: 'Close reason', value: campaign.closeReason ?? 'Not available' },
		{ label: 'Started responses', value: formatCount(campaign.startedResponseCount) },
		{ label: 'Draft responses', value: formatCount(campaign.draftResponseCount) },
		{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
		{ label: 'Latest response activity', value: latestResponseActivity(campaign) },
		{ label: 'Collection status', value: humanizeValue(campaign.collectionStatus) },
		{ label: 'Report visibility', value: humanizeValue(campaign.reportVisibilityStatus) },
		{
			label: 'Score coverage',
			value: humanizeValue(campaign.scoreCoverageStatus ?? 'no_submissions')
		},
		{ label: 'Scored submitted', value: formatCount(campaign.scoredSubmittedResponseCount ?? 0) },
		{
			label: 'Unscored submitted',
			value: formatCount(campaign.unscoredSubmittedResponseCount ?? 0)
		},
		{
			label: 'Not configured submitted',
			value: formatCount(campaign.notConfiguredSubmittedResponseCount ?? 0)
		},
		{
			label: 'Latest scoring activity',
			value: formatCollectionDateTime(campaign.latestScoringActivityAt)
		},
		{ label: 'Respondent links', value: formatCount(campaign.openLinkAssignmentCount) },
		{ label: 'Sent emails', value: formatCount(campaign.sentInvitationCount) },
		{
			label: 'Latest email activity',
			value: formatCollectionDateTime(campaign.latestDeliveryAttemptAt)
		}
	];
}

function toOperationsCampaignSummaryRows(
	campaign: CampaignSeriesOperationsCampaignResponse
): DisplayRow[] {
	return [
		{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
		{ label: 'Locale', value: campaign.defaultLocale },
		{ label: 'Collection started', value: formatCollectionDateTime(campaign.latestLaunchAt) },
		{ label: 'Closed', value: formatCollectionDateTime(campaign.closedAt) },
		{ label: 'Respondent links', value: formatCount(campaign.openLinkAssignmentCount) },
		{ label: 'Queued emails', value: formatCount(campaign.queuedInvitationCount) },
		{ label: 'Sent emails', value: formatCount(campaign.sentInvitationCount) },
		{ label: 'Failed emails', value: formatCount(campaign.failedInvitationCount) },
		{ label: 'Suppressed emails', value: formatCount(campaign.bouncedInvitationCount ?? 0) },
		{
			label: 'Latest email activity',
			value: formatCollectionDateTime(campaign.latestDeliveryAttemptAt)
		},
		{ label: 'Started responses', value: formatCount(campaign.startedResponseCount) },
		{ label: 'Draft responses', value: formatCount(campaign.draftResponseCount) },
		{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
		{ label: 'Latest response activity', value: latestResponseActivity(campaign) },
		{
			label: 'Score coverage',
			value: humanizeValue(campaign.scoreCoverageStatus ?? 'no_submissions')
		},
		{
			label: 'Unscored submitted',
			value: formatCount(campaign.unscoredSubmittedResponseCount ?? 0)
		}
	];
}

function toOperationsLaunchSnapshotRows(
	snapshot: CampaignSeriesOperationsLaunchSnapshotResponse
): DisplayRow[] {
	return [
		{ label: 'Frozen identity mode', value: humanizeValue(snapshot.responseIdentityMode) },
		{ label: 'Frozen locale', value: snapshot.defaultLocale },
		{ label: 'Template questions', value: formatCount(snapshot.templateQuestionCount) },
		{ label: 'Launched at', value: formatCollectionDateTime(snapshot.launchedAt) }
	];
}

function latestWorkspaceResponseActivity(workspace: CampaignSeriesOperationsWorkspaceResponse) {
	return latestResponseTimestamp(
		workspace.summary.latestResponseStartedAt,
		workspace.summary.latestResponseSubmittedAt
	);
}

function normalizeOperationsScoreCoverage(
	scoreCoverage: CampaignSeriesScoreCoverageResponse | null | undefined
): CampaignSeriesScoreCoverageResponse {
	return (
		scoreCoverage ?? {
			submittedResponseCount: 0,
			scoredSubmittedResponseCount: 0,
			unscoredSubmittedResponseCount: 0,
			notConfiguredSubmittedResponseCount: 0,
			campaignsWithScoringRuleCount: 0,
			campaignsWithoutScoringRuleCount: 0,
			latestScoringActivityAt: null,
			status: 'no_submissions',
			guidance: 'No submitted responses are available for score coverage yet.'
		}
	);
}

function toScoreCoverageMonitor(
	scoreCoverage: CampaignSeriesScoreCoverageResponse | null | undefined,
	includeCampaignCounts: boolean,
	locale: AppLocale = 'en'
) {
	const coverage = normalizeOperationsScoreCoverage(scoreCoverage);
	const summaryRows: DisplayRow[] = [
		{
			label: operationsReadModelMessage(locale, 'operations.readModel.row.submittedResponses'),
			value: formatCount(coverage.submittedResponseCount)
		},
		{
			label: operationsReadModelMessage(locale, 'operations.readModel.row.scoredSubmitted'),
			value: formatCount(coverage.scoredSubmittedResponseCount)
		},
		{
			label: operationsReadModelMessage(locale, 'operations.readModel.row.unscoredSubmitted'),
			value: formatCount(coverage.unscoredSubmittedResponseCount)
		},
		{
			label: operationsReadModelMessage(locale, 'operations.readModel.row.notConfigured'),
			value: formatCount(coverage.notConfiguredSubmittedResponseCount)
		}
	];

	if (includeCampaignCounts) {
		summaryRows.push(
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.campaignsWithScoring'),
				value: formatCount(coverage.campaignsWithScoringRuleCount)
			},
			{
				label: operationsReadModelMessage(
					locale,
					'operations.readModel.row.campaignsWithoutScoring'
				),
				value: formatCount(coverage.campaignsWithoutScoringRuleCount)
			}
		);
	}

	summaryRows.push({
		label: operationsReadModelMessage(locale, 'operations.readModel.row.latestScoringActivity'),
		value: formatCollectionDateTime(coverage.latestScoringActivityAt)
	});

	return {
		title: operationsReadModelMessage(locale, 'operations.readModel.scoreCoverage.title'),
		status: coverage.status,
		guidance: localizedScoreCoverageGuidance(coverage.guidance, coverage.status, locale),
		summaryRows
	};
}

function latestResponseActivity(campaign: CampaignSeriesOperationsCampaignResponse) {
	return latestResponseTimestamp(
		campaign.latestResponseStartedAt,
		campaign.latestResponseSubmittedAt
	);
}

function latestResponseTimestamp(startedAt: string | null, submittedAt: string | null) {
	if (!startedAt) {
		return formatCollectionDateTime(submittedAt);
	}

	if (!submittedAt) {
		return formatCollectionDateTime(startedAt);
	}

	return formatCollectionDateTime(Date.parse(startedAt) > Date.parse(submittedAt) ? startedAt : submittedAt);
}

function operationLaunchSnapshotRow(
	campaign: CampaignSeriesOperationsCampaignResponse
): DisplayRow {
	return campaign.latestLaunchSnapshotId
		? { label: 'Launch snapshot', value: campaign.latestLaunchSnapshotId, mono: true }
		: { label: 'Launch snapshot', value: 'Not available' };
}

function operationScoringRuleRow(campaign: CampaignSeriesOperationsCampaignResponse): DisplayRow {
	return campaign.scoringRuleId
		? { label: 'Scoring rule', value: campaign.scoringRuleId, mono: true }
		: { label: 'Scoring rule', value: 'Not available' };
}

function toOperationsCollectionOverview(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	scoreCoverage: CampaignSeriesScoreCoverageResponse,
	locale: AppLocale = 'en'
): OperationsCollectionOverviewItem[] {
	return [
		toOperationsCollectionStateItem(workspace, locale),
		toOperationsRespondentAccessItem(workspace, locale),
		toOperationsResponseProgressItem(workspace, locale),
		toOperationsScoreReadinessItem(workspace, scoreCoverage, locale)
	];
}

function toOperationsCollectionStateItem(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	locale: AppLocale = 'en'
): OperationsCollectionOverviewItem {
	const campaign = workspace.selectedCampaign;

	if (!campaign) {
		return {
			id: 'collection_state',
			label: operationsReadModelMessage(locale, 'operations.readModel.collectionState.emptyLabel'),
			status: 'blocked',
			badgeLabel: operationsReadModelMessage(
				locale,
				'operations.readModel.collectionState.blockedBadge'
			),
			summary: operationsReadModelMessage(
				locale,
				'operations.readModel.collectionState.noSelectedSummary'
			),
			guidance: operationsReadModelMessage(
				locale,
				'operations.readModel.collectionState.noSelectedGuidance'
			),
			detailRows: [
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.selectedWave'),
					value: operationsReadModelMessage(locale, 'operations.readModel.value.missing')
				},
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.status'),
					value: localizedCollectionStatusLabel(workspace.summary.collectionStatus, locale)
				},
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.collectionStarted'),
					value: appMessage(locale, 'operations.status.reportVisibility.notAvailable')
				},
				{
					label: operationsReadModelMessage(locale, 'operations.readModel.row.missingPrerequisites'),
					value: formatCount(workspace.summary.missingPrerequisiteCount)
				}
			]
		};
	}

	const status = toProductReadModelBadgeStatus(campaign.status);

	return {
		id: 'collection_state',
		label: operationsReadModelMessage(locale, 'operations.readModel.collectionState.label'),
		status,
		badgeLabel: sentenceCase(localizedCollectionStatusLabel(campaign.status, locale)),
		summary: operationsReadModelMessage(locale, 'operations.readModel.collectionState.selectedSummary', {
			campaignName: campaign.name.trim() || 'Untitled wave',
			statusLabel: localizedCollectionStatusLabel(campaign.status, locale)
		}),
		guidance: localizedCollectionGuidance(
			campaign.collectionGuidance || workspace.summary.collectionGuidance,
			locale
		),
		detailRows: [
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.selectedWave'),
				value: campaign.name.trim() || 'Untitled wave'
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.status'),
				value: localizedCollectionStatusLabel(campaign.status, locale)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.collectionStarted'),
				value: formatCollectionDateTime(campaign.latestLaunchAt)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.missingPrerequisites'),
				value: formatCount(workspace.summary.missingPrerequisiteCount)
			}
		]
	};
}

function toOperationsRespondentAccessItem(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	locale: AppLocale = 'en'
): OperationsCollectionOverviewItem {
	const campaign = workspace.selectedCampaign;
	const openLinkAssignments =
		campaign?.openLinkAssignmentCount ?? workspace.summary.openLinkAssignmentCount;
	const queuedInvitations =
		campaign?.queuedInvitationCount ?? workspace.summary.queuedInvitationCount;
	const sentInvitations = campaign?.sentInvitationCount ?? workspace.summary.sentInvitationCount;
	const failedInvitations =
		campaign?.failedInvitationCount ?? workspace.summary.failedInvitationCount;
	const bouncedInvitations =
		campaign?.bouncedInvitationCount ?? workspace.summary.bouncedInvitationCount ?? 0;
	const deliveryAttempts = campaign?.deliveryAttemptCount ?? workspace.summary.deliveryAttemptCount;
	const latestDeliveryAttempt = campaign?.latestDeliveryAttemptAt ?? null;
	const hasAccess = openLinkAssignments > 0 || sentInvitations > 0 || deliveryAttempts > 0;
	const status: ProductReadModelBadgeStatus = hasAccess
		? 'ready'
		: queuedInvitations > 0
			? 'pending'
			: 'blocked';

	return {
		id: 'respondent_access',
		label: operationsReadModelMessage(locale, 'operations.readModel.respondentAccess.label'),
		status,
		badgeLabel:
			status === 'ready'
				? operationsReadModelMessage(locale, 'operations.readModel.respondentAccess.badge.ready')
				: status === 'pending'
					? operationsReadModelMessage(locale, 'operations.readModel.respondentAccess.badge.pending')
					: operationsReadModelMessage(locale, 'operations.readModel.collectionState.blockedBadge'),
		summary: operationsReadModelMessage(locale, 'operations.readModel.respondentAccess.summary', {
			openLinkSummary: formatOpenLinkAccessCount(openLinkAssignments, locale),
			sentEmailSummary: formatSentEmailAccessCount(sentInvitations, locale),
			sentInvitations
		}),
		guidance: toRespondentAccessGuidance(
			openLinkAssignments,
			sentInvitations,
			deliveryAttempts,
			locale
		),
		detailRows: [
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.identityMode'),
				value: campaign
					? localizedIdentityModeLabel(campaign.responseIdentityMode, locale)
					: operationsReadModelMessage(locale, 'operations.readModel.value.missing')
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.respondentLinks'),
				value: formatCount(openLinkAssignments)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.queuedEmails'),
				value: formatCount(queuedInvitations)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.sentEmails'),
				value: formatCount(sentInvitations)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.failedEmails'),
				value: formatCount(failedInvitations)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.suppressedEmails'),
				value: formatCount(bouncedInvitations)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.latestEmailActivity'),
				value: formatCollectionDateTime(latestDeliveryAttempt)
			}
		]
	};
}

function toOperationsResponseProgressItem(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	locale: AppLocale = 'en'
): OperationsCollectionOverviewItem {
	const submittedResponses = workspace.summary.submittedResponseCount;
	const startedResponses = workspace.summary.startedResponseCount;
	const draftResponses = workspace.summary.draftResponseCount;
	const status: ProductReadModelBadgeStatus =
		submittedResponses > 0
			? 'ready'
			: startedResponses > 0 || draftResponses > 0
				? 'pending'
				: 'empty';

	return {
		id: 'response_progress',
		label: operationsReadModelMessage(locale, 'operations.readModel.responseProgress.label'),
		status,
		badgeLabel:
			submittedResponses > 0
				? operationsReadModelMessage(
						locale,
						'operations.readModel.responseProgress.badge.withSubmissions',
						{ submitted: submittedResponses }
					)
				: operationsReadModelMessage(
						locale,
						'operations.readModel.responseProgress.badge.empty'
					),
		summary: operationsReadModelMessage(locale, 'operations.readModel.responseProgress.summary', {
			started: startedResponses,
			drafts: draftResponses,
			submitted: submittedResponses
		}),
		guidance: localizedCollectionGuidance(workspace.summary.collectionGuidance, locale),
		detailRows: [
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.startedResponses'),
				value: formatCount(startedResponses)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.draftResponses'),
				value: formatCount(draftResponses)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.submittedResponses'),
				value: formatCount(submittedResponses)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.latestStarted'),
				value: formatCollectionDateTime(workspace.summary.latestResponseStartedAt)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.latestSubmitted'),
				value: formatCollectionDateTime(workspace.summary.latestResponseSubmittedAt)
			}
		]
	};
}

function toOperationsScoreReadinessItem(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	scoreCoverage: CampaignSeriesScoreCoverageResponse,
	locale: AppLocale = 'en'
): OperationsCollectionOverviewItem {
	const submittedResponses = scoreCoverage.submittedResponseCount;
	const status = toOperationsScoreReadinessStatus(scoreCoverage);

	return {
		id: 'score_readiness',
		label: operationsReadModelMessage(locale, 'operations.readModel.scoreReadiness.label'),
		status,
		badgeLabel: toOperationsScoreReadinessBadgeLabel(scoreCoverage, status, locale),
		summary:
			submittedResponses > 0
				? operationsReadModelMessage(
						locale,
						'operations.readModel.scoreReadiness.summary.withSubmissions',
						{
							scored: scoreCoverage.scoredSubmittedResponseCount,
							submitted: submittedResponses
						}
					)
				: operationsReadModelMessage(
						locale,
						'operations.readModel.scoreReadiness.summary.empty'
					),
		guidance: localizedScoreCoverageGuidance(scoreCoverage.guidance, scoreCoverage.status, locale),
		detailRows: [
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.reportVisibility'),
				value: localizedReportVisibilityStatus(workspace.summary.reportVisibilityStatus, locale)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.scoreCoverage'),
				value: localizedScoreCoverageStatusLabel(scoreCoverage.status, locale)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.scoredSubmitted'),
				value: formatCount(scoreCoverage.scoredSubmittedResponseCount)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.unscoredSubmitted'),
				value: formatCount(scoreCoverage.unscoredSubmittedResponseCount)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.notConfigured'),
				value: formatCount(scoreCoverage.notConfiguredSubmittedResponseCount)
			},
			{
				label: operationsReadModelMessage(locale, 'operations.readModel.row.latestScoringActivity'),
				value: formatCollectionDateTime(scoreCoverage.latestScoringActivityAt)
			}
		]
	};
}

function toOperationsScoreReadinessStatus(
	scoreCoverage: CampaignSeriesScoreCoverageResponse
): ProductReadModelBadgeStatus {
	if (scoreCoverage.status === 'complete') {
		return 'ready';
	}

	if (scoreCoverage.status === 'not_configured') {
		return 'not_configured';
	}

	if (scoreCoverage.status === 'no_submissions') {
		return 'empty';
	}

	if (
		scoreCoverage.unscoredSubmittedResponseCount > 0 ||
		scoreCoverage.notConfiguredSubmittedResponseCount > 0
	) {
		return 'pending';
	}

	return toProductReadModelBadgeStatus(scoreCoverage.status);
}

function toOperationsScoreReadinessBadgeLabel(
	scoreCoverage: CampaignSeriesScoreCoverageResponse,
	status: ProductReadModelBadgeStatus,
	locale: AppLocale = 'en'
) {
	if (scoreCoverage.status === 'complete') {
		return operationsReadModelMessage(locale, 'operations.readModel.scoreReadiness.badge.ready');
	}

	if (scoreCoverage.status === 'no_submissions') {
		return operationsReadModelMessage(locale, 'operations.readModel.scoreReadiness.badge.empty');
	}

	if (status === 'not_configured') {
		return operationsReadModelMessage(
			locale,
			'operations.readModel.scoreReadiness.badge.notConfigured'
		);
	}

	return sentenceCase(localizedScoreCoverageStatusLabel(scoreCoverage.status, locale));
}

function toRespondentAccessGuidance(
	openLinkAssignments: number,
	sentInvitations: number,
	deliveryAttempts: number,
	locale: AppLocale = 'en'
) {
	if (openLinkAssignments > 0 && (sentInvitations > 0 || deliveryAttempts > 0)) {
		return operationsReadModelMessage(locale, 'operations.readModel.respondentAccess.guidance.mixed');
	}

	if (openLinkAssignments > 0) {
		return operationsReadModelMessage(locale, 'operations.readModel.respondentAccess.guidance.openLink');
	}

	if (sentInvitations > 0 || deliveryAttempts > 0) {
		return operationsReadModelMessage(locale, 'operations.readModel.respondentAccess.guidance.email');
	}

	return operationsReadModelMessage(locale, 'operations.readModel.respondentAccess.guidance.none');
}

function operationsReadModelMessage(
	locale: AppLocale,
	id: AppMessageId,
	values: AppMessageValues = {}
) {
	return appMessage(locale, id, values);
}

function localizedCollectionGuidance(value: string, locale: AppLocale) {
	switch (value) {
		case 'Enough submitted responses exist for aggregate report visibility.':
			return operationsReadModelMessage(
				locale,
				'operations.readModel.guidance.collection.readyForAggregateReport'
			);
		case 'Report visibility readiness is unknown because disclosure policy is missing.':
			return operationsReadModelMessage(
				locale,
				'operations.readModel.guidance.collection.unknownDisclosure'
			);
		default:
			return value;
	}
}

function localizedScoreCoverageGuidance(
	value: string,
	status: string | null | undefined,
	locale: AppLocale
) {
	switch (value) {
		case 'All submitted responses have successful scoring activity.':
			return operationsReadModelMessage(locale, 'operations.readModel.guidance.score.complete');
		case 'Some submitted responses still need scoring activity before score-dependent reports are complete.':
			return operationsReadModelMessage(locale, 'operations.readModel.guidance.score.partial');
		case 'Submitted responses exist, but scoring is not configured for those campaigns.':
			return operationsReadModelMessage(locale, 'operations.readModel.guidance.score.notConfigured');
		case 'No submitted responses are available for score coverage yet.':
			return operationsReadModelMessage(locale, 'operations.readModel.guidance.score.noSubmissions');
		default:
			if (status === 'complete') {
				return operationsReadModelMessage(locale, 'operations.readModel.guidance.score.complete');
			}
			if (status === 'partial') {
				return operationsReadModelMessage(locale, 'operations.readModel.guidance.score.partial');
			}
			if (status === 'not_configured') {
				return operationsReadModelMessage(locale, 'operations.readModel.guidance.score.notConfigured');
			}
			if (status === 'no_submissions') {
				return operationsReadModelMessage(locale, 'operations.readModel.guidance.score.noSubmissions');
			}
			return value;
	}
}

function localizedCollectionStatusLabel(value: string | null | undefined, locale: AppLocale) {
	switch (value) {
		case 'live':
			return operationsReadModelMessage(locale, 'operations.readModel.status.live');
		case 'draft':
			return operationsReadModelMessage(locale, 'operations.readModel.status.draft');
		case 'closed':
			return operationsReadModelMessage(locale, 'operations.readModel.status.closed');
		default:
			return value ? humanizeValue(value) : appMessage(locale, 'operations.status.reportVisibility.notAvailable');
	}
}

function localizedScoreCoverageStatusLabel(value: string | null | undefined, locale: AppLocale) {
	switch (value) {
		case 'complete':
			return operationsReadModelMessage(locale, 'operations.readModel.status.complete');
		case 'partial':
			return operationsReadModelMessage(locale, 'operations.readModel.status.partial');
		case 'no_submissions':
			return operationsReadModelMessage(locale, 'operations.readModel.status.noSubmissions');
		case 'not_configured':
			return operationsReadModelMessage(locale, 'operations.readModel.status.notConfigured');
		default:
			return value ? humanizeValue(value) : appMessage(locale, 'operations.status.reportVisibility.notAvailable');
	}
}

function localizedReportVisibilityStatus(value: string | null | undefined, locale: AppLocale) {
	switch (value) {
		case 'ready_for_aggregate_report':
			return locale === 'en'
				? humanizeValue(value)
				: appMessage(locale, 'operations.status.reportVisibility.reportable');
		case 'reportable':
			return appMessage(locale, 'operations.status.reportVisibility.reportable');
		case 'visible':
			return appMessage(locale, 'operations.status.reportVisibility.visible');
		case 'blocked':
			return appMessage(locale, 'operations.status.reportVisibility.blocked');
		case 'unknown_policy':
			return appMessage(locale, 'operations.status.reportVisibility.unknownPolicy');
		default:
			return value ? humanizeValue(value) : appMessage(locale, 'operations.status.reportVisibility.notAvailable');
	}
}

function localizedIdentityModeLabel(value: string, locale: AppLocale) {
	switch (value) {
		case 'anonymous':
			return appMessage(locale, 'readModel.identity.anonymous');
		case 'anonymous_longitudinal':
			return appMessage(locale, 'readModel.identity.anonymousLongitudinal');
		case 'identified':
			return appMessage(locale, 'readModel.identity.identified');
		default:
			return humanizeValue(value);
	}
}

function formatOpenLinkAccessCount(count: number, locale: AppLocale) {
	if (locale === 'en') {
		return formatAccessCount(count, 'respondent link');
	}

	return formatLocalizedCount(locale, count, 'openRespondentLink');
}

function formatSentEmailAccessCount(count: number, locale: AppLocale) {
	if (locale === 'en') {
		return formatAccessCount(count, 'sent email');
	}

	return formatLocalizedCount(locale, count, 'sentEmail');
}

function formatCollectionDateTime(value: string | null | undefined) {
	if (!value) {
		return 'Not available';
	}

	const date = new Date(normalizeTimestampForDate(value));
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

function formatAccessCount(count: number, singular: string) {
	return `${formatCount(count)} ${pluralize(count, singular, `${singular}s`)}`;
}

function formatSentInvitationCount(count: number) {
	return `${formatCount(count)} sent ${pluralize(count, 'invitation', 'invitations')}`;
}

function toReportsResultsOverview(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	scoreCoverage: CampaignSeriesScoreCoverageResponse
): ReportsResultsOverviewItem[] {
	return [
		toReportsResultAvailabilityItem(workspace),
		toReportsCoverageVisibilityItem(workspace, scoreCoverage),
		toReportsLimitationsFinalityItem(workspace),
		toReportsExportNextUseItem(workspace)
	];
}

function toReportsResultAvailabilityItem(
	workspace: CampaignSeriesReportsWorkspaceResponse
): ReportsResultsOverviewItem {
	const campaign = workspace.selectedCampaign;

	if (!campaign) {
		return {
			id: 'result_availability',
			label: 'Result availability',
			status: 'blocked',
			badgeLabel: 'Blocked',
			summary: 'No selected campaign has reportable results.',
			guidance: 'Submit responses and compute scores before reviewing findings.',
			detailRows: [
				{ label: 'Selected wave', value: 'Missing' },
				{ label: 'Report status', value: 'blocked' },
				{
					label: 'Reportable campaigns',
					value: formatCount(workspace.summary.reportableCampaignCount)
				},
				{
					label: 'Submitted responses',
					value: formatCount(workspace.summary.submittedResponseCount)
				},
				{
					label: 'Missing prerequisites',
					value: formatCount(workspace.summary.missingPrerequisiteCount)
				}
			]
		};
	}

	const reportStatus = humanizeValue(campaign.reportStatus);

	return {
		id: 'result_availability',
		label: 'Result availability',
		status: toProductReadModelBadgeStatus(campaign.reportStatus),
		badgeLabel: sentenceCase(reportStatus),
		summary: `${campaign.name.trim() || 'Untitled wave'} has ${reportStatus} results from ${formatCount(campaign.submittedResponseCount)} submitted responses.`,
		guidance:
			'Use this as a preview of current findings until scoring coverage, disclosure, and finality are complete.',
		detailRows: [
			{ label: 'Selected wave', value: campaign.name.trim() || 'Untitled wave' },
			{ label: 'Report status', value: reportStatus },
			{
				label: 'Reportable campaigns',
				value: formatCount(workspace.summary.reportableCampaignCount)
			},
			{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
			{
				label: 'Missing prerequisites',
				value: formatCount(workspace.summary.missingPrerequisiteCount)
			}
		]
	};
}

function toReportsCoverageVisibilityItem(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	scoreCoverage: CampaignSeriesScoreCoverageResponse
): ReportsResultsOverviewItem {
	const campaign = workspace.selectedCampaign;
	const submittedResponses = scoreCoverage.submittedResponseCount;
	const visibleScores = campaign?.visibleScoreCount ?? workspace.summary.visibleScoreCount;
	const suppressedScores = campaign?.suppressedScoreCount ?? workspace.summary.suppressedScoreCount;
	const unscoredResponses = scoreCoverage.unscoredSubmittedResponseCount;
	const status = toReportsCoverageStatus(scoreCoverage);

	return {
		id: 'coverage_visibility',
		label: 'Coverage and visibility',
		status,
		badgeLabel:
			submittedResponses > 0
				? `${formatCount(scoreCoverage.scoredSubmittedResponseCount)} of ${formatCount(submittedResponses)} scored`
				: 'No scores',
		summary: `${formatCount(visibleScores)} visible scores, ${formatCount(suppressedScores)} suppressed scores, ${formatCount(unscoredResponses)} submitted responses still unscored.`,
		guidance:
			submittedResponses > 0
				? 'Review visible score coverage before treating the result set as complete.'
				: 'Collect submitted responses and compute scores before assessing coverage.',
		detailRows: [
			{ label: 'Submitted responses', value: formatCount(submittedResponses) },
			{ label: 'Scored submitted', value: formatCount(scoreCoverage.scoredSubmittedResponseCount) },
			{ label: 'Unscored submitted', value: formatCount(unscoredResponses) },
			{ label: 'Visible scores', value: formatCount(visibleScores) },
			{ label: 'Suppressed scores', value: formatCount(suppressedScores) },
			{ label: 'Disclosure', value: humanizeValue(campaign?.disclosureState ?? 'not_available') },
			{ label: 'Disclosure k', value: String(campaign?.disclosureKMin ?? 'Not configured') }
		]
	};
}

function toReportsCoverageStatus(
	scoreCoverage: CampaignSeriesScoreCoverageResponse
): ProductReadModelBadgeStatus {
	if (scoreCoverage.submittedResponseCount === 0 || scoreCoverage.status === 'no_submissions') {
		return 'empty';
	}

	if (
		scoreCoverage.unscoredSubmittedResponseCount > 0 ||
		scoreCoverage.notConfiguredSubmittedResponseCount > 0 ||
		scoreCoverage.status === 'partial'
	) {
		return 'pending';
	}

	if (scoreCoverage.status === 'complete') {
		return 'ready';
	}

	return toProductReadModelBadgeStatus(scoreCoverage.status);
}

function toReportsLimitationsFinalityItem(
	workspace: CampaignSeriesReportsWorkspaceResponse
): ReportsResultsOverviewItem {
	const campaign = workspace.selectedCampaign;

	if (!campaign) {
		return {
			id: 'limitations_finality',
			label: 'Limitations and finality',
			status: 'blocked',
			badgeLabel: 'No finality',
			summary: 'No selected campaign has final report state yet.',
			guidance: 'Launch, collect, score, and close a campaign before relying on report finality.',
			detailRows: [
				{ label: 'Campaign status', value: 'Missing' },
				{ label: 'Data finality', value: 'not reportable' },
				{
					label: 'Preliminary live reports',
					value: formatCount(workspace.summary.preliminaryLiveReportCount ?? 0)
				},
				{
					label: 'Closed-wave reports',
					value: formatCount(workspace.summary.closedWaveReportCount ?? 0)
				},
				{ label: 'Interpretation', value: 'not available' }
			]
		};
	}

	const dataFinality = toReportsDataFinality(campaign);
	const preliminaryLiveReports =
		workspace.summary.preliminaryLiveReportCount ??
		(campaign.status === 'live' || dataFinality === 'preliminary_live' ? 1 : 0);
	const closedWaveReports =
		workspace.summary.closedWaveReportCount ?? (dataFinality === 'closed_wave' ? 1 : 0);
	const status = toProductReadModelBadgeStatus(campaign.status);
	const campaignStatus = humanizeValue(campaign.status);
	const interpretationStatus = humanizeValue(campaign.interpretationStatus);
	const isClosedWave = dataFinality === 'closed_wave' || campaign.status === 'closed';

	return {
		id: 'limitations_finality',
		label: 'Limitations and finality',
		status,
		badgeLabel: isClosedWave
			? 'Closed wave'
			: campaign.status === 'live'
				? 'Live data'
				: sentenceCase(campaignStatus),
		summary: isClosedWave
			? `Results are from a ${campaignStatus} campaign with ${interpretationStatus} and ${formatCount(closedWaveReports)} ${pluralize(closedWaveReports, 'closed-wave report', 'closed-wave reports')}.`
			: `Results are from a ${campaignStatus} campaign with ${interpretationStatus} and no closed-wave finality yet.`,
		guidance: isClosedWave
			? 'Closed-wave data is more stable, but interpretation labels still need the recorded validation posture.'
			: 'Label this as preliminary until the wave is closed and interpretation posture is reviewed.',
		detailRows: [
			{ label: 'Campaign status', value: campaignStatus },
			{ label: 'Data finality', value: humanizeValue(dataFinality) },
			{ label: 'Preliminary live reports', value: formatCount(preliminaryLiveReports) },
			{ label: 'Closed-wave reports', value: formatCount(closedWaveReports) },
			{ label: 'Interpretation', value: interpretationStatus }
		]
	};
}

function toReportsDataFinality(campaign: CampaignSeriesReportsCampaignResponse) {
	if (campaign.dataFinality) {
		return campaign.dataFinality;
	}

	if (campaign.status === 'closed') {
		return 'closed_wave';
	}

	if (campaign.status === 'live') {
		return 'preliminary_live';
	}

	return 'not_reportable';
}

function toReportsExportNextUseItem(
	workspace: CampaignSeriesReportsWorkspaceResponse
): ReportsResultsOverviewItem {
	const campaign = workspace.selectedCampaign;
	const exportArtifactCount =
		campaign?.exportArtifactCount ?? workspace.summary.exportArtifactCount;
	const latestExportFile = campaign?.latestExportArtifactFileName ?? null;
	const latestExportStatus = campaign?.latestExportArtifactStatus ?? null;
	const canDownload = Boolean(campaign?.latestExportArtifactCanDownload);

	if (!campaign || exportArtifactCount === 0) {
		return {
			id: 'export_next_use',
			label: 'Export next use',
			status: 'blocked',
			badgeLabel: 'No exports',
			summary: 'No report export file is available yet.',
			guidance: 'Create an export after report results become available.',
			detailRows: [
				{ label: 'Export files', value: formatCount(exportArtifactCount) },
				{ label: 'Latest export file', value: latestExportFile ?? 'Not available' },
				{ label: 'Latest export status', value: latestExportStatus ?? 'Not available' },
				{ label: 'Latest export downloadable', value: canDownload ? 'Yes' : 'No' }
			]
		};
	}

	return {
		id: 'export_next_use',
		label: 'Export next use',
		status: canDownload ? 'ready' : 'pending',
		badgeLabel: `${formatCount(exportArtifactCount)} ${pluralize(exportArtifactCount, 'file', 'files')}`,
		summary: canDownload
			? `Latest export ${latestExportFile ?? 'Not available'} is downloadable.`
			: `Latest export ${latestExportFile ?? 'Not available'} is not downloadable yet.`,
		guidance:
			'Download the latest export file for handoff, or create a fresh export after results change.',
		detailRows: [
			{ label: 'Export files', value: formatCount(exportArtifactCount) },
			{ label: 'Latest export file', value: latestExportFile ?? 'Not available' },
			{ label: 'Latest export status', value: latestExportStatus ?? 'Not available' },
			{ label: 'Latest export downloadable', value: canDownload ? 'Yes' : 'No' }
		]
	};
}

function toReportsCampaignDetailRows(
	campaign: CampaignSeriesReportsCampaignResponse
): DisplayRow[] {
	return [
		{ label: 'Selected wave', value: campaign.name.trim() || 'Untitled wave' },
		{ label: 'Status', value: humanizeValue(campaign.status) },
		{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
		{ label: 'Locale', value: campaign.defaultLocale },
		{ label: 'Disclosure', value: humanizeValue(campaign.disclosureState) },
		{ label: 'Disclosure k', value: String(campaign.disclosureKMin ?? 'Not configured') },
		{ label: 'Report status', value: humanizeValue(campaign.reportStatus) },
		...optionalDataFinalityRow('Data finality', campaign.dataFinality),
		{ label: 'Interpretation', value: humanizeValue(campaign.interpretationStatus) },
		{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
		{ label: 'Scores', value: formatCount(campaign.scoreCount) },
		{ label: 'Visible scores', value: formatCount(campaign.visibleScoreCount) },
		{ label: 'Suppressed scores', value: formatCount(campaign.suppressedScoreCount) },
		{ label: 'Export files', value: formatCount(campaign.exportArtifactCount) }
	];
}

function toReportsCampaignProvenanceRows(
	campaign: CampaignSeriesReportsCampaignResponse
): DisplayRow[] {
	return [
		reportsIdRow('Launch snapshot', campaign.latestLaunchSnapshotId),
		{ label: 'Latest launch', value: formatCollectionDateTime(campaign.latestLaunchAt) },
		reportsIdRow('Scoring rule', campaign.scoringRuleId),
		reportsIdRow('Consent document', campaign.consentDocumentId),
		reportsIdRow('Retention policy', campaign.retentionPolicyId),
		reportsIdRow('Disclosure policy', campaign.disclosurePolicyId),
		...optionalDateTimeRow('Closed at', campaign.closedAt),
		...optionalIdRow('Closed by', campaign.closedByUserId),
		...optionalTextRow('Close reason', campaign.closeReason),
		reportsIdRow('Latest export record', campaign.latestExportArtifactId),
		{
			label: 'Latest export file',
			value: campaign.latestExportArtifactFileName ?? 'Not available'
		},
		{
			label: 'Latest export status',
			value: campaign.latestExportArtifactStatus ?? 'Not available'
		},
		{
			label: 'Latest export created',
			value: formatNullableDateTime(campaign.latestExportArtifactCreatedAt)
		},
		{
			label: 'Latest export completed',
			value: formatNullableDateTime(campaign.latestExportArtifactCompletedAt)
		},
		{
			label: 'Latest export started',
			value: formatNullableDateTime(campaign.latestExportArtifactStartedAt)
		},
		{
			label: 'Latest export failed',
			value: formatNullableDateTime(campaign.latestExportArtifactFailedAt)
		},
		{
			label: 'Latest export expires',
			value: formatNullableDateTime(campaign.latestExportArtifactExpiresAt)
		},
		{
			label: 'Latest export deleted',
			value: formatNullableDateTime(campaign.latestExportArtifactDeletedAt)
		},
		{
			label: 'Latest export failure reason',
			value: campaign.latestExportArtifactFailureReasonCode ?? 'Not available'
		},
		{
			label: 'Latest export downloadable',
			value: campaign.latestExportArtifactCanDownload ? 'Yes' : 'No'
		}
	];
}

function toReportsCampaignSummaryRows(
	campaign: CampaignSeriesReportsCampaignResponse
): DisplayRow[] {
	return [
		{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
		{ label: 'Locale', value: campaign.defaultLocale },
		reportsIdRow('Launch snapshot', campaign.latestLaunchSnapshotId),
		{ label: 'Latest launch', value: formatCollectionDateTime(campaign.latestLaunchAt) },
		{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
		{ label: 'Scores', value: formatCount(campaign.scoreCount) },
		{ label: 'Visible scores', value: formatCount(campaign.visibleScoreCount) },
		{ label: 'Suppressed scores', value: formatCount(campaign.suppressedScoreCount) },
		{ label: 'Disclosure', value: humanizeValue(campaign.disclosureState) },
		{ label: 'Report status', value: humanizeValue(campaign.reportStatus) },
		...optionalDataFinalityRow('Data finality', campaign.dataFinality),
		{ label: 'Export files', value: formatCount(campaign.exportArtifactCount) },
		{ label: 'Latest export', value: campaign.latestExportArtifactFileName ?? 'Not available' }
	];
}

function reportsIdRow(label: string, id: string | null): DisplayRow {
	return id ? { label, value: id, mono: true } : { label, value: 'Not available' };
}

function toWavesSelectedRows(
	baselineWave: CampaignSeriesWavesWaveResponse,
	comparisonWave: CampaignSeriesWavesWaveResponse,
	comparison: CampaignSeriesWavesComparisonResponse
): DisplayRow[] {
	return [
		{ label: 'Baseline wave', value: baselineWave.name.trim() || 'Untitled wave' },
		{ label: 'Comparison wave', value: comparisonWave.name.trim() || 'Untitled wave' },
		...optionalDataFinalityRow('Baseline finality', baselineWave.dataFinality),
		...optionalDataFinalityRow('Comparison finality', comparisonWave.dataFinality),
		{ label: 'Comparison status', value: humanizeValue(comparison.status) },
		{ label: 'Disclosure', value: humanizeValue(comparison.disclosureState) },
		{ label: 'Compatibility', value: humanizeValue(comparison.compatibilityState) },
		{ label: 'Interpretation', value: humanizeValue(comparison.interpretationStatus) },
		{ label: 'Disclosure k', value: String(comparison.disclosureKMin ?? 'Not configured') },
		{ label: 'Linked pairs', value: formatCount(comparison.linkedPairCount) },
		{ label: 'Visible scores', value: formatCount(comparison.visibleScoreCount) },
		{ label: 'Suppressed scores', value: formatCount(comparison.suppressedScoreCount) },
		{ label: 'Blocked scores', value: formatCount(comparison.blockedScoreCount) }
	];
}

function toWavesWaveProvenanceRows(
	prefix: string,
	wave: CampaignSeriesWavesWaveResponse
): DisplayRow[] {
	return [
		wavesIdRow(`${prefix} launch snapshot`, wave.latestLaunchSnapshotId),
		{ label: `${prefix} latest launch`, value: formatCollectionDateTime(wave.latestLaunchAt) },
		{ label: `${prefix} scoring rule`, value: wavesScoringRuleLabel(wave) },
		wavesIdRow(`${prefix} disclosure policy`, wave.disclosurePolicyId),
		...optionalDateTimeRow(`${prefix} closed at`, wave.closedAt),
		...optionalIdRow(`${prefix} closed by`, wave.closedByUserId),
		...optionalTextRow(`${prefix} close reason`, wave.closeReason)
	];
}

function toWavesWaveSummaryRows(wave: CampaignSeriesWavesWaveResponse): DisplayRow[] {
	return [
		{ label: 'Identity mode', value: humanizeValue(wave.responseIdentityMode) },
		{ label: 'Locale', value: wave.defaultLocale },
		{ label: 'Wave state', value: humanizeValue(wave.waveState) },
		...optionalDataFinalityRow('Data finality', wave.dataFinality),
		wavesIdRow('Launch snapshot', wave.latestLaunchSnapshotId),
		{ label: 'Latest launch', value: formatCollectionDateTime(wave.latestLaunchAt) },
		{ label: 'Scoring rule', value: wavesScoringRuleLabel(wave) },
		{ label: 'Disclosure k', value: String(wave.disclosureKMin ?? 'Not configured') },
		{ label: 'Submitted responses', value: formatCount(wave.submittedResponseCount) },
		{ label: 'Scores', value: formatCount(wave.scoreCount) },
		{ label: 'Linked repeat responses', value: formatCount(wave.linkedTrajectoryCount) }
	];
}

function wavesIdRow(label: string, id: string | null): DisplayRow {
	return id ? { label, value: id, mono: true } : { label, value: 'Not available' };
}

function wavesScoringRuleLabel(wave: CampaignSeriesWavesWaveResponse) {
	return wave.scoringRuleKey ?? (wave.scoringRuleId ? 'Configured' : 'Not available');
}

function selectedSeriesSurfaceConfig(surface: SelectedSeriesSurfaceId) {
	switch (surface) {
		case 'setup':
			return {
				label: 'Prepare study',
				eyebrow: 'Study preparation',
				emptyState: {
					title: 'No campaigns yet',
					message: 'Create a wave draft before running launch readiness.'
				},
				proofActionTitle: 'Preparation actions',
				proofActionDescription:
					'Choose a questionnaire source, prepare questionnaire and result outputs, create a wave draft, and check launch readiness.'
			};
		case 'operations':
			return {
				label: 'Collect responses',
				eyebrow: 'Study collection',
				emptyState: {
					title: 'No campaign operations yet',
					message: 'Create and launch a campaign before running operations.'
				},
				proofActionTitle: 'Collection actions',
				proofActionDescription:
					'Run the pre-launch check, start collection, share access, send invitations, and monitor responses.'
			};
		case 'reports':
			return {
				label: 'Review results',
				eyebrow: 'Study results',
				emptyState: {
					title: 'No reportable campaigns yet',
					message: 'Submit responses and compute scores before report previews are available.'
				},
				proofActionTitle: 'Results actions',
				proofActionDescription:
					'Review aggregate results, create export files, and download files when they are ready.'
			};
		case 'waves':
			return {
				label: 'Compare rounds',
				eyebrow: 'Repeated-round comparison',
				emptyState: {
					title: 'No repeated rounds yet',
					message: 'Create and launch at least two collection rounds before comparing results over time.'
				},
				proofActionTitle: 'Comparison actions',
				proofActionDescription:
					'Check whether repeated rounds can be compared, then review safe change-over-time summaries.'
			};
	}
}

function toGovernanceRow(labelId: AppMessageId, status: string, locale: AppLocale) {
	return {
		label: appMessage(locale, labelId),
		value: localizedStatusValue(status, locale),
		status: toProductReadModelBadgeStatus(status)
	};
}

function localizedStatusValue(status: string | null | undefined, locale: AppLocale) {
	switch (status) {
		case 'preview':
		case 'proof_only':
			return appMessage(locale, 'readModel.status.preview');
		case 'ready':
		case 'complete':
			return appMessage(locale, 'readModel.status.ready');
		case 'blocked':
		case 'failed':
			return appMessage(locale, 'readModel.status.blocked');
		case 'live':
			return appMessage(locale, 'readModel.status.live');
		case 'pending':
			return appMessage(locale, 'readModel.status.pending');
		case 'not_available':
			return appMessage(locale, 'readModel.status.notAvailable');
		case 'not_configured':
			return appMessage(locale, 'readModel.status.notConfigured');
		default:
			return status ? humanizeValue(status) : appMessage(locale, 'readModel.status.notAvailable');
	}
}

function localizeLegacyApiReadModelChrome<T>(value: T, locale: AppLocale): T {
	if (locale !== 'hr-HR') {
		return value;
	}

	return localizeLegacyApiReadModelValue(value, null) as T;
}

function localizeLegacyApiReadModelValue(value: unknown, key: string | null): unknown {
	if (Array.isArray(value)) {
		return value.map((item) => localizeLegacyApiReadModelValue(item, key));
	}

	if (value && typeof value === 'object') {
		return Object.fromEntries(
			Object.entries(value).map(([entryKey, entryValue]) => [
				entryKey,
				localizeLegacyApiReadModelValue(entryValue, entryKey)
			])
		);
	}

	if (typeof value === 'string' && shouldLocalizeLegacyApiReadModelKey(key)) {
		return localizeLegacyApiReadModelString(value);
	}

	return value;
}

function shouldLocalizeLegacyApiReadModelKey(key: string | null) {
	return (
		key === 'label' ||
		key === 'value' ||
		key === 'title' ||
		key === 'description' ||
		key === 'message' ||
		key === 'summary' ||
		key === 'guidance' ||
		key === 'statusLabel' ||
		key === 'badgeLabel' ||
		key === 'surfaceTitle' ||
		key === 'surfaceLabel' ||
		key === 'surfaceEyebrow' ||
		key === 'surfaceDescription' ||
		key === 'referenceTitle' ||
		key === 'referenceDescription' ||
		key === 'actionLabel' ||
		key === 'archiveActionLabel' ||
		key === 'proofActionTitle' ||
		key === 'proofActionDescription' ||
		key === 'readOnlyMessage' ||
		key === 'emptyState' ||
		key === 'purposeLabel' ||
		key === 'finalityLabel' ||
		key === 'nextUse' ||
		key === 'detail'
	);
}

function localizeLegacyApiReadModelString(value: string) {
	const exact = legacyApiReadModelCroatianChrome[value];
	if (exact) {
		return exact;
	}

	return value
		.replace(/^(\d+) campaign, (\d+) live$/u, '$1 mjerenje, $2 aktivno')
		.replace(/^(\d+) campaigns, (\d+) live$/u, '$1 mjerenja, $2 aktivno')
		.replace(/^(\d+) of (\d+) submitted responses scored$/u, 'Bodovano je $1 od $2 predanih odgovora.')
		.replace(/^(\d+) of (\d+) scored$/u, '$1 od $2 bodovano')
		.replace(/^(\d+) submitted$/u, 'Predano: $1 odgovor')
		.replace(/^(\d+) started, (\d+) draft, (\d+) submitted$/u, 'Započeto: $1; u tijeku: $2; predano: $3.')
		.replace(/^(\d+) visible scores, (\d+) suppressed scores, (\d+) submitted responses still unscored\\.$/u, '$1 vidljivih rezultata, $2 skrivenih rezultata, $3 predana odgovora još nisu bodovana.')
		.replace(/^(.+) has (.+) results from (\\d+) submitted responses\\.$/u, '$1 ima rezultate statusa $2 iz $3 predanih odgovora.')
		.replace(/^Latest export (.+) is downloadable\\.$/u, 'Zadnji izvoz $1 dostupan je za preuzimanje.')
		.replace(/^Results are from a (.+) campaign with (.+) and no closed-wave finality yet\\.$/u, 'Rezultati su iz mjerenja statusa $1 s tumačenjem $2 i još nemaju finalnost zatvorenog mjerenja.')
		.replace(/^Results are from a (.+) campaign with (.+) and (\\d+) closed-wave reports?\\.$/u, 'Rezultati su iz mjerenja statusa $1 s tumačenjem $2 i $3 izvještaja zatvorenog mjerenja.');
}

const legacyApiReadModelCroatianChrome: Record<string, string> = {
	'Not available': 'Nije dostupno',
	'Not configured': 'Nije postavljeno',
	Missing: 'Nedostaje',
	Configured: 'Postavljeno',
	Ready: 'Spremno',
	Blocked: 'Blokirano',
	Pending: 'Na čekanju',
	Preview: 'Pregled',
	Active: 'Aktivno',
	'No submissions': 'Nema odgovora',
	'No scores': 'Nema rezultata',
	'No finality': 'Nema finalnosti',
	'No exports': 'Nema izvoza',
	'Live data': 'Podaci u tijeku',
	'Reports ready': 'Izvještaji su spremni',

	Campaigns: 'Mjerenja',
	'Live campaigns': 'Aktivna mjerenja',
	'Submitted responses': 'Predani odgovori',
	'Missing prerequisites': 'Nedostajući preduvjeti',
	'Respondent links': 'Poveznice za ispitanike',
	'Queued emails': 'E-poruke u redu',
	'Sent emails': 'Poslane e-poruke',
	'Failed emails': 'Neuspjele e-poruke',
	'Suppressed emails': 'Blokirane e-poruke',
	'Started responses': 'Započeti odgovori',
	'Draft responses': 'Odgovori u tijeku',
	'Latest response activity': 'Zadnja aktivnost odgovora',
	'Latest started': 'Zadnje započeto',
	'Latest submitted': 'Zadnje predano',
	'Latest email activity': 'Zadnja aktivnost e-pošte',
	'Identity mode': 'Način identiteta',
	Locale: 'Jezik',
	Status: 'Status',
	Questions: 'Pitanja',
	Template: 'Upitnik',
	Rule: 'Pravilo',
	Source: 'Izvor',
	Policies: 'Pravila',
	Consent: 'Pristanak',
	Retention: 'Zadržavanje',
	Disclosure: 'Prikaz rezultata',
	Scoring: 'Bodovanje',
	'Selected wave': 'Odabrano mjerenje',
	Readiness: 'Spremnost',
	'Latest launch': 'Zadnje pokretanje',
	'Collection started': 'Prikupljanje pokrenuto',
	Closed: 'Zatvoreno',
	Title: 'Naslov',
	'Required grants': 'Obvezne dozvole',
	'Optional grants': 'Neobvezne dozvole',
	Published: 'Objavljeno',
	'Retain for': 'Zadrži tijekom',
	'Starts from': 'Počinje od',
	'Action after retention': 'Radnja nakon zadržavanja',
	'Next review': 'Sljedeći pregled',

	'Instrument and template': 'Upitnik',
	'Wave draft': 'Nacrt mjerenja',
	'Launch readiness': 'Spremnost pokretanja',
	'Questionnaire is available for wave drafts.': 'Upitnik je dostupan za nacrte mjerenja.',
	'Result outputs are available for launch-readiness checks.': 'Izlazi rezultata dostupni su za provjere spremnosti pokretanja.',
	'Wave draft is ready for recipient setup and launch checks.': 'Nacrt mjerenja spreman je za odabir primatelja i provjere pokretanja.',
	'Launch readiness is blocked': 'Spremnost pokretanja je blokirana',
	'Disclosure policy: Add a disclosure policy for this series.': 'Prikaz rezultata: dodajte pravilo prikaza rezultata za ovu studiju.',
	'2 of 3 policies configured': 'Postavljena su 2 od 3 pravila',
	'template version': 'verzija upitnika',
	draft: 'nacrt',
	live: 'u tijeku',
	closed: 'zatvoreno',
	preview: 'pregled',
	visible: 'vidljivo',
	blocked: 'blokirano',
	compatible: 'kompatibilno',
	'no submissions': 'nema odgovora',
	'anonymous': 'anonimno',
	'anonymous repeat participation': 'anonimno s ponovljenim sudjelovanjem',
	identified: 'identificirano',
	wave: 'mjerenje',

	'Collect responses': 'Prikupljanje odgovora',
	'Study collection': 'Prikupljanje studije',
	'Collection reference': 'Referenca prikupljanja',
	'Start the selected wave, share respondent access, monitor submissions, and close collection when finished.': 'Pokrenite odabrano mjerenje, podijelite pristup ispitanicima, pratite predaje i zatvorite prikupljanje kada završi.',
	'Launch records, prerequisite checks, and selected wave details stay here for review.': 'Zapisi pokretanja, provjere preduvjeta i detalji odabranog mjerenja ostaju ovdje za pregled.',
	'Collection actions': 'Radnje prikupljanja',
	'Run the pre-launch check, start collection, share respondent access, monitor submissions, and close the wave.': 'Pokrenite provjeru prije pokretanja, otvorite prikupljanje, podijelite pristup ispitanicima, pratite predaje i zatvorite mjerenje.',

	'Review results': 'Pregled rezultata',
	'Study results': 'Rezultati studije',
	'Results reference': 'Referenca rezultata',
	'Review result availability, coverage, limitations, and export next use for the selected campaign.': 'Pregledajte dostupnost rezultata, pokrivenost, ograničenja i sljedeću upotrebu izvoza za odabrano mjerenje.',
	'Selected wave details, limitations, prerequisite checks, and export records stay here for review.': 'Detalji odabranog mjerenja, ograničenja, provjere preduvjeta i zapisi izvoza ostaju ovdje za pregled.',
	'Results actions': 'Radnje rezultata',
	'Review aggregate results, create export files, and download files when they are ready.': 'Pregledajte skupne rezultate, izradite izvozne datoteke i preuzmite ih kada budu spremne.',
	'Result availability': 'Dostupnost rezultata',
	'Coverage and visibility': 'Pokrivenost i vidljivost',
	'Limitations and finality': 'Ograničenja i finalnost',
	'Export next use': 'Sljedeća upotreba izvoza',
	'Report status': 'Status izvještaja',
	'Reportable campaigns': 'Mjerenja spremna za izvještaj',
	'Visible scores': 'Vidljivi rezultati',
	'Suppressed scores': 'Skriveni rezultati',
	'Scored submitted': 'Bodovani predani odgovori',
	'Unscored submitted': 'Nebodovani predani odgovori',
	'Disclosure k': 'Prag prikaza',
	'Campaign status': 'Status mjerenja',
	'Data finality': 'Finalnost podataka',
	'Preliminary live reports': 'Preliminarni izvještaji u tijeku',
	'Closed-wave reports': 'Izvještaji zatvorenih mjerenja',
	Interpretation: 'Tumačenje',
	'Export files': 'Datoteke izvoza',
	'Latest export': 'Zadnji izvoz',
	'Latest export file': 'Zadnja izvozna datoteka',
	'Latest export status': 'Status zadnjeg izvoza',
	'Latest export record': 'Zadnji zapis izvoza',
	'Latest export created': 'Zadnji izvoz izrađen',
	'Latest export completed': 'Zadnji izvoz dovršen',
	'Latest export started': 'Zadnji izvoz pokrenut',
	'Latest export failed': 'Zadnji izvoz neuspješan',
	'Latest export expires': 'Zadnji izvoz istječe',
	'Latest export deleted': 'Zadnji izvoz obrisan',
	'Latest export failure reason': 'Razlog neuspjeha zadnjeg izvoza',
	'Latest export downloadable': 'Zadnji izvoz dostupan za preuzimanje',
	'Launch snapshot': 'Zapis pokretanja',
	'Scoring rule': 'Pravilo bodovanja',
	'Consent document': 'Dokument pristanka',
	'Retention policy': 'Pravilo zadržavanja',
	'Disclosure policy': 'Pravilo prikaza rezultata',
	'Use this as a preview of current findings until scoring coverage, disclosure, and finality are complete.': 'Koristite ovo kao pregled trenutnih nalaza dok pokrivenost bodovanja, prikaz rezultata i finalnost ne budu dovršeni.',
	'Review visible score coverage before treating the result set as complete.': 'Pregledajte pokrivenost vidljivih rezultata prije nego skup rezultata tretirate kao dovršen.',
	'Collect submitted responses and compute scores before assessing coverage.': 'Prikupite predane odgovore i izračunajte rezultate prije procjene pokrivenosti.',
	'Label this as preliminary until the wave is closed and interpretation posture is reviewed.': 'Označite ovo kao preliminarno dok mjerenje nije zatvoreno i tumačenje pregledano.',
	'Closed-wave data is more stable, but interpretation labels still need the recorded validation posture.': 'Podaci zatvorenog mjerenja stabilniji su, ali oznake tumačenja i dalje trebaju zabilježeni stav validacije.',
	'Create an export after report results become available.': 'Izradite izvoz nakon što rezultati izvještaja postanu dostupni.',
	'Download the latest export file for handoff, or create a fresh export after results change.': 'Preuzmite zadnju izvoznu datoteku za predaju ili izradite novi izvoz nakon promjene rezultata.',

	'Compare rounds': 'Usporedba krugova',
	'Repeated-round comparison': 'Usporedba ponovljenih krugova',
	'Comparison actions': 'Radnje usporedbe',
	'No repeated rounds yet': 'Još nema ponovljenih krugova',
	'Create and launch at least two collection rounds before comparing results over time.':
		'Izradite i pokrenite barem dva kruga prikupljanja prije usporedbe rezultata kroz vrijeme.',
	'Check whether repeated rounds can be compared, then review safe change-over-time summaries.': 'Provjerite mogu li se ponovljeni krugovi usporediti, zatim pregledajte sigurne sažetke promjene kroz vrijeme.',
	'Repeat-participation waves': 'Ponovljena mjerenja',
	'Submitted waves': 'Mjerenja s odgovorima',
	'Linked repeat responses': 'Povezani ponovljeni odgovori',
	'Complete repeat-response pairs': 'Potpuni parovi ponovljenih odgovora',
	'Comparable scores': 'Usporedivi rezultati',
	'Visible comparisons': 'Vidljive usporedbe',
	'Suppressed comparisons': 'Skrivene usporedbe',
	'Blocked comparisons': 'Blokirane usporedbe',
	'Baseline wave': 'Početno mjerenje',
	'Comparison wave': 'Usporedno mjerenje',
	'Comparison status': 'Status usporedbe',
	Compatibility: 'Kompatibilnost',
	'Linked pairs': 'Povezani parovi',
	'Blocked scores': 'Blokirani rezultati',
	'Wave state': 'Stanje mjerenja',
	'Baseline launch snapshot': 'Zapis pokretanja početnog mjerenja',
	'Baseline latest launch': 'Zadnje pokretanje početnog mjerenja',
	'Baseline scoring rule': 'Pravilo bodovanja početnog mjerenja',
	'Baseline disclosure policy': 'Pravilo prikaza početnog mjerenja',
	'Comparison launch snapshot': 'Zapis pokretanja usporednog mjerenja',
	'Comparison latest launch': 'Zadnje pokretanje usporednog mjerenja',
	'Comparison scoring rule': 'Pravilo bodovanja usporednog mjerenja',
	'Comparison disclosure policy': 'Pravilo prikaza usporednog mjerenja'
};

function toCampaignSeriesCard(item: CampaignSeriesListItemResponse, locale: AppLocale = 'en') {
	const archived = item.archived === true;
	const ownership = toCampaignSeriesOwnership(item, locale);
	const lifecycle = toCampaignSeriesLifecycle(item, ownership, locale);
	const title = item.name.trim() || appMessage(locale, 'overview.untitledSeries');
	const rows: DisplayRow[] = [
		{ label: appMessage(locale, 'portfolio.row.campaigns'), value: formatCount(item.campaignCount) },
		{ label: appMessage(locale, 'portfolio.row.liveCampaigns'), value: formatCount(item.liveCampaignCount) },
		{
			label: appMessage(locale, 'portfolio.row.submittedResponses'),
			value: formatCount(item.submittedResponseCount)
		},
		{ label: appMessage(locale, 'portfolio.row.latestActivity'), value: latestActivityLabel(item) }
	];
	if (archived) {
		rows.push({
			label: appMessage(locale, 'portfolio.row.archived'),
			value: item.archivedAt ?? appMessage(locale, 'portfolio.value.notAvailable')
		});
	}

	const liveWaves = Math.max(0, Math.min(item.liveCampaignCount, item.campaignCount));
	const doneWaves = Math.max(0, Math.min(item.campaignCount - liveWaves, 8 - Math.min(liveWaves, 8)));
	const waveDots: Array<'done' | 'live'> = [
		...Array<'done'>(doneWaves).fill('done'),
		...Array<'live'>(Math.min(liveWaves, 8)).fill('live')
	];

	return {
		id: item.id,
		title,
		href: `/app/campaign-series/${item.id}`,
		status: archived ? 'archived' : toProductReadModelBadgeStatus(item.readinessStatus),
		archived,
		waveDots,
		archiveActionLabel: archived
			? appMessage(locale, 'portfolio.action.restore')
			: appMessage(locale, 'portfolio.action.archive'),
		canMutate: !ownership.isSample,
		duplicateAction: ownership.isSample
			? {
					label: appMessage(locale, 'portfolio.action.duplicateAsStudy'),
					defaultName: `Copy of ${title}`
				}
			: null,
		ownership,
		lifecycle,
		primaryAction: {
			label: lifecycle.actionLabel,
			href: lifecycle.actionHref
		},
		rows
	};
}

type CampaignSeriesCardView = ReturnType<typeof toCampaignSeriesCard>;
type CampaignSeriesLifecycleId =
	| 'needs_setup'
	| 'in_collection'
	| 'results_ready'
	| 'archived'
	| 'open';

const campaignSeriesLifecycleOrder: CampaignSeriesLifecycleId[] = [
	'needs_setup',
	'in_collection',
	'results_ready',
	'archived',
	'open'
];

function campaignSeriesLifecycleLabel(id: CampaignSeriesLifecycleId, locale: AppLocale) {
	switch (id) {
		case 'needs_setup':
			return appMessage(locale, 'portfolio.lifecycle.needsSetup.label');
		case 'in_collection':
			return appMessage(locale, 'portfolio.lifecycle.inCollection.label');
		case 'results_ready':
			return appMessage(locale, 'portfolio.lifecycle.resultsReady.label');
		case 'archived':
			return appMessage(locale, 'portfolio.lifecycle.archived.label');
		case 'open':
			return appMessage(locale, 'portfolio.lifecycle.open.label');
	}
}

function campaignSeriesLifecycleDescription(id: CampaignSeriesLifecycleId, locale: AppLocale) {
	switch (id) {
		case 'needs_setup':
			return appMessage(locale, 'portfolio.lifecycle.needsSetup.description');
		case 'in_collection':
			return appMessage(locale, 'portfolio.lifecycle.inCollection.description');
		case 'results_ready':
			return appMessage(locale, 'portfolio.lifecycle.resultsReady.description');
		case 'archived':
			return appMessage(locale, 'portfolio.lifecycle.archived.description');
		case 'open':
			return appMessage(locale, 'portfolio.lifecycle.open.description');
	}
}

function toCampaignSeriesStudySections(items: CampaignSeriesCardView[], locale: AppLocale) {
	const sampleItems = items.filter((item) => item.ownership.isSample);
	const ownItems = items.filter((item) => !item.ownership.isSample);

	return [
		{
			id: 'sample_studies',
			title: appMessage(locale, 'portfolio.section.sample.title'),
			description: appMessage(locale, 'portfolio.section.sample.description'),
			emptyState: appMessage(locale, 'portfolio.section.sample.empty'),
			groups: toCampaignSeriesLifecycleGroups(sampleItems, locale)
		},
		{
			id: 'your_studies',
			title: appMessage(locale, 'portfolio.section.own.title'),
			description: appMessage(locale, 'portfolio.section.own.description'),
			emptyState: appMessage(locale, 'portfolio.section.own.empty'),
			groups: toCampaignSeriesLifecycleGroups(ownItems, locale)
		}
	];
}

function toCampaignSeriesLifecycleGroups(items: CampaignSeriesCardView[], locale: AppLocale) {
	return campaignSeriesLifecycleOrder
		.map((id) => ({
			id,
			label: campaignSeriesLifecycleLabel(id, locale),
			description: campaignSeriesLifecycleDescription(id, locale),
			items: items.filter((item) => item.lifecycle.id === id)
		}))
		.filter((group) => group.items.length > 0);
}

function toCampaignSeriesLifecycle(
	item: CampaignSeriesListItemResponse,
	ownership: CampaignSeriesOwnershipView,
	locale: AppLocale
) {
	const baseHref = `/app/campaign-series/${item.id}`;
	const isSample = ownership.isSample;

	if (item.archived === true) {
		return {
			id: 'archived' as const,
			label: appMessage(locale, 'portfolio.lifecycle.archived.label'),
			status: 'archived' as ProductReadModelBadgeStatus,
			actionLabel: appMessage(locale, 'workspace.action.openArchivedStudy'),
			actionHref: baseHref
		};
	}

	if (item.submittedResponseCount > 0) {
		return {
			id: 'results_ready' as const,
			label: appMessage(locale, 'portfolio.lifecycle.resultsReady.label'),
			status: 'ready' as ProductReadModelBadgeStatus,
			actionLabel: isSample
				? appMessage(locale, 'workspace.action.reviewSampleResults')
				: appMessage(locale, 'workspace.action.reviewResults'),
			actionHref: `${baseHref}/reports`
		};
	}

	if (item.liveCampaignCount > 0) {
		return {
			id: 'in_collection' as const,
			label: appMessage(locale, 'portfolio.lifecycle.inCollection.label'),
			status: 'live' as ProductReadModelBadgeStatus,
			actionLabel: isSample
				? appMessage(locale, 'workspace.action.inspectSampleCollection')
				: appMessage(locale, 'workspace.action.monitorCollection'),
			actionHref: `${baseHref}/operations`
		};
	}

	if (item.campaignCount === 0 || item.readinessStatus === 'not_configured') {
		return {
			id: 'needs_setup' as const,
			label: appMessage(locale, 'portfolio.lifecycle.needsSetup.label'),
			status: 'not_configured' as ProductReadModelBadgeStatus,
			actionLabel: isSample
				? appMessage(locale, 'workspace.action.inspectSampleSetup')
				: appMessage(locale, 'workspace.action.continueSetup'),
			actionHref: `${baseHref}/setup`
		};
	}

	return {
		id: 'open' as const,
		label: appMessage(locale, 'portfolio.lifecycle.open.label'),
		status: toProductReadModelBadgeStatus(item.readinessStatus),
		actionLabel: isSample
			? appMessage(locale, 'workspace.action.inspectStudy')
			: appMessage(locale, 'workspace.action.openStudy'),
		actionHref: baseHref
	};
}

function toCampaignSeriesArchiveState(hub: CampaignSeriesHubResponse) {
	const archived = hub.archived === true;
	return {
		archived,
		status: (archived ? 'archived' : 'ready') as ProductReadModelBadgeStatus,
		label: archived ? 'Archived' : 'Active',
		archivedAt: hub.archivedAt,
		reason: hub.archiveReason
	};
}

function toCampaignSeriesOwnership(
	series: CampaignSeriesOwnershipMetadata,
	locale: AppLocale = 'en'
): CampaignSeriesOwnershipView {
	const isSample = series.isSample === true || series.studyKind === 'sample';
	const sampleScenario = series.sampleScenario ?? null;

	return {
		label: isSample
			? appMessage(locale, 'portfolio.ownership.sample.label')
			: appMessage(locale, 'portfolio.ownership.own.label'),
		badgeStatus: isSample ? 'demo' : 'neutral',
		isSample,
		sampleScenario,
		readOnlyReason: series.readOnlyReason ?? null,
		readOnlyMessage: isSample ? toSampleStudyReadOnlyMessage(sampleScenario, locale) : null
	};
}

function toSampleStudyReadOnlyMessage(sampleScenario: string | null, locale: AppLocale) {
	switch (sampleScenario) {
		case 'setup':
			return appMessage(locale, 'portfolio.sampleReadOnly.setup');
		case 'blocked':
			return appMessage(locale, 'portfolio.sampleReadOnly.blocked');
		case 'in_collection':
			return appMessage(locale, 'portfolio.sampleReadOnly.inCollection');
		case 'longitudinal':
			return appMessage(locale, 'portfolio.sampleReadOnly.longitudinal');
		case 'mixed_lifecycle':
		case 'completed':
			return appMessage(locale, 'portfolio.sampleReadOnly.results');
		default:
			return appMessage(locale, 'portfolio.sampleReadOnly.default');
	}
}

function latestActivityLabel(item: CampaignSeriesListItemResponse) {
	return formatNullableDateTime(latestIsoTimestamp(item.latestSubmissionAt, item.latestLaunchAt));
}

function latestIsoTimestamp(first: string | null, second: string | null) {
	if (!first) {
		return second;
	}

	if (!second) {
		return first;
	}

	return Date.parse(first) >= Date.parse(second) ? first : second;
}

function readProblemDetail(body: unknown) {
	if (!body || typeof body !== 'object' || !('detail' in body)) {
		return null;
	}

	const detail = body.detail;
	return typeof detail === 'string' && detail.trim().length > 0 ? detail : null;
}

function toProductReadModelBadgeStatus(
	status: string | null | undefined
): ProductReadModelBadgeStatus {
	switch (status) {
		case 'ready':
		case 'archived':
		case 'blocked':
		case 'demo':
		case 'proof_only':
		case 'draft':
		case 'scheduled':
		case 'live':
		case 'closed':
		case 'cancelled':
		case 'pending':
		case 'empty':
		case 'neutral':
		case 'not_available':
		case 'not_configured':
			return status;
		default:
			return 'not_available';
	}
}

function toExportArtifactLibraryOverview(
	library: ExportArtifactLibraryResponse,
	locale: AppLocale
): ExportArtifactLibraryOverviewItem[] {
	const { totalCount, downloadableCount, failedCount, pendingCount } = library.summary;
	const purposeLabels = uniqueValues(
		library.artifacts.map((artifact) => toExportArtifactPurpose(artifact.artifactType, locale).label)
	);
	const sourceLabels = uniqueValues(library.artifacts.map((artifact) => artifact.targetLabel));
	const reportSummaryCount = library.artifacts.filter(
		(artifact) => toExportArtifactPurpose(artifact.artifactType, locale).kind === 'report_summary'
	).length;
	const responseDatasetCount = library.artifacts.filter(
		(artifact) => toExportArtifactPurpose(artifact.artifactType, locale).kind === 'response_dataset'
	).length;
	const campaignArtifactCount = library.artifacts.filter(
		(artifact) => artifact.targetKind === 'campaign'
	).length;
	const campaignSeriesArtifactCount = library.artifacts.filter(
		(artifact) => artifact.targetKind === 'campaign_series'
	).length;

	return [
		{
			id: 'ready_downloads',
			label: appMessage(locale, 'exports.library.readyDownloads.label'),
			status: downloadableCount > 0 ? 'ready' : 'empty',
			badgeLabel: appMessage(locale, 'exports.library.readyDownloads.badge', {
				count: downloadableCount
			}),
			summary:
				downloadableCount > 0
					? appMessage(locale, 'exports.library.readyDownloads.summary.ready', {
							count: downloadableCount
						})
					: appMessage(locale, 'exports.library.readyDownloads.summary.empty'),
			guidance:
				downloadableCount > 0
					? responseDatasetCount > 0
						? appMessage(locale, 'exports.library.readyDownloads.guidance.withDataset')
						: appMessage(locale, 'exports.library.readyDownloads.guidance.reportOnly')
					: appMessage(locale, 'exports.library.readyDownloads.guidance.empty'),
			detailRows: [
				{ label: appMessage(locale, 'exports.library.row.exportFiles'), value: formatCount(totalCount) },
				{
					label: appMessage(locale, 'exports.library.row.downloadable'),
					value: formatCount(downloadableCount)
				},
				{
					label: appMessage(locale, 'exports.library.row.reportSummaryDownloads'),
					value: formatCount(reportSummaryCount)
				},
				{
					label: appMessage(locale, 'exports.library.row.responseDatasets'),
					value: formatCount(responseDatasetCount)
				}
			]
		},
		{
			id: 'attention_needed',
			label: appMessage(locale, 'exports.library.attention.label'),
			status: failedCount > 0 ? 'failed' : pendingCount > 0 ? 'pending' : 'ready',
			badgeLabel:
				failedCount > 0
					? appMessage(locale, 'exports.library.attention.badge.failed', { count: failedCount })
					: pendingCount > 0
						? appMessage(locale, 'exports.library.attention.badge.pending', {
								count: pendingCount
							})
						: appMessage(locale, 'exports.library.attention.badge.ready'),
			summary:
				failedCount > 0
					? appMessage(locale, 'exports.library.attention.summary.failed', {
							count: failedCount
						})
					: pendingCount > 0
						? appMessage(locale, 'exports.library.attention.summary.pending', {
								count: pendingCount
							})
						: appMessage(locale, 'exports.library.attention.summary.ready'),
			guidance:
				failedCount > 0
					? appMessage(locale, 'exports.library.attention.guidance.failed')
					: pendingCount > 0
						? appMessage(locale, 'exports.library.attention.guidance.pending')
						: appMessage(locale, 'exports.library.attention.guidance.ready'),
			detailRows: [
				{ label: appMessage(locale, 'exports.library.row.failed'), value: formatCount(failedCount) },
				{ label: appMessage(locale, 'exports.library.row.pending'), value: formatCount(pendingCount) }
			]
		},
		{
			id: 'artifact_purpose',
			label: appMessage(locale, 'exports.library.purpose.label'),
			status: totalCount > 0 ? 'ready' : 'empty',
			badgeLabel:
				totalCount > 0
					? appMessage(locale, 'exports.library.purpose.badge.ready', {
							count: purposeLabels.length
						})
					: appMessage(locale, 'exports.library.purpose.badge.empty'),
			summary:
				totalCount > 0
					? appMessage(locale, 'exports.library.purpose.summary.ready', {
							purposeLabels:
								locale === 'hr-HR'
									? formatInlineList(purposeLabels, locale).toLocaleLowerCase(locale)
									: formatInlineList(purposeLabels, locale)
						})
					: appMessage(locale, 'exports.library.purpose.summary.empty'),
			guidance:
				totalCount > 0
					? appMessage(locale, 'exports.library.purpose.guidance.ready')
					: appMessage(locale, 'exports.library.purpose.guidance.empty'),
			detailRows: [
				{
					label: appMessage(locale, 'exports.library.row.reportSummaryExports'),
					value: formatCount(reportSummaryCount)
				},
				{
					label: appMessage(locale, 'exports.library.row.responseDatasetExports'),
					value: formatCount(responseDatasetCount)
				}
			]
		},
		{
			id: 'study_context',
			label: appMessage(locale, 'exports.library.context.label'),
			status: totalCount > 0 ? 'ready' : 'empty',
			badgeLabel:
				totalCount > 0
					? appMessage(locale, 'exports.library.context.badge.ready', {
							count: sourceLabels.length
						})
					: appMessage(locale, 'exports.library.context.badge.empty'),
			summary:
				totalCount > 0
					? appMessage(locale, 'exports.library.context.summary.ready', {
							sourceLabels: formatInlineList(sourceLabels, locale)
						})
					: appMessage(locale, 'exports.library.context.summary.empty'),
			guidance:
				totalCount > 0
					? appMessage(locale, 'exports.library.context.guidance.ready')
					: appMessage(locale, 'exports.library.context.guidance.empty'),
			detailRows: [
				{
					label: appMessage(locale, 'exports.library.row.campaignFiles'),
					value: formatCount(campaignArtifactCount)
				},
				{
					label: appMessage(locale, 'exports.library.row.studyFiles'),
					value: formatCount(campaignSeriesArtifactCount)
				}
			]
		}
	];
}

function toExportArtifactLibraryCard(
	artifact: ExportArtifactLibraryResponse['artifacts'][number],
	locale: AppLocale
) {
	const purpose = toExportArtifactPurpose(artifact.artifactType, locale);
	const finalityLabel = toExportArtifactFinalityLabel(artifact.dataFinality, locale);

	return {
		id: artifact.id,
		title: artifact.fileName,
		subtitle: artifact.targetLabel,
		purposeLabel: purpose.label,
		finalityLabel,
		nextUse: purpose.nextUse,
		status: toExportArtifactBadgeStatus(artifact.status),
		statusLabel: sentenceCase(humanizeValue(artifact.status)),
		href:
			artifact.targetKind === 'campaign_series'
				? `/app/campaign-series/${artifact.targetId}/reports`
				: null,
		rows: [
			{
				label: appMessage(locale, 'exports.library.row.studyContext'),
				value: toExportArtifactSourceContext(artifact.targetKind, artifact.targetLabel, locale)
			},
			{
				label: appMessage(locale, 'exports.library.row.fileType'),
				value: toExportArtifactFileType(artifact.artifactType, locale)
			},
			{
				label: appMessage(locale, 'exports.library.row.format'),
				value: toExportArtifactFileFormat(artifact.format, locale)
			},
			{ label: appMessage(locale, 'exports.library.row.dataFinality'), value: finalityLabel },
			{ label: appMessage(locale, 'exports.library.row.rows'), value: formatCount(artifact.rowCount) },
			{ label: appMessage(locale, 'exports.library.row.size'), value: formatBytes(artifact.byteSize) },
			{ label: appMessage(locale, 'exports.library.row.created'), value: formatDateTime(artifact.createdAt) },
			{
				label: appMessage(locale, 'exports.library.row.completed'),
				value: formatNullableDateTime(artifact.completedAt)
			},
			...(artifact.failureReasonCode
				? [{ label: appMessage(locale, 'exports.library.row.failure'), value: artifact.failureReasonCode }]
				: []),
			{
				label: appMessage(locale, 'exports.library.row.download'),
				value: artifact.canDownload
					? appMessage(locale, 'exports.library.download.available')
					: appMessage(locale, 'exports.library.download.notAvailable')
			}
		]
	};
}

function toExportArtifactSourceContext(targetKind: string, targetLabel: string, locale: AppLocale) {
	switch (targetKind) {
		case 'campaign_series':
			return appMessage(locale, 'exports.library.context.campaignSeries', { label: targetLabel });
		case 'campaign':
			return appMessage(locale, 'exports.library.context.campaign', { label: targetLabel });
		default:
			return `${sentenceCase(humanizeValue(targetKind))} / ${targetLabel}`;
	}
}

function toExportArtifactFileType(artifactType: string, locale: AppLocale) {
	switch (artifactType) {
		case 'campaign_series_results_matrix_csv_codebook':
		case 'report_proof_csv_codebook':
			return appMessage(locale, 'exports.library.fileType.reportSummary');
		case 'campaign_series_response_csv_codebook':
			return appMessage(locale, 'exports.library.fileType.responseDataset');
		default:
			return sentenceCase(humanizeValue(artifactType));
	}
}

function toExportArtifactFileFormat(format: string, locale: AppLocale) {
	switch (format) {
		case 'csv_codebook':
			return appMessage(locale, 'exports.library.fileFormat.csvCodebook');
		default:
			return sentenceCase(humanizeValue(format));
	}
}

function toExportArtifactPurpose(artifactType: string, locale: AppLocale) {
	switch (artifactType) {
		case 'campaign_series_results_matrix_csv_codebook':
		case 'report_proof_csv_codebook':
			return {
				kind: 'report_summary',
				label: appMessage(locale, 'exports.library.purpose.reportSummary.label'),
				nextUse: appMessage(locale, 'exports.library.purpose.reportSummary.nextUse')
			};
		case 'campaign_series_response_csv_codebook':
			return {
				kind: 'response_dataset',
				label: appMessage(locale, 'exports.library.purpose.responseDataset.label'),
				nextUse: appMessage(locale, 'exports.library.purpose.responseDataset.nextUse')
			};
		default:
			return {
				kind: 'other',
				label: sentenceCase(humanizeValue(artifactType)),
				nextUse: appMessage(locale, 'exports.library.purpose.other.nextUse')
			};
	}
}

function toExportArtifactFinalityLabel(dataFinality: string | null | undefined, locale: AppLocale) {
	switch (dataFinality) {
		case 'closed_wave':
			return appMessage(locale, 'exports.library.finality.closedWave');
		case 'preliminary_live':
			return 'Preliminary live data';
		case 'not_reportable':
			return 'Not reportable';
		case null:
		case undefined:
			return appMessage(locale, 'exports.library.finality.notClosedWave');
		default:
			return sentenceCase(humanizeValue(dataFinality));
	}
}

function uniqueValues(values: string[]) {
	return [...new Set(values.map((value) => value.trim()).filter(Boolean))];
}

function formatInlineList(values: string[], locale: AppLocale = 'en') {
	if (values.length === 0) {
		return locale === 'hr-HR' ? 'nema stavki' : 'no items';
	}

	if (values.length === 1) {
		return values[0];
	}

	if (values.length === 2) {
		return locale === 'hr-HR' ? `${values[0]} i ${values[1]}` : `${values[0]} and ${values[1]}`;
	}

	return locale === 'hr-HR'
		? `${values.slice(0, -1).join(', ')} i ${values[values.length - 1]}`
		: `${values.slice(0, -1).join(', ')}, and ${values[values.length - 1]}`;
}

function toExportArtifactBadgeStatus(status: string): ProductReadModelBadgeStatus {
	switch (status) {
		case 'succeeded':
			return 'ready';
		case 'failed':
			return 'failed';
		case 'queued':
		case 'rendering':
			return 'pending';
		default:
			return 'not_available';
	}
}

function labelFromCode(code: string) {
	const words = code
		.replace(/[._-]+/g, ' ')
		.trim()
		.split(/\s+/);
	const label = words.join(' ');
	return label.charAt(0).toUpperCase() + label.slice(1);
}

function humanizeValue(value: string) {
	if (value === 'proof_only') {
		return 'preview';
	}

	if (value === 'anonymous_longitudinal') {
		return 'anonymous repeat participation';
	}

	if (value === 'longitudinal') {
		return 'repeat participation';
	}

	if (value === 'report_proof_csv_codebook') {
		return 'results matrix CSV and codebook';
	}

	if (value === 'campaign_series_results_matrix_csv_codebook') {
		return 'results matrix CSV and codebook';
	}

	if (value === 'campaign_series_response_csv_codebook') {
		return 'response dataset CSV and codebook';
	}

	if (value === 'campaign_report_proof') {
		return 'results preview CSV';
	}

	return value.replace(/[_-]+/g, ' ');
}

function toProductDisplayCopy(value: string) {
	return value
		.replace(/\bExport artifacts\b/g, 'Export files')
		.replace(/\bexport artifacts\b/g, 'export files')
		.replace(/\bExport artifact\b/g, 'Export file')
		.replace(/\bexport artifact\b/g, 'export file')
		.replace(/\bTemplate version\b/g, 'Questionnaire')
		.replace(/\btemplate version\b/g, 'questionnaire')
		.replace(/\bScoring rule\b/g, 'Result outputs')
		.replace(/\bscoring rule\b/g, 'result outputs')
		.replace(/\bCampaign draft\b/g, 'Wave draft')
		.replace(/\bcampaign draft\b/g, 'wave draft')
		.replace(/\bproof-only\b/g, 'preview')
		.replace(/\bProof-only\b/g, 'Preview')
		.replace(/\bproof only\b/g, 'preview')
		.replace(/\bProof only\b/g, 'Preview');
}

function optionalCountRow(label: string, value: number | undefined): DisplayRow[] {
	return value === undefined ? [] : [{ label, value: formatCount(value) }];
}

function optionalTextRow(label: string, value: string | null | undefined): DisplayRow[] {
	return value === undefined ? [] : [{ label, value: value ?? 'Not available' }];
}

function optionalDateTimeRow(label: string, value: string | null | undefined): DisplayRow[] {
	return value === undefined ? [] : [{ label, value: formatNullableDateTime(value) }];
}

function optionalIdRow(label: string, id: string | null | undefined): DisplayRow[] {
	if (id === undefined) {
		return [];
	}

	return id ? [{ label, value: id, mono: true }] : [{ label, value: 'Not available' }];
}

function optionalDataFinalityRow(label: string, value: string | null | undefined): DisplayRow[] {
	return value === undefined ? [] : [{ label, value: humanizeValue(value ?? 'not_reportable') }];
}

function formatInterpretationMeta(interpretation: ScoreInterpretationResponse | null | undefined) {
	if (!interpretation) {
		return null;
	}

	return [
		humanizeValue(interpretation.status),
		humanizeValue(interpretation.source),
		interpretation.isValidated ? 'reviewed' : 'not reviewed',
		interpretation.isOfficial ? 'official' : 'not official'
	].join(' / ');
}

function sentenceCase(value: string) {
	const normalized = value.trim();

	return normalized.charAt(0).toUpperCase() + normalized.slice(1);
}

function formatCount(value: number) {
	return String(value);
}

function formatNullableDateTime(value: string | null | undefined) {
	if (!value) {
		return 'Not available';
	}

	return formatDateTime(value);
}

function formatDateTime(value: string) {
	const date = new Date(normalizeTimestampForDate(value));
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

function normalizeTimestampForDate(value: string) {
	return value.replace(/\.(\d{3})\d+(?=(Z|[+-]\d{2}:?\d{2})$)/, '.$1');
}

function pluralize(count: number, singular: string, plural: string) {
	return count === 1 ? singular : plural;
}

function formatNullableCount(value: number | null) {
	return value === null ? 'Pending' : String(value);
}

export function formatScoreOutputMetadata(
	nValid: number | null | undefined,
	nExpected: number | null | undefined,
	missingPolicyStatus: string | null | undefined
) {
	const values = [];
	if (nValid !== null && nValid !== undefined && nExpected !== null && nExpected !== undefined) {
		values.push(`n ${nValid}/${nExpected}`);
	}

	if (missingPolicyStatus) {
		values.push(missingPolicyStatus);
	}

	return values.length > 0 ? values.join(' / ') : null;
}

function formatNullableNumber(value: number | null) {
	return value === null ? 'Pending' : formatNumber(value);
}

function formatNumber(value: number) {
	return value.toFixed(2);
}

function formatDelta(value: number | null) {
	if (value === null) {
		return 'Pending';
	}

	return `${value >= 0 ? '+' : ''}${formatNumber(value)}`;
}

function formatBytes(value: number) {
	if (value < 1000) {
		return `${value} B`;
	}

	if (value < 1_000_000) {
		return `${(value / 1000).toFixed(1)} KB`;
	}

	return `${(value / 1_000_000).toFixed(1)} MB`;
}
