using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.Retention;

public sealed record ListWithdrawalRequestsQuery
    : IRequest<Result<IReadOnlyList<WithdrawalRequestReviewResponse>>>;

public sealed class ListWithdrawalRequestsHandler(
    ICurrentTenant currentTenant,
    IWithdrawalRuntimeStore store)
    : IRequestHandler<ListWithdrawalRequestsQuery, Result<IReadOnlyList<WithdrawalRequestReviewResponse>>>
{
    public Task<Result<IReadOnlyList<WithdrawalRequestReviewResponse>>> Handle(
        ListWithdrawalRequestsQuery request,
        CancellationToken cancellationToken)
    {
        return store.ListWithdrawalRequestsAsync(currentTenant.TenantId, cancellationToken);
    }
}

public sealed record GetWithdrawalRequestQuery(Guid RequestId)
    : IRequest<Result<WithdrawalRequestReviewResponse>>;

public sealed class GetWithdrawalRequestValidator : AbstractValidator<GetWithdrawalRequestQuery>
{
    public GetWithdrawalRequestValidator()
    {
        RuleFor(query => query.RequestId)
            .NotEmpty();
    }
}

public sealed class GetWithdrawalRequestHandler(
    ICurrentTenant currentTenant,
    IWithdrawalRuntimeStore store)
    : IRequestHandler<GetWithdrawalRequestQuery, Result<WithdrawalRequestReviewResponse>>
{
    public Task<Result<WithdrawalRequestReviewResponse>> Handle(
        GetWithdrawalRequestQuery request,
        CancellationToken cancellationToken)
    {
        return store.GetWithdrawalRequestAsync(currentTenant.TenantId, request.RequestId, cancellationToken);
    }
}
