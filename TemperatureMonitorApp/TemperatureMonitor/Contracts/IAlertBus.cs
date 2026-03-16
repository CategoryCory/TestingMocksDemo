using Temperature.Contracts;
using TemperatureMonitor.Events;

namespace TemperatureMonitor.Contracts;

/// <summary>
/// Defines a contract for an alert bus that allows raising critical temperature alerts and
/// subscribing to alert events.
/// </summary>
public interface IAlertBus
{
    /// <summary>
    /// Occurs when a critical temperature alert is raised.
    /// </summary>
    event EventHandler<AlertRaisedEventArgs> AlertRaised;

    /// <summary>
    /// Raises a critical temperature alert with the specified temperature analysis result.
    /// </summary>
    /// <param name="result">The temperature analysis result that triggered the alert.</param>
    void Raise(TemperatureAnalysisResult result);
}
