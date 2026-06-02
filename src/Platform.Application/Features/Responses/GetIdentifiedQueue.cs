using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record GetIdentifiedQueueQuery(string Token)
    : IRequest<Result<IdentifiedQueueEntryResponse>>;

public sealed class GetIdentifiedQueueValidator : AbstractValidator<GetIdentifiedQueueQuery>
{
    public GetIdentifiedQueueValidator()
    {
        RuleFor(query => query.Token).NotEmpty();
    }
}

public sealed class GetIdentifiedQueueHandler(IResponseCaptureStore store)
    : IRequestHandler<GetIdentifiedQueueQuery, Result<IdentifiedQueueEntryResponse>>
{
    public Task<Result<IdentifiedQueueEntryResponse>> Handle(
        GetIdentifiedQueueQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetIdentifiedQueueAsync(query.Token, cancellationToken);
    }
}
