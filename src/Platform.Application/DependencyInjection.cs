using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Auditing;
using Platform.Application.Auth;
using Platform.Application.Behaviors;
using Platform.Application.Features.ProductSurfaces;
using Platform.Application.Features.System.GetHealth;
using Platform.Application.Outbox;
using Platform.Application.Tenancy;

namespace Platform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPlatformApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<GetHealthQuery>();
        });

        services.AddValidatorsFromAssemblyContaining<GetHealthValidator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddHttpContextAccessor();
        services.AddAuthorization(options => options.AddPlatformPolicies());
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<ICurrentActor, HttpContextCurrentActor>();
        services.AddScoped<ICurrentAuditContext, CurrentAuditContext>();
        services.AddScoped<IOutboxEventBuffer, OutboxEventBuffer>();
        services.AddSingleton<IMicrosoftGraphAdminConsentUrlBuilder, NoOpMicrosoftGraphAdminConsentUrlBuilder>();
        services.AddSingleton<IAuthorizationPolicyProvider, PlatformAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, TenantMemberAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
