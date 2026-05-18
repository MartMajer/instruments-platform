using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Platform.IntegrationTests.Support.Logging;

public sealed class CapturedLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public IReadOnlyList<CapturedLogEntry> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName)
    {
        return new CapturedLogger(categoryName, _entries, () => _scopeProvider);
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public void Dispose()
    {
    }

    private sealed class CapturedLogger(
        string categoryName,
        ConcurrentQueue<CapturedLogEntry> entries,
        Func<IExternalScopeProvider> scopeProviderAccessor) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return scopeProviderAccessor().Push(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(formatter);

            var stateValues = CaptureState(state);
            stateValues.TryGetValue("{OriginalFormat}", out var messageTemplate);

            var scopes = new List<string>();
            scopeProviderAccessor().ForEachScope((scope, values) =>
            {
                values.AddRange(CaptureScope(scope));
            }, scopes);

            entries.Enqueue(new CapturedLogEntry(
                categoryName,
                logLevel,
                eventId,
                formatter(state, exception),
                messageTemplate,
                stateValues,
                exception?.GetType().Name,
                exception?.Message,
                scopes));
        }

        private static Dictionary<string, string?> CaptureState<TState>(TState state)
        {
            var values = new Dictionary<string, string?>(StringComparer.Ordinal);
            if (state is IEnumerable<KeyValuePair<string, object?>> structured)
            {
                foreach (var pair in structured)
                {
                    values[pair.Key] = pair.Value?.ToString();
                }

                return values;
            }

            values["State"] = state?.ToString();
            return values;
        }

        private static IEnumerable<string> CaptureScope(object? scope)
        {
            if (scope is null)
            {
                return [];
            }

            if (scope is IEnumerable<KeyValuePair<string, object?>> structured)
            {
                return structured.Select(pair => $"{pair.Key}={pair.Value}");
            }

            return [scope.ToString() ?? string.Empty];
        }
    }
}
