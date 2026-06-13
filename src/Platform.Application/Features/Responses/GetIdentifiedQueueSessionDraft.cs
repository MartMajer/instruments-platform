using FluentValidation;
using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.Responses;

public sealed record GetIdentifiedQueueSessionDraftQuery(
    string Token,
    Guid AssignmentId,
    Guid SessionId) : IRequest<Result<OpenLinkSessionDraftResponse>>;

public sealed class GetIdentifiedQueueSessionDraftValidator
    : AbstractValidator<GetIdentifiedQueueSessionDraftQuery>
{
    public GetIdentifiedQueueSessionDraftValidator()
    {
        RuleFor(query => query.Token).NotEmpty();
        RuleFor(query => query.AssignmentId).NotEmpty();
        RuleFor(query => query.SessionId).NotEmpty();
    }
}

public sealed class GetIdentifiedQueueSessionDraftHandler(IResponseCaptureStore store)
    : IRequestHandler<GetIdentifiedQueueSessionDraftQuery, Result<OpenLinkSessionDraftResponse>>
{
    public Task<Result<OpenLinkSessionDraftResponse>> Handle(
        GetIdentifiedQueueSessionDraftQuery query,
        CancellationToken cancellationToken)
    {
        return store.GetIdentifiedQueueSessionDraftAsync(
            query.Token,
            query.AssignmentId,
            query.SessionId,
            cancellationToken);
    }
}
