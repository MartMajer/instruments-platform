import type { ApiClient } from './client';

export type WorkspaceOverviewResponse = {
	tenantId: string;
	totals: WorkspaceOverviewTotalsResponse;
	commandCenter: WorkspaceCommandCenterResponse;
	recentSeries: CampaignSeriesListItemResponse[];
	studyCollections: WorkspaceStudyCollectionsResponse;
};

export type WorkspaceStudyCollectionsResponse = {
	sampleStudies: CampaignSeriesListItemResponse[];
	ownStudies: CampaignSeriesListItemResponse[];
};

export type WorkspaceOverviewTotalsResponse = {
	campaignSeriesCount: number;
	campaignCount: number;
	liveCampaignCount: number;
	submittedResponseCount: number;
	exportArtifactCount: number;
};

export type WorkspaceCommandCenterResponse = {
	items: WorkspaceCommandCenterItemResponse[];
};

export type WorkspaceCommandCenterItemResponse = {
	id: string;
	title: string;
	description: string;
	state: string;
	surface: string;
	route: string;
	actionLabel: string;
	priority: number;
	campaignSeriesId: string | null;
	campaignId: string | null;
	requiredPermission: string | null;
};

export type TenantSettingsWorkspaceResponse = {
	profile: TenantSettingsProfileResponse;
	counts: TenantSettingsWorkspaceCountsResponse;
	managementLinks: TenantSettingsManagementLinkResponse[];
};

export type TenantSettingsProfileResponse = {
	tenantId: string;
	slug: string;
	name: string;
	region: string;
	defaultLocale: string;
	status: string;
	createdAt: string;
	updatedAt: string;
};

export type TenantSettingsWorkspaceCountsResponse = {
	campaignSeriesCount: number;
	campaignCount: number;
	liveCampaignCount: number;
	submittedResponseCount: number;
	subjectCount: number;
	subjectGroupCount: number;
	tenantMemberCount: number;
	tenantRoleCount: number;
	exportArtifactCount: number;
};

export type TenantSettingsManagementLinkResponse = {
	id: string;
	label: string;
	description: string;
	route: string;
};

export type ExportArtifactLibraryResponse = {
	tenantId: string;
	summary: ExportArtifactLibrarySummaryResponse;
	artifacts: CampaignSeriesReportsExportArtifactResponse[];
};

export type ExportArtifactLibrarySummaryResponse = {
	totalCount: number;
	downloadableCount: number;
	failedCount: number;
	pendingCount: number;
};

export type TenantMemberRosterResponse = {
	tenantId: string;
	members: TenantMemberResponse[];
};

export type TenantMemberResponse = {
	userId: string;
	email: string;
	locale: string;
	createdAt: string;
	lastLoginAt: string | null;
	identityStatus: 'active' | 'pending_provider_link';
	roles: TenantMemberRoleResponse[];
	permissions: string[];
};

export type TenantMemberRoleResponse = {
	roleId: string;
	code: string;
	name: string;
	scopeType: string;
	scopeId: string | null;
	grantedAt: string;
};

export type TenantRoleListResponse = {
	roles: TenantRoleResponse[];
};

export type TenantRoleResponse = {
	roleId: string;
	code: string;
	name: string;
	permissions: string[];
};

export type CreateTenantMemberRequest = {
	email: string;
	roleCode: string;
	locale?: string;
};

export type ChangeTenantMemberRoleRequest = {
	roleCode: string;
};

export type TenantMemberMutationResponse = {
	member: TenantMemberResponse;
};

export type SubjectDirectoryResponse = {
	tenantId: string;
	summary: SubjectDirectorySummaryResponse;
	subjects: SubjectDirectoryItemResponse[];
};

export type SubjectDirectorySummaryResponse = {
	subjectCount: number;
	groupCount: number;
	managerRelationshipCount: number;
};

export type SubjectDirectoryItemResponse = {
	id: string;
	displayName: string | null;
	email: string | null;
	externalId: string | null;
	locale: string;
	attributes: string;
	managerSubjectId: string | null;
	managerDisplayName: string | null;
	directReportCount: number;
	groups: SubjectGroupMembershipResponse[];
};

