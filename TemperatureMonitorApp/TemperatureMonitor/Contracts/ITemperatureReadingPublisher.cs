using Temperature.Contracts;

namespace TemperatureMonitor.Contracts;

/// <summary>
/// Defines a contract for a temperature reading publisher that can publish temperature readings.
/// </summary>
public interface ITemperatureReadingPublisher
{
    /// <summary>
    /// Publishes a temperature reading.
    /// </summary>
    /// <param name="reading">The temperature reading to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishAsync(TemperatureReading reading, CancellationToken cancellationToken = default);
}
