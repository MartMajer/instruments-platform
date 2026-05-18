using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Platform.Application.Features.Responses;

namespace Platform.Api.RateLimiting;

public static class PublicRespondentRateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddPublicRespondentRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = PublicRespondentRateLimitingSettings.Load(configuration);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                await Results.Problem(
                        title: "public_respondent.rate_limited",
                        detail: "Too many respondent requests. Try again later.",
                        statusCode: StatusCodes.Status429TooManyRequests)
                    .ExecuteAsync(context.HttpContext);
            };

            options.AddPolicy(
                PublicRespondentRateLimitPolicies.Entry,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    PublicRespondentRateLimitPartitionKeys.ForEntry(httpContext),
                    _ => CreateFixedWindowLimiter(settings.EntryPermitLimit, settings.Window)));
            options.AddPolicy(
                PublicRespondentRateLimitPolicies.Session,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    PublicRespondentRateLimitPartitionKeys.ForSession(httpContext),
                    _ => CreateFixedWindowLimiter(settings.SessionPermitLimit, settings.Window)));
            options.AddPolicy(
                PublicRespondentRateLimitPolicies.Submit,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    PublicRespondentRateLimitPartitionKeys.ForSession(httpContext),
                    _ => CreateFixedWindowLimiter(settings.SubmitPermitLimit, settings.Window)));
            options.AddPolicy(
                RegistrationRateLimitPolicies.Intent,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    RegistrationRateLimitPartitionKeys.ForIntent(httpContext),
                    _ => CreateFixedWindowLimiter(settings.RegistrationPermitLimit, settings.Window)));
        });

        return services;
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowLimiter(
        int permitLimit,
        TimeSpan window)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }
}

internal sealed record PublicRespondentRateLimitingSettings(
    int EntryPermitLimit,
    int SessionPermitLimit,
    int SubmitPermitLimit,
    int RegistrationPermitLimit,
    TimeSpan Window)
{
    private const string SectionName = "PublicRespondentRateLimiting";

    public static PublicRespondentRateLimitingSettings Load(IConfiguration configuration)
    {
        return new PublicRespondentRateLimitingSettings(
            ReadPositiveInt(configuration, "EntryPermitLimit", 60),
            ReadPositiveInt(configuration, "SessionPermitLimit", 180),
            ReadPositiveInt(configuration, "SubmitPermitLimit", 30),
            ReadPositiveInt(configuration, "RegistrationPermitLimit", 10),
            TimeSpan.FromSeconds(ReadPositiveInt(configuration, "WindowSeconds", 60)));
    }

    private static int ReadPositiveInt(
        IConfiguration configuration,
        string name,
        int fallback)
    {
        var raw = configuration[$"{SectionName}:{name}"];
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
        {
            return parsed;
        }

        return fallback;
    }
}

internal static class PublicRespondentRateLimitPartitionKeys
{
    public static string ForEntry(HttpContext context)
    {
        return Build(context, includeSessionId: false);
    }

    public static string ForSession(HttpContext context)
    {
        return Build(context, includeSessionId: true);
    }

    private static string Build(HttpContext context, bool includeSessionId)
    {
        var credentialName = context.Request.RouteValues.ContainsKey("handle")
            ? "handle"
            : "token";
        var credentialHash = HashRouteValue(context, credentialName);
        var sessionHash = includeSessionId
            ? HashRouteValue(context, "sessionId")
            : "none";
        var remoteAddressHash = PublicEndpointRateLimitHash.HashString(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        return $"{remoteAddressHash}:{credentialName}:{credentialHash}:session:{sessionHash}";
    }

    private static string HashRouteValue(HttpContext context, string name)
    {
        if (!context.Request.RouteValues.TryGetValue(name, out var value) ||
            value is null)
        {
            return "missing";
        }

        return PublicEndpointRateLimitHash.HashString(value.ToString() ?? string.Empty);
    }
}

public static class RegistrationRateLimitPolicies
{
    public const string Intent = "registration.intent";
}

internal static class RegistrationRateLimitPartitionKeys
{
    public static string ForIntent(HttpContext context)
    {
        var remoteAddressHash = PublicEndpointRateLimitHash.HashString(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        return $"registration:intent:{remoteAddressHash}";
    }
}

internal static class PublicEndpointRateLimitHash
{
    public static string HashString(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