export type SubjectGroupMembershipResponse = {
	groupId: string;
	groupType: string;
	groupName: string;
	roleInGroup: string | null;
	validFrom: string | null;
	validTo: string | null;
};

export type SubjectGroupListResponse = {
	tenantId: string;
	groups: SubjectGroupResponse[];
};

export type SubjectGroupResponse = {
	id: string;
	type: string;
	name: string;
	parentGroupId: string | null;
	attributes: string;
	memberCount: number;
};

export type CreateSubjectRequest = {
	displayName?: string | null;
	email?: string | null;
	externalId?: string | null;
	locale?: string;
	attributes?: string;
};

export type UpdateSubjectRequest = CreateSubjectRequest;

export type CreateSubjectGroupRequest = {
	type: string;
	name: string;
	parentGroupId?: string | null;
	attributes?: string;
};

export type AddSubjectGroupMemberRequest = {
	subjectId: string;
	roleInGroup?: string | null;
	validFrom?: string | null;
	validTo?: string | null;
};

export type SetSubjectManagerRequest = {
	managerSubjectId?: string | null;
	validFrom?: string | null;
};

export type RespondentRulePreviewRequest = {
	rule: string;
	targetSubjectId?: string | null;
	groupId?: string | null;
	maxRows?: number;
};

export type RespondentRulePreviewResponse = {
	campaignSeriesId: string;
	campaignId: string;
	ruleKind: string;
	role: string;
	summary: RespondentRulePreviewSummaryResponse;
	rows: RespondentRulePreviewRowResponse[];
	warnings: RespondentRulePreviewWarningResponse[];
};

export type RespondentRulePreviewSummaryResponse = {
	targetCount: number;
	respondentCount: number;
	assignmentPairCount: number;
	skippedCount: number;
	warningCount: number;
	truncated: boolean;
};

export type RespondentRulePreviewRowResponse = {
	ordinal: number;
	ruleKind: string;
	role: string;
	target: RespondentRulePreviewSubjectResponse | null;
	respondent: RespondentRulePreviewSubjectResponse | null;
};

export type RespondentRulePreviewSubjectResponse = {
	id: string;
	label: string;
	displayName: string | null;
	email: string | null;
	externalId: string | null;
};

export type RespondentRulePreviewWarningResponse = {
	code: string;
	message: string;
	subjectId: string | null;
	groupId: string | null;
};

export type CampaignSeriesListResponse = {
	items: CampaignSeriesListItemResponse[];
};

export type CampaignSeriesStudyKind = 'own' | 'sample' | string;

export type CampaignSeriesOwnershipMetadata = {
	studyKind: CampaignSeriesStudyKind;
	isSample: boolean;
	sampleScenario: string | null;
	readOnlyReason: string | null;
};

export type CampaignSeriesPortfolioQuery = {
	search?: string | null;
	status?: string | null;
	sort?: string | null;
	visibility?: string | null;
};

export type CampaignSeriesListItemResponse = CampaignSeriesOwnershipMetadata & {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
	campaignCount: number;
	liveCampaignCount: number;
	submittedResponseCount: number;
	latestLaunchAt: string | null;
	latestSubmissionAt: string | null;
	readinessStatus: string;
	archived: boolean;
	archivedAt: string | null;
	archivedByUserId: string | null;
	archiveReason: string | null;
};

export type RenameCampaignSeriesRequest = {
	name: string;
};

export type CampaignSeriesRenameResponse = {
	id: string;
	name: string;
	updatedAt: string;
};

export type DuplicateCampaignSeriesRequest = {
	name: string;
};

export type CampaignSeriesDuplicateResponse = {
	id: string;
	name: string;
	studyKind: CampaignSeriesStudyKind;
	isSample: boolean;
	sourceCampaignSeriesId: string;
};

export type ArchiveCampaignSeriesRequest = {
	reason?: string | null;
};

export type CampaignSeriesArchiveStateResponse = {
	id: string;
	archived: boolean;
	updatedAt: string;
	archivedAt: string | null;
	archivedByUserId: string | null;
	archiveReason: string | null;
};

