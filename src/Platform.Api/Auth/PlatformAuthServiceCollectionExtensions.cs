using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Platform.Api.Auth;

public static class PlatformAuthServiceCollectionExtensions
{
    public const string BrowserCorsPolicyName = "PlatformBrowserFrontend";

    public static IServiceCollection AddPlatformAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = environment.IsDevelopment()
                ? "instruments-platform-csrf"
                : "__Host-instruments-platform-csrf";
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
        });

        if (IsDevelopmentAuthenticationEnabled(configuration, environment))
        {
            services
                .AddAuthentication(DevelopmentAuthenticationOptions.SchemeName)
                .AddScheme<DevelopmentAuthenticationOptions, DevelopmentAuthenticationHandler>(
                    DevelopmentAuthenticationOptions.SchemeName,
                    options => configuration
                        .GetSection(DevelopmentAuthenticationOptions.SectionName)
                        .Bind(options));

            return services;
        }

        if (IsInteractiveOidcEnabled(configuration))
        {
            AddInteractiveOidcAuthentication(services, configuration);
            return services;
        }

        AddJwtBearerAuthentication(services, configuration, environment);

        return services;
    }

    public static bool IsInteractiveOidcEnabled(IConfiguration configuration)
    {
        return configuration.GetValue<bool>("Authentication:Oidc:InteractiveEnabled");
    }

    private static void AddInteractiveOidcAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<PlatformOidcEvents>();
        services.AddScoped<PlatformSessionCookieEvents>();
        services.AddScoped<IPlatformOidcLoginResolver, EfPlatformOidcLoginResolver>();
        services.AddScoped<IPlatformSessionValidator, EfPlatformSessionValidator>();
        services.AddScoped<IPlatformSessionRevoker, PlatformSessionRevoker>();
        services.AddScoped<IPlatformSessionRevocationStore, EfPlatformSessionRevocationStore>();
        services.AddSingleton<IProviderSubjectHasher, Sha256ProviderSubjectHasher>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = PlatformAuthenticationSchemes.AppCookie;
                options.DefaultSignInScheme = PlatformAuthenticationSchemes.AppCookie;
                options.DefaultChallengeScheme = PlatformAuthenticationSchemes.AppCookie;
                options.DefaultSignOutScheme = PlatformAuthenticationSchemes.Oidc;
            })
            .AddCookie(PlatformAuthenticationSchemes.AppCookie, options =>
            {
                var oidc = configuration.GetSection("Authentication:Oidc");

                options.Cookie.Name = oidc["CookieName"] ?? "__Host-instruments-platform";
                options.Cookie.Path = "/";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = "/auth/login";
                options.AccessDeniedPath = "/auth/access-denied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(
                    Math.Max(1, oidc.GetValue("SessionMinutes", 480)));
                options.SlidingExpiration = true;
                options.EventsType = typeof(PlatformSessionCookieEvents);
            })
            .AddOpenIdConnect(PlatformAuthenticationSchemes.Oidc, options =>
            {
                var oidc = configuration.GetSection("Authentication:Oidc");

                options.Authority = oidc["Authority"];
                options.ClientId = oidc["ClientId"];
                options.ClientSecret = oidc["ClientSecret"];
                options.CallbackPath = oidc["CallbackPath"] ?? "/auth/callback";
                options.SignedOutCallbackPath = oidc["SignedOutCallbackPath"] ??
                    "/auth/signout-callback";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.MapInboundClaims = false;
                options.EventsType = typeof(PlatformOidcEvents);
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "email",
                    RoleClaimType = "roles"
                };
            });
    }

    private static void AddJwtBearerAuthentication(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var oidc = configuration.GetSection("Authentication:Oidc");
                var authority = oidc["Authority"];
                var audience = oidc["Audience"];
                var requireHttpsMetadata = oidc.GetValue("RequireHttpsMetadata", true);
                var authorityCanInitializeJwtBearer = environment.IsDevelopment() ||
                    OidcConfigurationHealthCheck.IsAbsoluteHttpsUri(authority);

                options.Authority = string.IsNullOrWhiteSpace(authority) ||
                    !authorityCanInitializeJwtBearer
                        ? null
                        : authority;
                options.Audience = string.IsNullOrWhiteSpace(audience) ? null : audience;
                options.RequireHttpsMetadata = environment.IsDevelopment()
                    ? requireHttpsMetadata
                    : true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "email",
                    RoleClaimType = "roles",
                    ValidateIssuer = environment.IsDevelopment()
                        ? !string.IsNullOrWhiteSpace(authority)
                        : true,
                    ValidateAudience = environment.IsDevelopment()
                        ? !string.IsNullOrWhiteSpace(audience)
                        : true
                };
            });
    }

    public static bool IsDevelopmentAuthenticationEnabled(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        return environment.IsDevelopment() &&
            configuration.GetValue<bool>($"{DevelopmentAuthenticationOptions.SectionName}:Enabled");
    }

    public static string[] GetDevelopmentCorsOrigins(IConfiguration configuration)
    {
        var origins = configuration
            .GetSection($"{DevelopmentAuthenticationOptions.SectionName}:AllowedOrigins")
            .Get<string[]>();

        return origins is { Length: > 0 }
            ? origins
            :
            [
                "http://127.0.0.1:5173",
                "http://localhost:5173"
            ];
    }

    public static string[] GetBrowserCorsOrigins(
        IConfiguration configuration,
        bool includeDevelopmentFallback)
    {
        var origins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (origins is { Length: > 0 })
        {
            return origins;
        }

        return includeDevelopmentFallback
            ? GetDevelopmentCorsOrigins(configuration)
            : [];
    }
}
