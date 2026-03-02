using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Temperature.Contracts;
using TemperatureMonitor.Contracts;

namespace TemperatureMonitor.Messaging;

/// <summary>
/// Implements a temperature result consumer that consumes temperature analysis results from a RabbitMQ
/// queue and processes them using a provided handler function.
/// </summary>
public class RabbitMqResultConsumer : ITemperatureResultConsumer, IAsyncDisposable
{
    /// <summary>
    /// The RabbitMQ channel used for consuming messages.
    /// </summary>
    private readonly IChannel _channel;

    /// <summary>
    /// The name of the RabbitMQ queue from which temperature analysis results will be consumed.
    /// </summary>
    private const string QueueName = "temperature_results";

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqResultConsumer"/> class.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel used for consuming messages.</param>
    private RabbitMqResultConsumer(IChannel channel) => _channel = channel;

    /// <summary>
    /// Creates a new instance of the <see cref="RabbitMqResultConsumer"/> class asynchronously.
    /// </summary>
    /// <param name="connection">The RabbitMQ connection used to create a channel.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="RabbitMqResultConsumer"/>.</returns>
    public static async Task<RabbitMqResultConsumer> CreateAsync(IConnection connection)
    {
        var channel = await connection.CreateChannelAsync();
        var consumer = new RabbitMqResultConsumer(channel);
        await consumer.InitializeAsync();
        return consumer;
    }

    /// <summary>
    /// Initializes the RabbitMQ channel by declaring the queue from which temperature analysis results
    /// will be consumed.
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
    /// Starts consuming temperature analysis results from the RabbitMQ queue and processes them using the provided handler function.
    /// </summary>
    /// <param name="handler">The function to handle each consumed temperature analysis result.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartConsumingAsync(Func<TemperatureAnalysisResult, Task> handler, CancellationToken cancellationToken = default)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var result = JsonSerializer.Deserialize<TemperatureAnalysisResult>(json);

            if (result is not null)
            {
                await handler(result);
            }

            await _channel.BasicAckAsync(deliveryTag: args.DeliveryTag, multiple: false);
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Disposes the RabbitMQ channel asynchronously when the consumer is no longer needed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync() => await _channel.DisposeAsync();
}