export type CloseCampaignRequest = {
	reason?: string | null;
};

export type CampaignCloseStateResponse = {
	id: string;
	status: string;
	updatedAt: string;
	closedAt?: string | null;
	closedByUserId?: string | null;
	closeReason?: string | null;
};

export type CampaignSeriesHubResponse = CampaignSeriesOwnershipMetadata & {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
	totals: CampaignSeriesHubTotalsResponse;
	governance: CampaignSeriesGovernanceSummaryResponse;
	lifecycle: CampaignSeriesLifecycleItemResponse[];
	campaigns: CampaignSeriesHubCampaignResponse[];
	archived: boolean;
	archivedAt: string | null;
	archivedByUserId: string | null;
	archiveReason: string | null;
};

export type CampaignSeriesHubTotalsResponse = {
	campaignCount: number;
	liveCampaignCount: number;
	submittedResponseCount: number;
	scoreCount: number;
	exportArtifactCount: number;
};

export type CampaignSeriesGovernanceSummaryResponse = {
	consentStatus: string;
	retentionStatus: string;
	disclosureStatus: string;
	scoringStatus: string;
};

export type CampaignSeriesLifecycleItemResponse = {
	id: 'setup' | 'operations' | 'reports' | 'waves';
	label: string;
	status: string;
	guidance: string;
	route: 'setup' | 'operations' | 'reports' | 'waves';
	actionLabel: string;
};

export type CampaignSeriesHubCampaignResponse = {
	id: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	defaultLocale: string;
	startAt: string | null;
	endAt: string | null;
	latestLaunchAt: string | null;
	submittedResponseCount: number;
	scoreCount: number;
	exportArtifactCount: number;
};

export type CampaignSeriesSetupWorkspaceResponse = {
	series: CampaignSeriesSetupSeriesResponse;
	summary: CampaignSeriesSetupSummaryResponse;
	selectedCampaign: CampaignSeriesSetupCampaignResponse | null;
	template: CampaignSeriesSetupTemplateResponse | null;
	scoring: CampaignSeriesSetupScoringResponse | null;
	policies: CampaignSeriesSetupPolicySummaryResponse;
	readiness: CampaignSeriesSetupReadinessResponse;
	missingPrerequisites: CampaignSeriesSetupMissingPrerequisiteResponse[];
	campaigns: CampaignSeriesSetupCampaignResponse[];
};

export type CampaignSeriesSetupSeriesResponse = CampaignSeriesOwnershipMetadata & {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
};

export type CampaignSeriesSetupSummaryResponse = {
	campaignCount: number;
	liveCampaignCount: number;
	missingPrerequisiteCount: number;
};

export type CampaignSeriesSetupCampaignResponse = {
	id: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	defaultLocale: string;
	templateVersionId: string;
	latestLaunchAt: string | null;
};

export type CampaignSeriesSetupTemplateResponse = {
	templateId: string;
	templateVersionId: string;
	templateName: string;
	semver: string;
	status: string;
	defaultLocale: string;
	instrumentId: string | null;
	questionCount: number;
};

export type CampaignSeriesSetupScoringResponse = {
	id: string;
	ruleKey: string;
	ruleVersion: string;
	status: string;
	source: string;
};

export type CampaignSeriesSetupPolicySummaryResponse = {
	consent: CampaignSeriesSetupPolicyResponse;
	retention: CampaignSeriesSetupPolicyResponse;
	disclosure: CampaignSeriesSetupPolicyResponse;
};

export type CampaignSeriesSetupPolicyResponse = {
	id: string | null;
	version: string | null;
	status: string;
	details?: CampaignSeriesSetupPolicyDetailResponse[] | null;
};

export type CampaignSeriesSetupPolicyDetailResponse = {
	label: string;
	value: string;
};

export type CampaignSeriesSetupReadinessResponse = {
	campaignId: string | null;
	status: string;
	ready: boolean;
};

export type CampaignSeriesSetupMissingPrerequisiteResponse = {
	code: string;
	label: string;
	message: string;
	severity: string;
};

