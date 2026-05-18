namespace Platform.Domain.Instruments;

public static class TranslationStatuses
{
    public const string DraftTranslation = "draft_translation";
    public const string BackTranslated = "back_translated";
    public const string Reconciled = "reconciled";
    public const string ApprovedCanonicalEquivalent = "approved_canonical_equivalent";
    public const string ApprovedDerivative = "approved_derivative";
    public const string Rejected = "rejected";

    private static readonly HashSet<string> Known =
    [
        DraftTranslation,
        BackTranslated,
        Reconciled,
        ApprovedCanonicalEquivalent,
        ApprovedDerivative,
        Rejected
    ];

    public static bool IsKnown(string value)
    {
        return Known.Contains(value);
    }
}
