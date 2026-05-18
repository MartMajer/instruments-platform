using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record GetOpenLinkSessionDraftQuery(
    string Token,
    Guid SessionId) : IRequest<Result<OpenLinkSessionDraftResponse>>;

public sealed class GetOpenLinkSessionDraftValidator : AbstractValidator<GetOpenLinkSessionDraftQuery>
{
    public GetOpenLinkSessionDraftValidator()
    {
        RuleFor(query => query.Token).NotEmpty();
        RuleFor(query => query.SessionId).NotEmpty();
    }
}

public sealed class GetOpenLinkSessionDraftHandler(IResponseCaptureStore store)
    : IRequestHandler<GetOpenLinkSessionDraftQuery, Result<OpenLinkSessionDraftResponse>>
{
    public Task<Result<OpenLinkSessionDraftResponse>> Handle(
        GetOpenLinkSessionDraftQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetOpenLinkSessionDraftAsync(
            query.Token,
            query.SessionId,
            cancellationToken);
    }
}
