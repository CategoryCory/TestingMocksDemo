namespace TemperatureMonitor.Persistence;

/// <summary>
/// Represents a record of a temperature analysis result that can be stored in a database.
/// </summary>
public sealed class TemperatureResultRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for the temperature result record.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the sensor that produced the temperature reading.
    /// </summary>
    public Guid SensorId { get; set; }

    /// <summary>
    /// Gets or sets the temperature in Celsius that was recorded by the sensor at the time of the analysis.
    /// </summary>
    public double TempCelsius { get; set; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the temperature reading was taken and analyzed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the status of the temperature analysis.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}