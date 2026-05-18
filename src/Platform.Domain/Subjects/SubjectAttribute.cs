namespace Platform.Domain.Subjects;

public sealed class SubjectAttribute
{
    public SubjectAttribute(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Key = key.Trim();
        Value = value;
    }

    public string Key { get; }

    public string Value { get; }
}
