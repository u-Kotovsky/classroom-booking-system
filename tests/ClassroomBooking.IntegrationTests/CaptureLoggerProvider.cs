
using Microsoft.Extensions.Logging;

namespace ClassroomBooking.IntegrationTests;

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<CapturedLogEntry> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName)
    {
        return new CapturingLogger(Entries);
    }

    public void Dispose()
    {
    }
}

internal sealed class CapturingLogger : ILogger
{
    private readonly List<CapturedLogEntry> _entries;

    public CapturingLogger(List<CapturedLogEntry> entries)
    {
        _entries = entries;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var values = new Dictionary<string, object?>();

        if (state is IReadOnlyList<KeyValuePair<string, object?>> stateValues)
        {
            foreach (var pair in stateValues)
            {
                values[pair.Key] = pair.Value;
            }
        }

        _entries.Add(new CapturedLogEntry(logLevel, formatter(state, exception), values));
    }
}

internal sealed record CapturedLogEntry(
    LogLevel Level,
    string Message,
    IReadOnlyDictionary<string, object?> State);