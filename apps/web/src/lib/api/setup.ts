import type { ApiClient } from './client';

export type SetupIdResponse = {
	id: string;
};

export type AuthSessionResponse = {
	userId: string;
	tenantId: string;
	email?: string | null;
	emailVerificationRequired?: boolean;
	permissions: string[];
};

export type CsrfTokenResponse = {
	csrfToken: string;
};

export type CreatePrivateInstrumentImportRequest = {
	code: string;
	version: string;
	fullName: string;
	domain: string;
	provenanceNote: string;
	rightsStatus: string;
	validityLabel: string;
	licenseType: string;
	citationApa?: string | null;
};

export type InstrumentSummaryResponse = {
	id: string;
	code: string;
	version: string;
	fullName: string;
	rightsStatus: string;
	validityLabel: string;
	canStartNewCampaign: boolean;
};

export type CreateTemplateVersionRequest = {
	templateName: string;
	semver: string;
	defaultLocale: string;
	instrumentId: string | null;
	sections: CreateTemplateSectionRequest[];
	scales: CreateQuestionScaleRequest[];
	questions: CreateTemplateQuestionRequest[];
};

export type CreateTemplateSectionRequest = {
	ordinal: number;
	code: string;
	titleDefault: string;
};

export type CreateQuestionScaleRequest = {
	code: string;
	type: string;
	minValue: number;
	maxValue: number;
	step: number;
	naAllowed: boolean;
	anchors: string;
};

export type CreateTemplateQuestionRequest = {
	ordinal: number;
	code: string;
	type: string;
	textDefault: string;
	sectionCode?: string | null;
	scaleCode?: string | null;
	required: boolean;
	reverseCoded: boolean;
	measurementLevel?: string | null;
	payload: string;
	missingCodes: string;
};

export type TemplateVersionDetailResponse = {
	templateId: string;
	templateVersionId: string;
	templateName: string;
	semver: string;
	status: string;
	defaultLocale: string;
	instrumentId: string | null;
	sections: TemplateSectionResponse[];
	scales: QuestionScaleResponse[];
	questions: TemplateQuestionResponse[];
};

export type TemplateSectionResponse = {
	id: string;
	ordinal: number;
	code: string | null;
	titleDefault: string;
};

export type QuestionScaleResponse = {
	id: string;
	code: string;
	type: string;
	minValue: number;
	maxValue: number;
	step: number;
	naAllowed: boolean;
	anchors: string;
};

export type TemplateQuestionResponse = {
	id: string;
	ordinal: number;
	code: string;
	type: string;
	scaleId: string | null;
	textDefault: string;
	required: boolean;
	reverseCoded: boolean;
	measurementLevel: string | null;
};

export type CreateScoringRuleRequest = {
	templateVersionId: string;
	ruleKey: string;
	ruleVersion: string;
	schemaVersion: string;
	engineMinVersion: string;
	document: string;
	produces: string;
	compatibility: string;
};

export type CreateCampaignSeriesStudyBriefRequest = {
	purpose?: string | null;
	audience?: string | null;
	designType?: string | null;
	intendedUse?: string | null;
	interpretationBoundary?: string | null;
	ownerNotes?: string | null;
};

export type CreateCampaignSeriesRequest = {
	name: string;
	studyBrief?: CreateCampaignSeriesStudyBriefRequest | null;
};

export type CampaignSeriesTwoWaveProofResponse = {
	campaignSeriesId: string;
	proofStatus: string;
	expectedWaveCount: number;
	launchedWaveCount: number;
	submittedWaveCount: number;
	linkedTrajectoryCount: number;
	completeTrajectoryCount: number;
	waves: TwoWaveProofWaveResponse[];
};

export type TwoWaveProofWaveResponse = {
	campaignId: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	submittedResponseCount: number;
};

export type CampaignSeriesWaveComparisonProofResponse = {
	campaignSeriesId: string;
	proofStatus: string;
	interpretationStatus: string;
	baselineWave: WaveComparisonWaveResponse | null;
	comparisonWave: WaveComparisonWaveResponse | null;
	disclosurePolicy: WaveComparisonDisclosurePolicyResponse | null;
	scores: WaveScoreComparisonResponse[];
};

export type WaveComparisonWaveResponse = {
	campaignId: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	launchedAt: string;
	scoringRuleId: string;
	scoringRuleKey: string;
	scoringRuleVersion: string;
	scoringRuleDocumentHash: string;
	submittedResponseCount: number;
};

