using Temperature.Contracts;

namespace TemperatureMonitor.Contracts;

/// <summary>
/// Defines a contract for a repository that can save temperature analysis results.
/// </summary>
public interface ITemperatureResultRepository
{
    /// <summary>
    /// Saves a temperature analysis result.
    /// </summary>
    /// <param name="analysisResult">The temperature analysis result to save.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveAsync(
        TemperatureAnalysisResult analysisResult,
        CancellationToken cancellationToken = default);
}
