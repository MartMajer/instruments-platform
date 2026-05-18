using System.Text.Json;
using Platform.SharedKernel;

namespace Platform.Domain.Scoring;

public sealed record ScoreInterpretationMetadata(
    string Status,
    string Source,
    string Provenance,
    IReadOnlyDictionary<string, IReadOnlyList<ScoreInterpretationBand>> BandsByScore)
{
    public ScoreInterpretationBand? Match(string scoreCode, decimal value)
    {
        if (string.IsNullOrWhiteSpace(scoreCode))
        {
            return null;
        }

        return BandsByScore.TryGetValue(NormalizeCode(scoreCode), out var bands)
            ? bands.FirstOrDefault(band => value >= band.Min && value <= band.Max)
            : null;
    }

    internal static string NormalizeCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Trim().ToLowerInvariant();
    }
}

public sealed record ScoreInterpretationBand(
    string ScoreCode,
    string Code,
    string Label,
    decimal Min,
    decimal Max);

public static class ScoreInterpretationMetadataParser
{
    public const string TenantAttested = "tenant_attested";
    public const string TenantDefined = "tenant_defined";

    public static Result<ScoreInterpretationMetadata?> ParseProduces(string producesJson)
    {
        if (string.IsNullOrWhiteSpace(producesJson))
        {
            return Result.Failure<ScoreInterpretationMetadata?>(
                Error.Validation("score.rule_produces_invalid", "Scoring rule produces must be a JSON object."));
        }

        try
        {
            using var document = JsonDocument.Parse(producesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure<ScoreInterpretationMetadata?>(
                    Error.Validation("score.rule_produces_invalid", "Scoring rule produces must be a JSON object."));
            }

            var declaredScoreCodes = ReadDeclaredScoreCodes(document.RootElement);
            if (declaredScoreCodes.IsFailure)
            {
                return Result.Failure<ScoreInterpretationMetadata?>(declaredScoreCodes.Error);
            }

            return Parse(document.RootElement, declaredScoreCodes.Value);
        }
        catch (JsonException)
        {
            return Result.Failure<ScoreInterpretationMetadata?>(
                Error.Validation("score.rule_produces_invalid", "Scoring rule produces must be valid JSON."));
        }
    }

    public static Result<ScoreInterpretationMetadata?> Parse(
        JsonElement produces,
        IReadOnlyList<string> declaredScoreCodes)
    {
        ArgumentNullException.ThrowIfNull(declaredScoreCodes);

        if (!produces.TryGetProperty("interpretation", out var interpretation))
        {
            return Result.Success<ScoreInterpretationMetadata?>(null);
        }

        if (interpretation.ValueKind != JsonValueKind.Object)
        {
            return InterpretationFailure("Scoring rule produces.interpretation must be a JSON object.");
        }

        var status = ReadRequiredString(interpretation, "status");
        if (status.IsFailure)
        {
            return InterpretationFailure("Scoring rule interpretation status is required.");
        }

        var normalizedStatus = ScoreInterpretationMetadata.NormalizeCode(status.Value);
        if (normalizedStatus != TenantAttested)
        {
            return InterpretationFailure("Scoring rule interpretation status must be tenant_attested.");
        }

        var source = ReadRequiredString(interpretation, "source");
        if (source.IsFailure)
        {
            return InterpretationFailure("Scoring rule interpretation source is required.");
        }

        var normalizedSource = ScoreInterpretationMetadata.NormalizeCode(source.Value);
        if (normalizedSource != TenantDefined)
        {
            return InterpretationFailure("Scoring rule interpretation source must be tenant_defined.");
        }

        var provenance = ReadRequiredString(interpretation, "provenance");
        if (provenance.IsFailure)
        {
            return InterpretationFailure("Scoring rule interpretation provenance is required.");
        }

        if (!interpretation.TryGetProperty("scores", out var scores) ||
            scores.ValueKind != JsonValueKind.Object)
        {
            return InterpretationFailure("Scoring rule interpretation scores must be a JSON object.");
        }

        var declaredScoreSet = declaredScoreCodes
            .Select(ScoreInterpretationMetadata.NormalizeCode)
            .ToHashSet(StringComparer.Ordinal);
        var bandsByScore = new Dictionary<string, IReadOnlyList<ScoreInterpretationBand>>(StringComparer.Ordinal);

        foreach (var scoreProperty in scores.EnumerateObject())
        {
            var scoreCode = ScoreInterpretationMetadata.NormalizeCode(scoreProperty.Name);
            if (!declaredScoreSet.Contains(scoreCode))
            {
                return InterpretationFailure(
                    $"Scoring rule interpretation score '{scoreCode}' is not declared in produces.scores.");
            }

            var bands = ReadBands(scoreCode, scoreProperty.Value);
            if (bands.IsFailure)
            {
                return Result.Failure<ScoreInterpretationMetadata?>(bands.Error);
            }

            bandsByScore.Add(scoreCode, bands.Value);
        }

        if (bandsByScore.Count == 0)
        {
            return InterpretationFailure("Scoring rule interpretation must declare at least one score.");
        }

        return Result.Success<ScoreInterpretationMetadata?>(
            new ScoreInterpretationMetadata(
                normalizedStatus,
                normalizedSource,
                provenance.Value.Trim(),
                bandsByScore));
    }