export type WaveComparisonDisclosurePolicyResponse = {
	id: string;
	version: string;
	kMin: number;
	suppressionStrategy: string;
};

export type ScoreInterpretationResponse = {
	status: string;
	source: string;
	bandCode: string;
	label: string;
	provenance: string;
	isValidated: boolean;
	isOfficial: boolean;
};

export type WaveScoreComparisonResponse = {
	dimensionCode: string;
	compatibilityStatus: string;
	disclosure: string;
	baselineSubmittedResponseCount: number;
	comparisonSubmittedResponseCount: number;
	linkedPairCount: number;
	baselineScoreCount: number | null;
	comparisonScoreCount: number | null;
	baselineNValidTotal?: number | null;
	baselineNExpectedTotal?: number | null;
	baselineMissingPolicyStatusSummary?: string | null;
	comparisonNValidTotal?: number | null;
	comparisonNExpectedTotal?: number | null;
	comparisonMissingPolicyStatusSummary?: string | null;
	baselineMean: number | null;
	comparisonMean: number | null;
	aggregateDelta: number | null;
	pairedDeltaMean: number | null;
	suppressionReason: string | null;
	compatibilityReason: string | null;
	baselineInterpretation?: ScoreInterpretationResponse | null;
	comparisonInterpretation?: ScoreInterpretationResponse | null;
};

export type CreateCampaignRequest = {
	templateVersionId: string;
	name: string;
	responseIdentityMode: string;
	campaignSeriesId: string | null;
	schedule: string;
	defaultLocale: string;
	startAt?: string | null;
	endAt?: string | null;
};

export type CampaignDraftResponse = {
	id: string;
	campaignSeriesId: string | null;
	templateVersionId: string;
	name: string;
	status: string;
	responseIdentityMode: string;
};

export type LaunchReadinessResponse = {
	campaignId: string;
	ready: boolean;
	issues: LaunchReadinessIssueResponse[];
};

export type LaunchReadinessIssueResponse = {
	code: string;
	severity: string;
	message: string;
};

export type CampaignRespondentRuleListResponse = {
	campaignId: string;
	rules: CampaignRespondentRuleResponse[];
};

export type CampaignRespondentRuleResponse = {
	id: string;
	ordinal: number;
	rule: string;
	ruleKind: string;
	role: string;
	targetSubjectId: string | null;
	groupId: string | null;
	assignmentPairCount: number;
	issues: LaunchReadinessIssueResponse[];
};

export type UpdateCampaignRespondentRulesRequest = {
	rules: UpdateCampaignRespondentRuleRequest[];
};

export type UpdateCampaignRespondentRuleRequest = {
	rule: string;
};

export type CampaignAssignmentListResponse = {
	campaignId: string;
	assignmentCount: number;
	assignments: CampaignAssignmentResponse[];
};

export type CampaignAssignmentResponse = {
	id: string;
	role: string;
	status: string;
	anonymous: boolean;
	targetSubjectId: string | null;
	target: CampaignAssignmentSubjectResponse | null;
	respondentSubjectId: string | null;
	respondent: CampaignAssignmentSubjectResponse | null;
	dueAt: string | null;
	createdAt: string;
};

export type CampaignAssignmentSubjectResponse = {
	id: string;
	label: string;
	displayName: string | null;
	email: string | null;
	externalId: string | null;
};

export type LaunchCampaignResponse = {
	campaignId: string;
	status: string;
	launchSnapshotId: string;
	templateVersionId: string;
	scoringRuleId: string;
	retentionPolicyId: string;
	disclosurePolicyId: string;
	responseIdentityMode: string;
	defaultLocale: string;
	launchedAt: string;
};

export type CampaignOpenLinkResponse = {
	campaignId: string;
	assignmentId: string;
	token: string;
	respondentPath: string;
};

export type CampaignIdentifiedEntryResponse = {
	campaignId: string;
	assignmentId: string;
	subjectId: string;
	token: string;
	respondentPath: string;
};

export type CreateCampaignInvitationBatchRequest = {
	recipients: InvitationRecipientRequest[];
};

export type InvitationRecipientRequest = {
	email: string;
};

export type CampaignInvitationBatchResponse = {
	campaignId: string;
	requestedRecipientCount: number;
	createdInvitationCount: number;
	invitations: CampaignInvitationResponse[];
};

