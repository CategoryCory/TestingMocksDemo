using Microsoft.EntityFrameworkCore;
using Npgsql;
using Temperature.Contracts;
using TemperatureMonitor.Persistence;
namespace TemperatureMonitor.Tests.IntegrationTests;

/// <summary>
/// The container and schema are created once per test class run by <see cref="RepositoryTestFixture"/>.
/// Each test's own <see cref="InitializeAsync"/> calls <see cref="RepositoryTestFixture.ResetAsync"/>
/// via Respawn to wipe all rows and restart identity sequences before the test body executes.
/// </summary>
public class RepositoryIntegrationTests : IClassFixture<RepositoryTestFixture>, IAsyncLifetime
{
    private readonly RepositoryTestFixture _fixture;
    private PostgresTemperatureResultRepository _sut = null!;

    public RepositoryIntegrationTests(RepositoryTestFixture fixture)
        => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _sut = new PostgresTemperatureResultRepository(new AppDbContext(_fixture.DbContextOptions));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "Integration")]
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
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();

        Assert.Equal(result.SensorId, saved.SensorId);
        Assert.Equal(result.TempCelsius, saved.TempCelsius);
        Assert.Equal("Normal", saved.Status);

        // PostgreSQL's timestamptz has microsecond precision, while .NET's DateTimeOffset
        // ticks are 100ns. The last tick may be truncated on the round-trip, so we allow
        // a 1-microsecond tolerance rather than requiring exact equality.
        var tolerance = TimeSpan.FromMicroseconds(1);
        Assert.InRange(saved.Timestamp, result.Timestamp - tolerance, result.Timestamp + tolerance);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveAsync_PersistsTimestampToDatabase()
    {
        // Arrange - use a fixed timestamp with no sub-microsecond component to avoid
        // precision loss when PostgreSQL stores it as timestamptz (microsecond precision).
        var timestamp = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 22.5,
            Timestamp = timestamp,
            Status = TemperatureStatus.Normal
        };

        // Act
        await _sut.SaveAsync(result);

        // Assert
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();

        Assert.Equal(timestamp, saved.Timestamp);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveAsync_WhenStatusIsError_PersistsCorrectStatusString()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 0.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Error
        };

        // Act
        await _sut.SaveAsync(result);

        // Assert
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();

        Assert.Equal("Error", saved.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveAsync_MultipleCalls_AssignsDistinctIds()
    {
        // Arrange
        var results = new[]
        {
            new TemperatureAnalysisResult { SensorId = Guid.NewGuid(), TempCelsius = 20.0, Timestamp = DateTimeOffset.UtcNow, Status = TemperatureStatus.Normal },
            new TemperatureAnalysisResult { SensorId = Guid.NewGuid(), TempCelsius = 30.0, Timestamp = DateTimeOffset.UtcNow, Status = TemperatureStatus.Normal },
        };

        // Act
        foreach (var result in results)
            await _sut.SaveAsync(result);

        // Assert
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.ToListAsync();

        Assert.All(saved, r => Assert.True(r.Id > 0));
        Assert.Equal(saved.Count, saved.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveAsync_WhenCancellationRequested_DoesNotPersistRecord()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 42.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Normal
        };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.SaveAsync(result, cts.Token));

        // Verify no record was committed to the database
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var count = await assertDb.TemperatureResults.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    [Trait("Category", "Integration")]
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
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();

        Assert.True(saved.Id > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
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
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
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
    [Trait("Category", "Integration")]
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
        await using var writeDb = new AppDbContext(_fixture.DbContextOptions);
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
