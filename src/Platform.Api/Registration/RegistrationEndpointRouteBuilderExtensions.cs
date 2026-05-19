using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Platform.Api.RateLimiting;
using Platform.Api.Auth;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Auth;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
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

public sealed record RegistrationSessionResponse(string Email);

public sealed record CreateRegistrationWorkspaceRequest(
    string OrganizationName,
    string AccessCode,
    string ReturnUrl = "/app");

public sealed record CreateRegistrationWorkspaceResponse(
    string AppUrl,
    Guid TenantId,
    string Email);

public sealed record RegistrationIdentity(
    string Email,
    string Provider,
    string ProviderSubjectHash);

public sealed record CreateRegistrationWorkspaceResult(
    string AppUrl,
    PlatformOidcLoginResolution Resolution);

public interface IRegistrationIntentService
{
    Task<Result<CreateRegistrationIntentResponse>> CreateAsync(
        CreateRegistrationIntentRequest request,
        CancellationToken cancellationToken);
}

public interface IRegistrationWorkspaceService
{
    Task<Result<CreateRegistrationWorkspaceResult>> CreateAsync(
        RegistrationIdentity identity,
        CreateRegistrationWorkspaceRequest request,
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

        var existingTenantId = await (
                from user in db.UserAccounts
                join tenant in db.Tenants on user.TenantId equals tenant.Id
                where user.Email == email.Value &&
                    user.DeletedAt == null &&
                    tenant.DeletedAt == null &&
                    tenant.Status == "active"
                orderby user.TenantId
                select (Guid?)user.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingTenantId.HasValue)
        {
            return Result.Failure<CreateRegistrationIntentResponse>(
                Error.Conflict(
                    "registration.email_exists",
                    "A workspace already exists for this email. Sign in instead.",
                    new Dictionary<string, object?>
                    {
                        ["loginUrl"] = BuildExistingWorkspaceLoginUrl(
                            existingTenantId.Value,
                            returnUrl,
                            email.Value)
                    }));
        }

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(Math.Max(1, configuration.GetValue("Registration:IntentMinutes", 15)));
        var token = tokenProtector.Create();
        var slug = AllocateSlug(Slugify(organizationName.Value), token.Hash);

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
                    ["returnUrl"] = returnUrl,
                    ["prompt"] = "login",
                    ["screen_hint"] = "signup",
                    ["login_hint"] = email.Value
                });

        return Result.Success(new CreateRegistrationIntentResponse(loginUrl, expiresAt));
    }

    private static string BuildExistingWorkspaceLoginUrl(Guid tenantId, string returnUrl, string email)
    {
        return QueryHelpers.AddQueryString(
            "/auth/login",
            new Dictionary<string, string?>
            {
                ["tenantId"] = tenantId.ToString(),
                ["returnUrl"] = returnUrl,
                ["prompt"] = "login",
                ["login_hint"] = email
            });
    }

    internal static string AllocateSlug(string baseSlug, string tokenHash)
    {
        const int suffixLength = 8;

        var suffix = tokenHash[..suffixLength].ToLowerInvariant();
        var rootLength = Math.Min(
            RegistrationIntent.SlugMaxLength - suffixLength - 1,
            baseSlug.Length);
        var root = baseSlug[..rootLength].Trim('-');
        return $"{(string.IsNullOrWhiteSpace(root) ? "workspace" : root)}-{suffix}";
    }

    internal static Result<string> NormalizeEmail(string email)
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

    internal static Result<string> NormalizeOrganizationName(string organizationName)
    {
        var normalized = organizationName.Trim();
        if (normalized.Length is 0 or > RegistrationIntent.OrganizationNameMaxLength)
        {
            return Result.Failure<string>(
                Error.Validation("registration.organization_invalid", "Enter a workspace or organization name."));
        }

        return Result.Success(normalized);
    }

    internal static string Slugify(string organizationName)
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