export type CampaignInvitationResponse = {
	assignmentId: string;
	invitationTokenId: string;
	notificationId: string;
	recipient: string;
	token?: string | null;
	respondentPath?: string | null;
	status: string;
};

export type CreateCampaignTestRecipientsRequest = {
	count: number;
	groupName?: string;
	emailDomain?: string;
	locale?: string;
};

export type CreateCampaignTestRecipientsResponse = {
	campaignId: string;
	groupId: string;
	groupName: string;
	createdSubjectCount: number;
	savedRecipientRuleCount: number;
	previewRecipientCount: number;
};

export type CreateCampaignTestResponsesRequest = {
	responseCount: number;
	targetOutcome: number;
	variation: 'tight' | 'normal' | 'noisy';
	includeComments: boolean;
};

export type CreateCampaignTestResponsesResponse = {
	campaignId: string;
	requestedResponseCount: number;
	submittedResponseCount: number;
	answerCount: number;
	scoredResponseCount: number;
	markedEmailSentCount: number;
	targetOutcome: number;
	variation: string;
};

export type ProcessCampaignEmailDeliveriesRequest = {
	batchSize?: number;
};

export type ProcessCampaignEmailDeliveriesResponse = {
	campaignId: string;
	requestedBatchSize: number;
	processedCount: number;
	sentCount: number;
	failedCount: number;
	bouncedCount?: number;
	deliveries: NotificationDeliveryProofResponse[];
};

export type RequeueFailedCampaignEmailDeliveriesRequest = {
	batchSize?: number;
	confirmedAnotherEmailAppropriate?: boolean;
	confirmedNoPriorDelivery?: boolean;
};

export type RequeueFailedCampaignEmailDeliveriesResponse = {
	campaignId: string;
	requestedBatchSize: number;
	requeuedCount: number;
};

export type CampaignEmailDeliveryRepairReadinessResponse = {
	stalePreparedAttemptCount: number;
	ambiguousFailedNotificationCount: number;
	retryableFailedNotificationCount: number;
	suppressedFailedNotificationCount: number;
	providerEventCount: number;
	latestProviderEventAt?: string | null;
	canRetryFailed: boolean;
	hasRepairWork: boolean;
	issues: CampaignEmailDeliveryRepairReadinessIssueResponse[];
};

export type CampaignEmailDeliveryRepairReadinessIssueResponse = {
	code: string;
	severity: string;
	message: string;
};

export type RecordProviderDeliveryEventRequest = {
	deliveryAttemptKey: string;
	eventType: 'accepted' | 'delivered' | 'bounced' | 'complained' | string;
	occurredAt?: string | null;
	providerEventId?: string | null;
	providerMessageId?: string | null;
	reason?: string | null;
};

export type RecordProviderDeliveryEventResponse = {
	notificationId: string;
	deliveryAttemptId: string;
	eventType: string;
	notificationStatus: string;
	suppressionCreated: boolean;
	duplicateEvent: boolean;
};

export type ListProviderDeliveryEventsResponse = {
	requestedLimit: number;
	events: ProviderDeliveryEventResponse[];
};

export type ProviderDeliveryEventResponse = {
	provider: string;
	eventType: string;
	occurredAt: string;
	receivedAt: string;
	notificationStatus: string;
	deliveryAttemptStatus: string;
	hasProviderEventId: boolean;
	hasProviderMessageId: boolean;
};

export type EmailDeliveryReadinessResponse = {
	provider: string;
	mode: string;
	canSendRealEmail: boolean;
	webhookConfigured: boolean;
	issues: EmailDeliveryReadinessIssueResponse[];
};

export type EmailDeliveryReadinessIssueResponse = {
	code: string;
	message: string;
	severity: string;
};

export type NotificationDeliveryProofResponse = {
	notificationId: string;
	recipient?: string | null;
	status: string;
	provider: string;
	providerMessageId?: string | null;
	respondentPath?: string | null;
	error?: string | null;
};

export type ListEmailSuppressionsResponse = {
	requestedLimit: number;
	activeCount: number;
	releasedCount: number;
	suppressions: EmailSuppressionResponse[];
};

export type EmailSuppressionResponse = {
	id: string;
	recipient: string;
	reason: string;
	source: string;
	note?: string | null;
	createdAt: string;
	releasedAt?: string | null;
	releaseReason?: string | null;
	active: boolean;
};

