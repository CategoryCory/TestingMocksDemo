using Temperature.Contracts;

namespace TemperatureMonitor.Events;

/// <summary>
/// Event arguments for when a critical temperature alert is raised, containing the details
/// of the temperature analysis result that triggered the alert.
/// </summary>
public sealed class AlertRaisedEventArgs : EventArgs
{
    /// <summary>
    /// The temperature analysis result that contains the details of the critical temperature
    /// condition detected.
    /// </summary>
    private readonly TemperatureAnalysisResult _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertRaisedEventArgs"/> class.
    /// </summary>
    /// <param name="result">
    /// The temperature analysis result that contains the details of the critical temperature
    /// condition detected.
    /// </param>
    public AlertRaisedEventArgs(TemperatureAnalysisResult result)
    {
        _result = result;
    }

    /// <summary>
    /// Gets the temperature analysis result that contains the details of the critical
    /// temperature condition detected.
    /// </summary>
    public TemperatureAnalysisResult Result => _result;

}
