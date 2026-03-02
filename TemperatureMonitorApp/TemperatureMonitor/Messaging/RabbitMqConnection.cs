using RabbitMQ.Client;

namespace TemperatureMonitor.Messaging;

/// <summary>
/// Provides a helper method to create a connection to RabbitMQ.
/// </summary>
public static class RabbitMqConnection
{
    /// <summary>
    /// Creates a new connection to RabbitMQ using the specified host name.
    /// </summary>
    /// <param name="hostName">The host name of the RabbitMQ server.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// created <see cref="IConnection"/>.
    /// </returns>
    public static async Task<IConnection> CreateAsync(string hostName)
    {
        var factory = new ConnectionFactory() { HostName = hostName };
        return await factory.CreateConnectionAsync();
    }
}
