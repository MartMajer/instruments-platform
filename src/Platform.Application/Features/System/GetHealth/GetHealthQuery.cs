using MediatR;

namespace Platform.Application.Features.System.GetHealth;

public sealed record GetHealthQuery(HealthProbeKind Probe = HealthProbeKind.Summary)
    : IRequest<GetHealthResponse>;
