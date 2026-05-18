using Platform.SharedKernel;

namespace Platform.Infrastructure.Reports;

internal static class ExportArtifactStorageKey
{
    public static Result<string[]> SplitSafeSegments(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Contains("\\", StringComparison.Ordinal) ||
            storageKey.StartsWith("/", StringComparison.Ordinal) ||
            Path.IsPathRooted(storageKey))
        {
            return Result.Failure<string[]>(InvalidKey());
        }

        var segments = storageKey.Split('/');
        foreach (var segment in segments)
        {
            if (segment is "" or "." or "..")
            {
                return Result.Failure<string[]>(InvalidKey());
            }

            foreach (var character in segment)
            {
                if (!IsSafeStorageKeyCharacter(character))
                {
                    return Result.Failure<string[]>(InvalidKey());
                }
            }
        }

        return Result.Success(segments);
    }

    public static Error InvalidKey()
    {
        return Error.Validation(
            "export_artifact_object.invalid_key",
            "Export artifact object key is invalid.");
    }

    private static bool IsSafeStorageKeyCharacter(char character)
    {
        return char.IsAsciiLetterOrDigit(character) ||
            character is '-' or '_' or '.';
    }
}
