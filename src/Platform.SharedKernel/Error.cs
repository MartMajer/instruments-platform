namespace Platform.SharedKernel;

public readonly record struct Error(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyDictionary<string, object?> Extensions = null!)
{
    public static readonly Error None = new(
        string.Empty,
        string.Empty,
        ErrorType.None,
        new Dictionary<string, object?>());

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation, new Dictionary<string, object?>());

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden, new Dictionary<string, object?>());

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound, new Dictionary<string, object?>());

    public static Error Conflict(
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? extensions = null) =>
        new(code, message, ErrorType.Conflict, extensions ?? new Dictionary<string, object?>());
}
