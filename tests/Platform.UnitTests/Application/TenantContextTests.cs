using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application;
using Platform.Application.Auth;
using Platform.Application.Tenancy;

namespace Platform.UnitTests.Application;

public sealed class TenantContextTests
{
    [Fact]
    public void Application_registration_includes_current_tenant()
    {
        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .BuildServiceProvider();

        var currentTenant = provider.GetRequiredService<ICurrentTenant>();

        Assert.False(currentTenant.HasTenant);
    }

    [Fact]
    public async Task Middleware_sets_current_tenant_from_valid_header()
    {
        var tenantId = Guid.NewGuid();
        var currentTenant = new CurrentTenant();
        var context = CreateHttpContext();
        var nextCalled = false;

        context.Request.Headers[TenantContextMiddleware.HeaderName] = tenantId.ToString();

        var middleware = new TenantContextMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, currentTenant);

        Assert.True(nextCalled);
        Assert.True(currentTenant.HasTenant);
        Assert.Equal(tenantId, currentTenant.TenantId);
        Assert.Equal("header", currentTenant.Source);
    }

    [Fact]
    public async Task Middleware_sets_current_tenant_from_single_authenticated_membership_when_header_is_missing()
    {
        var tenantId = Guid.NewGuid();
        var currentTenant = new CurrentTenant();
        var context = CreateHttpContext();
        var nextCalled = false;

        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(PlatformClaimTypes.TenantMembership, tenantId.ToString())],
                "test"));

        var middleware = new TenantContextMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, currentTenant);

        Assert.True(nextCalled);
        Assert.True(currentTenant.HasTenant);
        Assert.Equal(tenantId, currentTenant.TenantId);
        Assert.Equal("claim", currentTenant.Source);
    }

    [Fact]
    public async Task Middleware_does_not_infer_tenant_from_multiple_memberships()
    {
        var currentTenant = new CurrentTenant();
        var context = CreateHttpContext();
        var nextCalled = false;

        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(PlatformClaimTypes.TenantMembership, Guid.NewGuid().ToString()),
                    new Claim(PlatformClaimTypes.TenantMembership, Guid.NewGuid().ToString())
                ],
                "test"));

        var middleware = new TenantContextMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, currentTenant);

        Assert.True(nextCalled);
        Assert.False(currentTenant.HasTenant);
    }

    [Fact]
    public async Task Middleware_rejects_malformed_tenant_header_before_endpoint()
    {
        var currentTenant = new CurrentTenant();
        var context = CreateHttpContext();
        var nextCalled = false;

        context.Request.Headers[TenantContextMiddleware.HeaderName] = "not-a-guid";
        context.Response.Body = new MemoryStream();

        var middleware = new TenantContextMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, currentTenant);

        Assert.False(nextCalled);
        Assert.False(currentTenant.HasTenant);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Require_tenant_filter_returns_problem_when_context_is_missing()
    {
        var currentTenant = new CurrentTenant();
        var filter = new RequireTenantContextFilter(currentTenant);
        var endpointCalled = false;

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(CreateHttpContext()),
            _ =>
            {
                endpointCalled = true;
                return ValueTask.FromResult<object?>("ok");
            });

        Assert.False(endpointCalled);

        var httpResult = Assert.IsAssignableFrom<IResult>(result);
        var httpContext = CreateHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await httpResult.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Require_tenant_filter_calls_endpoint_when_context_is_present()
    {
        var tenantId = Guid.NewGuid();
        var currentTenant = new CurrentTenant();
        var filter = new RequireTenantContextFilter(currentTenant);
        var endpointCalled = false;

        currentTenant.SetTenant(tenantId, "header");

        var result = await filter.InvokeAsync(
            new TestEndpointFilterInvocationContext(CreateHttpContext()),
            _ =>
            {
                endpointCalled = true;
                return ValueTask.FromResult<object?>("ok");
            });

        Assert.True(endpointCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public void Current_tenant_cannot_be_changed_after_it_is_set()
    {
        var currentTenant = new CurrentTenant();

        currentTenant.SetTenant(Guid.NewGuid(), "header");

        Assert.Throws<InvalidOperationException>(() =>
            currentTenant.SetTenant(Guid.NewGuid(), "header"));
    }

    [Fact]
    public void Current_tenant_source_cannot_be_changed_after_it_is_set()
    {
        var currentTenant = new CurrentTenant();
        var tenantId = Guid.NewGuid();

        currentTenant.SetTenant(tenantId, "header");

        Assert.Throws<InvalidOperationException>(() =>
            currentTenant.SetTenant(tenantId, "route"));
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }
}
