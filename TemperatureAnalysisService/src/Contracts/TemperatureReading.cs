namespace TemperatureAnalysisService.Contracts;

/// <summary>
/// Represents a temperature reading from a sensor.
/// </summary>
/// <param name="SensorId">The unique identifier of the sensor.</param>
/// <param name="TempCelsius">The temperature in Celsius.</param>
/// <param name="Timestamp">The timestamp of the reading.</param>
public record TemperatureReading(
    Guid SensorId,
    double TempCelsius,
    DateTimeOffset Timestamp
);
