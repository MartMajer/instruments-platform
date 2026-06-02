namespace Platform.Application.Features.Responses;

public sealed record RespondentCampaignResponse(
    Guid CampaignId,
    Guid TemplateVersionId,
    string Name,
    string Status,
    string ResponseIdentityMode,
    string DefaultLocale,
    IReadOnlyList<RespondentQuestionResponse> Questions);

public sealed record RespondentQuestionResponse(
    Guid Id,
    int Ordinal,
    string Code,
    string Type,
    string TextDefault,
    bool Required,
    Guid? ScaleId = null,
    string? ScaleCode = null,
    int? ScaleMinValue = null,
    int? ScaleMaxValue = null,
    bool? ScaleNaAllowed = null,
    string? ScaleAnchors = null,
    string Payload = "{}");

public sealed record OpenLinkEntryResponse(
    Guid CampaignId,
    Guid AssignmentId,
    Guid TemplateVersionId,
    string Name,
    string Status,
    string ResponseIdentityMode,
    bool RequiresParticipantCode,
    string DefaultLocale,
    ConsentDocumentResponse ConsentDocument,
    IReadOnlyList<RespondentQuestionResponse> Questions,
    string? AssignmentRole = null,
    RespondentSubjectContextResponse? RespondentSubject = null,
    RespondentSubjectContextResponse? TargetSubject = null);

public sealed record RespondentSubjectContextResponse(
    Guid Id,
    string? DisplayName,
    string? Email,
    string? ExternalId);

public sealed record IdentifiedQueueEntryResponse(
    Guid CampaignId,
    Guid TemplateVersionId,
    string Name,
    string Status,
    string ResponseIdentityMode,
    string DefaultLocale,
    ConsentDocumentResponse ConsentDocument,
    SafeRespondentSubjectContextResponse RespondentSubject,
    IReadOnlyList<IdentifiedQueueAssignmentResponse> Assignments,
    int AssignmentCount,
    int StartedCount,
    int SubmittedCount,
    IReadOnlyList<RespondentQuestionResponse> Questions);

public sealed record IdentifiedQueueAssignmentResponse(
    Guid AssignmentId,
    string Role,
    string ResponseStatus,
    SafeRespondentSubjectContextResponse? TargetSubject,
    Guid? SessionId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? SubmittedAt);

public sealed record SafeRespondentSubjectContextResponse(
    Guid Id,
    string Label,
    string? DisplayName,
    string? Email);

public sealed record EmailInvitationUnsubscribeResponse(string Status);

public sealed record UnsubscribeEmailInvitationRequest(bool Confirmed);

public sealed record ConsentDocumentResponse(
    Guid Id,
    string Locale,
    string Version,
    string Title,
    string BodyMarkdown,
    IReadOnlyList<string> RequiredGrants,
    IReadOnlyList<string> OptionalGrants);

public sealed record LabAssignmentResponse(
    Guid AssignmentId,
    Guid CampaignId,
    string ResponseIdentityMode);

public sealed record CreateResponseSessionRequest(
    Guid AssignmentId,
    string Locale = "en");

public sealed record CreateOpenLinkSessionRequest(
    string Locale = "en",
    Guid? AcceptedConsentDocumentId = null,
    IReadOnlyList<string>? AcceptedGrants = null,
    string? ParticipantCode = null);

public sealed record ResponseSessionResponse(
    Guid Id,
    Guid AssignmentId,
    string Locale,
    DateTimeOffset? StartedAt,
    DateTimeOffset? SubmittedAt,
    int? TimeTakenMs,
    string? PublicHandle = null);

public sealed record OpenLinkSessionDraftResponse(
    ResponseSessionResponse Session,
    IReadOnlyList<SavedAnswerResponse> Answers,
    int SavedAnswerCount,
    OpenLinkEntryResponse? Entry = null);

public sealed record SavedAnswerResponse(
    Guid QuestionId,
    string? Value,
    string? Comment,
    bool IsSkipped,
    bool IsNa);

public sealed record SaveAnswersRequest(
    IReadOnlyList<SaveAnswerRequest> Answers);

public sealed record SaveAnswerRequest(
    Guid QuestionId,
    string? Value,
    string? Comment = null,
    bool IsSkipped = false,
    bool IsNa = false);

public sealed record SaveAnswersResponse(
    Guid SessionId,
    int SavedAnswerCount);

public sealed record SubmitResponseSessionRequest(
    int? TimeTakenMs = null);

public sealed record SubmitResponseSessionResponse(
    Guid Id,
    DateTimeOffset SubmittedAt);
