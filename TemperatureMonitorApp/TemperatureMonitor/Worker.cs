using Temperature.Contracts;
using TemperatureMonitor.Contracts;

namespace TemperatureMonitor;

/// <summary>
/// A background worker service that generates temperature readings, publishes them, consumes
/// analysis results, saves them to a repository, and raises alerts for critical conditions.
/// </summary>
public sealed class Worker : BackgroundService
{
    /// <summary>
    /// The temperature reading generator used to produce temperature readings.
    /// </summary>
    private readonly ITemperatureReadingGenerator _generator;

    /// <summary>
    /// The temperature reading publisher used to publish generated temperature readings.
    /// </summary>
    private readonly ITemperatureReadingPublisher _publisher;

    /// <summary>
    /// The temperature result consumer used to consume temperature analysis results from a message queue.
    /// </summary>
    private readonly ITemperatureResultConsumer _consumer;

    /// <summary>
    /// The alert bus used to raise alerts when critical temperature conditions are detected.
    /// </summary>
    private readonly IAlertBus _alertBus;

    /// <summary>
    /// The scope factory used to resolve scoped dependencies within the singleton hosted service.
    /// </summary>
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// The logger instance used to log informational messages and alerts throughout the worker's operation.
    /// </summary>
    private readonly ILogger<Worker> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class with the specified dependencies.
    /// </summary>
    /// <param name="generator">The temperature reading generator.</param>
    /// <param name="publisher">The temperature reading publisher.</param>
    /// <param name="consumer">The temperature result consumer.</param>
    /// <param name="alertBus">The alert bus.</param>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="logger">The logger instance.</param>
    public Worker(
        ITemperatureReadingGenerator generator,
        ITemperatureReadingPublisher publisher,
        ITemperatureResultConsumer consumer,
        IAlertBus alertBus,
        IServiceScopeFactory scopeFactory,
        ILogger<Worker> logger)
    {
        _generator = generator;
        _publisher = publisher;
        _consumer = consumer;
        _alertBus = alertBus;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background worker's main logic.
    /// </summary>
    /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the background operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumer.StartConsumingAsync(HandleResultAsync, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
           var reading = _generator.Generate();

           _logger.LogInformation(
                "Publishing reading: SensorId={SensorId}, TempCelsius={TempCelsius}",
                reading.SensorId,
                reading.TempCelsius
           );

           await _publisher.PublishAsync(reading, stoppingToken);
           await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    /// <summary>
    /// Handles a consumed temperature analysis result by logging it, saving it to the repository, and
    /// raising an alert if the result indicates a critical temperature condition.
    /// </summary>
    /// <param name="result">The temperature analysis result to handle.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal async Task HandleResultAsync(TemperatureAnalysisResult result)
    {
        _logger.LogInformation(
            "Received result: SensorId={SensorId}, TempCelsius={TempCelsius}, Status={Status}, Timestamp={Timestamp}",
            result.SensorId,
            result.TempCelsius,
            result.Status,
            result.Timestamp
        );

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITemperatureResultRepository>();
        await repository.SaveAsync(result);

        if (result.Status == TemperatureStatus.Critical)
        {
            _alertBus.Raise(result);
        }
    }
}
