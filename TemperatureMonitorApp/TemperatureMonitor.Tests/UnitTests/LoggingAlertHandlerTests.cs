using Microsoft.Extensions.Logging;
using Temperature.Contracts;
using TemperatureMonitor.Alerts;
using TemperatureMonitor.Tests.Helpers;

namespace TemperatureMonitor.Tests.UnitTests;

/// <summary>
/// Unit tests for <see cref="LoggingAlertHandler"/>.
///
/// These tests use a real <see cref="AlertBus"/> paired with <see cref="FakeLogger{T}"/> instead
/// of mocking <see cref="ILogger{TCategoryName}"/> with NSubstitute. This approach:
///   1. Avoids fragile assertions against internal generic method signatures on ILogger.
///   2. Verifies the full path from event fire → handler invocation → rendered log message.
///   3. Demonstrates that a FakeLogger is a lightweight, effective alternative to log-framework
///      mocking when the logged output itself is what matters.
/// </summary>
public sealed class LoggingAlertHandlerTests
{
    private readonly AlertBus _alertBus = new();
    private readonly FakeLogger<LoggingAlertHandler> _logger = new();

    // Constructing the handler is the act that subscribes it to the bus. The field is kept
    // (rather than a discard) to make the subject under test explicit to the reader.
    private readonly LoggingAlertHandler _sut;

    public LoggingAlertHandlerTests()
        => _sut = new LoggingAlertHandler(_alertBus, _logger);

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_SubscribesToAlertBus()
    {
        // This test verifies subscription indirectly through observable behaviour: if the
        // handler logged something after we fired the event, it must have subscribed.

        // Act
        _alertBus.Raise(new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 95.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        });

        // Assert
        Assert.Single(_logger.Entries);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OnAlertRaised_LogsAtWarningLevel()
    {
        // Act
        _alertBus.Raise(new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 95.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        });

        // Assert
        Assert.Equal(LogLevel.Warning, _logger.Entries.Single().Level);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OnAlertRaised_LogMessageContainsSensorId()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 95.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        };

        // Act
        _alertBus.Raise(result);

        // Assert
        Assert.Contains(result.SensorId.ToString(), _logger.Entries.Single().Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OnAlertRaised_LogMessageContainsTemperature()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 105.5,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        };

        // Act
        _alertBus.Raise(result);

        // Assert
        Assert.Contains("105.5", _logger.Entries.Single().Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OnAlertRaised_WhenMultipleAlertsRaised_LogsEachOne()
    {
        // Arrange
        var results = new[]
        {
            new TemperatureAnalysisResult { SensorId = Guid.NewGuid(), TempCelsius = 90.0, Timestamp = DateTimeOffset.UtcNow, Status = TemperatureStatus.Critical },
            new TemperatureAnalysisResult { SensorId = Guid.NewGuid(), TempCelsius = 103.0, Timestamp = DateTimeOffset.UtcNow, Status = TemperatureStatus.Critical },
        };

        // Act
        foreach (var result in results)
            _alertBus.Raise(result);

        // Assert — one log entry per alert, independent of each other
        Assert.Equal(2, _logger.Entries.Count);
        Assert.Contains(results[0].SensorId.ToString(), _logger.Entries[0].Message);
        Assert.Contains(results[1].SensorId.ToString(), _logger.Entries[1].Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OnAlertRaised_WhenNoAlertRaised_DoesNotLog()
    {
        // The handler is passive — it only acts in response to events.
        // This guards against accidental side-effects during construction.

        // Assert
        Assert.Empty(_logger.Entries);
    }
}
