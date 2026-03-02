using Temperature.Contracts;
using TemperatureMonitor.Contracts;

namespace TemperatureMonitor.Alerts;

/// <summary>
/// A simple alert service that logs critical temperature alerts using the built-in logging framework.
/// </summary>
public sealed class LoggingAlertService : IAlertService
{
    /// <summary>
    /// The logger instance used to log alert messages.
    /// </summary>
    private readonly ILogger<LoggingAlertService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingAlertService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used to log alert messages.</param>
    public LoggingAlertService(ILogger<LoggingAlertService> logger)
        => _logger = logger;

    /// <summary>
    /// Raises an alert by logging a warning message with the details of the critical temperature detected.
    /// </summary>
    /// <param name="analysisResult">The result of the temperature analysis.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RaiseAlertAsync(TemperatureAnalysisResult analysisResult, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "ALERT: Critical temperature detected! Sensor: {SensorId}, Temperature: {Temperature}°C, Timestamp: {Timestamp}",
            analysisResult.SensorId,
            analysisResult.TempCelsius,
            analysisResult.Timestamp);

        return Task.CompletedTask;
    }
}
