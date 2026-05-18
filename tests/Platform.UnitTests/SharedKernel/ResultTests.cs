using Platform.SharedKernel;

namespace Platform.UnitTests.SharedKernel;

public sealed class ResultTests
{
    [Fact]
    public void Success_carries_value_and_no_error()
    {
        var result = Result.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("ok", result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_carries_error_and_rejects_value_access()
    {
        var error = Error.Validation("instrument.rights_required", "Rights attestation is required.");

        var result = Result.Failure<string>(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        var exception = Record.Exception(() => _ = result.Value);
        Assert.IsType<InvalidOperationException>(exception);
    }
}
