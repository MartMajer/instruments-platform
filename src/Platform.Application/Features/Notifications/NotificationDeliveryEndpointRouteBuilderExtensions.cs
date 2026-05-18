using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Features.Setup;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.Notifications;

public static class NotificationDeliveryEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);

    public static IEndpointRouteBuilder MapNotificationDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/campaigns/{id:guid}/notification-deliveries/process", ProcessCampaignEmailDeliveries)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ProcessCampaignEmailDeliveries")
            .WithTags("Setup");

        app.MapPost("/campaigns/{id:guid}/notification-deliveries/requeue-failed", RequeueFailedCampaignEmailDeliveries)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("RequeueFailedCampaignEmailDeliveries")
            .WithTags("Setup");

        app.MapGet("/operational-notifications", ListOperationalNotifications)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListOperationalNotifications")
            .WithTags("Setup");

        app.MapGet("/operational-notifications/summary", GetOperationalNotificationSummary)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("GetOperationalNotificationSummary")
            .WithTags("Setup");

        app.MapPost("/operational-notifications/{notificationId:guid}/mark-read", MarkOperationalNotificationRead)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("MarkOperationalNotificationRead")
            .WithTags("Setup");

        app.MapPost("/operational-notifications/mark-all-read", MarkAllOperationalNotificationsRead)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("MarkAllOperationalNotificationsRead")
            .WithTags("Setup");

        return app;
    }

    private static async Task<IResult> ProcessCampaignEmailDeliveries(
        Guid id,
        ProcessCampaignEmailDeliveriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ProcessCampaignEmailDeliveriesCommand(id, request),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> RequeueFailedCampaignEmailDeliveries(
        Guid id,
        RequeueFailedCampaignEmailDeliveriesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RequeueFailedCampaignEmailDeliveriesCommand(id, request),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> ListOperationalNotifications(
        int? limit,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ListOperationalNotificationsQuery(limit ?? 25),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> GetOperationalNotificationSummary(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOperationalNotificationSummaryQuery(),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> MarkOperationalNotificationRead(
        Guid notificationId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new MarkOperationalNotificationReadCommand(notificationId),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> MarkAllOperationalNotificationsRead(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new MarkAllOperationalNotificationsReadCommand(),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }
}
