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
	label: 'Sample study' | 'Your study';
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

export function toWorkspaceOverviewView(overview: WorkspaceOverviewResponse) {
	return {
		tenantId: overview.tenantId,
		lifecycleSteps: workspaceLifecycleSteps,
		sampleStudies: overview.studyCollections.sampleStudies.map(toWorkspaceStudyCard),
		ownStudies: overview.studyCollections.ownStudies.map(toWorkspaceStudyCard),
		commandItems: overview.commandCenter.items.map(toWorkspaceCommandItem),
		totalRows: toWorkspaceTotalRows(overview.totals),
		recentSeries: overview.recentSeries.map(toCampaignSeriesCard)
	};
}

export function toTenantSettingsView(settings: TenantSettingsWorkspaceResponse) {
	return {
		title: settings.profile.name.trim() || 'Tenant settings',
		status: (settings.profile.status === 'active'
			? 'ready'
			: toProductReadModelBadgeStatus(settings.profile.status)) as ProductReadModelBadgeStatus,
		profileRows: [
			{ label: 'Slug', value: settings.profile.slug, mono: true },
			{ label: 'Region', value: settings.profile.region.toUpperCase() },
			{ label: 'Default locale', value: settings.profile.defaultLocale },
			{ label: 'Status', value: sentenceCase(humanizeValue(settings.profile.status)) },
			{ label: 'Created', value: formatDateTime(settings.profile.createdAt) },
			{ label: 'Updated', value: formatDateTime(settings.profile.updatedAt) }
		],
		metricRows: [
			{ label: 'Campaign series', value: formatCount(settings.counts.campaignSeriesCount) },
			{ label: 'Campaigns', value: formatCount(settings.counts.campaignCount) },
			{ label: 'Live campaigns', value: formatCount(settings.counts.liveCampaignCount) },
			{
				label: 'Submitted responses',
				value: formatCount(settings.counts.submittedResponseCount)
			},
			{ label: 'Subjects', value: formatCount(settings.counts.subjectCount) },
			{ label: 'Subject groups', value: formatCount(settings.counts.subjectGroupCount) },
			{ label: 'Tenant members', value: formatCount(settings.counts.tenantMemberCount) },
			{ label: 'Tenant roles', value: formatCount(settings.counts.tenantRoleCount) },
			{ label: 'Export files', value: formatCount(settings.counts.exportArtifactCount) }
		],
		managementLinks: settings.managementLinks.map((link) => ({
			id: link.id,
			label: link.label,
			description: link.description,
			href: link.route
		}))
	};
}

export function toInstrumentLibraryView(instruments: InstrumentSummaryResponse[]) {
	const launchEligibleCount = instruments.filter(
		(instrument) => instrument.canStartNewCampaign
	).length;
	const launchBlockedCount = instruments.length - launchEligibleCount;

	return {
		metricRows: [
			{ label: 'Instruments', value: formatCount(instruments.length) },
			{ label: 'Launch eligible', value: formatCount(launchEligibleCount) },
			{ label: 'Launch blocked', value: formatCount(launchBlockedCount) }
		],
		cards: instruments.map((instrument) => {
			const launchEligible = instrument.canStartNewCampaign;

			return {
				id: instrument.id,
				title: instrument.fullName.trim() || instrument.code,
				subtitle: `${instrument.code} ${instrument.version}`,
				status: (launchEligible ? 'ready' : 'blocked') as ProductReadModelBadgeStatus,
				statusLabel: launchEligible ? 'Launch eligible' : 'Launch blocked',
				rows: [
					{
						label: 'Rights',
						value: sentenceCase(humanizeValue(instrument.rightsStatus))
					},
					{
						label: 'Validity',
						value: instrument.validityLabel
					}
				]
			};
		})
	};
}

export function toExportArtifactLibraryView(library: ExportArtifactLibraryResponse) {
	return {
		surfaceTitle: 'Use exports',
		surfaceEyebrow: 'Study support',
		surfaceDescription:
			'Find generated CSV/codebook files by purpose, readiness, source study, and next use.',
		referenceTitle: 'Export reference',
		referenceDescription:
			'File metadata, lifecycle timestamps, failure codes, and download availability stay available for audit and troubleshooting.',
		exportOverview: toExportArtifactLibraryOverview(library),
		metricRows: [
			{ label: 'Export files', value: formatCount(library.summary.totalCount) },
			{ label: 'Downloadable', value: formatCount(library.summary.downloadableCount) },
			{ label: 'Failed', value: formatCount(library.summary.failedCount) },
			{ label: 'Pending', value: formatCount(library.summary.pendingCount) }
		],
		cards: library.artifacts.map(toExportArtifactLibraryCard)
	};
}

export function toCampaignSeriesListView(
	list: CampaignSeriesListResponse,
	query: CampaignSeriesPortfolioQuery = {}
) {
	const filtersActive = hasActiveCampaignSeriesFilters(query);
	const items = list.items.map(toCampaignSeriesCard);

	return {
		items,
		studySections: toCampaignSeriesStudySections(items),
		filtersActive,
		statusOptions: campaignSeriesPortfolioStatusOptions,
		sortOptions: campaignSeriesPortfolioSortOptions,
		visibilityOptions: campaignSeriesPortfolioVisibilityOptions,
		emptyState:
			list.items.length === 0
				? filtersActive
					? {
							title: 'No matching studies',
							message:
								'Adjust search, readiness, or visibility filters to show sample or own studies.'
						}
					: {
							title: 'No studies yet',
							message:
								'Create your study when you have setup access, or add sample studies for learning.'
						}
				: null
	};
}

