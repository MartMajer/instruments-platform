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

export type EnsureSampleStudiesResponse = {
	tenantId: string;
	existingSampleStudyCount: number;
	createdSampleStudyCount: number;
	createdCampaignSeriesIds: string[];
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
	/** Values the title/description were composed from, for localized recomposition. */
	params?: Record<string, string> | null;
};

export type TenantSettingsWorkspaceResponse = {
	profile: TenantSettingsProfileResponse;
	counts: TenantSettingsWorkspaceCountsResponse;
	reportBranding: TenantSettingsReportBrandingResponse;
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

export type TenantSettingsReportBrandingResponse = {
	organizationLabel: string;
	reportTitle: string;
	brandingSource: string;
	logoMode: string;
	accentColorHex: string;
	layoutVariant: string;
	deferredCustomizations: string[];
};

export type UpdateTenantReportBrandingRequest = {
	organizationLabel: string;
	reportTitle: string;
	accentColorHex: string;
	layoutVariant: string;
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

export type DirectoryConnectionStateResponse = {
	tenantId: string;
	provider: string;
	status: string;
	displayName: string;
	primaryDomain: string | null;
	grantedScopes: string[];
	lastConsentAt: string | null;
	lastSuccessfulImportAt: string | null;
	updatedAt: string | null;
	connected: boolean;
};

export type DirectoryImportRunHistoryResponse = {
	tenantId: string;
	runs: DirectoryImportRunListItemResponse[];
};

export type DirectoryImportRunListItemResponse = {
	id: string;
	directoryConnectionId: string;
	directoryImportRuleId: string | null;
	previewRunId: string | null;
	provider: string;
	mode: string;
	status: string;
	rowCount: number;
	importedRowCount: number;
	failedRowCount: number;
	warningCategoryCount: number;
	warningCategories: string[];
	createdAt: string;
	startedAt: string | null;
	completedAt: string | null;
};

export type DirectoryImportRuleListResponse = {
	tenantId: string;
	rules: DirectoryImportRuleResponse[];
};

export type DirectoryImportRuleResponse = {
	id: string;
	directoryConnectionId: string;
	name: string;
	status: string;
	stalePolicy: string;
	retainedFields: string[];
	createdAt: string;
	updatedAt: string;
};

export type SaveMicrosoftGraphImportRuleRequest = {
	name: string;
	markMissingSubjectsStale?: boolean;
	retainedFields?: string[] | null;
};

export type MicrosoftGraphDirectoryImportUser = {
	id: string;
	mail?: string | null;
	userPrincipalName?: string | null;
	displayName?: string | null;
	preferredLanguage?: string | null;
	department?: string | null;
	jobTitle?: string | null;
	employeeType?: string | null;
	officeLocation?: string | null;
	userType?: string | null;
	accountEnabled?: boolean;
};

export type MicrosoftGraphDirectoryImportGroup = {
	id: string;
	displayName: string;
};

export type MicrosoftGraphDirectoryImportMembership = {
	userId: string;
	groupId: string;
};

export type MicrosoftGraphDirectoryImportManagerRelationship = {
	userId: string;
	managerUserId: string;
};

export type MicrosoftGraphDirectoryImportWarning = {
	code: string;
	subject: string;
	message: string;
};

export type PreviewMicrosoftGraphImportRuleRequest = {
	microsoftTenantId: string;
	users: MicrosoftGraphDirectoryImportUser[];
	groups: MicrosoftGraphDirectoryImportGroup[];
	memberships: MicrosoftGraphDirectoryImportMembership[];
	allowUserPrincipalNameEmailFallback?: boolean;
	excludeGuests?: boolean;
	excludeDisabledAccounts?: boolean;
	managerRelationships?: MicrosoftGraphDirectoryImportManagerRelationship[] | null;
};

export type ApplyMicrosoftGraphImportRuleRequest = PreviewMicrosoftGraphImportRuleRequest & {
	previewImportRunId: string;
};

export type LiveApplyMicrosoftGraphImportRuleRequest = {
	previewImportRunId: string;
};

export type MicrosoftGraphImportRulePreviewResponse = {
	tenantId: string;
	directoryImportRuleId: string;
	directoryConnectionId: string;
	import: SubjectDirectoryCsvImportResponse;
	includedUserCount: number;
	includedMembershipCount: number;
	warnings: MicrosoftGraphDirectoryImportWarning[];
};

export type MicrosoftGraphImportRuleApplyResponse = MicrosoftGraphImportRulePreviewResponse;

export type CreateMicrosoftGraphConsentRequest = {
	requestedScopes?: string[] | null;
};

export type MicrosoftGraphConsentRequestResponse = {
	tenantId: string;
	consentRequestId: string;
	directoryConnectionId: string;
	provider: string;
	status: string;
	requestedScopes: string[];
	expiresAt: string;
	state: string;
	nonce: string;
	callbackPath: string;
	adminConsentUrl?: string | null;
};

export type CompleteMicrosoftGraphConsentCallbackRequest = {
	state: string;
	nonce?: string | null;
	adminConsent?: boolean;
	microsoftTenantId?: string | null;
	displayName?: string | null;
	primaryDomain?: string | null;
	error?: string | null;
	errorDescription?: string | null;
};

export type MicrosoftGraphConsentCallbackResponse = {
	tenantId: string;
	consentRequestId: string;
	directoryConnectionId: string | null;
	provider: string;
	status: string;
	connectionStatus: string;
	connected: boolean;
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
	directoryImportStale?: boolean;
	directoryImportStaleAt?: string | null;
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

export type SubjectDirectoryCsvImportRequest = {
	csvContent: string;
	dryRun?: boolean;
	sourceExternalIdPrefix?: string | null;
	markMissingSubjectsStale?: boolean;
	previewImportRunId?: string | null;
	directoryImportRuleId?: string | null;
};

export type SubjectDirectoryCsvImportResponse = {
	tenantId: string;
	rowCount: number;
	importedRowCount: number;
	createdSubjectCount: number;
	updatedSubjectCount: number;
	createdGroupCount: number;
	addedMembershipCount: number;
	skippedMembershipCount: number;
	rows: SubjectDirectoryCsvImportRowResponse[];
	dryRun: boolean;
	setManagerRelationshipCount?: number;
	skippedManagerRelationshipCount?: number;
	missingManagerReferenceCount?: number;
	markedStaleSubjectCount?: number;
	clearedStaleSubjectCount?: number;
	importAuditEventId?: string | null;
	importRunId?: string | null;
};

export type SubjectDirectoryCsvImportRowResponse = {
	rowNumber: number;
	status: string;
	externalId: string | null;
	email: string | null;
	displayName: string | null;
	groupType: string | null;
	groupName: string | null;
	action: string;
	issues: string[];
};

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

export type CampaignSeriesStudyBriefResponse = {
	purpose: string | null;
	audience: string | null;
	designType: string | null;
	intendedUse: string | null;
	interpretationBoundary: string | null;
	ownerNotes: string | null;
};

export type CampaignSeriesOwnershipMetadata = {
	studyKind: CampaignSeriesStudyKind;
	isSample: boolean;
	sampleScenario: string | null;
	readOnlyReason: string | null;
	studyBrief?: CampaignSeriesStudyBriefResponse | null;
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
	templateVersionId: string;
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
	groupCoverage?: CampaignSeriesOperationsGroupCoverageSummaryResponse | null;
};

export type CampaignSeriesOperationsGroupCoverageSummaryResponse = {
	kMin: number;
	unattributedInvitedCount: number;
	unattributedSubmittedCount: number;
	groups: CampaignSeriesOperationsGroupCoverageResponse[];
};

export type CampaignSeriesOperationsGroupCoverageResponse = {
	groupId: string;
	groupName: string;
	invitedCount: number;
	submittedCount: number;
	meetsThreshold: boolean;
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
	bouncedInvitationCount?: number;
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
	providerAcceptedEventCount?: number;
	providerDeliveredEventCount?: number;
	providerBouncedEventCount?: number;
	providerComplainedEventCount?: number;
	latestProviderEventAt?: string | null;
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
	targetAwareAssignmentCount?: number;
	queuedInvitationCount: number;
	sentInvitationCount: number;
	failedInvitationCount: number;
	bouncedInvitationCount?: number;
	deliveryAttemptCount: number;
	latestDeliveryAttemptAt: string | null;
	providerAcceptedEventCount?: number;
	providerDeliveredEventCount?: number;
	providerBouncedEventCount?: number;
	providerComplainedEventCount?: number;
	latestProviderEventAt?: string | null;
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
	resultsAnalytics?: CampaignSeriesResultsAnalyticsResponse | null;
	resultsDashboard?: CampaignSeriesResultsDashboardResponse | null;
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
	analytics?: CampaignSeriesResultsAnalyticsResponse | null;
};

export type ResultsDashboardWidgetData = {
	dashboard: CampaignSeriesResultsDashboardResponse;
};

export type CampaignSeriesResultsDashboardResponse = {
	selectedCampaignId: string | null;
	selectedCampaignName: string | null;
	disclosureKMin: number;
	disclosureState: string;
	metrics: ResultsDashboardMetricResponse[];
	outputBars: ResultsDashboardBarResponse[];
	groupBars: ResultsDashboardBarResponse[];
	waveTrendPoints: ResultsDashboardPointResponse[];
	notes: ResultsDashboardNoteResponse[];
};

export type ResultsDashboardMetricResponse = {
	id: string;
	value: number | null;
	unit: string;
	detail: string | null;
	tone: string;
};

export type ResultsDashboardBarResponse = {
	id: string;
	label: string;
	dimensionCode: string;
	disclosure: string;
	value: number | null;
	count: number | null;
	detail: string | null;
	suppressionReason: string | null;
};

export type ResultsDashboardPointResponse = {
	id: string;
	campaignId: string;
	campaignName: string;
	dimensionCode: string;
	disclosure: string;
	value: number | null;
	deltaFromPrevious: number | null;
	comparisonState: string;
	dataFinality: string;
	count: number | null;
	suppressionReason: string | null;
};

export type ResultsDashboardNoteResponse = {
	kind: string;
	severity: string;
	title: string;
	detail: string;
};

export type CampaignSeriesResultsAnalyticsResponse = {
	selectedCampaignId: string | null;
	selectedCampaignName: string | null;
	disclosureKMin: number;
	disclosureState: string;
	scoreOutputs: CampaignSeriesResultsScoreOutputResponse[];
	groupRows: CampaignSeriesResultsGroupMatrixRowResponse[];
	waveRows: CampaignSeriesResultsWaveMatrixRowResponse[];
	insights: CampaignSeriesResultsInsightResponse[];
};

export type CampaignSeriesResultsScoreOutputResponse = {
	dimensionCode: string;
	disclosure: string;
	submittedResponseCount: number | null;
	scoreCount: number | null;
	mean: number | null;
	median: number | null;
	standardDeviation: number | null;
	min: number | null;
	max: number | null;
	nValidTotal: number | null;
	nExpectedTotal: number | null;
	missingPolicyStatusSummary: string | null;
	suppressionReason: string | null;
};

export type CampaignSeriesResultsGroupMatrixRowResponse = {
	groupType: string;
	groupName: string;
	dimensionCode: string;
	disclosure: string;
	submittedResponseCount: number | null;
	scoreCount: number | null;
	mean: number | null;
	median: number | null;
	standardDeviation: number | null;
	min: number | null;
	max: number | null;
	suppressionReason: string | null;
};

export type CampaignSeriesResultsWaveMatrixRowResponse = {
	campaignId: string;
	campaignName: string;
	campaignStatus: string;
	dataFinality: string;
	closedAt: string | null;
	dimensionCode: string;
	disclosure: string;
	submittedResponseCount: number | null;
	scoreCount: number | null;
	mean: number | null;
	median: number | null;
	standardDeviation: number | null;
	min: number | null;
	max: number | null;
	suppressionReason: string | null;
	deltaFromPreviousMean: number | null;
	deltaFromFirstMean: number | null;
	comparisonState: string;
};

export type CampaignSeriesResultsInsightResponse = {
	kind: string;
	severity: string;
	title: string;
	detail: string;
	/** The one number the sentence was composed from (count or k), for localized recomposition. */
	count?: number | null;
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
	| ResultsDashboardWidgetData
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
	| ReportWidgetBase<'results-dashboard/v1', ResultsDashboardWidgetData>
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
		ensureSampleStudies: () =>
			client.request<EnsureSampleStudiesResponse>(
				'/sample-studies/ensure',
				jsonRequest('POST', {})
			),
		getWorkspaceOverview: () => client.request<WorkspaceOverviewResponse>('/workspace-overview'),
		getTenantSettings: () => client.request<TenantSettingsWorkspaceResponse>('/tenant-settings'),
		updateTenantReportBranding: (request: UpdateTenantReportBrandingRequest) =>
			client.request<TenantSettingsReportBrandingResponse>(
				'/tenant-settings/report-branding',
				jsonRequest('PUT', request)
			),
		listExportArtifacts: () => client.request<ExportArtifactLibraryResponse>('/export-artifacts'),
		listTenantMembers: () => client.request<TenantMemberRosterResponse>('/tenant-members'),
		listTenantRoles: () => client.request<TenantRoleListResponse>('/tenant-roles'),
		listSubjects: () => client.request<SubjectDirectoryResponse>('/subjects'),
		getMicrosoftGraphDirectoryConnectionState: () =>
			client.request<DirectoryConnectionStateResponse>('/directory-connections/microsoft-graph'),
		listMicrosoftGraphDirectoryImportRuns: () =>
			client.request<DirectoryImportRunHistoryResponse>(
				'/directory-connections/microsoft-graph/import-runs'
			),
		listMicrosoftGraphDirectoryImportRules: () =>
			client.request<DirectoryImportRuleListResponse>(
				'/directory-connections/microsoft-graph/import-rules'
			),
		saveMicrosoftGraphDirectoryImportRule: (request: SaveMicrosoftGraphImportRuleRequest) =>
			client.request<DirectoryImportRuleResponse>(
				'/directory-connections/microsoft-graph/import-rules',
				jsonRequest('POST', request)
			),
		previewMicrosoftGraphDirectoryImportRule: (
			ruleId: string,
			request: PreviewMicrosoftGraphImportRuleRequest
		) =>
			client.request<MicrosoftGraphImportRulePreviewResponse>(
				`/directory-connections/microsoft-graph/import-rules/${encodeURIComponent(ruleId)}/preview`,
				jsonRequest('POST', request)
			),
		applyMicrosoftGraphDirectoryImportRule: (
			ruleId: string,
			request: ApplyMicrosoftGraphImportRuleRequest
		) =>
			client.request<MicrosoftGraphImportRuleApplyResponse>(
				`/directory-connections/microsoft-graph/import-rules/${encodeURIComponent(ruleId)}/apply`,
				jsonRequest('POST', request)
			),
		previewLiveMicrosoftGraphDirectoryImportRule: (ruleId: string) =>
			client.request<MicrosoftGraphImportRulePreviewResponse>(
				`/directory-connections/microsoft-graph/import-rules/${encodeURIComponent(ruleId)}/live-preview`,
				jsonRequest('POST', {})
			),
		applyLiveMicrosoftGraphDirectoryImportRule: (
			ruleId: string,
			request: LiveApplyMicrosoftGraphImportRuleRequest
		) =>
			client.request<MicrosoftGraphImportRuleApplyResponse>(
				`/directory-connections/microsoft-graph/import-rules/${encodeURIComponent(ruleId)}/live-apply`,
				jsonRequest('POST', request)
			),
		archiveMicrosoftGraphDirectoryImportRule: (ruleId: string) =>
			client.request<DirectoryImportRuleResponse>(
				`/directory-connections/microsoft-graph/import-rules/${encodeURIComponent(ruleId)}`,
				jsonRequest('DELETE', {})
			),
		createMicrosoftGraphConsentRequest: (request: CreateMicrosoftGraphConsentRequest = {}) =>
			client.request<MicrosoftGraphConsentRequestResponse>(
				'/directory-connections/microsoft-graph/consent-requests',
				jsonRequest('POST', request)
			),
		completeMicrosoftGraphConsentCallback: (request: CompleteMicrosoftGraphConsentCallbackRequest) =>
			client.request<MicrosoftGraphConsentCallbackResponse>(
				'/directory-connections/microsoft-graph/consent-callback',
				jsonRequest('POST', request)
			),
		createSubject: (request: CreateSubjectRequest) =>
			client.request<SubjectDirectoryItemResponse>('/subjects', jsonRequest('POST', request)),
		importSubjectDirectoryCsv: (request: SubjectDirectoryCsvImportRequest) =>
			client.request<SubjectDirectoryCsvImportResponse>(
				'/subjects/imports/csv',
				jsonRequest('POST', request)
			),
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
