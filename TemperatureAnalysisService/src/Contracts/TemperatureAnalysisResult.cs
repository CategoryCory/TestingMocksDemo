namespace TemperatureAnalysisService.Contracts;

/// <summary>
/// Represents the result of analyzing a temperature reading.
/// </summary>
/// <param name="SensorId">The unique identifier of the sensor.</param>
/// <param name="TempCelsius">The temperature in Celsius.</param>
/// <param name="Timestamp">The timestamp of the reading.</param>
/// <param name="Status">The status of the temperature reading.</param>
public record TemperatureAnalysisResult(
    Guid SensorId,
    double TempCelsius,
    DateTimeOffset Timestamp,
    TemperatureStatus Status
);
