using MediatR;
using Platform.SharedKernel;

namespace Platform.Application.Features.FeatureName.SliceName;

public sealed class SliceNameHandler
    : IRequestHandler<SliceNameCommand, Result<SliceNameResponse>>
{
    public Task<Result<SliceNameResponse>> Handle(
        SliceNameCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<SliceNameResponse>(
            Error.Conflict(
                "SliceName.NotImplemented",
                "Implement the SliceName slice.")));
    }
}
