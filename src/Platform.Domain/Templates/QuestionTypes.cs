namespace Platform.Domain.Templates;

public static class QuestionTypes
{
    public const string Likert = "likert";
    public const string SingleChoice = "single";
    public const string MultiChoice = "multi";
    public const string Text = "text";
    public const string Number = "number";
    public const string Date = "date";
    public const string Matrix = "matrix";
    public const string Nps = "nps";
    public const string Ranking = "ranking";
    public const string File = "file";
    public const string Pairwise = "pairwise";

    public static bool IsKnown(string value)
    {
        return value is Likert
            or SingleChoice
            or MultiChoice
            or Text
            or Number
            or Date
            or Matrix
            or Nps
            or Ranking
            or File
            or Pairwise;
    }

    public static bool RequiresScale(string value)
    {
        return value is Likert or Nps;
    }
}
