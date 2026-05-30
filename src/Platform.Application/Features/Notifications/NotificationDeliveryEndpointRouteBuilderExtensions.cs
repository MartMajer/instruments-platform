using System.Globalization;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Application.Auth;
using Platform.Application.Features.Responses;
using Platform.Application.Features.Setup;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;

namespace Platform.Application.Features.Notifications;

public static class NotificationDeliveryEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);
    private static readonly JsonSerializerOptions ProviderWebhookJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly UTF8Encoding ProviderWebhookUtf8Encoding = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private const int ProviderWebhookMaxPayloadBytes = 64 * 1024;
    private const string AzureCommunicationServicesEventGridWebhookSecretConfigurationKey =
        "EmailDelivery:AzureCommunicationServices:EventGridWebhookSecret";

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

        app.MapGet("/campaigns/{id:guid}/notification-deliveries/repair-readiness", GetCampaignEmailDeliveryRepairReadiness)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("GetCampaignEmailDeliveryRepairReadiness")
            .WithTags("Setup");

        app.MapGet("/email-suppressions", ListEmailSuppressions)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListEmailSuppressions")
            .WithTags("Setup");

        app.MapPost("/email-suppressions", AddEmailSuppression)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("AddEmailSuppression")
            .WithTags("Setup");

        app.MapPost("/email-suppressions/{id:guid}/release", ReleaseEmailSuppression)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ReleaseEmailSuppression")
            .WithTags("Setup");

        app.MapGet("/notification-deliveries/provider-events", ListProviderDeliveryEvents)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListProviderDeliveryEvents")
            .WithTags("Setup");

        app.MapPost("/notification-deliveries/provider-events", RecordProviderDeliveryEvent)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("RecordProviderDeliveryEvent")
            .WithTags("Setup");

        app.MapPost("/notification-deliveries/provider-events/azure-communication-email", RecordAzureCommunicationEmailProviderDeliveryEventWebhook)
            .AllowAnonymous()
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.ProviderWebhook)
            .WithName("RecordAzureCommunicationEmailProviderDeliveryEventWebhook")
            .WithTags("Setup");

        app.MapGet("/notification-deliveries/email-readiness", GetEmailDeliveryReadiness)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("GetEmailDeliveryReadiness")
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

    private static async Task<IResult> GetCampaignEmailDeliveryRepairReadiness(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCampaignEmailDeliveryRepairReadinessQuery(id),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> ListEmailSuppressions(
        int? limit,
        bool? includeReleased,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ListEmailSuppressionsQuery(limit ?? 50, includeReleased ?? false),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> AddEmailSuppression(
        AddEmailSuppressionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddEmailSuppressionCommand(request),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> ReleaseEmailSuppression(
        Guid id,
        ReleaseEmailSuppressionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReleaseEmailSuppressionCommand(id, request),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> RecordProviderDeliveryEvent(
        RecordProviderDeliveryEventRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RecordProviderDeliveryEventCommand(request),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> ListProviderDeliveryEvents(
        int? limit,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ListProviderDeliveryEventsQuery(limit ?? 50),
            cancellationToken);

        return SetupHttpResults.ToOk(result);
    }

    private static IResult GetEmailDeliveryReadiness(IConfiguration configuration)
    {
        return Results.Ok(CreateEmailDeliveryReadiness(configuration));
    }

    private static async Task<IResult> RecordAzureCommunicationEmailProviderDeliveryEventWebhook(
        HttpRequest httpRequest,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AzureCommunicationEmailWebhook");
        var configuredProvider = configuration["EmailDelivery:Provider"]?.Trim();
        if (!string.Equals(configuredProvider, EmailDeliveryProviderNames.AzureCommunicationEmail, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Azure Communication Services Email webhook disabled. Provider={Provider}.",
                configuredProvider);
            return Results.NotFound();
        }

        var expectedSecret = configuration[AzureCommunicationServicesEventGridWebhookSecretConfigurationKey]?.Trim();
        if (string.IsNullOrWhiteSpace(expectedSecret) || expectedSecret.Length < 32)
        {
            logger.LogWarning("Azure Communication Services Email webhook secret is not configured safely.");
            return Results.Problem(
                title: "acs_email_webhook.misconfigured",
                detail: "Azure Communication Services Event Grid webhook secret is not configured safely.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (httpRequest.ContentLength is > ProviderWebhookMaxPayloadBytes)
        {
            return Results.Problem(
                title: "acs_email_webhook.payload_too_large",
                detail: "Azure Communication Services Event Grid payload is too large.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (!IsJsonContentType(httpRequest.ContentType))
        {
            return Results.Problem(
                title: "acs_email_webhook.content_type_invalid",
                detail: "Azure Communication Services Event Grid payload must use a JSON content type.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var bodyRead = await ReadProviderWebhookBodyAsync(httpRequest.Body, cancellationToken);
        if (bodyRead.PayloadTooLarge)
        {
            return Results.Problem(
                title: "acs_email_webhook.payload_too_large",
                detail: "Azure Communication Services Event Grid payload is too large.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (bodyRead.InvalidUtf8)
        {
            return Results.Problem(
                title: "acs_email_webhook.payload_invalid",
                detail: "Azure Communication Services Event Grid payload is not valid UTF-8 JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        JsonDocument payloadDocument;
        try
        {
            payloadDocument = JsonDocument.Parse(bodyRead.Body ?? string.Empty);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Azure Communication Services Event Grid payload rejected because JSON parsing failed.");
            return Results.Problem(
                title: "acs_email_webhook.payload_invalid",
                detail: "Azure Communication Services Event Grid payload is not valid JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        using (payloadDocument)
        {
            if (payloadDocument.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Results.Problem(
                    title: "acs_email_webhook.payload_invalid",
                    detail: "Azure Communication Services Event Grid payload must be a JSON array.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            foreach (var eventElement in payloadDocument.RootElement.EnumerateArray())
            {
                var eventType = GetOptionalString(eventElement, "eventType");
                if (string.Equals(eventType, "Microsoft.EventGrid.SubscriptionValidationEvent", StringComparison.Ordinal))
                {
                    if (eventElement.TryGetProperty("data", out var validationData) &&
                        validationData.ValueKind == JsonValueKind.Object)
                    {
                        var validationCode = GetOptionalString(validationData, "validationCode");
                        if (!string.IsNullOrWhiteSpace(validationCode))
                        {
                            return Results.Ok(new AzureEventGridValidationResponse(validationCode));
                        }
                    }

                    return Results.Problem(
                        title: "acs_email_webhook.validation_invalid",
                        detail: "Azure Event Grid subscription validation payload is invalid.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
            }

            if (!HasValidAcsEventGridSecret(httpRequest, expectedSecret))
            {
                logger.LogWarning("Azure Communication Services Event Grid webhook rejected because secret was missing or invalid.");
                return Results.Problem(
                    title: "acs_email_webhook.secret_invalid",
                    detail: "Azure Communication Services Event Grid webhook secret is invalid.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            foreach (var eventElement in payloadDocument.RootElement.EnumerateArray())
            {
                var eventType = GetOptionalString(eventElement, "eventType");
                if (string.Equals(eventType, "Microsoft.Communication.EmailEngagementTrackingReportReceived", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(eventType, "Microsoft.Communication.EmailDeliveryReportReceived", StringComparison.Ordinal))
                {
                    return Results.Problem(
                        title: "acs_email_webhook.event_type_unsupported",
                        detail: "Azure Communication Services Event Grid event type is not supported.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                if (!TryCreateAzureCommunicationEmailProviderEventRequest(
                    eventElement,
                    out var providerEvent,
                    out var failureDetail))
                {
                    return Results.Problem(
                        title: "acs_email_webhook.message_invalid",
                        detail: failureDetail,
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var result = await sender.Send(
                    new RecordProviderDeliveryEventByProviderMessageIdCommand(providerEvent!),
                    cancellationToken);
                if (!result.IsSuccess)
                {
                    logger.LogWarning("Azure Communication Services Email provider event recording failed.");
                    return SetupHttpResults.ToOk(result);
                }
            }
        }

        return Results.NoContent();
    }

    private static bool HasValidAcsEventGridSecret(HttpRequest httpRequest, string expectedSecret)
    {
        if (httpRequest.Headers.TryGetValue("Authorization", out var authorizationValues) &&
            string.Equals(
                authorizationValues.ToString(),
                $"Bearer {expectedSecret}",
                StringComparison.Ordinal))
        {
            return true;
        }

        return httpRequest.Headers.TryGetValue("X-Platform-Webhook-Secret", out var secretValues) &&
            string.Equals(secretValues.ToString(), expectedSecret, StringComparison.Ordinal);
    }

    private static async Task<(string? Body, bool PayloadTooLarge, bool InvalidUtf8)> ReadProviderWebhookBodyAsync(
        Stream requestBody,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var bodyBytes = new MemoryStream();

        while (true)
        {
            var bytesRead = await requestBody.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            if (bodyBytes.Length + bytesRead > ProviderWebhookMaxPayloadBytes)
            {
                return (Body: null, PayloadTooLarge: true, InvalidUtf8: false);
            }

            bodyBytes.Write(buffer.AsSpan(0, bytesRead));
        }

        try
        {
            return (ProviderWebhookUtf8Encoding.GetString(bodyBytes.ToArray()), PayloadTooLarge: false, InvalidUtf8: false);
        }
        catch (DecoderFallbackException)
        {
            return (Body: null, PayloadTooLarge: false, InvalidUtf8: true);
        }
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var separatorIndex = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = separatorIndex >= 0 ? contentType[..separatorIndex] : contentType;
        mediaType = mediaType.Trim();

        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
            mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCreateAzureCommunicationEmailProviderEventRequest(
        JsonElement eventElement,
        out RecordProviderDeliveryEventByProviderMessageIdRequest? providerEvent,
        out string failureDetail)
    {
        providerEvent = null;
        failureDetail = "Azure Communication Services Event Grid message could not be mapped to a provider delivery event.";

        var providerEventId = GetOptionalString(eventElement, "id");
        if (string.IsNullOrWhiteSpace(providerEventId) ||
            providerEventId.Length > 256 ||
            providerEventId.Any(char.IsControl))
        {
            failureDetail = "Azure Communication Services Event Grid message is missing a valid event id.";
            return false;
        }

        if (!eventElement.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Object)
        {
            failureDetail = "Azure Communication Services Event Grid message is missing delivery data.";
            return false;
        }

        var providerMessageId = GetOptionalString(data, "messageId");
        if (string.IsNullOrWhiteSpace(providerMessageId) ||
            providerMessageId.Length > 256 ||
            providerMessageId.Length != providerMessageId.Trim().Length ||
            providerMessageId.Any(char.IsControl))
        {
            failureDetail = "Azure Communication Services Event Grid message is missing a valid message id.";
            return false;
        }

        var deliveryStatus = GetOptionalString(data, "status");
        var mappedEventType = MapAzureCommunicationEmailDeliveryStatus(deliveryStatus);
        if (mappedEventType is null)
        {
            failureDetail = "Azure Communication Services Event Grid delivery status is not supported.";
            return false;
        }

        var occurredAt = GetEventGridEventTime(eventElement);
        if (occurredAt is null)
        {
            failureDetail = "Azure Communication Services Event Grid message is missing a valid event time.";
            return false;
        }

        providerEvent = new RecordProviderDeliveryEventByProviderMessageIdRequest(
            EmailDeliveryProviderNames.AzureCommunicationEmail,
            providerMessageId,
            mappedEventType,
            occurredAt,
            providerEventId,
            $"acs_email:{deliveryStatus!.Trim().ToLowerInvariant()}");
        return true;
    }

    private static string? MapAzureCommunicationEmailDeliveryStatus(string? status)
    {
        return status?.Trim() switch
        {
            "Delivered" => NotificationDeliveryEventTypes.Delivered,
            "Expanded" => NotificationDeliveryEventTypes.Accepted,
            "Bounced" or "Suppressed" or "Failed" or "FilteredSpam" or "Quarantined" =>
                NotificationDeliveryEventTypes.Bounced,
            _ => null
        };
    }

    private static DateTimeOffset? GetEventGridEventTime(JsonElement eventElement)
    {
        var eventTime = GetOptionalString(eventElement, "eventTime");
        if (string.IsNullOrWhiteSpace(eventTime))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            eventTime,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static EmailDeliveryReadinessResponse CreateEmailDeliveryReadiness(IConfiguration configuration)
    {
        return EmailDeliveryReadinessEvaluator.Create(new EmailDeliveryReadinessConfiguration(
            configuration["EmailDelivery:Provider"],
            configuration["EmailDelivery:SenderDomainVerified"],
            configuration["EmailDelivery:VerifiedSenderDomain"],
            configuration["EmailDelivery:FromAddress"],
            configuration["EmailDelivery:PublicAppBaseUrl"],
            configuration["EmailDelivery:InvitationFooterText"],
            configuration["EmailDelivery:AzureCommunicationServices:ConnectionString"],
            configuration["EmailDelivery:AzureCommunicationServices:Endpoint"],
            configuration["EmailDelivery:AzureCommunicationServices:AccessKey"],
            configuration["EmailDelivery:AzureCommunicationServices:EventGridWebhookSecret"]));
    }

    private sealed record AzureEventGridValidationResponse(string ValidationResponse);

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
