using Temperature.Contracts;

namespace TemperatureAnalysisService.Processing;

/// <summary>
/// Represents a service that analyzes temperature readings and determines their status based on a predefined threshold.
/// </summary>
public sealed class TemperatureAnalyzer
{
    /// <summary>
    /// The temperature threshold in Celsius above which a reading is considered "High".
    /// </summary>
    private readonly double _thresholdCelsius;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemperatureAnalyzer"/> class with the specified temperature threshold.
    /// </summary>
    /// <param name="thresholdCelsius"></param>
    public TemperatureAnalyzer(double thresholdCelsius) => _thresholdCelsius = thresholdCelsius;
    
    /// <summary>
    /// Analyzes a temperature reading and determines its status based on the predefined threshold.
    /// </summary>
    /// <param name="reading">The temperature reading to analyze.</param>
    /// <returns>The result of the temperature analysis.</returns>
    public TemperatureAnalysisResult Analyze(TemperatureReading reading)
    {
        var status = reading.TempCelsius > _thresholdCelsius
            ? TemperatureStatus.Critical
            : TemperatureStatus.Normal;

        return new()
        {
            SensorId = reading.SensorId,
            TempCelsius = reading.TempCelsius,
            Timestamp = reading.Timestamp,
            Status = status
        };
    }
}
