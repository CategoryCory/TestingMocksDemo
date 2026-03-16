using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Temperature.Contracts;
using TemperatureMonitor.Contracts;
using TemperatureMonitor.Persistence;

namespace TemperatureMonitor.Tests.IntegrationTests;

/// <summary>
/// Integration tests for <see cref="Worker.HandleResultAsync"/>.
///
/// The integration boundary here is the Worker's result-handling pipeline: real EF Core and a real
/// PostgreSQL instance are used to verify end-to-end persistence. However, <see cref="IAlertService"/>
/// is still mocked — it is an external side effect (e.g. sending a notification) that lives outside
/// this integration boundary and does not need a real implementation to validate the pipeline.
///
/// This illustrates that mocks don't disappear in integration tests; they move to the edges of
/// whatever boundary you have chosen to integrate.
/// </summary>
public class WorkerIntegrationTests : IClassFixture<WorkerTestFixture>, IAsyncLifetime
{
    private readonly WorkerTestFixture _fixture;

    // IAlertBus is mocked — it sits outside the integration boundary.
    // xUnit creates a new test class instance per test, so this is a fresh mock for each test.
    private readonly IAlertBus _alertBus = Substitute.For<IAlertBus>();

    private ServiceProvider _serviceProvider = null!;
    private Worker _sut = null!;

    public WorkerIntegrationTests(WorkerTestFixture fixture)
        => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();

        // Build a real service provider so Worker gets a genuine IServiceScopeFactory
        // backed by real EF Core and real Postgres.
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString));
        services.AddScoped<ITemperatureResultRepository, PostgresTemperatureResultRepository>();
        _serviceProvider = services.BuildServiceProvider();

        _sut = new Worker(
            generator:     Substitute.For<ITemperatureReadingGenerator>(),
            publisher:     Substitute.For<ITemperatureReadingPublisher>(),
            consumer:      Substitute.For<ITemperatureResultConsumer>(),
            alertBus:      _alertBus,
            scopeFactory:  _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            logger:        NullLogger<Worker>.Instance);
    }

    public async Task DisposeAsync() => await _serviceProvider.DisposeAsync();

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleResultAsync_PersistsResultToRealDatabase()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 42.5,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Normal
        };

        // Act
        await _sut.HandleResultAsync(result);

        // Assert
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
    public async Task HandleResultAsync_WhenStatusIsCritical_PersistsAndRaisesAlert()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 95.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        };

        // Act
        await _sut.HandleResultAsync(result);

        // Assert: real persistence happened
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();
        Assert.Equal("Critical", saved.Status);

        // Assert: alert boundary was triggered (mocked)
        _alertBus.Received(1).Raise(result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleResultAsync_WhenStatusIsNormal_PersistsButDoesNotRaiseAlert()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 22.5,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Normal
        };

        // Act
        await _sut.HandleResultAsync(result);

        // Assert: real persistence happened
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();
        Assert.Equal("Normal", saved.Status);

        // Assert: alert boundary was NOT triggered
        _alertBus.DidNotReceive().Raise(Arg.Any<TemperatureAnalysisResult>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleResultAsync_WhenStatusIsError_PersistsButDoesNotRaiseAlert()
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
        await _sut.HandleResultAsync(result);

        // Assert: real persistence happened
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();
        Assert.Equal("Error", saved.Status);

        // Assert: alert boundary was NOT triggered
        _alertBus.DidNotReceive().Raise(Arg.Any<TemperatureAnalysisResult>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleResultAsync_MultipleCalls_PersistsAllRecordsIndependently()
    {
        // Arrange
        // Each call exercises a distinct DI scope and DbContext instance, verifying that the
        // Worker's per-call scope creation works correctly with a real IServiceScopeFactory.
        var results = new[]
        {
            new TemperatureAnalysisResult { SensorId = Guid.NewGuid(), TempCelsius = 20.0, Timestamp = DateTimeOffset.UtcNow, Status = TemperatureStatus.Normal },
            new TemperatureAnalysisResult { SensorId = Guid.NewGuid(), TempCelsius = 95.0, Timestamp = DateTimeOffset.UtcNow, Status = TemperatureStatus.Critical },
        };

        // Act
        foreach (var result in results)
            await _sut.HandleResultAsync(result);

        // Assert: both records were independently committed
        await using var assertDb = new AppDbContext(_fixture.DbContextOptions);
        var saved = await assertDb.TemperatureResults.OrderBy(r => r.Id).ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.Equal(results[0].SensorId, saved[0].SensorId);
        Assert.Equal(results[1].SensorId, saved[1].SensorId);
    }
}