export function toCampaignSeriesHubView(hub: CampaignSeriesHubResponse) {
	const archiveState = toCampaignSeriesArchiveState(hub);
	const ownership = toCampaignSeriesOwnership(hub);
	const rows: DisplayRow[] = [
		{ label: 'Created', value: formatDateTime(hub.createdAt) },
		{ label: 'Updated', value: formatDateTime(hub.updatedAt) }
	];
	if (archiveState.archived) {
		rows.push({ label: 'Archived', value: formatNullableDateTime(archiveState.archivedAt) });
		if (archiveState.reason) {
			rows.push({ label: 'Archive reason', value: archiveState.reason });
		}
	}

	return {
		id: hub.id,
		surfaceTitle: 'Study overview',
		surfaceDescription:
			'Use this overview to prepare, collect, review results, and compare waves for the selected study.',
		referenceTitle: 'Study reference',
		referenceDescription:
			'Detailed records, governance status, and wave rows for this selected study.',
		title: hub.name.trim() || 'Untitled campaign series',
		subtitle: `${hub.totals.campaignCount} ${hub.totals.campaignCount === 1 ? 'campaign' : 'campaigns'}, ${hub.totals.liveCampaignCount} live`,
		rows,
		ownership,
		canMutate: !ownership.isSample,
		archiveState,
		totalRows: toCampaignSeriesHubTotalRows(hub.totals),
		governanceRows: [
			toGovernanceRow('Consent', hub.governance.consentStatus),
			toGovernanceRow('Retention', hub.governance.retentionStatus),
			toGovernanceRow('Disclosure', hub.governance.disclosureStatus),
			toGovernanceRow('Scoring', hub.governance.scoringStatus)
		],
		lifecycleMap: toCampaignSeriesHubLifecycleMap(hub),
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
			title: campaign.name.trim() || 'Untitled campaign',
			status: toProductReadModelBadgeStatus(campaign.status),
			rows: [
				{ label: 'Identity mode', value: humanizeValue(campaign.responseIdentityMode) },
				{ label: 'Locale', value: campaign.defaultLocale },
				{ label: 'Submitted responses', value: formatCount(campaign.submittedResponseCount) },
				{ label: 'Scores', value: formatCount(campaign.scoreCount) },
				{ label: 'Export files', value: formatCount(campaign.exportArtifactCount) }
			]
		}))
	};
}

function toCampaignSeriesHubLifecycleMap(hub: CampaignSeriesHubResponse) {
	return {
		title: 'Study lifecycle',
		description: 'Move through this study from preparation to collection, results, and waves.',
		items: hub.lifecycle.map((item) => {
			const phase = campaignSeriesHubLifecyclePhase(item.id);

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

function campaignSeriesHubLifecyclePhase(id: CampaignSeriesHubResponse['lifecycle'][number]['id']) {
	switch (id) {
		case 'setup':
			return {
				label: 'Prepare',
				description: 'Build the questionnaire, results setup, policies, wave, and launch check.'
			};
		case 'operations':
			return {
				label: 'Collect',
				description: 'Start the wave, share access, send invitations, and monitor submissions.'
			};
		case 'reports':
			return {
				label: 'Review results',
				description: 'Review findings, limitations, and export files after responses are ready.'
			};
		case 'waves':
			return {
				label: 'Compare waves',
				description: 'Create follow-up waves and compare results across collection rounds.'
			};
	}
}

export function toSelectedSeriesSurfaceView(
	hub: CampaignSeriesHubResponse,
	surface: SelectedSeriesSurfaceId
) {
	const hubView = toCampaignSeriesHubView(hub);
	const config = selectedSeriesSurfaceConfig(surface);

	return {
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
	};
}

export function toCampaignSeriesSetupWorkspaceView(
	workspace: CampaignSeriesSetupWorkspaceResponse
) {
	const ownership = toCampaignSeriesOwnership(workspace.series);

	return {
		id: workspace.series.id,
		title: workspace.series.name.trim() || 'Untitled campaign series',
		subtitle: `${workspace.summary.campaignCount} ${workspace.summary.campaignCount === 1 ? 'campaign' : 'campaigns'}, ${workspace.summary.liveCampaignCount} live`,
		ownership,
		canMutate: !ownership.isSample,
		readOnlyMessage: ownership.readOnlyMessage,
		surfaceLabel: 'Prepare study',
		surfaceEyebrow: 'Study preparation',
		surfaceDescription:
			'Prepare this study for collection by completing setup tasks and launch-readiness checks.',
		referenceTitle: 'Setup reference',
		referenceDescription:
			'Detailed setup records, policy status, selected wave fields, and launch-check notes stay here for review.',
		summaryRows: [
			{ label: 'Campaigns', value: formatCount(workspace.summary.campaignCount) },
			{ label: 'Live campaigns', value: formatCount(workspace.summary.liveCampaignCount) },
			{
				label: 'Missing prerequisites',
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
			title: campaign.name.trim() || 'Untitled campaign',
			status: toProductReadModelBadgeStatus(campaign.status),
			rows: toSetupCampaignSummaryRows(campaign)
		})),
		emptyState:
			workspace.campaigns.length === 0
				? {
						title: 'No campaigns yet',
						message: 'Create a campaign draft before running launch readiness.'
					}
				: null,
		proofActionTitle: 'Preparation actions',
		proofActionDescription:
			'Import the instrument, prepare template and scoring drafts, create a campaign draft, and check launch readiness.'
	};
}

export function toCampaignSeriesOperationsWorkspaceView(
	workspace: CampaignSeriesOperationsWorkspaceResponse
) {
	const ownership = toCampaignSeriesOwnership(workspace.series);
	const scoreCoverage = normalizeOperationsScoreCoverage(workspace.scoreCoverage);

	return {
		id: workspace.series.id,
		title: workspace.series.name.trim() || 'Untitled campaign series',
		subtitle: `${workspace.summary.campaignCount} ${workspace.summary.campaignCount === 1 ? 'campaign' : 'campaigns'}, ${workspace.summary.liveCampaignCount} live`,
		ownership,
		canMutate: !ownership.isSample,
		readOnlyMessage: ownership.readOnlyMessage,
		surfaceLabel: 'Collect responses',
		surfaceEyebrow: 'Study collection',
		surfaceDescription:
			'Start the selected wave, share respondent access, monitor submissions, and close collection when finished.',
		referenceTitle: 'Collection reference',
		referenceDescription:
			'Launch records, prerequisite checks, and selected wave details stay here for review.',
		summaryRows: [
			{ label: 'Campaigns', value: formatCount(workspace.summary.campaignCount) },
			{ label: 'Live campaigns', value: formatCount(workspace.summary.liveCampaignCount) },
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
			title: 'Response monitor',
			status: workspace.summary.collectionStatus,
			reportVisibilityStatus: workspace.summary.reportVisibilityStatus,
			guidance: workspace.summary.collectionGuidance,
			summaryRows: [
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
					label: 'Latest started',
					value: formatCollectionDateTime(workspace.summary.latestResponseStartedAt)
				},
				{
					label: 'Latest submitted',
					value: formatCollectionDateTime(workspace.summary.latestResponseSubmittedAt)
				}
			]
		},
		collectionOverview: toOperationsCollectionOverview(workspace, scoreCoverage),
		scoreCoverageMonitor: toScoreCoverageMonitor(scoreCoverage, true),
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
			title: campaign.name.trim() || 'Untitled campaign',
			status: toProductReadModelBadgeStatus(campaign.status),
			rows: toOperationsCampaignSummaryRows(campaign)
		})),
		emptyState:
			workspace.campaigns.length === 0
				? {
						title: 'No collection wave yet',
						message: 'Create a campaign draft in setup, then start collection here.'
					}
				: null,
		proofActionTitle: 'Collection actions',
		proofActionDescription:
			'Run the pre-launch check, start collection, share respondent access, monitor submissions, and close the wave.'
	};
}

