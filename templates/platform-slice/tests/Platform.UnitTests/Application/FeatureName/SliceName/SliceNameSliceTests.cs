using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application;
using Platform.Application.Features.FeatureName.SliceName;

namespace Platform.UnitTests.Application.FeatureName.SliceName;

public sealed class SliceNameSliceTests
{
    [Fact]
    public async Task Application_registration_dispatches_SliceName_Command()
    {
        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new SliceNameCommand());

        Assert.True(result.IsFailure);
        Assert.Equal("SliceName.NotImplemented", result.Error.Code);
    }

    [Fact]
    public void Application_registration_includes_SliceName_validator()
    {
        using var provider = new ServiceCollection()
            .AddPlatformApplication()
            .BuildServiceProvider();

        var validators = provider.GetServices<IValidator<SliceNameCommand>>();

        Assert.Contains(validators, validator => validator is SliceNameValidator);
    }
}