export type CampaignSeriesOperationsWorkspaceResponse = {
	series: CampaignSeriesOperationsSeriesResponse;
	summary: CampaignSeriesOperationsSummaryResponse;
	selectedCampaign: CampaignSeriesOperationsCampaignResponse | null;
	missingPrerequisites: CampaignSeriesOperationsMissingPrerequisiteResponse[];
	campaigns: CampaignSeriesOperationsCampaignResponse[];
	scoreCoverage?: CampaignSeriesScoreCoverageResponse | null;
};

export type CampaignSeriesOperationsSeriesResponse = CampaignSeriesOwnershipMetadata & {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
};

export type CampaignSeriesOperationsSummaryResponse = {
	campaignCount: number;
	liveCampaignCount: number;
	openLinkAssignmentCount: number;
	queuedInvitationCount: number;
	sentInvitationCount: number;
	failedInvitationCount: number;
	deliveryAttemptCount: number;
	startedResponseCount: number;
	draftResponseCount: number;
	submittedResponseCount: number;
	latestResponseStartedAt: string | null;
	latestResponseSubmittedAt: string | null;
	collectionStatus: string;
	reportVisibilityStatus: string;
	collectionGuidance: string;
	missingPrerequisiteCount: number;
};

export type CampaignSeriesOperationsCampaignResponse = {
	id: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	defaultLocale: string;
	latestLaunchSnapshotId: string | null;
	latestLaunchAt: string | null;
	launchSnapshot?: CampaignSeriesOperationsLaunchSnapshotResponse | null;
	closedAt?: string | null;
	closedByUserId?: string | null;
	closeReason?: string | null;
	scoringRuleId?: string | null;
	scoredSubmittedResponseCount?: number;
	unscoredSubmittedResponseCount?: number;
	notConfiguredSubmittedResponseCount?: number;
	latestScoringActivityAt?: string | null;
	scoreCoverageStatus?: string | null;
	startedResponseCount: number;
	draftResponseCount: number;
	submittedResponseCount: number;
	latestResponseStartedAt: string | null;
	latestResponseSubmittedAt: string | null;
	collectionStatus: string;
	reportVisibilityStatus: string;
	collectionGuidance: string;
	openLinkAssignmentCount: number;
	queuedInvitationCount: number;
	sentInvitationCount: number;
	failedInvitationCount: number;
	deliveryAttemptCount: number;
	latestDeliveryAttemptAt: string | null;
};

export type CampaignSeriesOperationsLaunchSnapshotResponse = {
	id: string;
	templateVersionId: string;
	scoringRuleId: string;
	scoringRuleDocumentHash: string;
	consentDocumentId: string | null;
	retentionPolicyId: string | null;
	disclosurePolicyId: string | null;
	responseIdentityMode: string;
	defaultLocale: string;
	templateQuestionCount: number;
	launchedAt: string;
	launchedByUserId: string | null;
};

export type CampaignSeriesOperationsMissingPrerequisiteResponse = {
	code: string;
	label: string;
	message: string;
	severity: string;
};

export type CampaignSeriesScoreCoverageResponse = {
	submittedResponseCount: number;
	scoredSubmittedResponseCount: number;
	unscoredSubmittedResponseCount: number;
	notConfiguredSubmittedResponseCount: number;
	campaignsWithScoringRuleCount: number;
	campaignsWithoutScoringRuleCount: number;
	latestScoringActivityAt: string | null;
	status: string;
	guidance: string;
};

export type CampaignSeriesScoreRemediationResponse = {
	campaignSeriesId: string;
	submittedResponseCount: number;
	eligibleSubmittedResponseCount: number;
	alreadyScoredSubmittedResponseCount: number;
	remediatedSubmittedResponseCount: number;
	skippedNotConfiguredSubmittedResponseCount: number;
	failedSubmittedResponseCount: number;
	latestScoringActivityAt: string | null;
};

