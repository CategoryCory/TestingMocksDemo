using Temperature.Contracts;
using TemperatureMonitor.Contracts;

namespace TemperatureMonitor.Simulation;

/// <summary>
/// Implements a random temperature reading generator that produces temperature readings with random
/// values within a specified range. This class is used for simulating temperature data in the application.
/// </summary>
public class RandomTemperatureGenerator : ITemperatureReadingGenerator
{
    /// <summary>
    /// A shared instance of the random number generator used to produce random temperature values.
    /// </summary>
    private static readonly Random _random = Random.Shared;

    /// <summary>
    /// The minimum temperature values in Celsius that the generator can produce.
    /// </summary>
    private const double MinCelsius = -10.0;

    /// <summary>
    /// The maximum temperature values in Celsius that the generator can produce.
    /// </summary>
    private const double MaxCelsius = 100.0;

    /// <summary>
    /// Generates a new temperature reading with a random temperature value in Celsius, a unique sensor ID,
    /// and the current timestamp.
    /// </summary>
    /// <returns>A new <see cref="TemperatureReading"/> instance with random values.</returns>
    public TemperatureReading Generate() => new()
    {
        SensorId = Guid.NewGuid(),
        TempCelsius = Math.Round(MinCelsius + _random.NextDouble() * (MaxCelsius - MinCelsius), 2),
        Timestamp = DateTime.UtcNow
    };
}