export type AddEmailSuppressionRequest = {
	recipient: string;
	reason?: string | null;
	note?: string | null;
};

export type ReleaseEmailSuppressionRequest = {
	reason?: string | null;
};

export type RespondentCampaignResponse = {
	campaignId: string;
	templateVersionId: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	defaultLocale: string;
	questions: RespondentQuestionResponse[];
};

export type RespondentQuestionResponse = {
	id: string;
	ordinal: number;
	code: string;
	type: string;
	textDefault: string;
	required: boolean;
	scaleId?: string | null;
	scaleCode?: string | null;
	scaleMinValue?: number | null;
	scaleMaxValue?: number | null;
	scaleNaAllowed?: boolean | null;
	scaleAnchors?: string | null;
	payload?: string | null;
};

export type OpenLinkEntryResponse = {
	campaignId: string;
	assignmentId: string;
	templateVersionId: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	requiresParticipantCode: boolean;
	defaultLocale: string;
	consentDocument: ConsentDocumentResponse;
	questions: RespondentQuestionResponse[];
};

export type EmailInvitationUnsubscribeResponse = {
	status: string;
};

export type UnsubscribeEmailInvitationRequest = {
	confirmed: boolean;
};

export type ConsentDocumentResponse = {
	id: string;
	locale: string;
	version: string;
	title: string;
	bodyMarkdown: string;
	requiredGrants: string[];
	optionalGrants: string[];
};

export type LabAssignmentResponse = {
	assignmentId: string;
	campaignId: string;
	responseIdentityMode: string;
};

export type CreateResponseSessionRequest = {
	assignmentId: string;
	locale: string;
};

export type CreateOpenLinkSessionRequest = {
	locale: string;
	acceptedConsentDocumentId?: string | null;
	acceptedGrants?: string[] | null;
	participantCode?: string | null;
};

export type ResponseSessionResponse = {
	id: string;
	assignmentId: string;
	locale: string;
	startedAt: string | null;
	submittedAt: string | null;
	timeTakenMs: number | null;
	publicHandle?: string | null;
};

export type OpenLinkSessionDraftResponse = {
	session: ResponseSessionResponse;
	answers: SavedAnswerResponse[];
	savedAnswerCount: number;
	entry?: OpenLinkEntryResponse | null;
};

export type SavedAnswerResponse = {
	questionId: string;
	value: string | null;
	comment?: string | null;
	isSkipped: boolean;
	isNa: boolean;
};

export type SaveAnswersRequest = {
	answers: SaveAnswerRequest[];
};

export type SaveAnswerRequest = {
	questionId: string;
	value: string | null;
	comment?: string | null;
	isSkipped?: boolean;
	isNa?: boolean;
};

export type SaveAnswersResponse = {
	sessionId: string;
	savedAnswerCount: number;
};

export type SubmitResponseSessionRequest = {
	timeTakenMs?: number | null;
};

export type SubmitResponseSessionResponse = {
	id: string;
	submittedAt: string;
};

export type ComputeScoresResponse = {
	scoreRunId: string;
	sessionId: string;
	scores: ComputedScoreResponse[];
};

export type ComputedScoreResponse = {
	dimensionCode: string;
	value: number;
	nValid: number;
	nExpected: number;
	missingPolicyStatus: string;
};

export type CampaignReportProofResponse = {
	campaignId: string;
	campaignSeriesId: string | null;
	campaignName: string;
	campaignStatus: string;
	proofStatus: string;
	interpretationStatus: string;
	launchSnapshot: ReportLaunchSnapshotResponse;
	disclosurePolicy: ReportDisclosurePolicyResponse;
	scores: ReportScoreSummaryResponse[];
	closedAt?: string | null;
	dataFinality?: string | null;
};

export type ReportLaunchSnapshotResponse = {
	id: string;
	templateVersionId: string;
	scoringRuleId: string;
	scoringRuleDocumentHash: string;
	consentDocumentId: string | null;
	retentionPolicyId: string | null;
	disclosurePolicyId: string | null;
	responseIdentityMode: string;
	launchedAt: string;
};

export type ReportDisclosurePolicyResponse = {
	id: string;
	version: string;
	kMin: number;
	suppressionStrategy: string;
};