public sealed class RegistrationWorkspaceService(
    IConfiguration configuration,
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    ICurrentTenant currentTenant,
    IBetaAccessCodeVerifier accessCodeVerifier) : IRegistrationWorkspaceService
{
    private const string OwnerRoleCode = "tenant_owner";
    private const string ResearcherRoleCode = "researcher";
    private const string AnalystRoleCode = "analyst";
    private const string ViewerRoleCode = "viewer";

    public async Task<Result<CreateRegistrationWorkspaceResult>> CreateAsync(
        RegistrationIdentity identity,
        CreateRegistrationWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Registration:Enabled", false))
        {
            return Result.Failure<CreateRegistrationWorkspaceResult>(
                Error.Forbidden("registration.disabled", "Private beta registration is not enabled."));
        }

        var organizationName = RegistrationIntentService.NormalizeOrganizationName(request.OrganizationName);
        if (organizationName.IsFailure)
        {
            return Result.Failure<CreateRegistrationWorkspaceResult>(organizationName.Error);
        }

        if (string.IsNullOrWhiteSpace(request.AccessCode) || !accessCodeVerifier.Verify(request.AccessCode))
        {
            return Result.Failure<CreateRegistrationWorkspaceResult>(
                Error.Validation("registration.invalid_access_code", "Private beta access code is invalid."));
        }

        var returnUrl = AuthReturnUrl.Normalize(request.ReturnUrl, "/app", configuration);
        if (returnUrl is null)
        {
            return Result.Failure<CreateRegistrationWorkspaceResult>(
                Error.Validation("registration.invalid_return_url", "Return URL must be a local application path."));
        }

        var now = DateTimeOffset.UtcNow;
        var sessionMinutes = Math.Max(1, configuration.GetValue("Authentication:Oidc:SessionMinutes", 480));
        var tenantId = PlatformIds.NewId();
        var userId = PlatformIds.NewId();
        var bindingId = PlatformIds.NewId();
        var sessionId = PlatformIds.NewId();
        var slug = RegistrationIntentService.AllocateSlug(
            RegistrationIntentService.Slugify(organizationName.Value),
            tenantId.ToString("N"));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var setupPermission = await EnsurePermissionAsync(PlatformPermissions.SetupManage, cancellationToken);
        var teamPermission = await EnsurePermissionAsync(PlatformPermissions.TeamManage, cancellationToken);
        var exportPermission = await EnsurePermissionAsync(PlatformPermissions.ExportRead, cancellationToken);

        currentTenant.SetTenant(tenantId, "registration");
        await tenantDbScope.SetTenantAsync(tenantId, cancellationToken: cancellationToken);

        db.Tenants.Add(new Tenant(tenantId, slug, organizationName.Value));
        await db.SaveChangesAsync(cancellationToken);

        var ownerRole = new Role(PlatformIds.NewId(), tenantId, OwnerRoleCode, "Tenant owner");
        var researcherRole = new Role(PlatformIds.NewId(), tenantId, ResearcherRoleCode, "Researcher");
        var analystRole = new Role(PlatformIds.NewId(), tenantId, AnalystRoleCode, "Analyst");
        var viewerRole = new Role(PlatformIds.NewId(), tenantId, ViewerRoleCode, "Viewer");
        db.Roles.AddRange(ownerRole, researcherRole, analystRole, viewerRole);
        db.RolePermissions.AddRange(
            new RolePermission(ownerRole.Id, setupPermission.Id),
            new RolePermission(ownerRole.Id, teamPermission.Id),
            new RolePermission(ownerRole.Id, exportPermission.Id),
            new RolePermission(researcherRole.Id, setupPermission.Id),
            new RolePermission(analystRole.Id, exportPermission.Id));

        db.UserAccounts.Add(new UserAccount(userId, tenantId, identity.Email));
        db.RoleAssignments.Add(new RoleAssignment(
            PlatformIds.NewId(),
            tenantId,
            userId,
            ownerRole.Id,
            RoleAssignmentScopes.Tenant));

        db.ExternalAuthIdentities.Add(new ExternalAuthIdentity(
            bindingId,
            tenantId,
            userId,
            identity.Provider,
            identity.ProviderSubjectHash,
            identity.Email,
            now));

        db.AuthSessions.Add(new AuthSession(
            sessionId,
            tenantId,
            userId,
            bindingId,
            now,
            now.AddMinutes(sessionMinutes)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new CreateRegistrationWorkspaceResult(
            returnUrl,
            new PlatformOidcLoginResolution(
                userId,
                tenantId,
                sessionId,
                [PlatformPermissions.ExportRead, PlatformPermissions.SetupManage, PlatformPermissions.TeamManage],
                identity.Email)));
    }

    private async Task<Permission> EnsurePermissionAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.SingleOrDefaultAsync(
            candidate => candidate.Code == code,
            cancellationToken);
        if (permission is not null)
        {
            return permission;
        }

        permission = new Permission(PlatformIds.NewId(), code);
        db.Permissions.Add(permission);
        await db.SaveChangesAsync(cancellationToken);

        return permission;
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
        services.AddScoped<IRegistrationWorkspaceService, RegistrationWorkspaceService>();

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

        app.MapGet("/registration/session", GetRegistrationSession)
            .AllowAnonymous()
            .WithName("GetRegistrationSession")
            .WithTags("Registration");

        app.MapPost("/registration/workspaces", CreateRegistrationWorkspace)
            .RequireAuthorization()
            .RequireRateLimiting(RegistrationRateLimitPolicies.Intent)
            .WithName("CreateRegistrationWorkspace")
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

    private static IResult GetRegistrationSession(HttpContext context)
    {
        var identity = GetRegistrationIdentity(context.User);

        return identity is null
            ? Results.Unauthorized()
            : Results.Ok(new RegistrationSessionResponse(identity.Email));
    }

    private static async Task<IResult> CreateRegistrationWorkspace(
        HttpContext context,
        CreateRegistrationWorkspaceRequest request,
        IRegistrationWorkspaceService service,
        CancellationToken cancellationToken)
    {
        var identity = GetRegistrationIdentity(context.User);
        if (identity is null)
        {
            return Results.Unauthorized();
        }

        var result = await service.CreateAsync(identity, request, cancellationToken);
        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        await context.SignInAsync(
            PlatformAuthenticationSchemes.AppCookie,
            CreatePlatformPrincipal(result.Value.Resolution));

        return Results.Ok(new CreateRegistrationWorkspaceResponse(
            result.Value.AppUrl,
            result.Value.Resolution.TenantId,
            result.Value.Resolution.Email));
    }

    private static RegistrationIdentity? GetRegistrationIdentity(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            !string.Equals(
                principal.FindFirst(PlatformRegistrationClaimTypes.Pending)?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var email = principal.FindFirst(PlatformRegistrationClaimTypes.Email)?.Value?.Trim();
        var provider = principal.FindFirst(PlatformRegistrationClaimTypes.Provider)?.Value?.Trim();
        var providerSubjectHash = principal
            .FindFirst(PlatformRegistrationClaimTypes.ProviderSubjectHash)?
            .Value?
            .Trim();

        return string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(provider) ||
            string.IsNullOrWhiteSpace(providerSubjectHash)
                ? null
                : new RegistrationIdentity(email, provider, providerSubjectHash);
    }

    private static ClaimsPrincipal CreatePlatformPrincipal(PlatformOidcLoginResolution resolution)
    {
        var claims = new List<Claim>
        {
            new(PlatformClaimTypes.UserId, resolution.UserId.ToString()),
            new(PlatformClaimTypes.SessionId, resolution.SessionId.ToString()),
            new(ClaimTypes.NameIdentifier, resolution.UserId.ToString()),
            new(ClaimTypes.Email, resolution.Email),
            new(PlatformClaimTypes.TenantMembership, resolution.TenantId.ToString())
        };

        claims.AddRange(resolution.Permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal)
            .Select(permission => new Claim(PlatformClaimTypes.Permission, permission)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, PlatformAuthenticationSchemes.AppCookie));
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
            statusCode: statusCode,
            extensions: error.Extensions.Count > 0
                ? new Dictionary<string, object?>(error.Extensions, StringComparer.Ordinal)
                : null);
    }
}
