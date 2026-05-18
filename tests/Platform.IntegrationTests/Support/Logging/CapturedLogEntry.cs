using Microsoft.Extensions.Logging;

namespace Platform.IntegrationTests.Support.Logging;

public sealed record CapturedLogEntry(
    string CategoryName,
    LogLevel Level,
    EventId EventId,
    string Message,
    string? MessageTemplate,
    IReadOnlyDictionary<string, string?> State,
    string? ExceptionType,
    string? ExceptionMessage,
    IReadOnlyList<string> Scopes);
