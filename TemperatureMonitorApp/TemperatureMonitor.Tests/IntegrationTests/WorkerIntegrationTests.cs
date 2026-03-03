using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Temperature.Contracts;
using TemperatureMonitor.Contracts;
using TemperatureMonitor.Persistence;
using Testcontainers.PostgreSql;

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
public class WorkerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3-bookworm").Build();

    // IAlertService is mocked — it sits outside the integration boundary
    private readonly IAlertService _alertService = Substitute.For<IAlertService>();

    private DbContextOptions<AppDbContext> _dbContextOptions = null!;
    private ServiceProvider _serviceProvider = null!;
    private Worker _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        // Create the schema
        await using var initDb = new AppDbContext(_dbContextOptions);
        await initDb.Database.EnsureCreatedAsync();

        // Build a real service provider so Worker gets a genuine IServiceScopeFactory
        // backed by real EF Core and real Postgres
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));
        services.AddScoped<ITemperatureResultRepository, PostgresTemperatureResultRepository>();
        _serviceProvider = services.BuildServiceProvider();

        _sut = new Worker(
            generator:     Substitute.For<ITemperatureReadingGenerator>(),
            publisher:     Substitute.For<ITemperatureReadingPublisher>(),
            consumer:      Substitute.For<ITemperatureResultConsumer>(),
            alertService:  _alertService,
            scopeFactory:  _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            logger:        NullLogger<Worker>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task HandlResultAsync_PersistsResultToRealDatabase()
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
        await using var assertDb = new AppDbContext(_dbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();

        Assert.Equal(result.SensorId, saved.SensorId);
        Assert.Equal(result.TempCelsius, saved.TempCelsius);
        Assert.Equal("Normal", saved.Status);
    }

    [Fact]
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
        await using var assertDb = new AppDbContext(_dbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();
        Assert.Equal("Critical", saved.Status);

        // Assert: alert boundary was triggered (mocked)
        await _alertService.Received(1).RaiseAlertAsync(result);
    }

    [Fact]
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
        await using var assertDb = new AppDbContext(_dbContextOptions);
        var saved = await assertDb.TemperatureResults.SingleAsync();
        Assert.Equal("Normal", saved.Status);

        // Assert: alert boundary was NOT triggered
        await _alertService.DidNotReceive().RaiseAlertAsync(Arg.Any<TemperatureAnalysisResult>());
    }
}
