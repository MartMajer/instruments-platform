using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Auth;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Tenancy;
using Platform.SharedKernel;

namespace Platform.Api.Auth;

public sealed record PlatformOidcLoginResolution(
    Guid UserId,
    Guid TenantId,
    Guid SessionId,
    IReadOnlyCollection<string> Permissions,
    string Email = "",
    bool EmailVerified = true);

public interface IPlatformOidcLoginResolver
{
    Task<PlatformOidcLoginResolution?> ResolveAsync(
        Guid tenantId,
        string email,
        bool emailVerified,
        string provider,
        string providerSubject,
        CancellationToken cancellationToken);
}

public interface IPlatformRegistrationLoginResolver
{
    Task<PlatformOidcLoginResolution?> ResolveAsync(
        string registrationToken,
        string email,
        bool emailVerified,
        string provider,
        string providerSubject,
        CancellationToken cancellationToken);
}

public static class PlatformRegistrationClaimTypes
{
    public const string Pending = "registration_pending";
    public const string Email = "registration_email";
    public const string Provider = "registration_provider";
    public const string ProviderSubjectHash = "registration_provider_subject_hash";
}

public sealed class PlatformOidcEvents(
    IPlatformOidcLoginResolver loginResolver,
    IPlatformRegistrationLoginResolver registrationLoginResolver,
    IProviderSubjectHasher providerSubjectHasher,
    IConfiguration configuration,
    ILogger<PlatformOidcEvents> logger) : OpenIdConnectEvents
{
    private const string Provider = "auth0";

    public const string AuthFailureReasonPropertyName = "platform_auth_failure_reason";

    public const string EmailUnverifiedFailureReason = "email_unverified";

    public const string EmailMismatchFailureReason = "email_mismatch";

    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        if (context.Properties.Parameters.TryGetValue("screen_hint", out var screenHint) &&
            screenHint is string screenHintValue &&
            !string.IsNullOrWhiteSpace(screenHintValue))
        {
            context.ProtocolMessage.SetParameter("screen_hint", screenHintValue);
        }
        if (context.Properties.Parameters.TryGetValue("login_hint", out var loginHint) &&
            loginHint is string loginHintValue &&
            !string.IsNullOrWhiteSpace(loginHintValue))
        {
            context.ProtocolMessage.SetParameter("login_hint", loginHintValue);
        }

        return Task.CompletedTask;
    }

    public override Task RemoteFailure(RemoteFailureContext context)
    {
        logger.LogWarning(
            "OIDC remote authentication failed with {FailureType}.",
            context.Failure?.GetType().Name ?? "unknown");

        context.HandleResponse();

        var authFailureReason = GetAuthFailureReason(context.Properties, context.Failure);
        var fallbackPath = string.Equals(authFailureReason, EmailUnverifiedFailureReason, StringComparison.Ordinal) &&
            HasRegistrationContext(context.Properties)
                ? "/register"
                : "/app";
        var requestedReturnUrl = string.Equals(fallbackPath, "/register", StringComparison.Ordinal)
            ? GetFallbackWebReturnUrl(configuration, fallbackPath)
            : context.Properties?.RedirectUri;
        var fallbackReturnUrl = GetFallbackWebReturnUrl(configuration, fallbackPath);
        var returnUrl = AuthReturnUrl.Normalize(
                requestedReturnUrl,
                fallbackReturnUrl,
                configuration) ??
            fallbackReturnUrl;

        context.Response.Redirect(AuthReturnUrl.AppendQuery(returnUrl, "auth", authFailureReason));

        return Task.CompletedTask;
    }

    private static string GetFallbackWebReturnUrl(IConfiguration configuration, string path = "/app")
    {
        var origin = PlatformAuthServiceCollectionExtensions
            .GetBrowserCorsOrigins(configuration, includeDevelopmentFallback: false)
            .FirstOrDefault();
        var normalizedPath = string.IsNullOrWhiteSpace(path) || !path.StartsWith("/", StringComparison.Ordinal)
            ? "/app"
            : path;

        return string.IsNullOrWhiteSpace(origin)
            ? normalizedPath
            : $"{origin.TrimEnd('/')}{normalizedPath}";
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var hasTenantLogin = TryGetLoginTenantId(context, out var tenantId);
        var registrationToken = GetRegistrationToken(context);
        var hasRegistrationLogin = !string.IsNullOrWhiteSpace(registrationToken);
        var hasRegistrationBootstrap = IsRegistrationBootstrap(context);
        var loginContextCount = Convert.ToInt32(hasTenantLogin) +
            Convert.ToInt32(hasRegistrationLogin) +
            Convert.ToInt32(hasRegistrationBootstrap);
        if (loginContextCount != 1)
        {
            logger.LogWarning("OIDC login rejected because login context was missing or ambiguous.");
            context.Fail("platform_login_context_required");
            return;
        }

        var email = context.Principal?.FindFirst("email")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("OIDC login rejected because the provider did not return an email claim.");
            context.Fail("platform_login_email_required");
            return;
        }

        var providerSubject = context.Principal?.FindFirst("sub")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(providerSubject))
        {
            logger.LogWarning("OIDC login rejected because the provider did not return a subject claim.");
            context.Fail("platform_login_provider_subject_required");
            return;
        }

        var emailVerified = IsEmailVerified(context.Principal);
        var normalizedEmail = email.ToLowerInvariant();
        var expectedEmail = GetExpectedLoginEmail(context);
        if (!string.IsNullOrWhiteSpace(expectedEmail) &&
            !string.Equals(normalizedEmail, expectedEmail, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("OIDC login rejected because the provider email did not match the requested login email.");
            MarkAuthFailure(context, EmailMismatchFailureReason);
            context.Fail("platform_login_expected_email_mismatch");
            return;
        }

        if (RequiresVerifiedEmail() && !emailVerified && !hasRegistrationLogin)
        {
            logger.LogWarning("OIDC login rejected because the email claim was not verified.");
            MarkAuthFailure(context, EmailUnverifiedFailureReason);
            context.Fail("platform_login_verified_email_required");
            return;
        }

        if (hasRegistrationBootstrap)
        {
            ProjectRegistrationBootstrapClaims(
                context,
                normalizedEmail,
                Provider,
                providerSubjectHasher.Hash(Provider, providerSubject));
            return;
        }

        var resolution = hasRegistrationLogin
            ? await registrationLoginResolver.ResolveAsync(
                registrationToken!,
                normalizedEmail,
                emailVerified,
                Provider,
                providerSubject,
                context.HttpContext.RequestAborted)
            : await loginResolver.ResolveAsync(
                tenantId,
                normalizedEmail,
                emailVerified,
                Provider,
                providerSubject,
                context.HttpContext.RequestAborted);

        if (resolution is null)
        {
            logger.LogWarning("OIDC login rejected because no platform tenant membership was resolved.");
            context.Fail("platform_login_not_resolved");
            return;
        }

        ProjectPlatformClaims(context, resolution, normalizedEmail);
    }

    private bool RequiresVerifiedEmail()
    {
        return configuration.GetValue("Authentication:Oidc:RequireVerifiedEmail", true);
    }

    private static void MarkAuthFailure(TokenValidatedContext context, string reason)
    {
        if (context.Properties is not null)
        {
            context.Properties.Items[AuthFailureReasonPropertyName] = reason;
        }
    }

    private static string GetAuthFailureReason(AuthenticationProperties? properties, Exception? failure = null)
    {
        if (properties?.Items.TryGetValue(AuthFailureReasonPropertyName, out var reason) == true &&
            (string.Equals(reason, EmailUnverifiedFailureReason, StringComparison.Ordinal) ||
                string.Equals(reason, EmailMismatchFailureReason, StringComparison.Ordinal)))
        {
            return reason!;
        }

        if (IsEmailMismatchFailure(failure))
        {
            return EmailMismatchFailureReason;
        }

        return IsEmailUnverifiedFailure(failure)
            ? EmailUnverifiedFailureReason
            : "failed";
    }

    private static bool IsEmailUnverifiedFailure(Exception? failure)
    {
        while (failure is not null)
        {
            if (failure.Message.Contains(
                    "platform_login_verified_email_required",
                    StringComparison.Ordinal))
            {
                return true;
            }

            failure = failure.InnerException;
        }

        return false;
    }

    private static bool IsEmailMismatchFailure(Exception? failure)
    {
        while (failure is not null)
        {
            if (failure.Message.Contains(
                    "platform_login_expected_email_mismatch",
                    StringComparison.Ordinal))
            {
                return true;
            }

            failure = failure.InnerException;
        }

        return false;
    }

    private static bool HasRegistrationContext(AuthenticationProperties? properties)
    {
        return properties?.Items.ContainsKey(AuthEndpointRouteBuilderExtensions.RegistrationTokenPropertyName) == true ||
            properties?.Items.ContainsKey(AuthEndpointRouteBuilderExtensions.RegistrationBootstrapPropertyName) == true;
    }

    private static bool TryGetLoginTenantId(TokenValidatedContext context, out Guid tenantId)
    {
        tenantId = default;

        return context.Properties?.Items.TryGetValue(
                AuthEndpointRouteBuilderExtensions.TenantIdPropertyName,
                out var value) == true &&
            Guid.TryParse(value, out tenantId);
    }

    private static string? GetRegistrationToken(TokenValidatedContext context)
    {
        return context.Properties?.Items.TryGetValue(
                AuthEndpointRouteBuilderExtensions.RegistrationTokenPropertyName,
                out var value) == true
            ? value
            : null;
    }

    private static string? GetExpectedLoginEmail(TokenValidatedContext context)
    {
        return context.Properties?.Items.TryGetValue(
                AuthEndpointRouteBuilderExtensions.ExpectedLoginEmailPropertyName,
                out var value) == true &&
            !string.IsNullOrWhiteSpace(value)
                ? value.Trim().ToLowerInvariant()
                : null;
    }

    private static bool IsRegistrationBootstrap(TokenValidatedContext context)
    {
        return context.Properties?.Items.TryGetValue(
                AuthEndpointRouteBuilderExtensions.RegistrationBootstrapPropertyName,
                out var value) == true &&
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmailVerified(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirst("email_verified")?.Value;

        return bool.TryParse(value, out var verified) && verified;
    }

    private static void ProjectPlatformClaims(
        TokenValidatedContext context,
        PlatformOidcLoginResolution resolution,
        string normalizedEmail)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("platform_login_identity_required");
            return;
        }

        foreach (var claim in identity.Claims.ToArray())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(PlatformClaimTypes.UserId, resolution.UserId.ToString()));
        identity.AddClaim(new Claim(PlatformClaimTypes.SessionId, resolution.SessionId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, resolution.UserId.ToString()));
        identity.AddClaim(new Claim(
            PlatformClaimTypes.EmailVerified,
            resolution.EmailVerified ? "true" : "false"));
        identity.AddClaim(new Claim(
            ClaimTypes.Email,
            string.IsNullOrWhiteSpace(resolution.Email) ? normalizedEmail : resolution.Email));
        identity.AddClaim(new Claim(
            PlatformClaimTypes.TenantMembership,
            resolution.TenantId.ToString()));

        foreach (var permission in resolution.Permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal))
        {
            identity.AddClaim(new Claim(PlatformClaimTypes.Permission, permission));
        }
    }

    private static void ProjectRegistrationBootstrapClaims(
        TokenValidatedContext context,
        string normalizedEmail,
        string provider,
        string providerSubjectHash)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("platform_login_identity_required");
            return;
        }

        foreach (var claim in identity.Claims.ToArray())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(ClaimTypes.Email, normalizedEmail));
        identity.AddClaim(new Claim(PlatformRegistrationClaimTypes.Pending, "true"));
        identity.AddClaim(new Claim(PlatformRegistrationClaimTypes.Email, normalizedEmail));
        identity.AddClaim(new Claim(PlatformRegistrationClaimTypes.Provider, provider));
        identity.AddClaim(new Claim(PlatformRegistrationClaimTypes.ProviderSubjectHash, providerSubjectHash));
    }

}

