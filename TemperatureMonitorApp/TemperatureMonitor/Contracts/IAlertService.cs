using Temperature.Contracts;

namespace TemperatureMonitor.Contracts;

/// <summary>
/// Defines a contract for an alert service that can raise alerts based on temperature analysis results.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Raises an alert based on the provided temperature analysis result.
    /// </summary>
    /// <param name="analysisResult">The result of the temperature analysis.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RaiseAlertAsync(
        TemperatureAnalysisResult analysisResult,
        CancellationToken cancellationToken = default);
}