export type CampaignSeriesReportsWorkspaceResponse = {
	series: CampaignSeriesReportsSeriesResponse;
	summary: CampaignSeriesReportsSummaryResponse;
	selectedCampaign: CampaignSeriesReportsCampaignResponse | null;
	missingPrerequisites: CampaignSeriesReportsMissingPrerequisiteResponse[];
	exportArtifacts: CampaignSeriesReportsExportArtifactResponse[];
	campaigns: CampaignSeriesReportsCampaignResponse[];
	scoreCoverage?: CampaignSeriesScoreCoverageResponse | null;
};

export type ReportWidgetState = 'ready' | 'empty' | 'blocked' | 'failed' | 'unsupported';

export type ReportWidgetSize = 'third' | 'half' | 'full';

export type ReportWidgetAction = {
	id: string;
	label: string;
	kind: 'api-command/v1' | string;
	href: string;
	method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' | string;
	enabled: boolean;
	disabledReason: string | null;
};

export type ReportWidgetDataSource = {
	href: string;
	method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' | string;
};

export type ReportWidgetPrerequisite = {
	code: string;
	label: string;
	message: string;
	severity: string;
};

export type ReportReadinessSummaryWidgetData = {
	campaignCount: number;
	liveCampaignCount: number;
	reportableCampaignCount: number;
	submittedResponseCount: number;
	scoreCount: number;
	visibleScoreCount: number;
	suppressedScoreCount: number;
	missingPrerequisiteCount: number;
	missingPrerequisites: ReportWidgetPrerequisite[];
};

export type ScoreCoverageSummaryWidgetData = {
	submittedResponseCount: number;
	scoredSubmittedResponseCount: number;
	unscoredSubmittedResponseCount: number;
	notConfiguredSubmittedResponseCount: number;
	campaignsWithScoringRuleCount: number;
	campaignsWithoutScoringRuleCount: number;
	latestScoringActivityAt: string | null;
	status: string;
	guidance: string;
};

export type SelectedCampaignReportStateWidgetData = {
	campaignId: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	defaultLocale: string;
	latestLaunchAt: string | null;
	submittedResponseCount: number;
	scoreCount: number;
	visibleScoreCount: number;
	suppressedScoreCount: number;
	disclosureState: string;
	disclosureKMin: number | null;
	reportStatus: string;
	interpretationStatus: string;
	latestExportArtifactId: string | null;
	latestExportArtifactFileName: string | null;
	latestExportArtifactStatus: string | null;
	latestExportArtifactCreatedAt: string | null;
	latestExportArtifactCompletedAt: string | null;
	latestExportArtifactFailedAt: string | null;
	latestExportArtifactFailureReasonCode: string | null;
	latestExportArtifactCanDownload: boolean;
	closedAt: string | null;
	dataFinality: string;
};

export type ExportArtifactRegistryWidgetData = {
	exportArtifactCount: number;
	artifacts: CampaignSeriesReportsExportArtifactResponse[];
};

export type VisualAnalyticsEntryWidgetData = {
	selectedCampaignId: string | null;
	visibleScoreCount: number;
	suppressedScoreCount: number;
	reportableCampaignCount: number;
};

export type FinalityProvenanceWidgetData = {
	preliminaryLiveReportCount: number;
	closedWaveReportCount: number;
	selectedCampaignId: string | null;
	selectedCampaignStatus: string | null;
	selectedDataFinality: string | null;
	selectedClosedAt: string | null;
	selectedLatestLaunchAt: string | null;
};

export type ReportWidgetData =
	| ReportReadinessSummaryWidgetData
	| ScoreCoverageSummaryWidgetData
	| SelectedCampaignReportStateWidgetData
	| ExportArtifactRegistryWidgetData
	| VisualAnalyticsEntryWidgetData
	| FinalityProvenanceWidgetData
	| Record<string, unknown>;

export type ReportWidgetBase<TKind extends string, TData extends ReportWidgetData | null> = {
	id: string;
	kind: TKind;
	title: string;
	size: ReportWidgetSize;
	state: ReportWidgetState;
	message: string | null;
	data: TData;
	dataSource: ReportWidgetDataSource | null;
	actions: ReportWidgetAction[];
};

