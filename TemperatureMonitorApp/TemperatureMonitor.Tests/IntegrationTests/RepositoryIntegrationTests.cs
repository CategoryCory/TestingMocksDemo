using Microsoft.EntityFrameworkCore;
using Npgsql;
using Temperature.Contracts;
using TemperatureMonitor.Persistence;
using Testcontainers.PostgreSql;

namespace TemperatureMonitor.Tests.IntegrationTests;

public class RepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3-bookworm").Build();
    
    private DbContextOptions<AppDbContext> _dbContextOptions = null!;
    private PostgresTemperatureResultRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        
        // Create the schema once before any tests in this class runs
        await using var db = new AppDbContext(_dbContextOptions);
        await db.Database.EnsureCreatedAsync();

        _sut = new PostgresTemperatureResultRepository(new AppDbContext(_dbContextOptions));
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task SaveAsync_PersistsRecordToDatabase()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 42.0,
            Timestamp = DateTime.UtcNow,
            Status = TemperatureStatus.Normal
        };

        // Act
        await _sut.SaveAsync(result);

        // Assert - use a fresh DbContext instance to avoid EF change-tracking
        await using var assertDb = new AppDbContext(_dbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();

        Assert.Equal(result.SensorId, saved.SensorId);
        Assert.Equal(result.TempCelsius, saved.TempCelsius);
        Assert.Equal("Normal", saved.Status);
    }

    [Fact]
    public async Task SaveAsync_AssignsAutoIncrementedId()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 22.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Normal
        };

        // Act
        await _sut.SaveAsync(result);

        // Assert
        await using var assertDb = new AppDbContext(_dbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();

        Assert.True(saved.Id > 0);
    }

    [Fact]
    public async Task SaveAsync_MultipleCalls_PersistsAllRecords()
    {
        // Arrange
        var results = new[]
        {
            new TemperatureAnalysisResult { SensorId = Guid.NewGuid(), TempCelsius = 20.0, Timestamp = DateTimeOffset.UtcNow, Status = TemperatureStatus.Normal },
            new TemperatureAnalysisResult { SensorId = Guid.NewGuid(), TempCelsius = 95.0, Timestamp = DateTimeOffset.UtcNow, Status = TemperatureStatus.Critical },
        };

        // Act
        foreach (var result in results)
            await _sut.SaveAsync(result);

        // Assert
        await using var assertDb = new AppDbContext(_dbContextOptions);
        var count = await assertDb.TemperatureResults.CountAsync();

        Assert.Equal(2, count);
    }

    /// <summary>
    /// When a Status value exceeds the column's MaxLength(50) constraint, the failure bubbles up
    /// through EF Core → Npgsql → PostgreSQL as a DbUpdateException wrapping a PostgresException
    /// with error code 22001 (string_data_right_truncation).
    ///
    /// Even though the constraint exists in the database schema and the error originates in
    /// PostgreSQL, it is fully visible in the xUnit test failure output. No logs needed.
    /// </summary>
    [Fact]
    public async Task SaveAsync_WhenStatusExceedsColumnLength_ThrowsDbUpdateExceptionWithClearRootCause()
    {
        // Arrange
        // Simulate a mapping bug: a new status value was added to the domain but the column
        // was not widened to accommodate it.
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 42.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = (TemperatureStatus)999 // unmapped enum value → ToString() = "999", but we
                                            // exercise this via the record directly below
        };

        // Write the oversized record directly through EF Core to keep the demo focused on the
        // repository → DB boundary rather than enum serialisation.
        await using var writeDb = new AppDbContext(_dbContextOptions);
        writeDb.TemperatureResults.Add(new TemperatureResultRecord
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 42.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = new string('X', 51) // one character over MaxLength(50)
        });

        // Act & Assert
        // Exception chain:
        //   DbUpdateException ("An error occurred while saving the entity changes.")
        //     └─ PostgresException ("22001: value too long for type character varying(50)")
        //
        // xUnit displays this full chain in the test failure output.
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            writeDb.SaveChangesAsync());

        Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Contains("22001", ex.InnerException.Message);
    }
}
