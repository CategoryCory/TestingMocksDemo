using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Temperature.Contracts;
using TemperatureMonitor.Contracts;
using TemperatureMonitor.Persistence;
using Testcontainers.PostgreSql;

namespace TemperatureMonitor.Tests.IntegrationTests;

public class IntentionalFailureDemo : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3-bookworm").Build();

    private ServiceProvider _serviceProvider = null!;
    private Worker _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // NOTE: EnsureCreatedAsync is deliberately omitted.
        // The container is running, but the TemperatureResults table does not exist.
        // This simulates a real deployment scenario where the schema is out of sync
        // with the application — a common real-world failure mode.

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));
        services.AddScoped<ITemperatureResultRepository, PostgresTemperatureResultRepository>();
        _serviceProvider = services.BuildServiceProvider();

        _sut = new Worker(
            generator:    Substitute.For<ITemperatureReadingGenerator>(),
            publisher:    Substitute.For<ITemperatureReadingPublisher>(),
            consumer:     Substitute.For<ITemperatureResultConsumer>(),
            alertBus: Substitute.For<IAlertBus>(),
            scopeFactory: _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            logger:       NullLogger<Worker>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // [Fact]
    [Fact(Skip = "This test is intentionally designed to fail to demonstrate how integration test failures report rich diagnostic information.")]
    public async Task DEMO_IntentionalFailure_SchemaNotCreated()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 55.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        };

        // Act — no Assert.ThrowsAsync, no try/catch.
        // When this fails, xUnit will report something like:
        //
        //   Microsoft.EntityFrameworkCore.DbUpdateException:
        //     An error occurred while saving the entity changes. See the inner exception for details.
        //     ---> Npgsql.PostgresException (0x80004005): 42P01: relation "TemperatureResults" does not exist
        //          POSITION: 13
        //       at Npgsql.Internal.NpgsqlConnector.<ReadMessage>...
        //       at Npgsql.NpgsqlCommand.<ExecuteReader>...
        //       at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReader...
        //       at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.Execute...
        //    --- End of inner exception stack trace ---
        //       at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync...
        //       at TemperatureMonitor.Persistence.PostgresTemperatureResultRepository.SaveAsync...
        //       at TemperatureMonitor.Worker.HandleResultAsync...
        //       at TemperatureMonitor.Tests.IntegrationTests.IntentionalFailureDemo.DEMO_IntentionalFailure_SchemaNotCreated...
        //
        // Every layer of the call stack is present. The developer can see immediately:
        //   - WHAT failed:  DbUpdateException → PostgresException
        //   - WHERE it failed: Worker → Repository → EF Core → Npgsql → PostgreSQL
        //   - WHY it failed: relation "TemperatureResults" does not exist
        await _sut.HandleResultAsync(result);
    }
}
