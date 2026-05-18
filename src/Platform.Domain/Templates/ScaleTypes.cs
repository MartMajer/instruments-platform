namespace Platform.Domain.Templates;

public static class ScaleTypes
{
    public const string Likert = "likert";
    public const string Nps = "nps";
    public const string Binary = "binary";
    public const string Numeric = "numeric";

    public static bool IsKnown(string value)
    {
        return value is Likert or Nps or Binary or Numeric;
    }
}
