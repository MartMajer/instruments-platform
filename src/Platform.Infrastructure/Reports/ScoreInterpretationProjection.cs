using Platform.Application.Features.Reports;
using Platform.Domain.Scoring;

namespace Platform.Infrastructure.Reports;

internal static class ScoreInterpretationProjection
{
    public static ScoreInterpretationResponse? Create(
        ScoreInterpretationMetadata? metadata,
        string scoreCode,
        decimal? value)
    {
        if (metadata is null || !value.HasValue)
        {
            return null;
        }

        var band = metadata.Match(scoreCode, value.Value);
        return band is null
            ? null
            : new ScoreInterpretationResponse(
                metadata.Status,
                metadata.Source,
                band.Code,
                band.Label,
                metadata.Provenance,
                IsValidated: false,
                IsOfficial: false);
    }
}
