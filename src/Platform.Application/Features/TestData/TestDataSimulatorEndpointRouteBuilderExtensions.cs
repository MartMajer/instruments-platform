using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Platform.Application.Auth;
using Platform.Application.Features.Setup;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.TestData;

public static class TestDataSimulatorEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);

    public static IEndpointRouteBuilder MapTestDataSimulatorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/test-data/campaigns/{id:guid}/recipients", CreateCampaignTestRecipients)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignTestRecipients")
            .WithTags("Test data");

        app.MapPost("/test-data/campaigns/{id:guid}/responses", CreateCampaignTestResponses)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateCampaignTestResponses")
            .WithTags("Test data");

        return app;
    }

    private static async Task<IResult> CreateCampaignTestRecipients(
        Guid id,
        CreateCampaignTestRecipientsRequest request,
        ISender sender,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!TestDataToolsEnabled(configuration, environment))
        {
            return Results.Problem(
                title: "test_data.disabled",
                detail: "Test data tools are disabled in this environment.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await sender.Send(new CreateCampaignTestRecipientsCommand(id, request), cancellationToken);

        return SetupHttpResults.ToCreated(
            result,
            value => $"/test-data/campaigns/{value.CampaignId}/recipients/{value.GroupId}");
    }

    private static async Task<IResult> CreateCampaignTestResponses(
        Guid id,
        CreateCampaignTestResponsesRequest request,
        ISender sender,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!TestDataToolsEnabled(configuration, environment))
        {
            return Results.Problem(
                title: "test_data.disabled",
                detail: "Test data tools are disabled in this environment.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await sender.Send(new CreateCampaignTestResponsesCommand(id, request), cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static bool TestDataToolsEnabled(IConfiguration configuration, IHostEnvironment environment)
    {
        return !environment.IsProduction() || configuration.GetValue("TestDataTools:Enabled", false);
    }
}