export type ReportScoreSummaryResponse = {
	dimensionCode: string;
	disclosure: string;
	submittedResponseCount: number;
	scoreCount: number | null;
	nValidTotal?: number | null;
	nExpectedTotal?: number | null;
	missingPolicyStatusSummary?: string | null;
	mean: number | null;
	min: number | null;
	max: number | null;
	suppressionReason: string | null;
	interpretation?: ScoreInterpretationResponse | null;
};

export type ReportProofExportArtifactResponse = {
	id: string;
	targetKind: string;
	targetId: string;
	targetLabel: string;
	campaignId: string | null;
	campaignSeriesId: string | null;
	artifactType: string;
	status: string;
	format: string;
	fileName: string;
	contentType: string;
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
	csvContent: string;
	codebookJson: string;
};

export type ExportArtifactDownloadResponse = {
	artifactId: string;
	fileName: string | null;
	contentType: string;
	byteSize: number;
	content: string;
};

export function createSetupApi(client: ApiClient) {
	return {
		getCurrentSession() {
			return client.request<AuthSessionResponse>('/auth/session');
		},
		getCsrfToken() {
			return client.request<CsrfTokenResponse>('/auth/csrf');
		},
		createPrivateInstrumentImport(request: CreatePrivateInstrumentImportRequest) {
			return client.request<InstrumentSummaryResponse>(
				'/instruments/private-imports',
				jsonPost(request)
			);
		},
		listInstruments() {
			return client.request<InstrumentSummaryResponse[]>('/instruments');
		},
		createTemplateVersion(request: CreateTemplateVersionRequest) {
			return client.request<TemplateVersionDetailResponse>('/template-versions', jsonPost(request));
		},
		getTemplateVersion(id: string) {
			return client.request<TemplateVersionDetailResponse>(`/template-versions/${id}`);
		},
		createScoringRule(request: CreateScoringRuleRequest) {
			return client.request<SetupIdResponse>('/scoring-rules', jsonPost(request));
		},
		createCampaignSeries(request: CreateCampaignSeriesRequest) {
			return client.request<SetupIdResponse>('/campaign-series', jsonPost(request));
		},
		getCampaignSeriesTwoWaveProof(campaignSeriesId: string) {
			return client.request<CampaignSeriesTwoWaveProofResponse>(
				`/campaign-series/${campaignSeriesId}/two-wave-proof`
			);
		},
		getCampaignSeriesWaveComparisonProof(campaignSeriesId: string) {
			return client.request<CampaignSeriesWaveComparisonProofResponse>(
				`/campaign-series/${campaignSeriesId}/wave-comparison-proof`
			);
		},
		createCampaign(request: CreateCampaignRequest) {
			return client.request<CampaignDraftResponse>('/campaigns', jsonPost(request));
		},
		getLaunchReadiness(campaignId: string) {
			return client.request<LaunchReadinessResponse>(`/campaigns/${campaignId}/launch-readiness`);
		},
		listCampaignRespondentRules(campaignId: string) {
			return client.request<CampaignRespondentRuleListResponse>(
				`/campaigns/${campaignId}/respondent-rules`
			);
		},
		updateCampaignRespondentRules(
			campaignId: string,
			request: UpdateCampaignRespondentRulesRequest
		) {
			return client.request<CampaignRespondentRuleListResponse>(
				`/campaigns/${campaignId}/respondent-rules`,
				jsonPut(request)
			);
		},
		listCampaignAssignments(campaignId: string) {
			return client.request<CampaignAssignmentListResponse>(`/campaigns/${campaignId}/assignments`);
		},
		launchCampaign(campaignId: string) {
			return client.request<LaunchCampaignResponse>(
				`/campaigns/${campaignId}/launch`,
				jsonPost({})
			);
		},
		createCampaignOpenLink(campaignId: string) {
			return client.request<CampaignOpenLinkResponse>(
				`/campaigns/${campaignId}/open-link`,
				jsonPost({})
			);
		},
		replaceCampaignOpenLink(campaignId: string) {
			return client.request<CampaignOpenLinkResponse>(
				`/campaigns/${campaignId}/open-link/replace`,
				jsonPost({})
			);
		},
		createCampaignIdentifiedEntry(campaignId: string) {
			return client.request<CampaignIdentifiedEntryResponse>(
				`/campaigns/${campaignId}/identified-entry`,
				jsonPost({})
			);
		},
		createCampaignInvitationBatch(
			campaignId: string,
			request: CreateCampaignInvitationBatchRequest
		) {
			return client.request<CampaignInvitationBatchResponse>(
				`/campaigns/${campaignId}/invitation-batches`,
				jsonPost(request)
			);
		},
		createCampaignTestRecipients(
			campaignId: string,
			request: CreateCampaignTestRecipientsRequest
		) {
			return client.request<CreateCampaignTestRecipientsResponse>(
				`/test-data/campaigns/${campaignId}/recipients`,
				jsonPost(request)
			);
		},
		createCampaignTestResponses(
			campaignId: string,
			request: CreateCampaignTestResponsesRequest
		) {
			return client.request<CreateCampaignTestResponsesResponse>(
				`/test-data/campaigns/${campaignId}/responses`,
				jsonPost(request)
			);
		},
		processCampaignEmailDeliveries(
			campaignId: string,
			request: ProcessCampaignEmailDeliveriesRequest = { batchSize: 25 }
		) {
			return client.request<ProcessCampaignEmailDeliveriesResponse>(
				`/campaigns/${campaignId}/notification-deliveries/process`,
				jsonPost(request)
			);
		},
		requeueFailedCampaignEmailDeliveries(
			campaignId: string,
			request: RequeueFailedCampaignEmailDeliveriesRequest = {
				batchSize: 25,
				confirmedAnotherEmailAppropriate: false
			}
		) {
			return client.request<RequeueFailedCampaignEmailDeliveriesResponse>(
				`/campaigns/${campaignId}/notification-deliveries/requeue-failed`,
				jsonPost(request)
			);
		},
		getCampaignEmailDeliveryRepairReadiness(campaignId: string) {
			return client.request<CampaignEmailDeliveryRepairReadinessResponse>(
				`/campaigns/${campaignId}/notification-deliveries/repair-readiness`
			);
		},
		listEmailSuppressions(limit = 50, includeReleased = false) {
			return client.request<ListEmailSuppressionsResponse>(
				`/email-suppressions?limit=${limit}&includeReleased=${includeReleased}`
			);
		},
		addEmailSuppression(request: AddEmailSuppressionRequest) {
			return client.request<EmailSuppressionResponse>('/email-suppressions', jsonPost(request));
		},
		releaseEmailSuppression(id: string, request: ReleaseEmailSuppressionRequest = {}) {
			return client.request<EmailSuppressionResponse>(
				`/email-suppressions/${id}/release`,
				jsonPost(request)
			);
		},
		recordProviderDeliveryEvent(request: RecordProviderDeliveryEventRequest) {
			return client.request<RecordProviderDeliveryEventResponse>(
				'/notification-deliveries/provider-events',
				jsonPost(request)
			);
		},
		listProviderDeliveryEvents(limit = 25) {
			return client.request<ListProviderDeliveryEventsResponse>(
				`/notification-deliveries/provider-events?limit=${limit}`
			);
		},
		getEmailDeliveryReadiness() {
			return client.request<EmailDeliveryReadinessResponse>(
				'/notification-deliveries/email-readiness'
			);
		},
		getRespondentCampaign(campaignId: string) {
			return client.request<RespondentCampaignResponse>(`/respondent/campaigns/${campaignId}`);
		},
		getOpenLinkEntry(token: string) {
			return client.request<OpenLinkEntryResponse>(`/respondent/open-links/${token}`);
		},
		unsubscribeEmailInvitation(token: string) {
			return client.request<EmailInvitationUnsubscribeResponse>(
				`/respondent/open-links/${token}/unsubscribe`,
				jsonPost({ confirmed: true } satisfies UnsubscribeEmailInvitationRequest)
			);
		},
		getIdentifiedEntry(token: string) {
			return client.request<OpenLinkEntryResponse>(`/respondent/identified-entries/${token}`);
		},
		createOpenLinkSession(token: string, request: CreateOpenLinkSessionRequest) {
			return client.request<ResponseSessionResponse>(
				`/respondent/open-links/${token}/sessions`,
				jsonPost(request)
			);
		},
		createIdentifiedEntrySession(token: string, request: CreateOpenLinkSessionRequest) {
			return client.request<ResponseSessionResponse>(
				`/respondent/identified-entries/${token}/sessions`,
				jsonPost(request)
			);
		},
		getOpenLinkSessionDraft(token: string, sessionId: string) {
			return client.request<OpenLinkSessionDraftResponse>(
				`/respondent/open-links/${token}/sessions/${sessionId}/draft`
			);
		},
		getPublicSessionDraft(handle: string) {
			return client.request<OpenLinkSessionDraftResponse>(
				`/respondent/public-sessions/${handle}/draft`
			);
		},
		saveOpenLinkAnswers(token: string, sessionId: string, request: SaveAnswersRequest) {
			return client.request<SaveAnswersResponse>(
				`/respondent/open-links/${token}/sessions/${sessionId}/answers`,
				jsonPut(request)
			);
		},
		savePublicSessionAnswers(handle: string, request: SaveAnswersRequest) {
			return client.request<SaveAnswersResponse>(
				`/respondent/public-sessions/${handle}/answers`,
				jsonPut(request)
			);
		},
		submitOpenLinkSession(token: string, sessionId: string, request: SubmitResponseSessionRequest) {
			return client.request<SubmitResponseSessionResponse>(
				`/respondent/open-links/${token}/sessions/${sessionId}/submit`,
				jsonPost(request)
			);
		},
		submitPublicSession(handle: string, request: SubmitResponseSessionRequest) {
			return client.request<SubmitResponseSessionResponse>(
				`/respondent/public-sessions/${handle}/submit`,
				jsonPost(request)
			);
		},
		createLabAssignment(campaignId: string) {
			return client.request<LabAssignmentResponse>(
				`/respondent/campaigns/${campaignId}/lab-assignment`,
				jsonPost({})
			);
		},
		createResponseSession(request: CreateResponseSessionRequest) {
			return client.request<ResponseSessionResponse>('/respondent/sessions', jsonPost(request));
		},
		saveAnswers(sessionId: string, request: SaveAnswersRequest) {
			return client.request<SaveAnswersResponse>(
				`/respondent/sessions/${sessionId}/answers`,
				jsonPut(request)
			);
		},
		submitResponseSession(sessionId: string, request: SubmitResponseSessionRequest) {
			return client.request<SubmitResponseSessionResponse>(
				`/respondent/sessions/${sessionId}/submit`,
				jsonPost(request)
			);
		},
		computeResponseScores(sessionId: string) {
			return client.request<ComputeScoresResponse>(
				`/respondent/sessions/${sessionId}/scores`,
				jsonPost({})
			);
		},
		getCampaignReportProof(campaignId: string) {
			return client.request<CampaignReportProofResponse>(`/campaigns/${campaignId}/report-proof`);
		},
		createCampaignReportProofExport(campaignId: string) {
			return client.request<ReportProofExportArtifactResponse>(
				`/campaigns/${campaignId}/report-proof/exports`,
				jsonPost({})
			);
		},
		createCampaignSeriesResponseExport(campaignSeriesId: string) {
			return client.request<ReportProofExportArtifactResponse>(
				`/campaign-series/${campaignSeriesId}/response-exports`,
				jsonPost({})
			);
		},
		getExportArtifact(artifactId: string) {
			return client.request<ReportProofExportArtifactResponse>(`/export-artifacts/${artifactId}`);
		},
		async downloadExportArtifactCsv(artifactId: string) {
			const response = await client.requestText(`/export-artifacts/${artifactId}/download`, {
				headers: {
					accept: 'text/csv'
				}
			});

			return {
				artifactId,
				fileName: parseContentDispositionFileName(response.contentDisposition),
				contentType: response.contentType,
				byteSize: response.byteSize,
				content: response.body
			} satisfies ExportArtifactDownloadResponse;
		}
	};
}

function jsonPost(body: unknown): RequestInit {
	return {
		method: 'POST',
		headers: {
			'content-type': 'application/json'
		},
		body: JSON.stringify(body)
	};
}

function jsonPut(body: unknown): RequestInit {
	return {
		method: 'PUT',
		headers: {
			'content-type': 'application/json'
		},
		body: JSON.stringify(body)
	};
}

function parseContentDispositionFileName(header: string | null) {
	if (!header) {
		return null;
	}

	const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(header);
	if (utf8Match?.[1]) {
		return decodeURIComponent(utf8Match[1].trim().replace(/^"|"$/g, ''));
	}

	const asciiMatch = /filename="?([^";]+)"?/i.exec(header);
	return asciiMatch?.[1]?.trim() ?? null;
}
