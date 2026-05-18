using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record GetIdentifiedEntryQuery(string Token)
    : IRequest<Result<OpenLinkEntryResponse>>;

public sealed class GetIdentifiedEntryValidator : AbstractValidator<GetIdentifiedEntryQuery>
{
    public GetIdentifiedEntryValidator()
    {
        RuleFor(query => query.Token).NotEmpty();
    }
}

public sealed class GetIdentifiedEntryHandler(IResponseCaptureStore store)
    : IRequestHandler<GetIdentifiedEntryQuery, Result<OpenLinkEntryResponse>>
{
    public Task<Result<OpenLinkEntryResponse>> Handle(
        GetIdentifiedEntryQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetIdentifiedEntryAsync(query.Token, cancellationToken);
    }
}
