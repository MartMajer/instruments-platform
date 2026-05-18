using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application;
using Platform.Application.Features.System.GetHealth;

namespace Platform.UnitTests.Application;

public sealed class GetHealthSliceTests
{
    [Fact]
    public async Task Application_registration_dispatches_health_query()
    {
        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new GetHealthQuery());

        Assert.Equal("instruments-platform", response.Service);
        Assert.Equal("ok", response.Status);
    }

    [Fact]
    public void Application_registration_includes_health_validator()
    {
        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .BuildServiceProvider();

        var validators = provider.GetServices<IValidator<GetHealthQuery>>();

        Assert.Contains(validators, validator => validator is GetHealthValidator);
    }

    [Fact]
    public async Task Live_health_does_not_execute_dependency_checks()
    {
        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .AddScoped<IPlatformHealthCheck, ThrowingHealthCheck>()
            .BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new GetHealthQuery(HealthProbeKind.Live));

        Assert.Equal("instruments-platform", response.Service);
        Assert.Equal("ok", response.Status);
        Assert.Empty(response.Checks);
    }

    [Fact]
    public async Task Ready_health_reports_unready_dependency_checks()
    {
        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .AddScoped<IPlatformHealthCheck>(_ =>
                new FixedHealthCheck("database", PlatformHealthCheckStatus.Unready))
            .BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new GetHealthQuery(HealthProbeKind.Ready));

        Assert.Equal("instruments-platform", response.Service);
        Assert.Equal("unready", response.Status);
        var check = Assert.Single(response.Checks);
        Assert.Equal("database", check.Name);
        Assert.Equal("unready", check.Status);
    }

    [Fact]
    public async Task Ready_health_treats_throwing_dependency_checks_as_unready()
    {
        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .AddScoped<IPlatformHealthCheck, ThrowingHealthCheck>()
            .BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new GetHealthQuery(HealthProbeKind.Ready));

        Assert.Equal("unready", response.Status);
        var check = Assert.Single(response.Checks);
        Assert.Equal("throwing", check.Name);
        Assert.Equal("unready", check.Status);
    }

    private sealed class FixedHealthCheck(string name, PlatformHealthCheckStatus status) : IPlatformHealthCheck
    {
        public string Name => name;

        public Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new PlatformHealthCheckResult(name, status));
        }
    }

    private sealed class ThrowingHealthCheck : IPlatformHealthCheck
    {
        public string Name => "throwing";

        public Task<PlatformHealthCheckResult> CheckAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Live probe should not execute readiness checks.");
        }
    }
}
