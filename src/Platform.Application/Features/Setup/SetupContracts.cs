namespace Platform.Application.Features.Setup;

public sealed record SetupIdResponse(Guid Id);

public sealed record CreatePrivateInstrumentImportRequest(
    string Code,
    string Version,
    string FullName,
    string Domain,
    string ProvenanceNote,
    string RightsStatus,
    string ValidityLabel,
    string LicenseType = "unknown",
    string? CitationApa = null);

public sealed record InstrumentSummaryResponse(
    Guid Id,
    string Code,
    string Version,
    string FullName,
    string RightsStatus,
    string ValidityLabel,
    bool CanStartNewCampaign);

public sealed record CreateTemplateVersionRequest(
    string TemplateName,
    string Semver,
    string DefaultLocale,
    Guid? InstrumentId,
    IReadOnlyList<CreateTemplateSectionRequest> Sections,
    IReadOnlyList<CreateQuestionScaleRequest> Scales,
    IReadOnlyList<CreateTemplateQuestionRequest> Questions);

public sealed record CreateTemplateVersionDraftRequest(string Semver);

public sealed record UpdateTemplateVersionDraftContentRequest(
    IReadOnlyList<CreateTemplateSectionRequest> Sections,
    IReadOnlyList<CreateQuestionScaleRequest> Scales,
    IReadOnlyList<CreateTemplateQuestionRequest> Questions);

public sealed record CreateTemplateSectionRequest(
    int Ordinal,
    string Code,
    string TitleDefault);

public sealed record CreateQuestionScaleRequest(
    string Code,
    string Type,
    int MinValue,
    int MaxValue,
    int Step,
    bool NaAllowed,
    string Anchors);

public sealed record CreateTemplateQuestionRequest(
    int Ordinal,
    string Code,
    string Type,
    string TextDefault,
    string? SectionCode = null,
    string? ScaleCode = null,
    bool Required = true,
    bool ReverseCoded = false,
    string? MeasurementLevel = null,
    string Payload = "{}",
    string MissingCodes = "[]");

public sealed record TemplateVersionDetailResponse(
    Guid TemplateId,
    Guid TemplateVersionId,
    string TemplateName,
    string Semver,
    string Status,
    string DefaultLocale,
    Guid? InstrumentId,
    IReadOnlyList<TemplateSectionResponse> Sections,
    IReadOnlyList<QuestionScaleResponse> Scales,
    IReadOnlyList<TemplateQuestionResponse> Questions);

public sealed record TemplateVersionListResponse(
    Guid TemplateId,
    Guid AnchorTemplateVersionId,
    IReadOnlyList<TemplateVersionSummaryResponse> Versions);

public sealed record TemplateVersionSummaryResponse(
    Guid TemplateVersionId,
    string Semver,
    string Status,
    bool IsLocked,
    bool IsGlobal,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy);

public sealed record TemplateSectionResponse(Guid Id, int Ordinal, string? Code, string TitleDefault);

public sealed record QuestionScaleResponse(
    Guid Id,
    string Code,
    string Type,
    int MinValue,
    int MaxValue,
    int Step,
    bool NaAllowed,
    string Anchors);

public sealed record TemplateQuestionResponse(
    Guid Id,
    Guid SectionId,
    int Ordinal,
    string Code,
    string Type,
    Guid? ScaleId,
    string TextDefault,
    string? DescriptionDefault,
    bool Required,
    bool ReverseCoded,
    string? MeasurementLevel,
    decimal Weight,
    string? VariableLabel,
    string Payload,
    string MissingCodes);

public sealed record CreateScoringRuleRequest(
    Guid TemplateVersionId,
    string RuleKey,
    string RuleVersion,
    string SchemaVersion,
    string EngineMinVersion,
    string Document,
    string Produces,
    string Compatibility = "{}");

public sealed record RetireTemplateVersionDraftScoringResponse(
    Guid TemplateVersionId,
    int RetiredScoringRuleCount);

public sealed record CreateCampaignSeriesRequest(
    string Name,
    CreateCampaignSeriesStudyBriefRequest? StudyBrief = null);

public sealed record SelectCampaignSeriesSetupTemplateRequest(Guid TemplateVersionId);

public sealed record SelectCampaignSeriesSetupTemplateResponse(Guid CampaignSeriesId, Guid TemplateVersionId);

