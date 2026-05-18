using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.FeatureName.SliceName;

public sealed record SliceNameCommand : IRequest<Result<SliceNameResponse>>;
