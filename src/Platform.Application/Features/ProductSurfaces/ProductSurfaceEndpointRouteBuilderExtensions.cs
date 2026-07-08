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

        app.MapPut("/tenant-settings/report-branding", UpdateTenantReportBranding)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("UpdateTenantReportBranding")
            .WithTags("ProductSurfaces");

        app.MapPut("/tenant-settings/app-branding", UpdateTenantAppBranding)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("UpdateTenantAppBranding")
            .WithTags("ProductSurfaces");

        app.MapPost("/tenant-settings/app-branding/logo", UploadTenantAppBrandingLogo)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("UploadTenantAppBrandingLogo")
            .WithTags("ProductSurfaces");

        app.MapGet("/tenant-settings/app-branding", GetTenantAppBranding)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetTenantAppBranding")
            .WithTags("ProductSurfaces");

        app.MapGet("/tenant-settings/app-branding/logo", GetTenantAppBrandingLogo)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember)
            .WithName("GetTenantAppBrandingLogo")
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

        app.MapGet("/directory-connections/microsoft-graph", GetMicrosoftGraphDirectoryConnectionState)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("GetMicrosoftGraphDirectoryConnectionState")
            .WithTags("ProductSurfaces");

        app.MapGet("/directory-connections/microsoft-graph/import-runs", ListMicrosoftGraphDirectoryImportRuns)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListMicrosoftGraphDirectoryImportRuns")
            .WithTags("ProductSurfaces");

        app.MapGet("/directory-connections/microsoft-graph/import-rules", ListMicrosoftGraphDirectoryImportRules)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListMicrosoftGraphDirectoryImportRules")
            .WithTags("ProductSurfaces");

        app.MapPost("/directory-connections/microsoft-graph/import-rules", SaveMicrosoftGraphDirectoryImportRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("SaveMicrosoftGraphDirectoryImportRule")
            .WithTags("ProductSurfaces");

        app.MapPost("/directory-connections/microsoft-graph/import-rules/{ruleId:guid}/preview", PreviewMicrosoftGraphDirectoryImportRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("PreviewMicrosoftGraphDirectoryImportRule")
            .WithTags("ProductSurfaces");

        app.MapPost("/directory-connections/microsoft-graph/import-rules/{ruleId:guid}/apply", ApplyMicrosoftGraphDirectoryImportRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ApplyMicrosoftGraphDirectoryImportRule")
            .WithTags("ProductSurfaces");

        app.MapPost("/directory-connections/microsoft-graph/import-rules/{ruleId:guid}/live-preview", PreviewLiveMicrosoftGraphDirectoryImportRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("PreviewLiveMicrosoftGraphDirectoryImportRule")
            .WithTags("ProductSurfaces");

        app.MapPost("/directory-connections/microsoft-graph/import-rules/{ruleId:guid}/live-apply", ApplyLiveMicrosoftGraphDirectoryImportRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ApplyLiveMicrosoftGraphDirectoryImportRule")
            .WithTags("ProductSurfaces");

        app.MapDelete("/directory-connections/microsoft-graph/import-rules/{ruleId:guid}", ArchiveMicrosoftGraphDirectoryImportRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ArchiveMicrosoftGraphDirectoryImportRule")
            .WithTags("ProductSurfaces");

        app.MapPost("/directory-connections/microsoft-graph/consent-requests", CreateMicrosoftGraphConsentRequest)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateMicrosoftGraphConsentRequest")
            .WithTags("ProductSurfaces");

        app.MapPost("/directory-connections/microsoft-graph/consent-callback", CompleteMicrosoftGraphConsentCallback)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CompleteMicrosoftGraphConsentCallback")
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
        EnsureSampleStudiesRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new EnsureSampleStudiesCommand(request?.Locale),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetTenantSettings(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantSettingsQuery(), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> UpdateTenantReportBranding(
        UpdateTenantReportBrandingRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateTenantReportBrandingCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> UpdateTenantAppBranding(
        UpdateTenantAppBrandingRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateTenantAppBrandingCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> UploadTenantAppBrandingLogo(
        HttpRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Raw-body upload (Content-Type: image/*) rather than multipart form, so
        // it clears the app's cookie CSRF check without the minimal-API form
        // antiforgery machinery. The body is bounded well under any DoS concern.
        var read = await ReadBoundedBodyAsync(request, TenantAppBrandingLogo.MaxBytes, cancellationToken);
        if (read.IsFailure)
        {
            return ProductSurfaceHttpResults.ToOk(read);
        }

        var result = await sender.Send(
            new UploadTenantAppBrandingLogoCommand(request.ContentType, read.Value),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetTenantAppBranding(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantAppBrandingQuery(), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetTenantAppBrandingLogo(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantAppBrandingLogoQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return ProductSurfaceHttpResults.ToOk(result);
        }

        return Results.File(result.Value.Content, result.Value.ContentType);
    }

    private static async Task<Platform.SharedKernel.Result<byte[]>> ReadBoundedBodyAsync(
        HttpRequest request,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength is > 0 and var declared && declared > maxBytes)
        {
            return Platform.SharedKernel.Result.Failure<byte[]>(Platform.SharedKernel.Error.Validation(
                "app_branding_logo.too_large",
                $"Logo exceeds the {maxBytes / 1024} KB limit."));
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int bytesRead;
        while ((bytesRead = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + bytesRead > maxBytes)
            {
                return Platform.SharedKernel.Result.Failure<byte[]>(Platform.SharedKernel.Error.Validation(
                    "app_branding_logo.too_large",
                    $"Logo exceeds the {maxBytes / 1024} KB limit."));
            }

            await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken);
        }

        return Platform.SharedKernel.Result.Success(buffer.ToArray());
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
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListSubjectsQuery(), cancellationToken));
    }

    private static async Task<IResult> GetMicrosoftGraphDirectoryConnectionState(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new GetMicrosoftGraphDirectoryConnectionStateQuery(), cancellationToken));
    }

    private static async Task<IResult> ListMicrosoftGraphDirectoryImportRuns(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListMicrosoftGraphDirectoryImportRunsQuery(), cancellationToken));
    }

    private static async Task<IResult> ListMicrosoftGraphDirectoryImportRules(
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await sender.Send(new ListMicrosoftGraphDirectoryImportRulesQuery(), cancellationToken));
    }

    private static async Task<IResult> SaveMicrosoftGraphDirectoryImportRule(
        SaveMicrosoftGraphImportRuleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SaveMicrosoftGraphDirectoryImportRuleCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ArchiveMicrosoftGraphDirectoryImportRule(
        Guid ruleId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveMicrosoftGraphDirectoryImportRuleCommand(ruleId), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> PreviewMicrosoftGraphDirectoryImportRule(
        Guid ruleId,
        PreviewMicrosoftGraphImportRuleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PreviewMicrosoftGraphDirectoryImportRuleCommand(ruleId, request),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ApplyMicrosoftGraphDirectoryImportRule(
        Guid ruleId,
        ApplyMicrosoftGraphImportRuleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ApplyMicrosoftGraphDirectoryImportRuleCommand(ruleId, request),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> PreviewLiveMicrosoftGraphDirectoryImportRule(
        Guid ruleId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PreviewLiveMicrosoftGraphDirectoryImportRuleCommand(ruleId),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ApplyLiveMicrosoftGraphDirectoryImportRule(
        Guid ruleId,
        LiveApplyMicrosoftGraphImportRuleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ApplyLiveMicrosoftGraphDirectoryImportRuleCommand(ruleId, request),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateMicrosoftGraphConsentRequest(
        CreateMicrosoftGraphConsentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateMicrosoftGraphConsentRequestCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> CompleteMicrosoftGraphConsentCallback(
        CompleteMicrosoftGraphConsentCallbackRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteMicrosoftGraphConsentCallbackCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
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
