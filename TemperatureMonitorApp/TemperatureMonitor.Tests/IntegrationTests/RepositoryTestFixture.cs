using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using TemperatureMonitor.Persistence;
using Testcontainers.PostgreSql;

namespace TemperatureMonitor.Tests.IntegrationTests;

/// <summary>
/// xUnit class fixture that manages the lifetime of a PostgreSQL Testcontainer and a Respawn
/// <see cref="Respawner"/> for the <see cref="RepositoryIntegrationTests"/> test class.
///
/// Lifecycle (xUnit IClassFixture):
///   - <see cref="InitializeAsync"/>: called once before the first test in the class runs.
///     Starts the container, creates the schema, and initialises the Respawner.
///   - <see cref="ResetAsync"/>: called by each test's own InitializeAsync to wipe all rows
///     and restart identity sequences before the test body executes.
///   - <see cref="DisposeAsync"/>: called once after the last test in the class completes.
/// </summary>
public class RepositoryTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.3-bookworm").Build();

    private Respawner _respawner = null!;

    public DbContextOptions<AppDbContext> DbContextOptions { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        DbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        // Create the schema once for the lifetime of the fixture.
        await using var db = new AppDbContext(DbContextOptions);
        await db.Database.EnsureCreatedAsync();

        // Respawner inspects the schema on creation so it knows the exact set of tables to
        // truncate. It must be initialised after EnsureCreatedAsync.
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres
        });
    }

    /// <summary>
    /// Truncates all tables and restarts identity sequences, returning the database to a blank
    /// state. Call this at the start of each test via the test class's own InitializeAsync.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