export type KnownReportWidget =
	| ReportWidgetBase<'report-readiness-summary/v1', ReportReadinessSummaryWidgetData>
	| ReportWidgetBase<'score-coverage-summary/v1', ScoreCoverageSummaryWidgetData>
	| ReportWidgetBase<'selected-campaign-report-state/v1', SelectedCampaignReportStateWidgetData>
	| ReportWidgetBase<'export-artifact-registry/v1', ExportArtifactRegistryWidgetData>
	| ReportWidgetBase<'visual-analytics-entry/v1', VisualAnalyticsEntryWidgetData>
	| ReportWidgetBase<'finality-provenance-summary/v1', FinalityProvenanceWidgetData>;

export type ReportWidget = KnownReportWidget | ReportWidgetBase<string, ReportWidgetData | null>;

export type CampaignSeriesReportsWidgetManifestResponse = {
	campaignSeriesId: string;
	surface: 'reports';
	surfaceVersion: string;
	layout: {
		kind: string;
		density: string;
	};
	widgets: ReportWidget[];
};

export type CampaignSeriesReportsSeriesResponse = CampaignSeriesOwnershipMetadata & {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
};

export type CampaignSeriesReportsSummaryResponse = {
	campaignCount: number;
	liveCampaignCount: number;
	reportableCampaignCount: number;
	submittedResponseCount: number;
	scoreCount: number;
	exportArtifactCount: number;
	visibleScoreCount: number;
	suppressedScoreCount: number;
	missingPrerequisiteCount: number;
	preliminaryLiveReportCount?: number;
	closedWaveReportCount?: number;
};

export type CampaignSeriesReportsCampaignResponse = {
	id: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	defaultLocale: string;
	latestLaunchSnapshotId: string | null;
	latestLaunchAt: string | null;
	scoringRuleId: string | null;
	consentDocumentId: string | null;
	retentionPolicyId: string | null;
	disclosurePolicyId: string | null;
	submittedResponseCount: number;
	scoreCount: number;
	exportArtifactCount: number;
	visibleScoreCount: number;
	suppressedScoreCount: number;
	disclosureState: string;
	disclosureKMin: number | null;
	reportStatus: string;
	interpretationStatus: string;
	latestExportArtifactId: string | null;
	latestExportArtifactFileName: string | null;
	latestExportArtifactStatus: string | null;
	latestExportArtifactCreatedAt: string | null;
	latestExportArtifactCompletedAt: string | null;
	latestExportArtifactStartedAt: string | null;
	latestExportArtifactFailedAt: string | null;
	latestExportArtifactExpiresAt: string | null;
	latestExportArtifactDeletedAt: string | null;
	latestExportArtifactFailureReasonCode: string | null;
	latestExportArtifactCanDownload: boolean;
	closedAt?: string | null;
	closedByUserId?: string | null;
	closeReason?: string | null;
	dataFinality?: string | null;
};

export type CampaignSeriesReportsMissingPrerequisiteResponse = {
	code: string;
	label: string;
	message: string;
	severity: string;
};

export type CampaignSeriesReportsExportArtifactResponse = {
	id: string;
	targetKind: string;
	targetId: string;
	targetLabel: string;
	campaignId: string | null;
	campaignName: string | null;
	artifactType: string;
	status: string;
	format: string;
	fileName: string;
	rowCount: number;
	byteSize: number;
	checksumSha256: string | null;
	createdAt: string;
	completedAt: string | null;
	startedAt: string | null;
	failedAt: string | null;
	expiresAt: string | null;
	deletedAt: string | null;
	failureReasonCode: string | null;
	canDownload: boolean;
	campaignStatus?: string | null;
	campaignClosedAt?: string | null;
	dataFinality?: string | null;
};

export type CampaignSeriesWavesWorkspaceResponse = {
	series: CampaignSeriesWavesSeriesResponse;
	summary: CampaignSeriesWavesSummaryResponse;
	selectedBaselineWave: CampaignSeriesWavesWaveResponse | null;
	selectedComparisonWave: CampaignSeriesWavesWaveResponse | null;
	comparison: CampaignSeriesWavesComparisonResponse;
	missingPrerequisites: CampaignSeriesWavesMissingPrerequisiteResponse[];
	waves: CampaignSeriesWavesWaveResponse[];
};

