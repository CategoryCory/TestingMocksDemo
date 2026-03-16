using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Temperature.Contracts;
using TemperatureMonitor.Persistence;

namespace TemperatureMonitor.Tests.UnitTests;

/// <summary>
/// Unit tests for <see cref="PostgresTemperatureResultRepository"/>.
/// </summary>
public class PostgresTemperatureResultRepositoryTests
{
    // --- Mocks ---
    private readonly AppDbContext _dbContext;
    private readonly DbSet<TemperatureResultRecord> _temperatureResults;

    // --- System Under Test ---
    private readonly PostgresTemperatureResultRepository _sut;

    public PostgresTemperatureResultRepositoryTests()
    {
        // DbContextOptions with no provider is sufficient here because AppDbContext is fully
        // substituted — no real database operations will be executed.
        var options = new DbContextOptionsBuilder<AppDbContext>().Options;
        _dbContext = Substitute.For<AppDbContext>(options);

        _temperatureResults = Substitute.For<DbSet<TemperatureResultRecord>>();

        // TemperatureResults is a non-virtual property with a public setter, so we assign
        // the mock directly rather than using .Returns().
        _dbContext.TemperatureResults = _temperatureResults;

        _sut = new PostgresTemperatureResultRepository(_dbContext);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveAsync_AddsRecordWithCorrectFieldMappings()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 36.6,
            Timestamp = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero),
            Status = TemperatureStatus.Normal
        };

        // Act
        await _sut.SaveAsync(result);

        // Assert
        _temperatureResults.Received(1).Add(Arg.Is<TemperatureResultRecord>(r =>
            r.SensorId == result.SensorId &&
            r.TempCelsius == result.TempCelsius &&
            r.Timestamp == result.Timestamp &&
            r.Status == result.Status.ToString()));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveAsync_CallsSaveChangesAsyncOnce()
    {
        // Arrange
        var result = new TemperatureAnalysisResult { Status = TemperatureStatus.Normal };

        // Act
        await _sut.SaveAsync(result);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(TemperatureStatus.Normal, "Normal")]
    [InlineData(TemperatureStatus.Critical, "Critical")]
    [InlineData(TemperatureStatus.Error, "Error")]
    [Trait("Category", "Unit")]
    public async Task SaveAsync_SerializesStatusEnumAsString(TemperatureStatus status, string expectedStatusString)
    {
        // Arrange
        var result = new TemperatureAnalysisResult { Status = status };

        // Act
        await _sut.SaveAsync(result);

        // Assert
        _temperatureResults.Received(1).Add(Arg.Is<TemperatureResultRecord>(r =>
            r.Status == expectedStatusString));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveAsync_ForwardsCancellationTokenToSaveChanges()
    {
        // Arrange
        var result = new TemperatureAnalysisResult { Status = TemperatureStatus.Normal };
        using var cts = new CancellationTokenSource();

        // Act
        await _sut.SaveAsync(result, cts.Token);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveAsync_WhenCancellationRequested_PropagatesException()
    {
        // Arrange
        var result = new TemperatureAnalysisResult { Status = TemperatureStatus.Normal };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _dbContext
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<int>(cts.Token));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.SaveAsync(result, cts.Token));
    }
}
