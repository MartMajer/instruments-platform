using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Features.ProductSurfaces;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.DirectoryImports;

public static class DirectoryImportEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);

    public static IEndpointRouteBuilder MapDirectoryImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/directory-imports/workspace", ListDirectoryImportWorkspace)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListDirectoryImportWorkspace")
            .WithTags("DirectoryImports");

        app.MapPost("/directory-connections", CreateDirectoryConnection)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateDirectoryConnection")
            .WithTags("DirectoryImports");

        app.MapPost(
                "/directory-connections/microsoft-graph/admin-consent/start",
                StartMicrosoftGraphAdminConsent)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("StartMicrosoftGraphAdminConsent")
            .WithTags("DirectoryImports");

        app.MapGet(
                "/directory-connections/microsoft-graph/admin-consent/callback",
                CompleteMicrosoftGraphAdminConsent)
            .WithName("CompleteMicrosoftGraphAdminConsent")
            .WithTags("DirectoryImports");

        app.MapPost("/directory-import-rules", CreateDirectoryImportRule)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateDirectoryImportRule")
            .WithTags("DirectoryImports");

        app.MapPost("/directory-import-rules/{ruleId:guid}/preview", PreviewDirectoryImport)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("PreviewDirectoryImport")
            .WithTags("DirectoryImports");

        app.MapPost("/directory-import-runs/{previewRunId:guid}/apply", ApplyDirectoryImport)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ApplyDirectoryImport")
            .WithTags("DirectoryImports");

        return app;
    }

    private static async Task<IResult> ListDirectoryImportWorkspace(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListDirectoryImportWorkspaceQuery(), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateDirectoryConnection(
        CreateDirectoryConnectionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateDirectoryConnectionCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> StartMicrosoftGraphAdminConsent(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartMicrosoftGraphAdminConsentCommand(), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> CompleteMicrosoftGraphAdminConsent(
        string? admin_consent,
        string? tenant,
        string? scope,
        string? state,
        string? error,
        string? error_description,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompleteMicrosoftGraphAdminConsentCommand(
                new CompleteMicrosoftGraphAdminConsentRequest(
                    admin_consent,
                    tenant,
                    scope,
                    state,
                    error,
                    error_description)),
            cancellationToken);

        return result.IsSuccess
            ? Results.Redirect(result.Value.RedirectUrl)
            : ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> CreateDirectoryImportRule(
        CreateDirectoryImportRuleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateDirectoryImportRuleCommand(request), cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> PreviewDirectoryImport(
        Guid ruleId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PreviewDirectoryImportCommand(new PreviewDirectoryImportRequest(ruleId)),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }

    private static async Task<IResult> ApplyDirectoryImport(
        Guid previewRunId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ApplyDirectoryImportCommand(new ApplyDirectoryImportRequest(previewRunId)),
            cancellationToken);

        return ProductSurfaceHttpResults.ToOk(result);
    }
}
