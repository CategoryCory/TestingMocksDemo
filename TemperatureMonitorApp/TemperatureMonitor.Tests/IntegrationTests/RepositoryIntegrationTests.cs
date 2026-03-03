using Microsoft.EntityFrameworkCore;
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
}
