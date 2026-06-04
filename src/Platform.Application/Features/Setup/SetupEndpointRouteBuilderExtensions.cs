using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.Setup;

public static class SetupEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);

    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/instruments/private-imports", CreatePrivateInstrumentImport)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreatePrivateInstrumentImport")
            .WithTags("Setup");

        app.MapGet("/instruments", ListInstruments)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("ListInstruments")
            .WithTags("Setup");

        app.MapPost("/template-versions", CreateTemplateVersion)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateTemplateVersion")
            .WithTags("Setup");

        app.MapGet("/template-versions/{id:guid}", GetTemplateVersion)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetTemplateVersion")
            .WithTags("Setup");

        app.MapPost("/scoring-rules", CreateScoringRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateScoringRule")
            .WithTags("Setup");

        app.MapPost("/campaign-series", CreateCampaignSeries)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignSeries")
            .WithTags("Setup");

        app.MapGet("/campaign-series/{id:guid}/two-wave-proof", GetCampaignSeriesTwoWaveProof)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignSeriesTwoWaveProof")
            .WithTags("Setup");

        app.MapPost("/campaigns", CreateCampaign)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaign")
            .WithTags("Setup");

        app.MapGet("/campaigns/{id:guid}/launch-readiness", GetLaunchReadiness)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetLaunchReadiness")
            .WithTags("Setup");

        app.MapGet("/campaigns/{id:guid}/respondent-rules", ListCampaignRespondentRules)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("ListCampaignRespondentRules")
            .WithTags("Setup");

        app.MapPut("/campaigns/{id:guid}/respondent-rules", UpdateCampaignRespondentRules)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("UpdateCampaignRespondentRules")
            .WithTags("Setup");

        app.MapGet("/campaigns/{id:guid}/assignments", ListCampaignAssignments)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("ListCampaignAssignments")
            .WithTags("Setup");

        app.MapPost("/campaigns/{id:guid}/launch", LaunchCampaign)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("LaunchCampaign")
            .WithTags("Setup");

        app.MapPost("/campaigns/{id:guid}/open-link", CreateCampaignOpenLink)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignOpenLink")
            .WithTags("Setup");

        app.MapPost("/campaigns/{id:guid}/open-link/replace", ReplaceCampaignOpenLink)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ReplaceCampaignOpenLink")
            .WithTags("Setup");

        app.MapPost("/campaigns/{id:guid}/identified-entry", CreateCampaignIdentifiedEntry)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignIdentifiedEntry")
            .WithTags("Setup");

        app.MapPost("/campaigns/{id:guid}/identified-queue-access", CreateCampaignIdentifiedQueueAccess)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignIdentifiedQueueAccess")
            .WithTags("Setup");

        app.MapPost("/campaigns/{id:guid}/identified-queue-invitation-batches", CreateCampaignIdentifiedQueueInvitationBatch)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignIdentifiedQueueInvitationBatch")
            .WithTags("Setup");

        app.MapPost("/campaigns/{id:guid}/invitation-batches", CreateCampaignInvitationBatch)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignInvitationBatch")
            .WithTags("Setup");

        return app;
    }

    private static async Task<IResult> CreatePrivateInstrumentImport(
        CreatePrivateInstrumentImportRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePrivateInstrumentImportCommand(request),
            cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/instruments/{value.Id}");
    }

    private static async Task<IResult> ListInstruments(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListInstrumentsQuery(), cancellationToken));
    }

    private static async Task<IResult> CreateTemplateVersion(
        CreateTemplateVersionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateTemplateVersionCommand(request),
            cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/template-versions/{value.TemplateVersionId}");
    }

    private static async Task<IResult> GetTemplateVersion(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTemplateVersionQuery(id), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateScoringRule(
        CreateScoringRuleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateSetupScoringRuleCommand(request),
            cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/scoring-rules/{value.Id}");
    }

    private static async Task<IResult> CreateCampaignSeries(
        CreateCampaignSeriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateSetupCampaignSeriesCommand(request),
            cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/campaign-series/{value.Id}");
    }

    private static async Task<IResult> GetCampaignSeriesTwoWaveProof(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignSeriesTwoWaveProofQuery(id), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateCampaign(
        CreateCampaignRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateSetupCampaignCommand(request),
            cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/campaigns/{value.Id}");
    }

    private static async Task<IResult> GetLaunchReadiness(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLaunchReadinessQuery(id), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> ListCampaignRespondentRules(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListCampaignRespondentRulesQuery(id), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> UpdateCampaignRespondentRules(
        Guid id,
        UpdateCampaignRespondentRulesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateCampaignRespondentRulesCommand(id, request), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> ListCampaignAssignments(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListCampaignAssignmentsQuery(id), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> LaunchCampaign(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LaunchCampaignCommand(id), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateCampaignOpenLink(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCampaignOpenLinkCommand(id), cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => value.RespondentPath);
    }

    private static async Task<IResult> ReplaceCampaignOpenLink(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReplaceCampaignOpenLinkCommand(id), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateCampaignIdentifiedEntry(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCampaignIdentifiedEntryCommand(id), cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => value.RespondentPath);
    }

    private static async Task<IResult> CreateCampaignIdentifiedQueueAccess(
        Guid id,
        CreateCampaignIdentifiedQueueAccessRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCampaignIdentifiedQueueAccessCommand(
                id,
                request ?? new CreateCampaignIdentifiedQueueAccessRequest()),
            cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/campaigns/{value.CampaignId}/identified-queue-access");
    }

    private static async Task<IResult> CreateCampaignInvitationBatch(
        Guid id,
        CreateCampaignInvitationBatchRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCampaignInvitationBatchCommand(id, request), cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/campaigns/{value.CampaignId}/invitation-batches");
    }

    private static async Task<IResult> CreateCampaignIdentifiedQueueInvitationBatch(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCampaignIdentifiedQueueInvitationBatchCommand(id),
            cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/campaigns/{value.CampaignId}/identified-queue-invitation-batches");
    }
}
