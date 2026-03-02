using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TemperatureAnalysisService.Contracts;

namespace TemperatureAnalysisService.Messaging;

/// <summary>
/// Represents a consumer that can receive messages from a RabbitMQ queue.
/// </summary>
public sealed class RabbitMqConsumer
{
    /// <summary>
    /// The RabbitMQ channel used for consuming messages.
    /// </summary>
    private readonly IChannel _channel;

    /// <summary>
    /// The name of the RabbitMQ queue from which messages will be consumed.
    /// </summary>
    private readonly string _queueName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqConsumer"/> class.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel to use for consuming messages.</param>
    /// <param name="queueName">The name of the RabbitMQ queue from which messages will be consumed.</param>
    public RabbitMqConsumer(IChannel channel, string queueName)
    {
        _channel = channel;
        _queueName = queueName;
    }

    /// <summary>
    /// Asynchronously initializes the consumer by declaring the target RabbitMQ queue.
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
    /// Asynchronously starts consuming messages from the RabbitMQ queue.
    /// </summary>
    /// <param name="handler">The handler to be invoked for each received message.</param>
    /// <returns>A task that represents the asynchronous consuming operation.</returns>
    public async Task StartConsumingAsync(Func<TemperatureReading, Task> handler)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var reading = JsonSerializer.Deserialize<TemperatureReading>(json);

            if (reading is not null && reading.SensorId != Guid.Empty)
            {
                await handler(reading);
            }

            await _channel.BasicAckAsync(deliveryTag: args.DeliveryTag, multiple: false);
        };

        await _channel.BasicConsumeAsync(queue: _queueName,
                                         autoAck: false,
                                         consumer: consumer);
    }
}
