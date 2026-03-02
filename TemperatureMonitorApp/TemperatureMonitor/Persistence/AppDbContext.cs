using Microsoft.EntityFrameworkCore;

namespace TemperatureMonitor.Persistence;

/// <summary>
/// Represents the application's database context for managing temperature analysis results.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the DbSet of temperature analysis results.
    /// </summary>
    public DbSet<TemperatureResultRecord> TemperatureResults { get; set; } = null!;

    /// <summary>
    /// Configures the model by defining the schema for the TemperatureResultRecord entity.
    /// </summary>
    /// <param name="builder">The model builder used to configure the entity.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<TemperatureResultRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.SensorId).IsRequired();
            entity.Property(e => e.TempCelsius).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        });
    }
}
