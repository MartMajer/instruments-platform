using System.Text;
using Konscious.Security.Cryptography;
using Platform.Application.Features.ParticipantCodes;

namespace Platform.Infrastructure.ParticipantCodes;

public sealed class Argon2idParticipantCodeHasher(ParticipantCodeHashingOptions options) : IParticipantCodeHasher
{
    public Task<ParticipantCodeHashResult> HashAsync(
        string rawCode,
        byte[] seriesSalt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCode);
        ArgumentNullException.ThrowIfNull(seriesSalt);
        if (seriesSalt.Length != 32)
        {
            throw new ArgumentException("Campaign series code salt must be exactly 32 bytes.", nameof(seriesSalt));
        }

        options.EnsureValid();
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = Normalize(rawCode);
        var passwordBytes = Encoding.UTF8.GetBytes(normalized);
        var salt = seriesSalt.ToArray();

        return Task.Run(
            () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var argon2 = new Argon2id(passwordBytes)
                    {
                        Salt = salt,
                        MemorySize = options.MemoryKiB,
                        Iterations = options.Iterations,
                        DegreeOfParallelism = options.Parallelism
                    };
                    var hash = argon2.GetBytes(options.OutputBytes);

                    return new ParticipantCodeHashResult(
                        hash,
                        new ParticipantCodeHashingParameters(
                            options.MemoryKiB,
                            options.Iterations,
                            options.Parallelism,
                            options.OutputBytes));
                }
                finally
                {
                    Array.Clear(passwordBytes);
                    Array.Clear(salt);
                }
            },
            cancellationToken);
    }

    private static string Normalize(string rawCode)
    {
        var parts = rawCode
            .Trim()
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', parts);
    }
}