public sealed record CreateCampaignSeriesStudyBriefRequest(
    string? Purpose = null,
    string? Audience = null,
    string? DesignType = null,
    string? IntendedUse = null,
    string? InterpretationBoundary = null,
    string? OwnerNotes = null);

public sealed record CreateCampaignRequest(
    Guid TemplateVersionId,
    string Name,
    string ResponseIdentityMode,
    Guid? CampaignSeriesId = null,
    string Schedule = "{}",
    string DefaultLocale = "en",
    DateTimeOffset? StartAt = null,
    DateTimeOffset? EndAt = null);

public sealed record CampaignDraftResponse(
    Guid Id,
    Guid? CampaignSeriesId,
    Guid TemplateVersionId,
    string Name,
    string Status,
    string ResponseIdentityMode);

public sealed record LaunchReadinessResponse(
    Guid CampaignId,
    bool Ready,
    IReadOnlyList<LaunchReadinessIssueResponse> Issues);

public sealed record LaunchReadinessIssueResponse(
    string Code,
    string Severity,
    string Message);

public sealed record CampaignRespondentRuleListResponse(
    Guid CampaignId,
    IReadOnlyList<CampaignRespondentRuleResponse> Rules);

public sealed record CampaignRespondentRuleResponse(
    Guid Id,
    int Ordinal,
    string Rule,
    string RuleKind,
    string Role,
    Guid? TargetSubjectId,
    Guid? GroupId,
    int AssignmentPairCount,
    IReadOnlyList<LaunchReadinessIssueResponse> Issues);

public sealed record UpdateCampaignRespondentRulesRequest(
    IReadOnlyList<UpdateCampaignRespondentRuleRequest> Rules);

public sealed record UpdateCampaignRespondentRuleRequest(string Rule);

public sealed record CampaignAssignmentListResponse(
    Guid CampaignId,
    int AssignmentCount,
    IReadOnlyList<CampaignAssignmentResponse> Assignments);

public sealed record CampaignAssignmentResponse(
    Guid Id,
    string Role,
    string Status,
    bool Anonymous,
    Guid? TargetSubjectId,
    CampaignAssignmentSubjectResponse? Target,
    Guid? RespondentSubjectId,
    CampaignAssignmentSubjectResponse? Respondent,
    DateTimeOffset? DueAt,
    DateTimeOffset CreatedAt);

public sealed record CampaignAssignmentSubjectResponse(
    Guid Id,
    string Label,
    string? DisplayName,
    string? Email,
    string? ExternalId);

public sealed record LaunchCampaignResponse(
    Guid CampaignId,
    string Status,
    Guid LaunchSnapshotId,
    Guid TemplateVersionId,
    Guid ScoringRuleId,
    Guid RetentionPolicyId,
    Guid DisclosurePolicyId,
    string ResponseIdentityMode,
    string DefaultLocale,
    DateTimeOffset LaunchedAt);

public sealed record CampaignOpenLinkResponse(
    Guid CampaignId,
    Guid AssignmentId,
    string Token,
    string RespondentPath);

public sealed record CampaignIdentifiedEntryResponse(
    Guid CampaignId,
    Guid AssignmentId,
    Guid SubjectId,
    string Token,
    string RespondentPath);

public sealed record CampaignIdentifiedQueueAccessResponse(
    Guid CampaignId,
    int RespondentCount,
    int AssignmentCount,
    int CreatedAccessCount,
    int ExistingAccessCount,
    IReadOnlyList<CampaignIdentifiedQueueAccessLinkResponse> Links);

public sealed record CampaignIdentifiedQueueAccessLinkResponse(
    Guid InvitationTokenId,
    Guid RespondentSubjectId,
    int AssignmentCount,
    string? Token,
    string? RespondentPath,
    string Status);

public sealed record CreateCampaignInvitationBatchRequest(
    IReadOnlyList<InvitationRecipientRequest> Recipients);

public sealed record InvitationRecipientRequest(string Email);

public sealed record CampaignInvitationBatchResponse(
    Guid CampaignId,
    int RequestedRecipientCount,
    int CreatedInvitationCount,
    IReadOnlyList<CampaignInvitationResponse> Invitations);

public sealed record CampaignInvitationResponse(
    Guid AssignmentId,
    Guid InvitationTokenId,
    Guid NotificationId,
    string Recipient,
    string? Token,
    string? RespondentPath,
    string Status);
