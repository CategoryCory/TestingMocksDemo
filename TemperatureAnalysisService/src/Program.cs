using RabbitMQ.Client;
using TemperatureAnalysisService.Messaging;
using TemperatureAnalysisService.Processing;

namespace TemperatureAnalysisService;

/// <summary>
/// The main entry point for the Temperature Analysis Service application.
/// </summary>
public class Program
{
    /// <summary>
    /// The main method that initializes and runs the Temperature Analysis Service application.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        const string rabbitHost = "localhost";
        const string inputQueue = "temperature_readings";
        const string outputQueue = "temperature_results";
        const double thresholdCelsius = 80.0;

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton(sp =>
            RabbitMqConnection.CreateAsync(rabbitHost).GetAwaiter().GetResult());
        
        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<RabbitMqConnection>().CreateChannelAsync().GetAwaiter().GetResult());
        
        builder.Services.AddSingleton(sp =>
        {
            var consumer = new RabbitMqConsumer(
                channel: sp.GetRequiredService<IChannel>(),
                queueName: inputQueue);
            
            consumer.InitializeAsync().GetAwaiter().GetResult();
            return consumer;
        });

        builder.Services.AddSingleton(sp =>
        {
            var publisher = new RabbitMqPublisher(
                channel: sp.GetRequiredService<IChannel>(),
                queueName: outputQueue);
            
            publisher.InitializeAsync().GetAwaiter().GetResult();
            return publisher;
        });

        builder.Services.AddSingleton(new TemperatureAnalyzer(thresholdCelsius));

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}