public sealed class EfPlatformOidcLoginResolver(
    ApplicationDbContext db,
    ITenantDbScope tenantDbScope,
    ICurrentTenant currentTenant,
    IProviderSubjectHasher providerSubjectHasher,
    IConfiguration configuration) : IPlatformOidcLoginResolver
{
    public async Task<PlatformOidcLoginResolution?> ResolveAsync(
        Guid tenantId,
        string email,
        bool emailVerified,
        string provider,
        string providerSubject,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var providerSubjectHash = providerSubjectHasher.Hash(normalizedProvider, providerSubject);

        if (currentTenant.HasTenant && currentTenant.TenantId != tenantId)
        {
            return null;
        }

        if (!emailVerified)
        {
            return null;
        }

        if (!currentTenant.HasTenant)
        {
            currentTenant.SetTenant(tenantId, "oidc");
        }

        var now = DateTimeOffset.UtcNow;
        var sessionMinutes = Math.Max(1, configuration.GetValue("Authentication:Oidc:SessionMinutes", 480));

        await using var transaction = await tenantDbScope.BeginTransactionAsync(
            tenantId,
            cancellationToken: cancellationToken);

        var user = await db.UserAccounts
            .Where(candidate =>
                candidate.TenantId == tenantId &&
                candidate.DeletedAt == null &&
                candidate.Email == email)
            .Select(candidate => new
            {
                candidate.Id
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var hasTenantMembership = await db.RoleAssignments
            .AnyAsync(assignment =>
                assignment.TenantId == tenantId &&
                assignment.UserId == user.Id &&
                assignment.ScopeType == RoleAssignmentScopes.Tenant,
                cancellationToken);

        if (!hasTenantMembership)
        {
            return null;
        }

        var binding = await db.ExternalAuthIdentities
            .SingleOrDefaultAsync(identity =>
                identity.TenantId == tenantId &&
                identity.UserId == user.Id &&
                identity.Provider == normalizedProvider,
                cancellationToken);

        if (binding is not null)
        {
            if (binding.DisabledAt.HasValue ||
                !string.Equals(binding.ProviderSubjectHash, providerSubjectHash, StringComparison.Ordinal))
            {
                return null;
            }

            if (!binding.IsEmailVerified && !emailVerified)
            {
                return null;
            }

            if (emailVerified)
            {
                binding.RecordEmailVerified(now);
            }
            else
            {
                binding.RecordSeen(now);
            }
        }
        else
        {
            if (!emailVerified)
            {
                return null;
            }

            var subjectAlreadyBound = await db.ExternalAuthIdentities
                .AnyAsync(identity =>
                    identity.TenantId == tenantId &&
                    identity.Provider == normalizedProvider &&
                    identity.ProviderSubjectHash == providerSubjectHash,
                    cancellationToken);

            if (subjectAlreadyBound)
            {
                return null;
            }

            binding = new ExternalAuthIdentity(
                PlatformIds.NewId(),
                tenantId,
                user.Id,
                normalizedProvider,
                providerSubjectHash,
                email,
                now);
            binding.RecordEmailVerified(now);
            db.ExternalAuthIdentities.Add(binding);
        }

        var permissions = await (
                from assignment in db.RoleAssignments
                join rolePermission in db.RolePermissions on assignment.RoleId equals rolePermission.RoleId
                join permission in db.Permissions on rolePermission.PermissionId equals permission.Id
                where assignment.TenantId == tenantId &&
                    assignment.UserId == user.Id &&
                    assignment.ScopeType == RoleAssignmentScopes.Tenant
                select permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToArrayAsync(cancellationToken);

        var session = new AuthSession(
            PlatformIds.NewId(),
            tenantId,
            user.Id,
            binding.Id,
            now,
            now.AddMinutes(sessionMinutes));
        db.AuthSessions.Add(session);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PlatformOidcLoginResolution(user.Id, tenantId, session.Id, permissions, email, EmailVerified: true);
    }
}
