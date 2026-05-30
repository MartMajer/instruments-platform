using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.ProductSurfaces;

public static class ProductSurfaceEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);
    private static readonly string TeamManagePolicy = PlatformPolicies.Permission(PlatformPermissions.TeamManage);

    public static IEndpointRouteBuilder MapProductSurfaceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/workspace-overview", GetWorkspaceOverview)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetWorkspaceOverview")
            .WithTags("ProductSurfaces");

        app.MapPost("/sample-studies/ensure", EnsureSampleStudies)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("EnsureSampleStudies")
            .WithTags("ProductSurfaces");

        app.MapGet("/tenant-settings", GetTenantSettings)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetTenantSettings")
            .WithTags("ProductSurfaces");

        app.MapPut("/tenant-settings/language", UpdateTenantLanguage)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("UpdateTenantLanguage")
            .WithTags("ProductSurfaces");

        app.MapPut("/tenant-settings/email-templates/{templateCode}/{locale}", UpdateTenantEmailTemplate)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("UpdateTenantEmailTemplate")
            .WithTags("ProductSurfaces");

        app.MapDelete("/tenant-settings/email-templates/{templateCode}/{locale}", ResetTenantEmailTemplate)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ResetTenantEmailTemplate")
            .WithTags("ProductSurfaces");

        app.MapGet("/export-artifacts", ListExportArtifacts)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("ListProductExportArtifacts")
            .WithTags("ProductSurfaces");

        app.MapGet("/campaign-series", ListCampaignSeries)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("ListProductCampaignSeries")
            .WithTags("ProductSurfaces");

        app.MapGet("/tenant-members", ListTenantMembers)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("ListTenantMembers")
            .WithTags("ProductSurfaces");

        app.MapGet("/tenant-roles", ListTenantRoles)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("ListTenantRoles")
            .WithTags("ProductSurfaces");

        app.MapGet("/subjects", ListSubjects)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListSubjects")
            .WithTags("ProductSurfaces");

        app.MapPost("/subjects", CreateSubject)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateSubject")
            .WithTags("ProductSurfaces");

        app.MapPost("/subjects/imports/csv", ImportSubjectDirectoryCsv)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ImportSubjectDirectoryCsv")
            .WithTags("ProductSurfaces");

        app.MapPut("/subjects/{subjectId:guid}", UpdateSubject)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("UpdateSubject")
            .WithTags("ProductSurfaces");

        app.MapPost("/subjects/{subjectId:guid}/status", SetSubjectDirectoryStatus)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("SetSubjectDirectoryStatus")
            .WithTags("ProductSurfaces");

        app.MapPost("/subjects/{subjectId:guid}/deactivate", DeactivateSubject)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("DeactivateSubject")
            .WithTags("ProductSurfaces");

        app.MapGet("/subject-groups", ListSubjectGroups)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListSubjectGroups")
            .WithTags("ProductSurfaces");

        app.MapPost("/subject-groups", CreateSubjectGroup)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateSubjectGroup")
            .WithTags("ProductSurfaces");

        app.MapPost("/subject-groups/{groupId:guid}/members", AddSubjectGroupMember)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("AddSubjectGroupMember")
            .WithTags("ProductSurfaces");

        app.MapDelete("/subject-groups/{groupId:guid}/members/{subjectId:guid}", RemoveSubjectGroupMember)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("RemoveSubjectGroupMember")
            .WithTags("ProductSurfaces");

        app.MapPut("/subjects/{subjectId:guid}/manager", SetSubjectManager)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("SetSubjectManager")
            .WithTags("ProductSurfaces");

        app.MapPost("/tenant-members", CreateTenantMember)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, TeamManagePolicy)
            .WithName("CreateTenantMember")
            .WithTags("ProductSurfaces");

        app.MapPut("/tenant-members/{userId:guid}/tenant-role", ChangeTenantMemberRole)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, TeamManagePolicy)
            .WithName("ChangeTenantMemberRole")
            .WithTags("ProductSurfaces");

        app.MapPost("/tenant-members/{userId:guid}/suspend", SuspendTenantMember)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, TeamManagePolicy)
            .WithName("SuspendTenantMember")
            .WithTags("ProductSurfaces");

        app.MapPost("/tenant-members/{userId:guid}/reactivate", ReactivateTenantMember)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, TeamManagePolicy)
            .WithName("ReactivateTenantMember")
            .WithTags("ProductSurfaces");

        app.MapDelete("/tenant-members/{userId:guid}", RemoveTenantMember)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, TeamManagePolicy)
            .WithName("RemoveTenantMember")
            .WithTags("ProductSurfaces");

        app.MapGet("/campaign-series/{id:guid}", GetCampaignSeriesHub)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignSeriesHub")
            .WithTags("ProductSurfaces");

        app.MapPatch("/campaign-series/{id:guid}", RenameCampaignSeries)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("RenameCampaignSeries")
            .WithTags("ProductSurfaces");

        app.MapPost("/campaign-series/{id:guid}/duplicate", DuplicateCampaignSeries)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("DuplicateCampaignSeries")
            .WithTags("ProductSurfaces");

        app.MapPost("/campaign-series/{id:guid}/archive", ArchiveCampaignSeries)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ArchiveCampaignSeries")
            .WithTags("ProductSurfaces");

        app.MapPost("/campaign-series/{id:guid}/restore", RestoreCampaignSeries)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("RestoreCampaignSeries")
            .WithTags("ProductSurfaces");

        app.MapPost("/campaign-series/{seriesId:guid}/campaigns/{campaignId:guid}/close", CloseCampaign)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CloseCampaign")
            .WithTags("ProductSurfaces");

        app.MapPost(
                "/campaign-series/{seriesId:guid}/campaigns/{campaignId:guid}/respondent-rule-preview",
                PreviewRespondentRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("PreviewRespondentRule")
            .WithTags("ProductSurfaces");

        app.MapPost("/campaign-series/{id:guid}/score-remediation", RemediateCampaignSeriesScores)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("RemediateCampaignSeriesScores")
            .WithTags("ProductSurfaces");

        app.MapGet("/campaign-series/{id:guid}/setup-workspace", GetCampaignSeriesSetupWorkspace)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignSeriesSetupWorkspace")
            .WithTags("ProductSurfaces");

        app.MapGet("/campaign-series/{id:guid}/operations-workspace", GetCampaignSeriesOperationsWorkspace)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignSeriesOperationsWorkspace")
            .WithTags("ProductSurfaces");

        app.MapGet("/campaign-series/{id:guid}/reports-workspace", GetCampaignSeriesReportsWorkspace)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignSeriesReportsWorkspace")
            .WithTags("ProductSurfaces");

        app.MapGet("/campaign-series/{id:guid}/reports-widget-manifest", GetCampaignSeriesReportsWidgetManifest)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignSeriesReportsWidgetManifest")
            .WithTags("ProductSurfaces");

        app.MapGet("/campaign-series/{id:guid}/waves-workspace", GetCampaignSeriesWavesWorkspace)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetCampaignSeriesWavesWorkspace")
            .WithTags("ProductSurfaces");

        return app;
    }

    private static async Task<IResult> GetWorkspaceOverview(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new GetWorkspaceOverviewQuery(), cancellationToken));
    }

    private static async Task<IResult> EnsureSampleStudies(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new EnsureSampleStudiesCommand(), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetTenantSettings(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantSettingsQuery(), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> UpdateTenantLanguage(
        UpdateTenantLanguageRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateTenantLanguageCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> UpdateTenantEmailTemplate(
        string templateCode,
        string locale,
        UpdateEmailTemplateRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateTenantEmailTemplateCommand(templateCode, locale, request),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ResetTenantEmailTemplate(
        string templateCode,
        string locale,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ResetTenantEmailTemplateCommand(templateCode, locale),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ListCampaignSeries(
        string? search,
        string? status,
        string? sort,
        string? visibility,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListCampaignSeriesQuery(search, status, sort, visibility), cancellationToken));
    }

    private static async Task<IResult> ListExportArtifacts(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListExportArtifactsQuery(), cancellationToken));
    }

    private static async Task<IResult> ListTenantMembers(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListTenantMembersQuery(), cancellationToken));
    }

    private static async Task<IResult> ListTenantRoles(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListTenantRolesQuery(), cancellationToken));
    }

    private static async Task<IResult> ListSubjects(
        string? search,
        int? skip,
        int? take,
        string? sort,
        string? source,
        string? status,
        Guid? groupId,
        string? manager,
        string? contact,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(
            new ListSubjectsQuery(search, skip ?? 0, take, sort, source, status, groupId, manager, contact),
            cancellationToken));
    }

    private static async Task<IResult> CreateSubject(
        CreateSubjectRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSubjectCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> UpdateSubject(
        Guid subjectId,
        UpdateSubjectRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateSubjectCommand(subjectId, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> DeactivateSubject(
        Guid subjectId,
        DeactivateSubjectRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateSubjectCommand(subjectId, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> SetSubjectDirectoryStatus(
        Guid subjectId,
        SetSubjectDirectoryStatusRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetSubjectDirectoryStatusCommand(subjectId, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ImportSubjectDirectoryCsv(
        SubjectDirectoryCsvImportRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ImportSubjectDirectoryCsvCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ListSubjectGroups(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListSubjectGroupsQuery(), cancellationToken));
    }

    private static async Task<IResult> CreateSubjectGroup(
        CreateSubjectGroupRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSubjectGroupCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> AddSubjectGroupMember(
        Guid groupId,
        AddSubjectGroupMemberRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddSubjectGroupMemberCommand(groupId, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> RemoveSubjectGroupMember(
        Guid groupId,
        Guid subjectId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveSubjectGroupMemberCommand(groupId, subjectId), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> SetSubjectManager(
        Guid subjectId,
        SetSubjectManagerRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetSubjectManagerCommand(subjectId, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateTenantMember(
        CreateTenantMemberRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateTenantMemberCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ChangeTenantMemberRole(
        Guid userId,
        ChangeTenantMemberRoleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeTenantMemberRoleCommand(userId, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> SuspendTenantMember(
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SuspendTenantMemberCommand(userId), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ReactivateTenantMember(
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReactivateTenantMemberCommand(userId), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> RemoveTenantMember(
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveTenantMemberCommand(userId), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetCampaignSeriesHub(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignSeriesHubQuery(id), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> RenameCampaignSeries(
        Guid id,
        RenameCampaignSeriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RenameCampaignSeriesCommand(id, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> DuplicateCampaignSeries(
        Guid id,
        DuplicateCampaignSeriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DuplicateCampaignSeriesCommand(id, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ArchiveCampaignSeries(
        Guid id,
        ArchiveCampaignSeriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveCampaignSeriesCommand(id, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> RestoreCampaignSeries(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreCampaignSeriesCommand(id), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> CloseCampaign(
        Guid seriesId,
        Guid campaignId,
        CloseCampaignRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CloseCampaignCommand(seriesId, campaignId, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> PreviewRespondentRule(
        Guid seriesId,
        Guid campaignId,
        RespondentRulePreviewRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PreviewRespondentRuleQuery(seriesId, campaignId, request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> RemediateCampaignSeriesScores(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemediateCampaignSeriesScoresCommand(id), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetCampaignSeriesSetupWorkspace(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignSeriesSetupWorkspaceQuery(id), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetCampaignSeriesOperationsWorkspace(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignSeriesOperationsWorkspaceQuery(id), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetCampaignSeriesReportsWorkspace(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignSeriesReportsWorkspaceQuery(id), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetCampaignSeriesReportsWidgetManifest(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignSeriesReportsWidgetManifestQuery(id), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetCampaignSeriesWavesWorkspace(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignSeriesWavesWorkspaceQuery(id), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }
}
