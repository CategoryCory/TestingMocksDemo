using Temperature.Contracts;

namespace TemperatureMonitor.Contracts;

/// <summary>
/// Defines a contract for a temperature reading generator that can produce temperature readings.
/// </summary>
public interface ITemperatureReadingGenerator
{
    /// <summary>
    /// Generates a new temperature reading.
    /// </summary>
    /// <returns>A <see cref="TemperatureReading"/> representing the current temperature.</returns>
    TemperatureReading Generate();
}
