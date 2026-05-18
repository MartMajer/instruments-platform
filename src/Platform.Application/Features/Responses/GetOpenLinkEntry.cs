using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record GetOpenLinkEntryQuery(string Token)
    : IRequest<Result<OpenLinkEntryResponse>>;

public sealed class GetOpenLinkEntryValidator : AbstractValidator<GetOpenLinkEntryQuery>
{
    public GetOpenLinkEntryValidator()
    {
        RuleFor(query => query.Token).NotEmpty();
    }
}

public sealed class GetOpenLinkEntryHandler(IResponseCaptureStore store)
    : IRequestHandler<GetOpenLinkEntryQuery, Result<OpenLinkEntryResponse>>
{
    public Task<Result<OpenLinkEntryResponse>> Handle(
        GetOpenLinkEntryQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetOpenLinkEntryAsync(query.Token, cancellationToken);
    }
}
