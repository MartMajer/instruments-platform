using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.Responses;

public static class ResponseCaptureEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);

    public static IEndpointRouteBuilder MapResponseCaptureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/respondent/campaigns/{campaignId:guid}", GetCampaign)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetRespondentCampaign")
            .WithTags("Responses");

        app.MapGet("/respondent/open-links/{token}", GetOpenLinkEntry)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Entry)
            .WithName("GetOpenLinkEntry")
            .WithTags("Responses");

        app.MapPost("/respondent/open-links/{token}/sessions", CreateOpenLinkSession)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Entry)
            .WithName("CreateOpenLinkSession")
            .WithTags("Responses");

        app.MapGet("/respondent/identified-entries/{token}", GetIdentifiedEntry)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Entry)
            .WithName("GetIdentifiedEntry")
            .WithTags("Responses");

        app.MapPost("/respondent/identified-entries/{token}/sessions", CreateIdentifiedEntrySession)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Entry)
            .WithName("CreateIdentifiedEntrySession")
            .WithTags("Responses");

        app.MapGet("/respondent/open-links/{token}/sessions/{sessionId:guid}/draft", GetOpenLinkSessionDraft)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Session)
            .WithName("GetOpenLinkSessionDraft")
            .WithTags("Responses");

        app.MapPut("/respondent/open-links/{token}/sessions/{sessionId:guid}/answers", SaveOpenLinkAnswers)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Session)
            .WithName("SaveOpenLinkAnswers")
            .WithTags("Responses");

        app.MapPost("/respondent/open-links/{token}/sessions/{sessionId:guid}/submit", SubmitOpenLinkSession)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Submit)
            .WithName("SubmitOpenLinkSession")
            .WithTags("Responses");

        app.MapGet("/respondent/public-sessions/{handle}/draft", GetPublicSessionDraft)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Session)
            .WithName("GetPublicSessionDraft")
            .WithTags("Responses");

        app.MapPut("/respondent/public-sessions/{handle}/answers", SavePublicSessionAnswers)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Session)
            .WithName("SavePublicSessionAnswers")
            .WithTags("Responses");

        app.MapPost("/respondent/public-sessions/{handle}/submit", SubmitPublicSession)
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.Submit)
            .WithName("SubmitPublicSession")
            .WithTags("Responses");

        app.MapPost("/respondent/campaigns/{campaignId:guid}/lab-assignment", CreateLabAssignment)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateLabAssignment")
            .WithTags("Responses");

        app.MapPost("/respondent/sessions", CreateSession)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateResponseSession")
            .WithTags("Responses");

        app.MapPut("/respondent/sessions/{sessionId:guid}/answers", SaveAnswers)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("SaveResponseAnswers")
            .WithTags("Responses");

        app.MapPost("/respondent/sessions/{sessionId:guid}/submit", SubmitSession)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("SubmitResponseSession")
            .WithTags("Responses");

        return app;
    }

    private static async Task<IResult> GetCampaign(
        Guid campaignId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRespondentCampaignQuery(campaignId), cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetOpenLinkEntry(
        string token,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOpenLinkEntryQuery(token), cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateOpenLinkSession(
        string token,
        CreateOpenLinkSessionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateOpenLinkSessionCommand(token, request),
            cancellationToken);

        return ResponseCaptureHttpResults.ToCreated(
            result,
            value => $"/respondent/open-links/{token}/sessions/{value.Id}");
    }

    private static async Task<IResult> GetIdentifiedEntry(
        string token,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetIdentifiedEntryQuery(token), cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateIdentifiedEntrySession(
        string token,
        CreateOpenLinkSessionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateIdentifiedEntrySessionCommand(token, request),
            cancellationToken);

        return ResponseCaptureHttpResults.ToCreated(
            result,
            value => $"/respondent/identified-entries/{token}/sessions/{value.Id}");
    }

    private static async Task<IResult> GetOpenLinkSessionDraft(
        string token,
        Guid sessionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOpenLinkSessionDraftQuery(token, sessionId),
            cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> SaveOpenLinkAnswers(
        string token,
        Guid sessionId,
        SaveAnswersRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SaveOpenLinkAnswersCommand(token, sessionId, request),
            cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> SubmitOpenLinkSession(
        string token,
        Guid sessionId,
        SubmitResponseSessionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubmitOpenLinkSessionCommand(token, sessionId, request),
            cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetPublicSessionDraft(
        string handle,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPublicSessionDraftQuery(handle),
            cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> SavePublicSessionAnswers(
        string handle,
        SaveAnswersRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SavePublicSessionAnswersCommand(handle, request),
            cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> SubmitPublicSession(
        string handle,
        SubmitResponseSessionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubmitPublicSessionCommand(handle, request),
            cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateLabAssignment(
        Guid campaignId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateLabAssignmentCommand(campaignId), cancellationToken);

        return ResponseCaptureHttpResults.ToCreated(
            result,
            value => $"/assignments/{value.AssignmentId}");
    }

    private static async Task<IResult> CreateSession(
        CreateResponseSessionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateResponseSessionCommand(request), cancellationToken);

        return ResponseCaptureHttpResults.ToCreated(
            result,
            value => $"/respondent/sessions/{value.Id}");
    }

    private static async Task<IResult> SaveAnswers(
        Guid sessionId,
        SaveAnswersRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SaveResponseAnswersCommand(sessionId, request), cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }

    private static async Task<IResult> SubmitSession(
        Guid sessionId,
        SubmitResponseSessionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SubmitResponseSessionCommand(sessionId, request), cancellationToken);

        return ResponseCaptureHttpResults.ToOk(result);
    }
}
