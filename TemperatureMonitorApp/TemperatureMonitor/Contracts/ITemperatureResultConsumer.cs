using Temperature.Contracts;

namespace TemperatureMonitor.Contracts;

/// <summary>
/// Defines a contract for a consumer that can consume temperature analysis results.
/// </summary>
public interface ITemperatureResultConsumer
{
    /// <summary>
    /// Starts consuming temperature analysis results using the provided handler.
    /// </summary>
    /// <param name="handler">A function to handle each temperature analysis result.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StartConsumingAsync(
        Func<TemperatureAnalysisResult, Task> handler,
        CancellationToken cancellationToken = default);
}
