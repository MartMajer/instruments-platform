namespace Platform.Domain.Templates;

public static class MeasurementLevels
{
    public const string Nominal = "nominal";
    public const string Ordinal = "ordinal";
    public const string Scale = "scale";

    public static bool IsKnown(string value)
    {
        return value is Nominal or Ordinal or Scale;
    }
}
