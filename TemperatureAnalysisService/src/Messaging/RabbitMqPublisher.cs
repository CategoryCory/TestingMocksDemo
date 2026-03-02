using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace TemperatureAnalysisService.Messaging;

/// <summary>
/// Represents a publisher that can send messages to a RabbitMQ queue.
/// </summary>
public sealed class RabbitMqPublisher
{
    /// <summary>
    /// The RabbitMQ channel used for publishing messages.
    /// </summary>
    private readonly IChannel _channel;

    /// <summary>
    /// The name of the RabbitMQ queue to which messages will be published.
    /// </summary>
    private readonly string _queueName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqPublisher"/> class.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel to use for publishing messages.</param>
    /// <param name="queueName">The name of the RabbitMQ queue to which messages will be published.</param>
    public RabbitMqPublisher(IChannel channel, string queueName)
    {
        _channel = channel;
        _queueName = queueName;
    }

    /// <summary>
    /// Asynchronously initializes the publisher by declaring the target RabbitMQ queue.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        await _channel.QueueDeclareAsync(queue: _queueName,
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);
    }

    /// <summary>
    /// Asynchronously publishes a message to the RabbitMQ queue.
    /// </summary>
    /// <typeparam name="T">The type of the message to be published.</typeparam>
    /// <param name="message">The message to be published.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    public async Task PublishAsync<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(exchange: "",
                                         routingKey: _queueName,
                                         body: body);
    }
}
