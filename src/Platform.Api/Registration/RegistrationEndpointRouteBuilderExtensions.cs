using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Platform.Api.RateLimiting;
using Platform.Api.Auth;
using Platform.Domain.Auth;
using Platform.Infrastructure.Data;
using Platform.SharedKernel;

namespace Platform.Api.Registration;

public sealed record CreateRegistrationIntentRequest(
    string Email,
    string OrganizationName,
    string AccessCode,
    string ReturnUrl = "/app");

public sealed record CreateRegistrationIntentResponse(
    string LoginUrl,
    DateTimeOffset ExpiresAt);

public interface IRegistrationIntentService
{
    Task<Result<CreateRegistrationIntentResponse>> CreateAsync(
        CreateRegistrationIntentRequest request,
        CancellationToken cancellationToken);
}

public sealed class RegistrationIntentService(
    IConfiguration configuration,
    ApplicationDbContext db,
    IBetaAccessCodeVerifier accessCodeVerifier,
    IRegistrationTokenProtector tokenProtector,
    TimeProvider timeProvider) : IRegistrationIntentService
{
    public async Task<Result<CreateRegistrationIntentResponse>> CreateAsync(
        CreateRegistrationIntentRequest request,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Registration:Enabled", false))
        {
            return Result.Failure<CreateRegistrationIntentResponse>(
                Error.Forbidden("registration.disabled", "Private beta registration is not enabled."));
        }

        var email = NormalizeEmail(request.Email);
        if (email.IsFailure)
        {
            return Result.Failure<CreateRegistrationIntentResponse>(email.Error);
        }

        var organizationName = NormalizeOrganizationName(request.OrganizationName);
        if (organizationName.IsFailure)
        {
            return Result.Failure<CreateRegistrationIntentResponse>(organizationName.Error);
        }

        if (string.IsNullOrWhiteSpace(request.AccessCode) || !accessCodeVerifier.Verify(request.AccessCode))
        {
            return Result.Failure<CreateRegistrationIntentResponse>(
                Error.Validation("registration.invalid_access_code", "Private beta access code is invalid."));
        }

        var returnUrl = AuthReturnUrl.Normalize(request.ReturnUrl, "/app", configuration);
        if (returnUrl is null)
        {
            return Result.Failure<CreateRegistrationIntentResponse>(
                Error.Validation("registration.invalid_return_url", "Return URL must be a local application path."));
        }

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(Math.Max(1, configuration.GetValue("Registration:IntentMinutes", 15)));
        var token = tokenProtector.Create();
        var slug = await AllocateSlugAsync(Slugify(organizationName.Value), now, cancellationToken);

        var intent = new RegistrationIntent(
            PlatformIds.NewId(),
            token.Hash,
            email.Value,
            organizationName.Value,
            slug,
            now,
            expiresAt);
        db.RegistrationIntents.Add(intent);
        await db.SaveChangesAsync(cancellationToken);

        var loginUrl = QueryHelpers.AddQueryString(
            "/auth/login",
            new Dictionary<string, string?>
            {
                ["registrationToken"] = token.RawToken,
                ["returnUrl"] = returnUrl
            });

        return Result.Success(new CreateRegistrationIntentResponse(loginUrl, expiresAt));
    }

    private async Task<string> AllocateSlugAsync(
        string baseSlug,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var candidate = baseSlug;
        var suffix = 2;
        while (await SlugExistsAsync(candidate, now, cancellationToken))
        {
            var suffixText = $"-{suffix}";
            var rootLength = Math.Min(
                RegistrationIntent.SlugMaxLength - suffixText.Length,
                baseSlug.Length);
            candidate = baseSlug[..rootLength].Trim('-') + suffixText;
            suffix++;
        }

        return candidate;
    }

    private async Task<bool> SlugExistsAsync(
        string slug,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Tenants.AnyAsync(tenant => tenant.Slug == slug, cancellationToken))
        {
            return true;
        }

        return await db.RegistrationIntents.AnyAsync(
            intent =>
                intent.Slug == slug &&
                intent.Status == RegistrationIntentStatuses.Pending &&
                intent.ExpiresAt > now,
            cancellationToken);
    }

    private static Result<string> NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > RegistrationIntent.EmailMaxLength ||
            normalized.Contains(' ', StringComparison.Ordinal))
        {
            return Result.Failure<string>(
                Error.Validation("registration.email_invalid", "Enter a valid email address."));
        }

        try
        {
            var parsed = new MailAddress(normalized);
            if (!string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string>(
                    Error.Validation("registration.email_invalid", "Enter a valid email address."));
            }
        }
        catch (FormatException)
        {
            return Result.Failure<string>(
                Error.Validation("registration.email_invalid", "Enter a valid email address."));
        }

        return Result.Success(normalized);
    }

    private static Result<string> NormalizeOrganizationName(string organizationName)
    {
        var normalized = organizationName.Trim();
        if (normalized.Length is 0 or > RegistrationIntent.OrganizationNameMaxLength)
        {
            return Result.Failure<string>(
                Error.Validation("registration.organization_invalid", "Enter a workspace or organization name."));
        }

        return Result.Success(normalized);
    }

    private static string Slugify(string organizationName)
    {
        var builder = new StringBuilder(RegistrationIntent.SlugMaxLength);
        var previousSeparator = false;
        foreach (var rune in organizationName.ToLowerInvariant().EnumerateRunes())
        {
            if (rune.Value is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(rune.ToString());
                previousSeparator = false;
            }
            else if (!previousSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousSeparator = true;
            }

            if (builder.Length >= RegistrationIntent.SlugMaxLength)
            {
                break;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "workspace" : slug;
    }
}

public interface IBetaAccessCodeVerifier
{
    bool Verify(string accessCode);
}

public sealed class Sha256BetaAccessCodeVerifier(IConfiguration configuration) : IBetaAccessCodeVerifier
{
    public bool Verify(string accessCode)
    {
        var hashes = configuration
            .GetSection("Registration:BetaAccessCodeSha256Hashes")
            .Get<string[]>() ?? [];
        if (hashes.Length == 0)
        {
            return false;
        }

        var submittedHash = SHA256.HashData(Encoding.UTF8.GetBytes(accessCode.Trim()));
        foreach (var configuredHash in hashes)
        {
            var expectedHash = ParseHash(configuredHash);
            if (expectedHash is not null &&
                expectedHash.Length == submittedHash.Length &&
                CryptographicOperations.FixedTimeEquals(expectedHash, submittedHash))
            {
                return true;
            }
        }

        return false;
    }

    private static byte[]? ParseHash(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 64)
        {
            try
            {
                return Convert.FromHexString(trimmed);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        try
        {
            return Convert.FromBase64String(trimmed);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

public sealed record RegistrationTokenPair(string RawToken, string Hash);

public interface IRegistrationTokenProtector
{
    RegistrationTokenPair Create();

    string Hash(string rawToken);
}

public sealed class Sha256RegistrationTokenProtector : IRegistrationTokenProtector
{
    public RegistrationTokenPair Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = WebEncoders.Base64UrlEncode(bytes);

        return new RegistrationTokenPair(rawToken, Hash(rawToken));
    }

    public string Hash(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()))).ToLowerInvariant();
    }
}

public static class RegistrationServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformRegistration(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBetaAccessCodeVerifier, Sha256BetaAccessCodeVerifier>();
        services.AddSingleton<IRegistrationTokenProtector, Sha256RegistrationTokenProtector>();
        services.AddScoped<IRegistrationIntentService, RegistrationIntentService>();

        return services;
    }
}

public static class RegistrationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/registration/intents", CreateRegistrationIntent)
            .AllowAnonymous()
            .RequireRateLimiting(RegistrationRateLimitPolicies.Intent)
            .WithName("CreateRegistrationIntent")
            .WithTags("Registration");

        return app;
    }

    private static async Task<IResult> CreateRegistrationIntent(
        CreateRegistrationIntentRequest request,
        IRegistrationIntentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error);
    }

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode);
    }
}
