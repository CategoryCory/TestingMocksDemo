namespace TemperatureAnalysisService.Contracts;

/// <summary>
/// Represents the status of a temperature reading.
/// </summary>
public enum TemperatureStatus
{
    /// <summary>
    /// The temperature is within the normal range.
    /// </summary>
    Normal,

    /// <summary>
    /// The temperature is above the normal range.
    /// </summary>
    High,

    /// <summary>
    /// An error occurred while analyzing the temperature reading.
    /// </summary>
    Error,
}
