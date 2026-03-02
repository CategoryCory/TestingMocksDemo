using TemperatureAnalysisService.Messaging;
using TemperatureAnalysisService.Processing;

namespace TemperatureAnalysisService;

/// <summary>
/// Represents a background worker that consumes temperature readings from a RabbitMQ queue.
/// </summary>
public class Worker : BackgroundService
{
    /// <summary>
    /// The RabbitMQ consumer used to receive temperature readings from the input queue.
    /// </summary>
    private readonly RabbitMqConsumer _consumer;

    /// <summary>
    /// The RabbitMQ publisher used to send temperature analysis results to the output queue.
    /// </summary>
    private readonly RabbitMqPublisher _publisher;

    /// <summary>
    /// The temperature analyzer used to analyze temperature readings and determine their status based on a predefined threshold.
    /// </summary>
    private readonly TemperatureAnalyzer _analyzer;

    /// <summary>
    /// The logger used to log information about the worker's operations and status.
    /// </summary>
    private readonly ILogger<Worker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="consumer">The RabbitMQ consumer used to receive temperature readings from the input queue.</param>
    /// <param name="publisher">The RabbitMQ publisher used to send temperature analysis results to the output queue.</param>
    /// <param name="analyzer">The temperature analyzer used to analyze temperature readings and determine their status based on a predefined threshold.</param>
    /// <param name="logger">The logger used to log information about the worker's operations and status.</param>
    public Worker(
        RabbitMqConsumer consumer,
        RabbitMqPublisher publisher,
        TemperatureAnalyzer analyzer,
        ILogger<Worker> logger)
    {
        _consumer = consumer;
        _analyzer = analyzer;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Asynchronously executes the worker's operations.
    /// </summary>
    /// <param name="stoppingToken">The token that signals when the worker should stop executing.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        await _consumer.StartConsumingAsync(async reading =>
        {
            var result = _analyzer.Analyze(reading);
            await _publisher.PublishAsync(result);
        });
    }
}
