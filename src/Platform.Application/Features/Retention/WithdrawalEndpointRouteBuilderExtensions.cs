using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public static class WithdrawalEndpointRouteBuilderExtensions
{
    private static readonly string SetupManagePolicy = PlatformPolicies.Permission(PlatformPermissions.SetupManage);

    public static IEndpointRouteBuilder MapWithdrawalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/withdrawal-requests", ListWithdrawalRequests)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ListWithdrawalRequests")
            .WithTags("Retention");

        app.MapGet("/withdrawal-requests/{id:guid}", GetWithdrawalRequest)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("GetWithdrawalRequest")
            .WithTags("Retention");

        app.MapPost("/withdrawal-requests/{id:guid}/approve", ApproveWithdrawalRequest)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ApproveWithdrawalRequest")
            .WithTags("Retention");

        app.MapPost("/withdrawal-requests/{id:guid}/deny", DenyWithdrawalRequest)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("DenyWithdrawalRequest")
            .WithTags("Retention");

        app.MapPost("/withdrawal-requests/{id:guid}/execute", ExecuteWithdrawalRequest)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("ExecuteWithdrawalRequest")
            .WithTags("Retention");

        app.MapPost("/withdrawal-requests/tokens", IssueWithdrawalRequestToken)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("IssueWithdrawalRequestToken")
            .WithTags("Retention");

        app.MapPost("/withdrawal-requests/anonymous", CreateAnonymousWithdrawalRequest)
            .AllowAnonymous()
            .WithName("CreateAnonymousWithdrawalRequest")
            .WithTags("Retention");

        app.MapPost("/withdrawal-requests", CreateWithdrawalRequest)
            .RequireTenantContext()
            .RequireAuthorization(PlatformPolicies.TenantMember, SetupManagePolicy)
            .WithName("CreateWithdrawalRequest")
            .WithTags("Retention");

        return app;
    }

    private static async Task<IResult> ListWithdrawalRequests(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListWithdrawalRequestsQuery(), cancellationToken);

        return ToOk(result);
    }

    private static async Task<IResult> GetWithdrawalRequest(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWithdrawalRequestQuery(id), cancellationToken);

        return ToOk(result);
    }

    private static async Task<IResult> ApproveWithdrawalRequest(
        Guid id,
        WithdrawalRequestDecisionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ApproveWithdrawalRequestCommand(id, request), cancellationToken);

        return ToOk(result);
    }

    private static async Task<IResult> DenyWithdrawalRequest(
        Guid id,
        WithdrawalRequestDecisionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DenyWithdrawalRequestCommand(id, request), cancellationToken);

        return ToOk(result);
    }

    private static async Task<IResult> ExecuteWithdrawalRequest(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExecuteWithdrawalRequestCommand(id), cancellationToken);

        return ToOk(result);
    }

    private static async Task<IResult> IssueWithdrawalRequestToken(
        IssueWithdrawalRequestTokenRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new IssueWithdrawalRequestTokenEndpointCommand(request), cancellationToken);

        return ToOk(result);
    }

    private static async Task<IResult> CreateWithdrawalRequest(
        CreateWithdrawalRequestRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateTenantWithdrawalRequestCommand(request), cancellationToken);

        return ToCreated(result, value => $"/withdrawal-requests/{value.RequestId}");
    }

    private static async Task<IResult> CreateAnonymousWithdrawalRequest(
        CreateAnonymousWithdrawalRequestRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateAnonymousWithdrawalRequestEndpointCommand(request), cancellationToken);

        return ToCreated(result, value => $"/withdrawal-requests/{value.RequestId}");
    }

    private static IResult ToOk<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return ToProblem(result.Error);
    }

    private static IResult ToCreated<T>(Result<T> result, Func<T, string> location)
    {
        if (result.IsSuccess)
        {
            return Results.Created(location(result.Value), result.Value);
        }

        return ToProblem(result.Error);
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Type switch
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
            statusCode: status);
    }
}