export function toCampaignSeriesReportsWorkspaceView(
	workspace: CampaignSeriesReportsWorkspaceResponse
) {
	const ownership = toCampaignSeriesOwnership(workspace.series);
	const scoreCoverage = normalizeOperationsScoreCoverage(workspace.scoreCoverage);

	return {
		id: workspace.series.id,
		title: workspace.series.name.trim() || 'Untitled campaign series',
		subtitle: `${workspace.summary.campaignCount} ${workspace.summary.campaignCount === 1 ? 'campaign' : 'campaigns'}, ${workspace.summary.liveCampaignCount} live`,
		ownership,
		canMutate: !ownership.isSample,
		readOnlyMessage: ownership.readOnlyMessage,
		surfaceLabel: 'Review results',
		surfaceEyebrow: 'Study results',
		surfaceDescription:
			'Review result availability, coverage, limitations, and export next use for the selected campaign.',
		referenceTitle: 'Results reference',
		referenceDescription:
			'Selected wave details, limitations, prerequisite checks, and export records stay here for review.',
		resultsOverview: toReportsResultsOverview(workspace, scoreCoverage),
		summaryRows: [
			{ label: 'Campaigns', value: formatCount(workspace.summary.campaignCount) },
			{ label: 'Live campaigns', value: formatCount(workspace.summary.liveCampaignCount) },
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
		scoreCoverageSignal: toScoreCoverageMonitor(scoreCoverage, false),
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
			title: campaign.name.trim() || 'Untitled campaign',
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
	};
}

export function toCampaignSeriesWavesWorkspaceView(
	workspace: CampaignSeriesWavesWorkspaceResponse
) {
	const ownership = toCampaignSeriesOwnership(workspace.series);

	return {
		id: workspace.series.id,
		title: workspace.series.name.trim() || 'Untitled campaign series',
		subtitle: `${workspace.summary.campaignCount} ${workspace.summary.campaignCount === 1 ? 'campaign' : 'campaigns'}, ${workspace.summary.liveCampaignCount} live`,
		ownership,
		canMutate: !ownership.isSample,
		readOnlyMessage: ownership.readOnlyMessage,
		surfaceLabel: 'Compare waves',
		surfaceEyebrow: 'Wave comparison',
		summaryRows: [
			{ label: 'Campaigns', value: formatCount(workspace.summary.campaignCount) },
			{ label: 'Live campaigns', value: formatCount(workspace.summary.liveCampaignCount) },
			{
				label: 'Longitudinal waves',
				value: formatCount(workspace.summary.longitudinalWaveCount)
			},
			{ label: 'Submitted waves', value: formatCount(workspace.summary.submittedWaveCount) },
			{
				label: 'Linked trajectories',
				value: formatCount(workspace.summary.linkedTrajectoryCount)
			},
			{
				label: 'Complete trajectories',
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
						title: 'No waves yet',
						message: 'Create and launch at least two waves before comparing results over time.'
					}
				: null,
		proofActionTitle: 'Comparison actions',
		proofActionDescription:
			'Check whether repeated waves can be compared, then review safe change-over-time summaries.'
	};
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
}) {
	if (input.checking) {
		return {
			state: 'checking',
			title: 'Checking access',
			message: 'Loading authenticated workspace access.',
			tenantId: null,
			userId: null
		};
	}

	if (input.session) {
		return {
			state: 'authenticated',
			title: 'Signed in',
			message: `Tenant ${input.session.tenantId}`,
			tenantId: input.session.tenantId,
			userId: input.session.userId
		};
	}

	if (input.error instanceof ApiError && input.error.status === 401) {
		return {
			state: 'unauthenticated',
			title: 'Sign in required',
			message: 'Sign in before opening tenant product surfaces.',
			tenantId: null,
			userId: null
		};
	}

	if (input.error instanceof ApiError && input.error.status === 403) {
		return {
			state: 'forbidden',
			title: 'Tenant access unavailable',
			message: 'Your session does not have access to this tenant workspace.',
			tenantId: null,
			userId: null
		};
	}

	return {
		state: 'failed',
		title: 'Session check failed',
		message:
			input.error instanceof ApiError
				? `Session check failed with status ${input.error.status}.`
				: 'Session check failed.',
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

function toWorkspaceTotalRows(totals: WorkspaceOverviewResponse['totals']): DisplayRow[] {
	return [
		{ label: 'Campaign series', value: formatCount(totals.campaignSeriesCount) },
		{ label: 'Campaigns', value: formatCount(totals.campaignCount) },
		{ label: 'Live campaigns', value: formatCount(totals.liveCampaignCount) },
		{ label: 'Submitted responses', value: formatCount(totals.submittedResponseCount) },
		{ label: 'Export files', value: formatCount(totals.exportArtifactCount) }
	];
}

function toWorkspaceCommandItem(item: WorkspaceOverviewResponse['commandCenter']['items'][number]) {
	const surfaceLabel = toWorkspaceCommandSurfaceLabel(item.surface);
	const rows: DisplayRow[] = [{ label: 'Surface', value: surfaceLabel }];

	return {
		id: item.id,
		title: item.title.trim() || 'Workspace action',
		description: item.description.trim() || 'Open the linked product surface for details.',
		href: item.route,
		actionLabel: item.actionLabel.trim() || 'Open',
		status: toProductReadModelBadgeStatus(item.state),
		priority: item.priority,
		surfaceLabel,
		rows
	};
}

const workspaceLifecycleSteps = [
	{
		id: 'prepare',
		label: 'Prepare',
		description: 'Set up the instrument, questions, scoring, and launch rules.'
	},
	{
		id: 'collect',
		label: 'Collect',
		description: 'Launch the study and track response progress.'
	},
	{
		id: 'review',
		label: 'Review',
		description: 'Inspect coverage, findings, limitations, and comparisons.'
	},
	{
		id: 'export',
		label: 'Export',
		description: 'Use generated CSV and codebook files for analysis.'
	}
] as const;

function toWorkspaceStudyCard(item: CampaignSeriesListItemResponse) {
	const card = toCampaignSeriesCard(item);
	const action = toWorkspaceStudyAction(item);

	return {
		...card,
		actionLabel: action.label,
		actionHref: action.href
	};
}

function toWorkspaceStudyAction(item: CampaignSeriesListItemResponse) {
	const baseHref = `/app/campaign-series/${item.id}`;
	const isSample = item.isSample === true || item.studyKind === 'sample';

	if (isSample) {
		if (item.submittedResponseCount > 0) {
			return { label: 'Review sample results', href: `${baseHref}/reports` };
		}

		if (item.liveCampaignCount > 0) {
			return { label: 'Inspect sample collection', href: `${baseHref}/operations` };
		}

		if (item.campaignCount === 0 || item.readinessStatus === 'not_configured') {
			return { label: 'Inspect sample setup', href: `${baseHref}/setup` };
		}

		return { label: 'Open study', href: baseHref };
	}

	if (item.readinessStatus === 'not_configured') {
		return { label: 'Continue setup', href: `${baseHref}/setup` };
	}

	if (item.liveCampaignCount > 0) {
		return { label: 'Monitor collection', href: `${baseHref}/operations` };
	}

	if (item.submittedResponseCount > 0) {
		return { label: 'Review results', href: `${baseHref}/reports` };
	}

	return { label: 'Open study', href: baseHref };
}

function toWorkspaceCommandSurfaceLabel(surface: string) {
	const labels: Record<string, string> = {
		campaign_series: 'Campaign series',
		directory: 'Directory',
		operations: 'Operations',
		reports: 'Reports',
		setup: 'Setup',
		team: 'Team',
		waves: 'Waves',
		workspace: 'Workspace'
	};

	return labels[surface] ?? labelFromCode(surface);
}

const campaignSeriesPortfolioStatusOptions = [
	{ value: 'all', label: 'All readiness' },
	{ value: 'not_configured', label: 'Not configured' },
	{ value: 'pending', label: 'Pending' },
	{ value: 'proof_only', label: 'Preview' }
] as const;

const campaignSeriesPortfolioSortOptions = [
	{ value: 'activity_desc', label: 'Latest activity' },
	{ value: 'updated_desc', label: 'Recently updated' },
	{ value: 'created_desc', label: 'Recently created' },
	{ value: 'name_asc', label: 'Name A-Z' }
] as const;

const campaignSeriesPortfolioVisibilityOptions = [
	{ value: 'active', label: 'Active' },
	{ value: 'archived', label: 'Archived' },
	{ value: 'all', label: 'All visibility' }
] as const;

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
		{ label: 'Campaigns', value: formatCount(totals.campaignCount) },
		{ label: 'Live campaigns', value: formatCount(totals.liveCampaignCount) },
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
				{ label: 'Campaigns', value: formatCount(hub.totals.campaignCount) },
				{ label: 'Live campaigns', value: formatCount(hub.totals.liveCampaignCount) }
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
				{ label: 'Campaigns', value: formatCount(hub.totals.campaignCount) },
				{ label: 'Live campaigns', value: formatCount(hub.totals.liveCampaignCount) },
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
		title: campaign.name.trim() || 'Untitled campaign',
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
				? 'Template is available for campaign drafts.'
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
				? 'Results setup is available for launch-readiness checks.'
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
			label: 'Campaign draft',
			status: campaign ? 'ready' : 'blocked',
			badgeLabel: campaign ? 'Ready' : 'Blocked',
			summary: campaign
				? `${campaign.name.trim() || 'Untitled campaign'} / ${humanizeValue(campaign.status)} / ${humanizeValue(campaign.responseIdentityMode)}`
				: 'Missing campaign draft',
			guidance: campaign
				? 'Campaign draft is ready for recipient setup and launch-readiness checks.'
				: 'Create a campaign draft before checking launch readiness.',
			detailRows: campaign
				? [
						{ label: 'Selected campaign', value: campaign.name.trim() || 'Untitled campaign' },
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
							: 'Create a campaign draft before checking launch readiness.'
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
		{ label: 'Selected campaign', value: campaign.name.trim() || 'Untitled campaign' },
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
		{ label: 'Selected campaign', value: campaign.name.trim() || 'Untitled campaign' },
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
	includeCampaignCounts: boolean
) {
	const coverage = normalizeOperationsScoreCoverage(scoreCoverage);
	const summaryRows: DisplayRow[] = [
		{ label: 'Submitted responses', value: formatCount(coverage.submittedResponseCount) },
		{ label: 'Scored submitted', value: formatCount(coverage.scoredSubmittedResponseCount) },
		{ label: 'Unscored submitted', value: formatCount(coverage.unscoredSubmittedResponseCount) },
		{ label: 'Not configured', value: formatCount(coverage.notConfiguredSubmittedResponseCount) }
	];

	if (includeCampaignCounts) {
		summaryRows.push(
			{
				label: 'Campaigns with scoring',
				value: formatCount(coverage.campaignsWithScoringRuleCount)
			},
			{
				label: 'Campaigns without scoring',
				value: formatCount(coverage.campaignsWithoutScoringRuleCount)
			}
		);
	}

	summaryRows.push({
		label: 'Latest scoring activity',
		value: formatCollectionDateTime(coverage.latestScoringActivityAt)
	});

	return {
		title: 'Score coverage',
		status: coverage.status,
		guidance: coverage.guidance,
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
	scoreCoverage: CampaignSeriesScoreCoverageResponse
): OperationsCollectionOverviewItem[] {
	return [
		toOperationsCollectionStateItem(workspace),
		toOperationsRespondentAccessItem(workspace),
		toOperationsResponseProgressItem(workspace),
		toOperationsScoreReadinessItem(workspace, scoreCoverage)
	];
}

function toOperationsCollectionStateItem(
	workspace: CampaignSeriesOperationsWorkspaceResponse
): OperationsCollectionOverviewItem {
	const campaign = workspace.selectedCampaign;

	if (!campaign) {
		return {
			id: 'collection_state',
			label: 'Collection state',
			status: 'blocked',
			badgeLabel: 'Blocked',
			summary: 'No selected campaign is collecting responses',
			guidance: 'Create and launch a campaign before collecting responses.',
			detailRows: [
				{ label: 'Selected campaign', value: 'Missing' },
				{ label: 'Status', value: humanizeValue(workspace.summary.collectionStatus) },
				{ label: 'Collection started', value: 'Not available' },
				{
					label: 'Missing prerequisites',
					value: formatCount(workspace.summary.missingPrerequisiteCount)
				}
			]
		};
	}

	const status = toProductReadModelBadgeStatus(campaign.status);

	return {
		id: 'collection_state',
		label: 'Collection status',
		status,
		badgeLabel: sentenceCase(humanizeValue(campaign.status)),
		summary: `${campaign.name.trim() || 'Untitled campaign'} is ${humanizeValue(campaign.status)}`,
		guidance: campaign.collectionGuidance || workspace.summary.collectionGuidance,
		detailRows: [
			{ label: 'Selected campaign', value: campaign.name.trim() || 'Untitled campaign' },
			{ label: 'Status', value: humanizeValue(campaign.status) },
			{ label: 'Collection started', value: formatCollectionDateTime(campaign.latestLaunchAt) },
			{
				label: 'Missing prerequisites',
				value: formatCount(workspace.summary.missingPrerequisiteCount)
			}
		]
	};
}

function toOperationsRespondentAccessItem(
	workspace: CampaignSeriesOperationsWorkspaceResponse
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
		label: 'Respondent access',
		status,
		badgeLabel:
			status === 'ready' ? 'Access ready' : status === 'pending' ? 'Preparing access' : 'Blocked',
		summary: `${formatAccessCount(openLinkAssignments, 'respondent link')}, ${formatAccessCount(sentInvitations, 'sent email')}`,
		guidance: toRespondentAccessGuidance(openLinkAssignments, sentInvitations, deliveryAttempts),
		detailRows: [
			{
				label: 'Identity mode',
				value: campaign ? humanizeValue(campaign.responseIdentityMode) : 'Missing'
			},
			{ label: 'Respondent links', value: formatCount(openLinkAssignments) },
			{ label: 'Queued emails', value: formatCount(queuedInvitations) },
			{ label: 'Sent emails', value: formatCount(sentInvitations) },
			{ label: 'Failed emails', value: formatCount(failedInvitations) },
			{ label: 'Suppressed emails', value: formatCount(bouncedInvitations) },
			{ label: 'Latest email activity', value: formatCollectionDateTime(latestDeliveryAttempt) }
		]
	};
}

function toOperationsResponseProgressItem(
	workspace: CampaignSeriesOperationsWorkspaceResponse
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
		label: 'Response progress',
		status,
		badgeLabel:
			submittedResponses > 0 ? `${formatCount(submittedResponses)} submitted` : 'No submissions',
		summary: `${formatCount(startedResponses)} started, ${formatCount(draftResponses)} draft, ${formatCount(submittedResponses)} submitted`,
		guidance: workspace.summary.collectionGuidance,
		detailRows: [
			{ label: 'Started responses', value: formatCount(startedResponses) },
			{ label: 'Draft responses', value: formatCount(draftResponses) },
			{ label: 'Submitted responses', value: formatCount(submittedResponses) },
			{
				label: 'Latest started',
				value: formatCollectionDateTime(workspace.summary.latestResponseStartedAt)
			},
			{
				label: 'Latest submitted',
				value: formatCollectionDateTime(workspace.summary.latestResponseSubmittedAt)
			}
		]
	};
}

function toOperationsScoreReadinessItem(
	workspace: CampaignSeriesOperationsWorkspaceResponse,
	scoreCoverage: CampaignSeriesScoreCoverageResponse
): OperationsCollectionOverviewItem {
	const submittedResponses = scoreCoverage.submittedResponseCount;
	const status = toOperationsScoreReadinessStatus(scoreCoverage);

	return {
		id: 'score_readiness',
		label: 'Score and report readiness',
		status,
		badgeLabel: toOperationsScoreReadinessBadgeLabel(scoreCoverage, status),
		summary:
			submittedResponses > 0
				? `${formatCount(scoreCoverage.scoredSubmittedResponseCount)} of ${formatCount(submittedResponses)} submitted responses scored`
				: 'No submitted responses to score yet',
		guidance: scoreCoverage.guidance,
		detailRows: [
			{
				label: 'Report visibility',
				value: humanizeValue(workspace.summary.reportVisibilityStatus)
			},
			{ label: 'Score coverage', value: humanizeValue(scoreCoverage.status) },
			{ label: 'Scored submitted', value: formatCount(scoreCoverage.scoredSubmittedResponseCount) },
			{
				label: 'Unscored submitted',
				value: formatCount(scoreCoverage.unscoredSubmittedResponseCount)
			},
			{
				label: 'Not configured',
				value: formatCount(scoreCoverage.notConfiguredSubmittedResponseCount)
			},
			{
				label: 'Latest scoring activity',
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
	status: ProductReadModelBadgeStatus
) {
	if (scoreCoverage.status === 'complete') {
		return 'Reports ready';
	}

	if (scoreCoverage.status === 'no_submissions') {
		return 'No submissions';
	}

	if (status === 'not_configured') {
		return 'Not configured';
	}

	return sentenceCase(humanizeValue(scoreCoverage.status));
}

function toRespondentAccessGuidance(
	openLinkAssignments: number,
	sentInvitations: number,
	deliveryAttempts: number
) {
	if (openLinkAssignments > 0 && (sentInvitations > 0 || deliveryAttempts > 0)) {
		return 'Respondents can enter through shared links and sent emails.';
	}

	if (openLinkAssignments > 0) {
		return 'Respondents can enter through shared links.';
	}

	if (sentInvitations > 0 || deliveryAttempts > 0) {
		return 'Respondents can enter through sent emails.';
	}

	return 'Prepare respondent access before collecting responses.';
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
				{ label: 'Selected campaign', value: 'Missing' },
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
		summary: `${campaign.name.trim() || 'Untitled campaign'} has ${reportStatus} results from ${formatCount(campaign.submittedResponseCount)} submitted responses.`,
		guidance:
			'Use this as a preview of current findings until scoring coverage, disclosure, and finality are complete.',
		detailRows: [
			{ label: 'Selected campaign', value: campaign.name.trim() || 'Untitled campaign' },
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
		{ label: 'Selected campaign', value: campaign.name.trim() || 'Untitled campaign' },
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
		{ label: 'Linked trajectories', value: formatCount(wave.linkedTrajectoryCount) }
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
					message: 'Create a campaign draft before running launch readiness.'
				},
				proofActionTitle: 'Setup actions',
				proofActionDescription:
					'Import the instrument, prepare template and scoring drafts, create a campaign draft, and check launch readiness.'
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
				label: 'Compare waves',
				eyebrow: 'Wave comparison',
				emptyState: {
					title: 'No waves yet',
					message: 'Create and launch at least two waves before comparing results over time.'
				},
				proofActionTitle: 'Comparison actions',
				proofActionDescription:
					'Check whether repeated waves can be compared, then review safe change-over-time summaries.'
			};
	}
}

function toGovernanceRow(label: string, status: string) {
	return {
		label,
		value: humanizeValue(status),
		status: toProductReadModelBadgeStatus(status)
	};
}

function toCampaignSeriesCard(item: CampaignSeriesListItemResponse) {
	const archived = item.archived === true;
	const ownership = toCampaignSeriesOwnership(item);
	const lifecycle = toCampaignSeriesLifecycle(item, ownership);
	const title = item.name.trim() || 'Untitled campaign series';
	const rows: DisplayRow[] = [
		{ label: 'Campaigns', value: formatCount(item.campaignCount) },
		{ label: 'Live campaigns', value: formatCount(item.liveCampaignCount) },
		{ label: 'Submitted responses', value: formatCount(item.submittedResponseCount) },
		{ label: 'Latest activity', value: latestActivityLabel(item) }
	];
	if (archived) {
		rows.push({ label: 'Archived', value: item.archivedAt ?? 'Not available' });
	}

	return {
		id: item.id,
		title,
		href: `/app/campaign-series/${item.id}`,
		status: archived ? 'archived' : toProductReadModelBadgeStatus(item.readinessStatus),
		archived,
		archiveActionLabel: archived ? 'Restore' : 'Archive',
		canMutate: !ownership.isSample,
		duplicateAction: ownership.isSample
			? {
					label: 'Duplicate as study',
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

const campaignSeriesLifecycleLabels: Record<CampaignSeriesLifecycleId, string> = {
	needs_setup: 'Needs setup',
	in_collection: 'In collection',
	results_ready: 'Results ready',
	archived: 'Archived',
	open: 'Open study'
};

const campaignSeriesLifecycleDescriptions: Record<CampaignSeriesLifecycleId, string> = {
	needs_setup: 'Studies that need setup before collection is useful.',
	in_collection: 'Studies with live collection activity to monitor.',
	results_ready: 'Studies with submitted responses ready for review.',
	archived: 'Studies kept for reference after active work ended.',
	open: 'Studies available for normal inspection.'
};

function toCampaignSeriesStudySections(items: CampaignSeriesCardView[]) {
	const sampleItems = items.filter((item) => item.ownership.isSample);
	const ownItems = items.filter((item) => !item.ownership.isSample);

	return [
		{
			id: 'sample_studies',
			title: 'Sample studies',
			description: 'Read-only examples you can inspect before creating your own study.',
			emptyState: 'No sample studies match this view. Clear filters to inspect examples.',
			groups: toCampaignSeriesLifecycleGroups(sampleItems)
		},
		{
			id: 'your_studies',
			title: 'Your studies',
			description: 'Editable studies owned by this workspace.',
			emptyState:
				'No own studies match this view. Clear filters or create your study when you have setup access.',
			groups: toCampaignSeriesLifecycleGroups(ownItems)
		}
	];
}

function toCampaignSeriesLifecycleGroups(items: CampaignSeriesCardView[]) {
	return campaignSeriesLifecycleOrder
		.map((id) => ({
			id,
			label: campaignSeriesLifecycleLabels[id],
			description: campaignSeriesLifecycleDescriptions[id],
			items: items.filter((item) => item.lifecycle.id === id)
		}))
		.filter((group) => group.items.length > 0);
}

function toCampaignSeriesLifecycle(
	item: CampaignSeriesListItemResponse,
	ownership: CampaignSeriesOwnershipView
) {
	const baseHref = `/app/campaign-series/${item.id}`;
	const isSample = ownership.isSample;

	if (item.archived === true) {
		return {
			id: 'archived' as const,
			label: 'Archived',
			status: 'archived' as ProductReadModelBadgeStatus,
			actionLabel: 'Open archived study',
			actionHref: baseHref
		};
	}

	if (item.submittedResponseCount > 0) {
		return {
			id: 'results_ready' as const,
			label: 'Results ready',
			status: 'ready' as ProductReadModelBadgeStatus,
			actionLabel: isSample ? 'Review sample results' : 'Review results',
			actionHref: `${baseHref}/reports`
		};
	}

	if (item.liveCampaignCount > 0) {
		return {
			id: 'in_collection' as const,
			label: 'In collection',
			status: 'live' as ProductReadModelBadgeStatus,
			actionLabel: isSample ? 'Inspect collection' : 'Monitor collection',
			actionHref: `${baseHref}/operations`
		};
	}

	if (item.campaignCount === 0 || item.readinessStatus === 'not_configured') {
		return {
			id: 'needs_setup' as const,
			label: 'Needs setup',
			status: 'not_configured' as ProductReadModelBadgeStatus,
			actionLabel: isSample ? 'Inspect setup' : 'Continue setup',
			actionHref: `${baseHref}/setup`
		};
	}

	return {
		id: 'open' as const,
		label: 'Open study',
		status: toProductReadModelBadgeStatus(item.readinessStatus),
		actionLabel: isSample ? 'Inspect study' : 'Open study',
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
	series: CampaignSeriesOwnershipMetadata
): CampaignSeriesOwnershipView {
	const isSample = series.isSample === true || series.studyKind === 'sample';
	const sampleScenario = series.sampleScenario ?? null;

	return {
		label: isSample ? 'Sample study' : 'Your study',
		badgeStatus: isSample ? 'demo' : 'neutral',
		isSample,
		sampleScenario,
		readOnlyReason: series.readOnlyReason ?? null,
		readOnlyMessage: isSample ? toSampleStudyReadOnlyMessage(sampleScenario) : null
	};
}

function toSampleStudyReadOnlyMessage(sampleScenario: string | null) {
	switch (sampleScenario) {
		case 'setup':
			return 'Setup sample: read-only starter content showing study preparation before launch.';
		case 'blocked':
			return 'Setup sample: read-only starter content showing blocked preparation before launch.';
		case 'in_collection':
			return 'Collection sample: read-only starter content showing live or partial response collection.';
		case 'longitudinal':
			return 'Longitudinal sample: read-only starter content showing repeated waves and linked trajectory review.';
		case 'mixed_lifecycle':
		case 'completed':
			return 'Results sample: read-only starter content showing collected responses, scores, reports, and exports.';
		default:
			return 'Sample study: read-only starter content you can inspect before duplicating.';
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
	library: ExportArtifactLibraryResponse
): ExportArtifactLibraryOverviewItem[] {
	const { totalCount, downloadableCount, failedCount, pendingCount } = library.summary;
	const purposeLabels = uniqueValues(
		library.artifacts.map((artifact) => toExportArtifactPurpose(artifact.artifactType).label)
	);
	const sourceLabels = uniqueValues(library.artifacts.map((artifact) => artifact.targetLabel));
	const reportSummaryCount = library.artifacts.filter(
		(artifact) => toExportArtifactPurpose(artifact.artifactType).kind === 'report_summary'
	).length;
	const responseDatasetCount = library.artifacts.filter(
		(artifact) => toExportArtifactPurpose(artifact.artifactType).kind === 'response_dataset'
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
			label: 'Downloadable files',
			status: downloadableCount > 0 ? 'ready' : 'empty',
			badgeLabel: `${formatCount(downloadableCount)} downloadable`,
			summary:
				downloadableCount > 0
					? `${formatCount(downloadableCount)} export ${pluralize(
							downloadableCount,
							'file is',
							'files are'
						)} ready to download.`
					: 'No export files are ready to download yet.',
			guidance:
				downloadableCount > 0
					? responseDatasetCount > 0
						? 'Use response dataset exports for analysis handoff. Use report-summary exports for review packets, client summaries, or codebook checks.'
						: 'Report-summary files are downloadable for review packets, client summaries, or codebook checks. No analysis-ready response dataset is available yet.'
					: 'Create an export from a study results page after results are available.',
			detailRows: [
				{ label: 'Export files', value: formatCount(totalCount) },
				{ label: 'Downloadable', value: formatCount(downloadableCount) },
				{ label: 'Report-summary exports', value: formatCount(reportSummaryCount) },
				{ label: 'Response datasets', value: formatCount(responseDatasetCount) }
			]
		},
		{
			id: 'attention_needed',
			label: 'Needs attention',
			status: failedCount > 0 ? 'failed' : pendingCount > 0 ? 'pending' : 'ready',
			badgeLabel:
				failedCount > 0
					? `${formatCount(failedCount)} failed`
					: pendingCount > 0
						? `${formatCount(pendingCount)} pending`
						: 'No attention items',
			summary:
				failedCount > 0
					? `${formatCount(failedCount)} export ${pluralize(
							failedCount,
							'file needs',
							'files need'
						)} attention.`
					: pendingCount > 0
						? `${formatCount(pendingCount)} export ${pluralize(
								pendingCount,
								'file is',
								'files are'
							)} still queued or rendering.`
						: 'No failed or pending export files.',
			guidance:
				failedCount > 0
					? 'Review the failed export file, then recreate it from the source study after the cause is resolved.'
					: pendingCount > 0
						? 'Wait for generation to finish before using the export file for handoff.'
						: 'New export issues will appear here when generation fails or remains pending.',
			detailRows: [
				{ label: 'Failed', value: formatCount(failedCount) },
				{ label: 'Pending', value: formatCount(pendingCount) }
			]
		},
		{
			id: 'artifact_purpose',
			label: 'File purpose',
			status: totalCount > 0 ? 'ready' : 'empty',
			badgeLabel:
				totalCount > 0
					? `${formatCount(purposeLabels.length)} ${pluralize(
							purposeLabels.length,
							'purpose',
							'purposes'
						)}`
					: 'No files',
			summary:
				totalCount > 0
					? `Exports cover ${formatInlineList(purposeLabels)}.`
					: 'No generated export purposes are available yet.',
			guidance:
				totalCount > 0
					? 'Choose report summary exports for result handoff; choose response dataset exports for analysis with the codebook.'
					: 'Create report summary or response dataset exports from a study when results are ready.',
			detailRows: [
				{ label: 'Report summary exports', value: formatCount(reportSummaryCount) },
				{ label: 'Response dataset exports', value: formatCount(responseDatasetCount) }
			]
		},
		{
			id: 'study_context',
			label: 'Study context and next use',
			status: totalCount > 0 ? 'ready' : 'empty',
			badgeLabel:
				totalCount > 0
					? `${formatCount(sourceLabels.length)} ${pluralize(sourceLabels.length, 'source', 'sources')}`
					: 'No sources',
			summary:
				totalCount > 0
					? `Export files are tied to ${formatInlineList(sourceLabels)}.`
					: 'No export files are tied to a study yet.',
			guidance:
				totalCount > 0
					? 'Open the source study or report context when you need to understand how an export file was generated.'
					: 'Generated export files will link back to their study or report context when that context is available.',
			detailRows: [
				{ label: 'Campaign files', value: formatCount(campaignArtifactCount) },
				{ label: 'Study files', value: formatCount(campaignSeriesArtifactCount) }
			]
		}
	];
}

function toExportArtifactLibraryCard(artifact: ExportArtifactLibraryResponse['artifacts'][number]) {
	const purpose = toExportArtifactPurpose(artifact.artifactType);
	const finalityLabel = toExportArtifactFinalityLabel(artifact.dataFinality);

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
				label: 'Study context',
				value: `${sentenceCase(humanizeValue(artifact.targetKind))} / ${artifact.targetLabel}`
			},
			{
				label: 'File type',
				value: sentenceCase(humanizeValue(artifact.artifactType))
			},
			{
				label: 'Format',
				value: sentenceCase(humanizeValue(artifact.format))
			},
			{ label: 'Data finality', value: finalityLabel },
			{ label: 'Rows', value: formatCount(artifact.rowCount) },
			{ label: 'Size', value: formatBytes(artifact.byteSize) },
			{ label: 'Created', value: formatDateTime(artifact.createdAt) },
			{ label: 'Completed', value: formatNullableDateTime(artifact.completedAt) },
			...(artifact.failureReasonCode
				? [{ label: 'Failure', value: artifact.failureReasonCode }]
				: []),
			{ label: 'Download', value: artifact.canDownload ? 'Available' : 'Not available' }
		]
	};
}

function toExportArtifactPurpose(artifactType: string) {
	switch (artifactType) {
		case 'report_proof_csv_codebook':
			return {
				kind: 'report_summary',
				label: 'Report summary export',
				nextUse: 'Use this export for report handoff, summary review, or codebook checks.'
			};
		case 'campaign_series_response_csv_codebook':
			return {
				kind: 'response_dataset',
				label: 'Response dataset export',
				nextUse: 'Use this export for response-level analysis with the generated codebook.'
			};
		default:
			return {
				kind: 'other',
				label: sentenceCase(humanizeValue(artifactType)),
				nextUse: 'Use this export with its source context and generated codebook.'
			};
	}
}

function toExportArtifactFinalityLabel(dataFinality: string | null | undefined) {
	switch (dataFinality) {
		case 'closed_wave':
			return 'Closed wave';
		case 'preliminary_live':
			return 'Preliminary live data';
		case 'not_reportable':
			return 'Not reportable';
		case null:
		case undefined:
			return 'Not tied to a closed wave';
		default:
			return sentenceCase(humanizeValue(dataFinality));
	}
}

function uniqueValues(values: string[]) {
	return [...new Set(values.map((value) => value.trim()).filter(Boolean))];
}

function formatInlineList(values: string[]) {
	if (values.length === 0) {
		return 'no items';
	}

	if (values.length === 1) {
		return values[0];
	}

	if (values.length === 2) {
		return `${values[0]} and ${values[1]}`;
	}

	return `${values.slice(0, -1).join(', ')}, and ${values[values.length - 1]}`;
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

	if (value === 'report_proof_csv_codebook') {
		return 'report summary CSV and codebook';
	}

	if (value === 'campaign_series_response_csv_codebook') {
		return 'response dataset CSV and codebook';
	}

	if (value === 'campaign_report_proof') {
		return 'report summary CSV';
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
		.replace(/\bScoring rule\b/g, 'Results setup')
		.replace(/\bscoring rule\b/g, 'results setup')
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
