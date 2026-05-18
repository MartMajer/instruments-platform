using Platform.Infrastructure.ParticipantCodes;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class ParticipantCodeHashingTests
{
    private static readonly byte[] SeriesSalt =
    [
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
    ];

    [Fact]
    public async Task Hash_normalizes_equivalent_raw_codes_to_the_same_hash()
    {
        var hasher = CreateFastTestHasher();

        var first = await hasher.HashAsync("  Cable   Horse Battery  ", SeriesSalt, CancellationToken.None);
        var second = await hasher.HashAsync("cable horse battery", SeriesSalt, CancellationToken.None);

        Assert.Equal(32, first.Hash.Length);
        Assert.Equal(first.Hash, second.Hash);
        Assert.Equal(32, first.Parameters.OutputBytes);
    }

    [Fact]
    public async Task Hash_uses_campaign_series_salt_scope()
    {
        var hasher = CreateFastTestHasher();
        var otherSalt = SeriesSalt.Select(value => (byte)(value ^ 0xff)).ToArray();

        var first = await hasher.HashAsync("stable participant code", SeriesSalt, CancellationToken.None);
        var second = await hasher.HashAsync("stable participant code", otherSalt, CancellationToken.None);

        Assert.NotEqual(first.Hash, second.Hash);
    }

    [Fact]
    public void Options_reject_downward_parameters_without_explicit_test_escape_hatch()
    {
        var options = new ParticipantCodeHashingOptions
        {
            MemoryKiB = 1024,
            Iterations = 1,
            Parallelism = 1,
            OutputBytes = 16
        };

        var exception = Assert.Throws<InvalidOperationException>(options.EnsureValid);

        Assert.Contains("Argon2id", exception.Message, StringComparison.Ordinal);
        Assert.Contains("65536", exception.Message, StringComparison.Ordinal);
    }

    private static Argon2idParticipantCodeHasher CreateFastTestHasher()
    {
        return new Argon2idParticipantCodeHasher(new ParticipantCodeHashingOptions
        {
            AllowUnsafeTestParameters = true,
            MemoryKiB = 1024,
            Iterations = 1,
            Parallelism = 1,
            OutputBytes = 32
        });
    }
}