    private static Result<IReadOnlyList<ScoreInterpretationBand>> ReadBands(
        string scoreCode,
        JsonElement bandsElement)
    {
        if (bandsElement.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyList<ScoreInterpretationBand>>(
                Error.Validation(
                    "score.rule_interpretation_invalid",
                    $"Scoring rule interpretation score '{scoreCode}' bands must be an array."));
        }

        var bands = new List<ScoreInterpretationBand>();
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var bandElement in bandsElement.EnumerateArray())
        {
            if (bandElement.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure<IReadOnlyList<ScoreInterpretationBand>>(
                    Error.Validation(
                        "score.rule_interpretation_invalid",
                        $"Scoring rule interpretation score '{scoreCode}' band must be a JSON object."));
            }

            var code = ReadRequiredString(bandElement, "code");
            if (code.IsFailure)
            {
                return BandFailure(scoreCode, "code is required.");
            }

            var normalizedCode = ScoreInterpretationMetadata.NormalizeCode(code.Value);
            if (!seenCodes.Add(normalizedCode))
            {
                return BandFailure(scoreCode, $"band '{normalizedCode}' is duplicated.");
            }

            var label = ReadRequiredString(bandElement, "label");
            if (label.IsFailure)
            {
                return BandFailure(scoreCode, "label is required.");
            }

            if (!TryReadDecimal(bandElement, "min", out var min))
            {
                return BandFailure(scoreCode, "min must be numeric.");
            }

            if (!TryReadDecimal(bandElement, "max", out var max))
            {
                return BandFailure(scoreCode, "max must be numeric.");
            }

            if (min > max)
            {
                return BandFailure(scoreCode, "min must be less than or equal to max.");
            }

            bands.Add(new ScoreInterpretationBand(
                scoreCode,
                normalizedCode,
                label.Value.Trim(),
                min,
                max));
        }

        if (bands.Count == 0)
        {
            return BandFailure(scoreCode, "at least one band is required.");
        }

        var ordered = bands
            .OrderBy(band => band.Min)
            .ThenBy(band => band.Max)
            .ToArray();
        for (var index = 1; index < ordered.Length; index++)
        {
            if (ordered[index - 1].Max >= ordered[index].Min)
            {
                return BandFailure(scoreCode, "bands must not overlap.");
            }
        }

        return Result.Success<IReadOnlyList<ScoreInterpretationBand>>(ordered);
    }

    private static Result<IReadOnlyList<string>> ReadDeclaredScoreCodes(JsonElement produces)
    {
        if (!produces.TryGetProperty("scores", out var scores) ||
            scores.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<IReadOnlyList<string>>(
                Error.Validation("score.rule_produces_invalid", "Scoring rule produces must declare scores."));
        }

        var values = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in scores.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(item.GetString()))
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation("score.rule_produces_invalid", "Scoring rule score code must be a non-empty string."));
            }

            var normalized = ScoreInterpretationMetadata.NormalizeCode(item.GetString()!);
            if (!seen.Add(normalized))
            {
                return Result.Failure<IReadOnlyList<string>>(
                    Error.Validation("score.rule_produces_invalid", $"Scoring rule score code '{normalized}' is duplicated."));
            }

            values.Add(normalized);
        }

        return values.Count == 0
            ? Result.Failure<IReadOnlyList<string>>(
                Error.Validation("score.rule_produces_invalid", "Scoring rule produces.scores must not be empty."))
            : Result.Success<IReadOnlyList<string>>(values);
    }

    private static Result<string> ReadRequiredString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString())
            ? Result.Success(property.GetString()!)
            : Result.Failure<string>(
                Error.Validation(
                    "score.rule_interpretation_invalid",
                    $"Scoring rule interpretation {propertyName} is required."));
    }

    private static bool TryReadDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = default;

        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetDecimal(out value);
    }

    private static Result<ScoreInterpretationMetadata?> InterpretationFailure(string message)
    {
        return Result.Failure<ScoreInterpretationMetadata?>(
            Error.Validation("score.rule_interpretation_invalid", message));
    }

    private static Result<IReadOnlyList<ScoreInterpretationBand>> BandFailure(
        string scoreCode,
        string reason)
    {
        return Result.Failure<IReadOnlyList<ScoreInterpretationBand>>(
            Error.Validation(
                "score.rule_interpretation_invalid",
                $"Scoring rule interpretation score '{scoreCode}' {reason}"));
    }
}
