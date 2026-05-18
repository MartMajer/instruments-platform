namespace Platform.Application.Features.System.GetHealth;

public sealed record GetHealthResponse(
    string Service,
    string Status,
    IReadOnlyList<GetHealthCheckResponse> Checks);

public sealed record GetHealthCheckResponse(string Name, string Status);
