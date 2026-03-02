using System.Text.Json.Serialization;

namespace Temperature.Contracts;

/// <summary>
/// Represents the result of analyzing a temperature reading.
/// </summary>
public sealed class TemperatureAnalysisResult
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

    /// <summary>
    /// The status of the temperature reading, indicating whether it is normal or high based on a predefined threshold.
    /// </summary>
    [JsonPropertyName("status")]
    public TemperatureStatus Status { get; set; }
}
