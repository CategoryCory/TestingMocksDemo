using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Temperature.Contracts;
using TemperatureMonitor.Contracts;

namespace TemperatureMonitor.Messaging;

/// <summary>
/// Implements a temperature reading publisher that publishes temperature readings to a RabbitMQ queue.
/// </summary>
public sealed class RabbitMqReadingPublisher : ITemperatureReadingPublisher, IAsyncDisposable
{
    /// <summary>
    /// The RabbitMQ channel used for publishing messages.
    /// </summary>
    private readonly IChannel _channel;

    /// <summary>
    /// The name of the RabbitMQ queue to which temperature readings will be published.
    /// </summary>
    private const string QueueName = "temperature_readings";

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqReadingPublisher"/> class.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel used for publishing messages.</param>
    private RabbitMqReadingPublisher(IChannel channel) => _channel = channel;

    /// <summary>
    /// Creates a new instance of the <see cref="RabbitMqReadingPublisher"/> class asynchronously.
    /// </summary>
    /// <param name="connection">The RabbitMQ connection used to create a channel.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="RabbitMqReadingPublisher"/>.</returns>
    public static async Task<RabbitMqReadingPublisher> CreateAsync(IConnection connection)
    {
        var channel = await connection.CreateChannelAsync();
        var publisher = new RabbitMqReadingPublisher(channel);
        await publisher.InitializeAsync();
        return publisher;
    }

    /// <summary>
    /// Initializes the RabbitMQ channel by declaring the queue to which temperature readings
    /// will be published.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task InitializeAsync()
    {
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    /// <summary>
    /// Publishes a temperature reading to the RabbitMQ queue by serializing it to JSON and sending
    /// it as a message body.
    /// </summary>
    /// <param name="reading">The temperature reading to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PublishAsync(TemperatureReading reading, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(reading);
        var body = Encoding.UTF8.GetBytes(json);
        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: QueueName,
            body: body,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Disposes the RabbitMQ channel asynchronously when the publisher is no longer needed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync() => await _channel.DisposeAsync();
}
