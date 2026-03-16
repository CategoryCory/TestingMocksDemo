using TemperatureMonitor.Contracts;
using TemperatureMonitor.Events;

namespace TemperatureMonitor.Alerts;

/// <summary>
/// A simple alert service that logs critical temperature alerts using the built-in logging framework.
/// </summary>
public sealed class LoggingAlertHandler
{
    /// <summary>
    /// The logger instance used to log alert messages.
    /// </summary>
    private readonly ILogger<LoggingAlertHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingAlertHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used to log alert messages.</param>
    public LoggingAlertHandler(IAlertBus alertBus, ILogger<LoggingAlertHandler> logger)
    {
        _logger = logger;
        alertBus.AlertRaised += OnAlertRaised;
    }

    /// <summary>
    /// Handles the AlertRaised event by logging a warning message with the details of the
    /// critical temperature alert.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data containing the temperature analysis result.</param>
    private void OnAlertRaised(object? sender, AlertRaisedEventArgs e)
        => _logger.LogWarning(
            "ALERT: Critical temperature detected! Sensor: {SensorId}, Temperature: {Temperature}°C, Timestamp: {Timestamp}",
            e.Result.SensorId,
            e.Result.TempCelsius,
            e.Result.Timestamp);
}
