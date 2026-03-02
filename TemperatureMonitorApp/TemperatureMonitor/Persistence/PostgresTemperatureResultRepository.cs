using Temperature.Contracts;
using TemperatureMonitor.Contracts;

namespace TemperatureMonitor.Persistence;

/// <summary>
/// Implements a temperature result repository that saves temperature analysis results to a PostgreSQL
/// database using Entity Framework Core.
/// </summary>
public class PostgresTemperatureResultRepository : ITemperatureResultRepository
{
    /// <summary>
    /// The Entity Framework Core database context used to interact with the PostgreSQL database.
    /// </summary>
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresTemperatureResultRepository"/> class with
    /// the specified database context.
    /// </summary>
    /// <param name="dbContext">
    /// The Entity Framework Core database context used to interact with the
    /// PostgreSQL database.
    /// </param>
    public PostgresTemperatureResultRepository(AppDbContext dbContext)
        => _dbContext = dbContext;

    /// <summary>
    /// Saves a temperature analysis result to the PostgreSQL database by creating a new record and
    /// adding it to the database context.
    /// </summary>
    /// <param name="analysisResult">The temperature analysis result to be saved.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SaveAsync(TemperatureAnalysisResult analysisResult, CancellationToken cancellationToken = default)
    {
        var record = new TemperatureResultRecord
        {
            SensorId = analysisResult.SensorId,
            TempCelsius = analysisResult.TempCelsius,
            Timestamp = analysisResult.Timestamp,
            Status = analysisResult.Status.ToString()
        };

        _dbContext.TemperatureResults.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
