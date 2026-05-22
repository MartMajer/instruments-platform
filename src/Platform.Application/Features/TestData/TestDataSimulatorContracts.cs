namespace Platform.Application.Features.TestData;

public sealed record CreateCampaignTestRecipientsRequest(
    int Count = 50,
    string GroupName = "Test respondents",
    string EmailDomain = "test.validatedscale.local",
    string Locale = "en");

public sealed record CreateCampaignTestRecipientsResponse(
    Guid CampaignId,
    Guid GroupId,
    string GroupName,
    int CreatedSubjectCount,
    int SavedRecipientRuleCount,
    int PreviewRecipientCount);

public sealed record CreateCampaignTestResponsesRequest(
    int ResponseCount = 25,
    decimal TargetOutcome = 7.0m,
    string Variation = "normal",
    bool IncludeComments = true);

public sealed record CreateCampaignTestResponsesResponse(
    Guid CampaignId,
    int RequestedResponseCount,
    int SubmittedResponseCount,
    int AnswerCount,
    int ScoredResponseCount,
    int MarkedEmailSentCount,
    decimal TargetOutcome,
    string Variation);

public sealed record TestDataSimulatorQuestion(
    Guid Id,
    string Code,
    string Type,
    bool Required,
    int? ScaleMinValue,
    int? ScaleMaxValue,
    string Payload);

public sealed record TestDataSimulatorAnswer(
    Guid QuestionId,
    string QuestionCode,
    string? Value,
    string? Comment = null,
    bool IsSkipped = false,
    bool IsNa = false);