export type CampaignSeriesWavesSeriesResponse = CampaignSeriesOwnershipMetadata & {
	id: string;
	name: string;
	createdAt: string;
	updatedAt: string;
};

export type CampaignSeriesWavesSummaryResponse = {
	campaignCount: number;
	liveCampaignCount: number;
	longitudinalWaveCount: number;
	submittedWaveCount: number;
	linkedTrajectoryCount: number;
	completeTrajectoryCount: number;
	comparableScoreCount: number;
	visibleComparisonCount: number;
	suppressedComparisonCount: number;
	blockedComparisonCount: number;
	missingPrerequisiteCount: number;
	preliminaryLiveWaveCount?: number;
	closedWaveCount?: number;
};

export type CampaignSeriesWavesWaveResponse = {
	id: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	defaultLocale: string;
	latestLaunchSnapshotId: string | null;
	latestLaunchAt: string | null;
	scoringRuleId: string | null;
	scoringRuleKey: string | null;
	scoringRuleVersion: string | null;
	disclosurePolicyId: string | null;
	disclosureKMin: number | null;
	submittedResponseCount: number;
	scoreCount: number;
	linkedTrajectoryCount: number;
	waveState: string;
	closedAt?: string | null;
	closedByUserId?: string | null;
	closeReason?: string | null;
	dataFinality?: string | null;
};

export type CampaignSeriesWavesComparisonResponse = {
	status: string;
	disclosureState: string;
	compatibilityState: string;
	interpretationStatus: string;
	disclosureKMin: number | null;
	linkedPairCount: number;
	visibleScoreCount: number;
	suppressedScoreCount: number;
	blockedScoreCount: number;
};

export type CampaignSeriesWavesMissingPrerequisiteResponse = {
	code: string;
	label: string;
	message: string;
	severity: string;
};

