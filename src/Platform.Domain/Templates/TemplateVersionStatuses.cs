namespace Platform.Domain.Templates;

public static class TemplateVersionStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Retired = "retired";

    public static bool IsKnown(string value)
    {
        return value is Draft or Published or Retired;
    }
}
