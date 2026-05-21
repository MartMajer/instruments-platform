using System.Globalization;
using System.Security.Cryptography;
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

namespace Platform.Application.Features.Notifications;

public static class NotificationDeliveryEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);
    private static readonly JsonSerializerOptions ProviderWebhookJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly UTF8Encoding ProviderWebhookUtf8Encoding = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private static readonly TimeSpan ProviderWebhookReplayWindow = TimeSpan.FromMinutes(10);
    private const int ProviderWebhookMaxPayloadBytes = 64 * 1024;
    private const string ProviderWebhookSecretConfigurationKey = "EmailDelivery:ProviderWebhookSecret";
    private const string AwsSesSnsTopicArnConfigurationKey = "EmailDelivery:AwsSes:SnsTopicArn";
    private const string ProviderWebhookSignatureHeader = "X-Platform-Webhook-Signature";
    private const string ProviderWebhookTimestampHeader = "X-Platform-Webhook-Timestamp";

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

        app.MapPost("/notification-deliveries/provider-events/webhook", RecordProviderDeliveryEventWebhook)
            .AllowAnonymous()
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.ProviderWebhook)
            .WithName("RecordProviderDeliveryEventWebhook")
            .WithTags("Setup");

        app.MapPost("/notification-deliveries/provider-events/aws-ses-sns", RecordAwsSesSnsProviderDeliveryEventWebhook)
            .AllowAnonymous()
            .RequireRateLimiting(PublicRespondentRateLimitPolicies.ProviderWebhook)
            .WithName("RecordAwsSesSnsProviderDeliveryEventWebhook")
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

    private static async Task<IResult> RecordProviderDeliveryEventWebhook(
        HttpRequest httpRequest,
        IConfiguration configuration,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var webhookSecret = configuration[ProviderWebhookSecretConfigurationKey]?.Trim();
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return Results.NotFound();
        }

        if (webhookSecret.Length < 32)
        {
            return Results.Problem(
                title: "provider_webhook.misconfigured",
                detail: "Provider webhook secret is not configured safely.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (httpRequest.ContentLength is > ProviderWebhookMaxPayloadBytes)
        {
            return Results.Problem(
                title: "provider_webhook.payload_too_large",
                detail: "Provider webhook payload is too large.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (!IsJsonContentType(httpRequest.ContentType))
        {
            return Results.Problem(
                title: "provider_webhook.content_type_invalid",
                detail: "Provider webhook payload must use a JSON content type.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var bodyRead = await ReadProviderWebhookBodyAsync(httpRequest.Body, cancellationToken);
        if (bodyRead.PayloadTooLarge)
        {
            return Results.Problem(
                title: "provider_webhook.payload_too_large",
                detail: "Provider webhook payload is too large.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (bodyRead.InvalidUtf8)
        {
            return Results.Problem(
                title: "provider_webhook.payload_invalid",
                detail: "Provider webhook payload is not valid UTF-8 JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var body = bodyRead.Body ?? string.Empty;
        if (!IsValidProviderWebhookSignature(httpRequest, webhookSecret, body))
        {
            return Results.Problem(
                title: "provider_webhook.signature_invalid",
                detail: "Provider webhook signature is invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        RecordProviderDeliveryEventRequest? providerEvent;
        try
        {
            providerEvent = JsonSerializer.Deserialize<RecordProviderDeliveryEventRequest>(
                body,
                ProviderWebhookJsonOptions);
        }
        catch (JsonException)
        {
            return Results.Problem(
                title: "provider_webhook.payload_invalid",
                detail: "Provider webhook payload is not valid JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (providerEvent is null)
        {
            return Results.Problem(
                title: "provider_webhook.payload_invalid",
                detail: "Provider webhook payload is empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryParseTenantId(providerEvent.DeliveryAttemptKey, out var tenantId))
        {
            return Results.Problem(
                title: "provider_webhook.context_invalid",
                detail: "Provider webhook delivery attempt key is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await sender.Send(
            new RecordProviderDeliveryEventCommand(providerEvent, tenantId),
            cancellationToken);

        return result.IsSuccess
           ? Results.NoContent()
           : SetupHttpResults.ToOk(result);
    }

    private static async Task<IResult> RecordAwsSesSnsProviderDeliveryEventWebhook(
        HttpRequest httpRequest,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        IAwsSnsSignatureVerifier signatureVerifier,
        IAwsSnsSubscriptionConfirmer subscriptionConfirmer,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AwsSesSnsWebhook");
        logger.LogInformation(
            "[SNS-DIAG-20260521] AWS SES SNS webhook received. ContentType={ContentType}; ContentLength={ContentLength}.",
            httpRequest.ContentType,
            httpRequest.ContentLength);

        var configuredProvider = configuration["EmailDelivery:Provider"]?.Trim();
        var configuredManagedProviderName = configuration["EmailDelivery:ManagedProviderName"]?.Trim();
        if (!string.Equals(
            configuredProvider,
            EmailDeliveryProviderNames.Smtp,
            StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                configuredManagedProviderName,
                "aws-ses",
                StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS webhook disabled. Provider={Provider}; ManagedProviderName={ManagedProviderName}.",
                configuredProvider,
                configuredManagedProviderName);
            return Results.NotFound();
        }

        var expectedTopicArn = configuration[AwsSesSnsTopicArnConfigurationKey]?.Trim();
        if (!IsSafeAwsSesSnsTopicArn(expectedTopicArn))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS topic configuration invalid. HasTopicArn={HasTopicArn}.",
                !string.IsNullOrWhiteSpace(expectedTopicArn));
            return Results.Problem(
                title: "aws_ses_webhook.misconfigured",
                detail: "AWS SES SNS topic is not configured safely.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (httpRequest.ContentLength is > ProviderWebhookMaxPayloadBytes)
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS payload rejected before read. ContentLength={ContentLength}; MaxBytes={MaxBytes}.",
                httpRequest.ContentLength,
                ProviderWebhookMaxPayloadBytes);
            return Results.Problem(
                title: "aws_ses_webhook.payload_too_large",
                detail: "AWS SES SNS payload is too large.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (!IsAwsSnsContentType(httpRequest.ContentType))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS content type rejected. ContentType={ContentType}.",
                httpRequest.ContentType);
            return Results.Problem(
                title: "aws_ses_webhook.content_type_invalid",
                detail: "AWS SES SNS payload must use a JSON or SNS text content type.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var bodyRead = await ReadProviderWebhookBodyAsync(httpRequest.Body, cancellationToken);
        if (bodyRead.PayloadTooLarge)
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS payload rejected while reading. MaxBytes={MaxBytes}.",
                ProviderWebhookMaxPayloadBytes);
            return Results.Problem(
                title: "aws_ses_webhook.payload_too_large",
                detail: "AWS SES SNS payload is too large.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (bodyRead.InvalidUtf8)
        {
            logger.LogWarning("[SNS-DIAG-20260521] AWS SES SNS payload rejected because UTF-8 decoding failed.");
            return Results.Problem(
                title: "aws_ses_webhook.payload_invalid",
                detail: "AWS SES SNS payload is not valid UTF-8 JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        AwsSesSnsEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<AwsSesSnsEnvelope>(
                bodyRead.Body ?? string.Empty,
                ProviderWebhookJsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "[SNS-DIAG-20260521] AWS SES SNS payload rejected because JSON parsing failed.");
            return Results.Problem(
                title: "aws_ses_webhook.payload_invalid",
                detail: "AWS SES SNS payload is not valid JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (envelope is null ||
            string.IsNullOrWhiteSpace(envelope.Type) ||
            string.IsNullOrWhiteSpace(envelope.MessageId) ||
            string.IsNullOrWhiteSpace(envelope.TopicArn) ||
            string.IsNullOrWhiteSpace(envelope.Message) ||
            string.IsNullOrWhiteSpace(envelope.SignatureVersion) ||
            string.IsNullOrWhiteSpace(envelope.Signature) ||
            string.IsNullOrWhiteSpace(envelope.SigningCertURL))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS envelope missing fields. HasType={HasType}; HasMessageId={HasMessageId}; HasTopicArn={HasTopicArn}; HasMessage={HasMessage}; HasSignatureVersion={HasSignatureVersion}; HasSignature={HasSignature}; HasSigningCertUrl={HasSigningCertUrl}.",
                !string.IsNullOrWhiteSpace(envelope?.Type),
                !string.IsNullOrWhiteSpace(envelope?.MessageId),
                !string.IsNullOrWhiteSpace(envelope?.TopicArn),
                !string.IsNullOrWhiteSpace(envelope?.Message),
                !string.IsNullOrWhiteSpace(envelope?.SignatureVersion),
                !string.IsNullOrWhiteSpace(envelope?.Signature),
                !string.IsNullOrWhiteSpace(envelope?.SigningCertURL));
            return Results.Problem(
                title: "aws_ses_webhook.payload_invalid",
                detail: "AWS SES SNS payload is missing required envelope fields.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        logger.LogInformation(
            "[SNS-DIAG-20260521] AWS SES SNS envelope parsed. Type={MessageType}; SignatureVersion={SignatureVersion}; TopicMatches={TopicMatches}; HasTimestamp={HasTimestamp}; HasSubscribeUrl={HasSubscribeUrl}; HasToken={HasToken}; SigningCertHost={SigningCertHost}; SubscribeHost={SubscribeHost}.",
            envelope.Type,
            envelope.SignatureVersion,
            string.Equals(envelope.TopicArn, expectedTopicArn, StringComparison.Ordinal),
            !string.IsNullOrWhiteSpace(envelope.Timestamp),
            !string.IsNullOrWhiteSpace(envelope.SubscribeURL),
            !string.IsNullOrWhiteSpace(envelope.Token),
            TryGetSafeHost(envelope.SigningCertURL),
            TryGetSafeHost(envelope.SubscribeURL));

        if (!string.Equals(envelope.Type, "Notification", StringComparison.Ordinal) &&
            !string.Equals(envelope.Type, "SubscriptionConfirmation", StringComparison.Ordinal))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS message type unsupported. Type={MessageType}.",
                envelope.Type);
            return Results.Problem(
                title: "aws_ses_webhook.message_type_unsupported",
                detail: "AWS SES SNS webhook accepts notification and subscription confirmation messages only.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(envelope.SignatureVersion, "2", StringComparison.Ordinal))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS signature version unsupported. SignatureVersion={SignatureVersion}.",
                envelope.SignatureVersion);
            return Results.Problem(
                title: "aws_ses_webhook.signature_version_unsupported",
                detail: "AWS SES SNS webhook requires SignatureVersion 2.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!IsSafeAwsSnsSigningCertUrl(envelope.SigningCertURL))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS signing certificate URL rejected. SigningCertHost={SigningCertHost}.",
                TryGetSafeHost(envelope.SigningCertURL));
            return Results.Problem(
                title: "aws_ses_webhook.signing_cert_url_invalid",
                detail: "AWS SES SNS signing certificate URL is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(envelope.Timestamp))
        {
            logger.LogWarning("[SNS-DIAG-20260521] AWS SES SNS envelope missing timestamp.");
            return Results.Problem(
                title: "aws_ses_webhook.payload_invalid",
                detail: "AWS SES SNS payload is missing required envelope timestamp.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.Equals(envelope.Type, "SubscriptionConfirmation", StringComparison.Ordinal) &&
            (string.IsNullOrWhiteSpace(envelope.SubscribeURL) ||
                string.IsNullOrWhiteSpace(envelope.Token) ||
                !IsSafeAwsSnsSubscribeUrl(envelope.SubscribeURL, envelope.TopicArn, envelope.Token)))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS subscription confirmation payload rejected. HasSubscribeUrl={HasSubscribeUrl}; HasToken={HasToken}; SubscribeHost={SubscribeHost}.",
                !string.IsNullOrWhiteSpace(envelope.SubscribeURL),
                !string.IsNullOrWhiteSpace(envelope.Token),
                TryGetSafeHost(envelope.SubscribeURL));
            return Results.Problem(
                title: "aws_ses_webhook.subscription_confirmation_invalid",
                detail: "AWS SES SNS subscription confirmation payload is invalid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(envelope.TopicArn, expectedTopicArn, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS topic mismatch. ReceivedTopicArnIsSafe={ReceivedTopicArnIsSafe}; ExpectedTopicArnConfigured={ExpectedTopicArnConfigured}.",
                IsSafeAwsSesSnsTopicArn(envelope.TopicArn),
                !string.IsNullOrWhiteSpace(expectedTopicArn));
            return Results.Problem(
                title: "aws_ses_webhook.topic_mismatch",
                detail: "AWS SES SNS topic is not expected.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var signatureValid = await signatureVerifier.VerifyAsync(
            new AwsSnsSignatureVerificationRequest(
                envelope.Type,
                envelope.MessageId,
                envelope.TopicArn,
                envelope.Message,
                envelope.Subject,
                envelope.Timestamp,
                envelope.SignatureVersion,
                envelope.Signature,
                envelope.SigningCertURL,
                envelope.SubscribeURL,
                envelope.Token),
            cancellationToken);
        if (!signatureValid)
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS signature verification failed. Type={MessageType}; SignatureVersion={SignatureVersion}; SigningCertHost={SigningCertHost}.",
                envelope.Type,
                envelope.SignatureVersion,
                TryGetSafeHost(envelope.SigningCertURL));
            return Results.Problem(
                title: "aws_ses_webhook.signature_invalid",
                detail: "AWS SES SNS signature is invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (string.Equals(envelope.Type, "SubscriptionConfirmation", StringComparison.Ordinal))
        {
            logger.LogInformation(
                "[SNS-DIAG-20260521] AWS SES SNS subscription confirmation accepted; confirming with AWS. SubscribeHost={SubscribeHost}.",
                TryGetSafeHost(envelope.SubscribeURL));

            var confirmed = await subscriptionConfirmer.ConfirmAsync(
                envelope.SubscribeURL!,
                cancellationToken);
            if (!confirmed)
            {
                logger.LogWarning(
                    "[SNS-DIAG-20260521] AWS SES SNS subscription confirmation callback failed. SubscribeHost={SubscribeHost}.",
                    TryGetSafeHost(envelope.SubscribeURL));
                return Results.Problem(
                    title: "aws_ses_webhook.subscription_confirmation_failed",
                    detail: "AWS SES SNS subscription confirmation failed.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            logger.LogInformation("[SNS-DIAG-20260521] AWS SES SNS subscription confirmation completed.");
            return Results.NoContent();
        }

        if (!TryCreateAwsSesProviderEventRequest(
            envelope,
            out var providerEvent,
            out var tenantId,
            out var messageFailureTitle,
            out var messageFailureDetail))
        {
            logger.LogWarning(
                "[SNS-DIAG-20260521] AWS SES SNS notification message rejected. FailureTitle={FailureTitle}.",
                messageFailureTitle);
            return Results.Problem(
                title: messageFailureTitle,
                detail: messageFailureDetail,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await sender.Send(
            new RecordProviderDeliveryEventCommand(providerEvent!, tenantId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        logger.LogWarning("[SNS-DIAG-20260521] AWS SES SNS notification provider event recording failed.");
        return SetupHttpResults.ToOk(result);
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

    private static bool IsAwsSnsContentType(string? contentType)
    {
        if (IsJsonContentType(contentType))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var separatorIndex = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = separatorIndex >= 0 ? contentType[..separatorIndex] : contentType;
        return string.Equals(mediaType.Trim(), "text/plain", StringComparison.OrdinalIgnoreCase);
    }

    private static string TryGetSafeHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return "invalid";
        }

        return uri.Host;
    }

    private static bool IsSafeAwsSesSnsTopicArn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var arn = value.Trim();
        if (arn.Length != value.Length ||
            arn.Length > 256)
        {
            return false;
        }

        foreach (var character in arn)
        {
            if (char.IsControl(character) ||
                char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        var parts = arn.Split(':', 6);
        if (parts.Length != 6 ||
            parts[0] != "arn" ||
            parts[2] != "sns" ||
            string.IsNullOrWhiteSpace(parts[3]) ||
            parts[4].Length != 12 ||
            string.IsNullOrWhiteSpace(parts[5]))
        {
            return false;
        }

        foreach (var character in parts[4])
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        if (parts[1] is not ("aws" or "aws-us-gov" or "aws-cn"))
        {
            return false;
        }

        return IsSafeAwsRegion(parts[3]) &&
            IsSafeAwsSnsTopicName(parts[5]);
    }

    private static bool IsSafeAwsRegion(string region)
    {
        if (region.Length is < 5 or > 32)
        {
            return false;
        }

        foreach (var character in region)
        {
            if (character is not (>= 'a' and <= 'z') &&
                character is not (>= '0' and <= '9') &&
                character != '-')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSafeAwsSnsTopicName(string topicName)
    {
        if (topicName.Length is < 1 or > 256)
        {
            return false;
        }

        foreach (var character in topicName)
        {
            if (character is not (>= 'a' and <= 'z') &&
                character is not (>= 'A' and <= 'Z') &&
                character is not (>= '0' and <= '9') &&
                character is not ('-' or '_' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSafeAwsSnsSigningCertUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var url = value.Trim();
        if (url.Length != value.Length ||
            url.Length > 512 ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            (!uri.IsDefaultPort && uri.Port != 443) ||
            !string.IsNullOrWhiteSpace(uri.UserInfo) ||
            !string.IsNullOrWhiteSpace(uri.Query) ||
            !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            return false;
        }

        if (!TryGetAwsSnsSigningRegion(uri.Host, out var region) ||
            !IsSafeAwsRegion(region))
        {
            return false;
        }

        var path = uri.AbsolutePath;
        return path.Length > "/SimpleNotificationService-.pem".Length &&
            path.Length <= 256 &&
            path.StartsWith("/SimpleNotificationService-", StringComparison.Ordinal) &&
            path.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("//", StringComparison.Ordinal);
    }

    private static bool IsSafeAwsSnsSubscribeUrl(
        string? value,
        string expectedTopicArn,
        string expectedToken)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.IsNullOrWhiteSpace(expectedTopicArn) ||
            string.IsNullOrWhiteSpace(expectedToken))
        {
            return false;
        }

        var url = value.Trim();
        if (url.Length != value.Length ||
            url.Length > 2048 ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            (!uri.IsDefaultPort && uri.Port != 443) ||
            !string.IsNullOrWhiteSpace(uri.UserInfo) ||
            !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            return false;
        }

        return TryGetAwsSnsSigningRegion(uri.Host, out var region) &&
            IsSafeAwsRegion(region) &&
            uri.AbsolutePath is "" or "/" &&
            HasQueryParameter(uri, "Action", "ConfirmSubscription") &&
            HasQueryParameter(uri, "TopicArn", expectedTopicArn) &&
            HasQueryParameter(uri, "Token", expectedToken);
    }

    private static bool HasQueryParameter(Uri uri, string name, string expectedValue)
    {
        var query = uri.Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        if (query[0] == '?')
        {
            query = query[1..];
        }

        foreach (var segment in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = segment.Split('=', 2);
            var key = DecodeQueryValue(pair[0]);
            if (!string.Equals(key, name, StringComparison.Ordinal))
            {
                continue;
            }

            var value = pair.Length == 2 ? DecodeQueryValue(pair[1]) : string.Empty;
            return string.Equals(value, expectedValue, StringComparison.Ordinal);
        }

        return false;
    }

    private static string DecodeQueryValue(string value)
    {
        return Uri.UnescapeDataString(value.Replace("+", " ", StringComparison.Ordinal));
    }

    private static bool TryGetAwsSnsSigningRegion(string host, out string region)
    {
        region = string.Empty;
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var normalizedHost = host.ToLowerInvariant();
        if (!normalizedHost.StartsWith("sns.", StringComparison.Ordinal))
        {
            return false;
        }

        var regionStart = "sns.".Length;
        foreach (var suffix in new[] { ".amazonaws.com.cn", ".amazonaws.com", ".api.aws" })
        {
            if (!normalizedHost.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var regionLength = normalizedHost.Length - regionStart - suffix.Length;
            if (regionLength <= 0)
            {
                return false;
            }

            region = normalizedHost.Substring(regionStart, regionLength);
            return true;
        }

        return false;
    }

    private static bool TryCreateAwsSesProviderEventRequest(
        AwsSesSnsEnvelope envelope,
        out RecordProviderDeliveryEventRequest? providerEvent,
        out Guid tenantId,
        out string failureTitle,
        out string failureDetail)
    {
        providerEvent = null;
        tenantId = Guid.Empty;
        failureTitle = "aws_ses_webhook.message_invalid";
        failureDetail = "AWS SES SNS message could not be mapped to a provider delivery event.";

        JsonDocument messageDocument;
        try
        {
            messageDocument = JsonDocument.Parse(envelope.Message!);
        }
        catch (JsonException)
        {
            failureDetail = "AWS SES SNS Message is not valid JSON.";
            return false;
        }

        using (messageDocument)
        {
            var root = messageDocument.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                failureDetail = "AWS SES SNS Message must be a JSON object.";
                return false;
            }

            var sesEventType = GetOptionalString(root, "notificationType") ??
                GetOptionalString(root, "eventType");
            var mappedEventType = MapAwsSesEventType(sesEventType);
            if (mappedEventType is null)
            {
                failureTitle = "aws_ses_webhook.event_type_unsupported";
                failureDetail = "AWS SES SNS Message event type is not supported.";
                return false;
            }

            if (!root.TryGetProperty("mail", out var mail) ||
                mail.ValueKind != JsonValueKind.Object)
            {
                failureDetail = "AWS SES SNS Message is missing mail metadata.";
                return false;
            }

            var deliveryAttemptKey = GetSingleAwsSesMailHeader(
                mail,
                "X-Platform-Delivery-Key",
                out var duplicateDeliveryKeyHeader);
            if (duplicateDeliveryKeyHeader)
            {
                failureDetail = "AWS SES SNS Message contains multiple platform delivery key headers.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(deliveryAttemptKey) ||
                !TryParseTenantId(deliveryAttemptKey, out tenantId))
            {
                failureDetail = "AWS SES SNS Message is missing a valid platform delivery key header.";
                return false;
            }

            var providerMessageId = GetOptionalString(mail, "messageId");
            if (string.IsNullOrWhiteSpace(providerMessageId) ||
                providerMessageId.Length > 256)
            {
                failureDetail = "AWS SES SNS Message is missing a valid SES message id.";
                return false;
            }

            if (!TryGetAwsSesEventTimestamp(
                root,
                mail,
                sesEventType!,
                out var occurredAt,
                out failureDetail))
            {
                return false;
            }

            providerEvent = new RecordProviderDeliveryEventRequest(
                deliveryAttemptKey,
                mappedEventType,
                occurredAt,
                envelope.MessageId,
                providerMessageId,
                $"aws_ses:{sesEventType!.ToLowerInvariant()}");
            return true;
        }
    }

    private static string? MapAwsSesEventType(string? sesEventType)
    {
        return sesEventType switch
        {
            "Send" => "accepted",
            "Delivery" => "delivered",
            "Bounce" => "bounced",
            "Complaint" => "complained",
            _ => null
        };
    }

    private static bool TryGetAwsSesEventTimestamp(
        JsonElement root,
        JsonElement mail,
        string sesEventType,
        out DateTimeOffset? occurredAt,
        out string failureDetail)
    {
        occurredAt = null;
        failureDetail = string.Empty;

        var eventObjectName = sesEventType switch
        {
            "Delivery" => "delivery",
            "Bounce" => "bounce",
            "Complaint" => "complaint",
            _ => null
        };

        string? timestamp;
        if (eventObjectName is null)
        {
            timestamp = GetOptionalString(mail, "timestamp");
        }
        else
        {
            if (!root.TryGetProperty(eventObjectName, out var eventObject) ||
                eventObject.ValueKind != JsonValueKind.Object)
            {
                failureDetail = "AWS SES SNS Message is missing event details.";
                return false;
            }

            timestamp = GetOptionalString(eventObject, "timestamp") ??
                GetOptionalString(mail, "timestamp");
        }

        if (string.IsNullOrWhiteSpace(timestamp))
        {
            return true;
        }

        if (DateTimeOffset.TryParse(
            timestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsedTimestamp))
        {
            occurredAt = parsedTimestamp;
            return true;
        }

        failureDetail = "AWS SES SNS Message contains an invalid event timestamp.";
        return false;
    }

    private static string? GetSingleAwsSesMailHeader(
        JsonElement mail,
        string headerName,
        out bool duplicateHeader)
    {
        duplicateHeader = false;
        if (!mail.TryGetProperty("headers", out var headers) ||
            headers.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? value = null;
        foreach (var header in headers.EnumerateArray())
        {
            if (header.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = GetOptionalString(header, "name");
            if (!string.Equals(name, headerName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (value is not null)
            {
                duplicateHeader = true;
                return null;
            }

            value = GetOptionalString(header, "value");
        }

        return value;
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
            configuration["EmailDelivery:ManagedProviderName"],
            configuration["EmailDelivery:SenderDomainVerified"],
            configuration["EmailDelivery:VerifiedSenderDomain"],
            configuration["EmailDelivery:FromAddress"],
            configuration["EmailDelivery:PublicAppBaseUrl"],
            configuration["EmailDelivery:InvitationFooterText"],
            configuration["EmailDelivery:Smtp:Host"],
            configuration["EmailDelivery:Smtp:Port"],
            configuration["EmailDelivery:Smtp:EnableSsl"],
            configuration["EmailDelivery:Smtp:UserName"],
            configuration["EmailDelivery:Smtp:Password"],
            configuration[ProviderWebhookSecretConfigurationKey],
            configuration[AwsSesSnsTopicArnConfigurationKey]));
    }

    private sealed record AwsSesSnsEnvelope(
        string? Type,
        string? MessageId,
        string? TopicArn,
        string? Message,
        string? Timestamp,
        string? SignatureVersion,
        string? Signature,
        string? SigningCertURL,
        string? SubscribeURL,
        string? Token,
        string? Subject);

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

    private static bool IsValidProviderWebhookSignature(
        HttpRequest request,
        string webhookSecret,
        string body)
    {
        var timestampHeader = request.Headers[ProviderWebhookTimestampHeader].ToString();
        if (!long.TryParse(timestampHeader, out var timestampSeconds))
        {
            return false;
        }

        DateTimeOffset timestamp;
        try
        {
            timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - timestamp > ProviderWebhookReplayWindow ||
            timestamp - DateTimeOffset.UtcNow > ProviderWebhookReplayWindow)
        {
            return false;
        }

        var signature = NormalizeSignature(request.Headers[ProviderWebhookSignatureHeader].ToString());
        if (signature is null)
        {
            return false;
        }

        var signedPayload = $"{timestampSeconds}.{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var expectedSignature = Convert.ToHexString(expectedBytes).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedSignature),
            Encoding.ASCII.GetBytes(signature));
    }

    private static string? NormalizeSignature(string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return null;
        }

        var signature = signatureHeader.Trim();
        if (signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            signature = signature["sha256=".Length..];
        }

        if (signature.Length != 64 ||
            signature.Any(character => !Uri.IsHexDigit(character)))
        {
            return null;
        }

        return signature.ToLowerInvariant();
    }

    private static bool TryParseTenantId(string? deliveryAttemptKey, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(deliveryAttemptKey))
        {
            return false;
        }

        var parts = deliveryAttemptKey.Split(':', 3, StringSplitOptions.TrimEntries);
        return parts.Length == 3 &&
            string.Equals(parts[0], "campaign-email", StringComparison.Ordinal) &&
            Guid.TryParse(parts[1], out tenantId);
    }

}
