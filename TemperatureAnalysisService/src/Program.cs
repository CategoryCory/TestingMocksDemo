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
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
            
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton(
            new TemperatureAnalyzer(thresholdCelsius: configuration.GetValue<double>("ThresholdCelsius")));

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}
