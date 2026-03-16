using Temperature.Contracts;
using TemperatureMonitor.Alerts;
using TemperatureMonitor.Events;

namespace TemperatureMonitor.Tests.UnitTests;

/// <summary>
/// Unit tests for <see cref="AlertBus"/>.
///
/// These tests operate directly against the concrete implementation using real event subscriptions —
/// no mocks are needed because the behavior under test IS the event mechanism itself. This is a
/// case where testing through the interface abstraction would hide exactly what we want to observe.
/// </summary>
public sealed class AlertBusTests
{
    private readonly AlertBus _sut = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void Raise_WhenSubscriberRegistered_FiresAlertRaisedEvent()
    {
        // Arrange
        var eventFired = false;
        _sut.AlertRaised += (_, _) => eventFired = true;

        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 95.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        };

        // Act
        _sut.Raise(result);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Raise_PassesCorrectResultInEventArgs()
    {
        // Arrange
        AlertRaisedEventArgs? captured = null;
        _sut.AlertRaised += (_, e) => captured = e;

        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 95.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        };

        // Act
        _sut.Raise(result);

        // Assert — same reference, not just equal values
        Assert.NotNull(captured);
        Assert.Same(result, captured.Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Raise_PassesBusAsSender()
    {
        // Arrange
        object? capturedSender = null;
        _sut.AlertRaised += (sender, _) => capturedSender = sender;

        // Act
        _sut.Raise(new TemperatureAnalysisResult { Status = TemperatureStatus.Critical });

        // Assert — the bus itself is the event sender, consistent with .NET event conventions
        Assert.Same(_sut, capturedSender);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Raise_WhenMultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        // Demonstrates the core pub/sub value of the event bus: multiple independent
        // handlers (logging, email, SCADA, audit) can all subscribe and all fire.
        var receivedCount = 0;
        _sut.AlertRaised += (_, _) => receivedCount++;
        _sut.AlertRaised += (_, _) => receivedCount++;
        _sut.AlertRaised += (_, _) => receivedCount++;

        // Act
        _sut.Raise(new TemperatureAnalysisResult { Status = TemperatureStatus.Critical });

        // Assert
        Assert.Equal(3, receivedCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Raise_WhenNoSubscribers_DoesNotThrow()
    {
        // The ?. null-conditional invocation in AlertBus.Raise guards against a null
        // invocation list. This test locks in that contract.

        // Act & Assert
        var exception = Record.Exception(
            () => _sut.Raise(new TemperatureAnalysisResult { Status = TemperatureStatus.Critical }));

        Assert.Null(exception);
    }
}