export function createProductApi(client: ApiClient) {
	return {
		getWorkspaceOverview: () => client.request<WorkspaceOverviewResponse>('/workspace-overview'),
		getTenantSettings: () => client.request<TenantSettingsWorkspaceResponse>('/tenant-settings'),
		listExportArtifacts: () => client.request<ExportArtifactLibraryResponse>('/export-artifacts'),
		listTenantMembers: () => client.request<TenantMemberRosterResponse>('/tenant-members'),
		listTenantRoles: () => client.request<TenantRoleListResponse>('/tenant-roles'),
		listSubjects: () => client.request<SubjectDirectoryResponse>('/subjects'),
		createSubject: (request: CreateSubjectRequest) =>
			client.request<SubjectDirectoryItemResponse>('/subjects', jsonRequest('POST', request)),
		updateSubject: (subjectId: string, request: UpdateSubjectRequest) =>
			client.request<SubjectDirectoryItemResponse>(
				`/subjects/${encodeURIComponent(subjectId)}`,
				jsonRequest('PUT', request)
			),
		listSubjectGroups: () => client.request<SubjectGroupListResponse>('/subject-groups'),
		createSubjectGroup: (request: CreateSubjectGroupRequest) =>
			client.request<SubjectGroupResponse>('/subject-groups', jsonRequest('POST', request)),
		addSubjectGroupMember: (groupId: string, request: AddSubjectGroupMemberRequest) =>
			client.request<SubjectGroupMembershipResponse>(
				`/subject-groups/${encodeURIComponent(groupId)}/members`,
				jsonRequest('POST', request)
			),
		setSubjectManager: (subjectId: string, request: SetSubjectManagerRequest) =>
			client.request<SubjectDirectoryItemResponse>(
				`/subjects/${encodeURIComponent(subjectId)}/manager`,
				jsonRequest('PUT', request)
			),
		previewRespondentRule: (
			seriesId: string,
			campaignId: string,
			request: RespondentRulePreviewRequest
		) =>
			client.request<RespondentRulePreviewResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/campaigns/${encodeURIComponent(campaignId)}/respondent-rule-preview`,
				jsonRequest('POST', request)
			),
		createTenantMember: (request: CreateTenantMemberRequest) =>
			client.request<TenantMemberMutationResponse>('/tenant-members', jsonRequest('POST', request)),
		changeTenantMemberRole: (userId: string, request: ChangeTenantMemberRoleRequest) =>
			client.request<TenantMemberMutationResponse>(
				`/tenant-members/${encodeURIComponent(userId)}/tenant-role`,
				jsonRequest('PUT', request)
			),
		listCampaignSeries: (query?: CampaignSeriesPortfolioQuery) =>
			client.request<CampaignSeriesListResponse>(withQuery('/campaign-series', query)),
		renameCampaignSeries: (seriesId: string, request: RenameCampaignSeriesRequest) =>
			client.request<CampaignSeriesRenameResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}`,
				jsonRequest('PATCH', request)
			),
		duplicateCampaignSeries: (seriesId: string, request: DuplicateCampaignSeriesRequest) =>
			client.request<CampaignSeriesDuplicateResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/duplicate`,
				jsonRequest('POST', request)
			),
		archiveCampaignSeries: (seriesId: string, request: ArchiveCampaignSeriesRequest = {}) =>
			client.request<CampaignSeriesArchiveStateResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/archive`,
				jsonRequest('POST', request)
			),
		restoreCampaignSeries: (seriesId: string) =>
			client.request<CampaignSeriesArchiveStateResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/restore`,
				jsonRequest('POST', {})
			),
		closeCampaign: (seriesId: string, campaignId: string, request: CloseCampaignRequest = {}) =>
			client.request<CampaignCloseStateResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/campaigns/${encodeURIComponent(campaignId)}/close`,
				jsonRequest('POST', request)
			),
		remediateCampaignSeriesScores: (seriesId: string) =>
			client.request<CampaignSeriesScoreRemediationResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/score-remediation`,
				jsonRequest('POST', {})
			),
		getCampaignSeriesHub: (seriesId: string) =>
			client.request<CampaignSeriesHubResponse>(`/campaign-series/${encodeURIComponent(seriesId)}`),
		getCampaignSeriesSetupWorkspace: (seriesId: string) =>
			client.request<CampaignSeriesSetupWorkspaceResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/setup-workspace`
			),
		getCampaignSeriesOperationsWorkspace: (seriesId: string) =>
			client.request<CampaignSeriesOperationsWorkspaceResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/operations-workspace`
			),
		getCampaignSeriesReportsWorkspace: (seriesId: string) =>
			client.request<CampaignSeriesReportsWorkspaceResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/reports-workspace`
			),
		getCampaignSeriesReportsWidgetManifest: (seriesId: string) =>
			client.request<CampaignSeriesReportsWidgetManifestResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/reports-widget-manifest`
			),
		getCampaignSeriesWavesWorkspace: (seriesId: string) =>
			client.request<CampaignSeriesWavesWorkspaceResponse>(
				`/campaign-series/${encodeURIComponent(seriesId)}/waves-workspace`
			)
	};
}

function withQuery(path: string, query: CampaignSeriesPortfolioQuery | undefined) {
	if (!query) {
		return path;
	}

	const parameters = new URLSearchParams();
	appendQueryValue(parameters, 'search', query.search);
	appendQueryValue(parameters, 'status', query.status);
	appendQueryValue(parameters, 'sort', query.sort);
	appendQueryValue(parameters, 'visibility', query.visibility);

	const serialized = parameters.toString();
	return serialized.length > 0 ? `${path}?${serialized}` : path;
}

function appendQueryValue(
	parameters: URLSearchParams,
	name: string,
	value: string | null | undefined
) {
	const normalized = value?.trim();
	if (normalized) {
		parameters.set(name, normalized);
	}
}

function jsonRequest(method: string, body: unknown): RequestInit {
	return {
		method,
		headers: {
			'content-type': 'application/json'
		},
		body: JSON.stringify(body)
	};
}
