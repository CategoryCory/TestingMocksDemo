using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client.Exceptions;
using TemperatureMonitor.Alerts;
using TemperatureMonitor.Contracts;
using TemperatureMonitor.Messaging;
using TemperatureMonitor.Persistence;
using TemperatureMonitor.Simulation;

namespace TemperatureMonitor;

/// <summary>
/// The main entry point for the Temperature Monitor application.
/// </summary>
public class Program
{
    /// <summary>
    /// The main method that configures and runs the Temperature Monitor application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var rabbitMqHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var connectionString = builder.Configuration.GetConnectionString("Default");

        const int maxRetryAttempts = 10;
        const int retryDelaySeconds = 5;

        ITemperatureReadingPublisher publisher = null!;
        ITemperatureResultConsumer consumer = null!;

        for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            try
            {
                var connection = await RabbitMqConnection.CreateAsync(rabbitMqHost);
                publisher = await RabbitMqReadingPublisher.CreateAsync(connection);
                consumer = await RabbitMqResultConsumer.CreateAsync(connection);
                break;
            }
            catch (BrokerUnreachableException ex) when (attempt < maxRetryAttempts)
            {
                Console.WriteLine($"[Attempt {attempt}/{maxRetryAttempts}] RabbitMQ not ready: {ex.Message}. Retrying in {retryDelaySeconds} seconds...");
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
            }
        }

        builder.Services.AddSingleton(publisher);
        builder.Services.AddSingleton(consumer);
        builder.Services.AddSingleton<IAlertService, LoggingAlertService>();
        builder.Services.AddSingleton<ITemperatureReadingGenerator, RandomTemperatureGenerator>();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddScoped<ITemperatureResultRepository, PostgresTemperatureResultRepository>();

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        await host.RunAsync();
    }
}
