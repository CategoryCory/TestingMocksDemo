using System.Text.Json.Serialization;

namespace TemperatureAnalysisService.Contracts;

/// <summary>
/// Represents a temperature reading from a sensor.
/// </summary>
public sealed class TemperatureReading
{
    /// <summary>
    /// The unique identifier of the sensor that produced the temperature reading.
    /// </summary>
    [JsonPropertyName("sensorId")]
    public Guid SensorId { get; set; }

    /// <summary>
    /// The temperature in Celsius recorded by the sensor at the time of the reading.
    /// </summary>
    [JsonPropertyName("tempCelsius")]
    public double TempCelsius { get; set; }

    /// <summary>
    /// The timestamp of the temperature reading, indicating when the reading was taken.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}
