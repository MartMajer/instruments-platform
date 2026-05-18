using MediatR;
using Platform.Application.Tenancy;

namespace Platform.Application.Features.Setup;

public sealed record ListInstrumentsQuery : IRequest<IReadOnlyList<InstrumentSummaryResponse>>;

public sealed class ListInstrumentsHandler(
    ICurrentTenant currentTenant,
    ISetupWorkflowStore store)
    : IRequestHandler<ListInstrumentsQuery, IReadOnlyList<InstrumentSummaryResponse>>
{
    public Task<IReadOnlyList<InstrumentSummaryResponse>> Handle(
        ListInstrumentsQuery request,
        CancellationToken cancellationToken)
    {
        return store.ListInstrumentsAsync(currentTenant.TenantId, cancellationToken);
    }
}
