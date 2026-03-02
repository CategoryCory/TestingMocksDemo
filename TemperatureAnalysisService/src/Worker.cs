using TemperatureAnalysisService.Messaging;
using TemperatureAnalysisService.Processing;

namespace TemperatureAnalysisService;

/// <summary>
/// Represents a background worker that consumes temperature readings from a RabbitMQ queue.
/// </summary>
public class Worker : BackgroundService
{
    /// <summary>
    /// The temperature analyzer used to analyze temperature readings and determine their status based on a predefined threshold.
    /// </summary>
    private readonly TemperatureAnalyzer _analyzer;

    /// <summary>
    /// The logger used to log information about the worker's operations and status.
    /// </summary>
    private readonly ILogger<Worker> _logger;

    /// <summary>
    /// The RabbitMQ host address used to connect to the RabbitMQ server for consuming and publishing messages.
    /// </summary>
    private readonly string _rabbitHost;

    /// <summary>
    /// The name of the RabbitMQ queue from which temperature readings will be consumed.
    /// </summary>
    private const string InputQueue = "temperature_readings";

    /// <summary>
    /// The name of the RabbitMQ queue to which temperature analysis results will be published.
    /// </summary>
    private const string OutputQueue = "temperature_results";

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="analyzer">The temperature analyzer used to analyze temperature readings and determine their status based on a predefined threshold.</param>
    /// <param name="logger">The logger used to log information about the worker's operations and status.</param>
    public Worker(TemperatureAnalyzer analyzer, ILogger<Worker> logger)
    {
        _analyzer = analyzer;
        _logger = logger;
        _rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    }

    /// <summary>
    /// Asynchronously executes the worker's operations.
    /// </summary>
    /// <param name="stoppingToken">The token that signals when the worker should stop executing.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to RabbitMQ at {Host}...", _rabbitHost);

                await using var connection = await RabbitMqConnection.CreateAsync(_rabbitHost);

                var channel = await connection.CreateChannelAsync();

                var consumer = new RabbitMqConsumer(channel, InputQueue);
                var publisher = new RabbitMqPublisher(channel, OutputQueue);

                await consumer.InitializeAsync();
                await publisher.InitializeAsync();

                _logger.LogInformation("Connected to RabbitMQ at {Host}. Starting to consume messages...", _rabbitHost);

                await consumer.StartConsumingAsync(async reading =>
                {
                    var result = _analyzer.Analyze(reading);
                    await publisher.PublishAsync(result);
                });

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ not ready. Retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
