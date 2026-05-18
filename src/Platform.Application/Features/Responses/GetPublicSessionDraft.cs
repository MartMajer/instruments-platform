using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record GetPublicSessionDraftQuery(string Handle)
    : IRequest<Result<OpenLinkSessionDraftResponse>>;

public sealed class GetPublicSessionDraftValidator : AbstractValidator<GetPublicSessionDraftQuery>
{
    public GetPublicSessionDraftValidator()
    {
        RuleFor(query => query.Handle).NotEmpty();
    }
}

public sealed class GetPublicSessionDraftHandler(IResponseCaptureStore store)
    : IRequestHandler<GetPublicSessionDraftQuery, Result<OpenLinkSessionDraftResponse>>
{
    public Task<Result<OpenLinkSessionDraftResponse>> Handle(
        GetPublicSessionDraftQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetPublicSessionDraftAsync(
            query.Handle,
            cancellationToken);
    }
}
