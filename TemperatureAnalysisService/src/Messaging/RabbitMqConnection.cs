using RabbitMQ.Client;

namespace TemperatureAnalysisService.Messaging;

/// <summary>
/// Represents a connection to a RabbitMQ server, providing methods to create channels for communication.
/// </summary>
public sealed class RabbitMqConnection : IAsyncDisposable
{
    /// <summary>
    /// The underlying RabbitMQ connection used to create channels for communication with the server.
    /// </summary>
    private readonly IConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqConnection"/> class with the specified RabbitMQ connection.
    /// </summary>
    /// <param name="connection">The underlying RabbitMQ connection to use for creating channels.</param>
    private RabbitMqConnection(IConnection connection) => _connection = connection;

    /// <summary>
    /// Asynchronously creates a new instance of the <see cref="RabbitMqConnection"/> class.
    /// </summary>
    /// <param name="hostName">The hostname of the RabbitMQ server to connect to.</param>
    /// <returns>
    /// A task that represents the asynchronous operation and returns a new <see cref="RabbitMqConnection"/> instance.
    /// </returns>
    public static async Task<RabbitMqConnection> CreateAsync(string hostName = "localhost")
    {
        var factory = new ConnectionFactory() { HostName = hostName };
        var connection = await factory.CreateConnectionAsync();

        return new RabbitMqConnection(connection);
    }

    /// <summary>
    /// Asynchronously creates a new channel for communication with the RabbitMQ server.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation and returns a new <see cref="IChannel"/> instance.
    /// </returns>
    public Task<IChannel> CreateChannelAsync()
        => _connection.CreateChannelAsync();

    /// <summary>
    /// Asynchronously disposes of the RabbitMQ connection, releasing any resources associated with it.
    /// </summary>
    /// <returns>A task that represents the asynchronous disposal operation.</returns>
    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
