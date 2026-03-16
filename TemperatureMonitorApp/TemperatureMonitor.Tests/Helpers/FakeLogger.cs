using Microsoft.Extensions.Logging;

namespace TemperatureMonitor.Tests.Helpers;

/// <summary>
/// A simple in-memory logger for use in unit tests. Captures log entries so that tests can
/// inspect what was logged without taking a dependency on a real logging framework.
///
/// This is preferable to mocking <see cref="ILogger{TCategoryName}"/> with NSubstitute because
/// <see cref="ILogger{TCategoryName}.Log{TState}"/> is a generic method whose internal <c>TState</c>
/// type (<c>FormattedLogValues</c>) is not part of the public contract. Asserting against actual
/// rendered message strings is both more readable and more resilient to framework changes.
/// </summary>
public sealed class FakeLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = [];

    /// <summary>The log entries captured since the logger was created.</summary>
    public IReadOnlyList<LogEntry> Entries => _entries;

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => _entries.Add(new LogEntry(logLevel, formatter(state, exception)));

    /// <summary>A single captured log entry.</summary>
    public sealed record LogEntry(LogLevel Level, string Message);
}
