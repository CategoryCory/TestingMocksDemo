using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Temperature.Contracts;
using TemperatureMonitor.Contracts;

namespace TemperatureMonitor.Tests.UnitTests;

public sealed class WorkerHandleResultTests
{
    // --- Mocks ---
    private readonly ITemperatureReadingGenerator _generator = Substitute.For<ITemperatureReadingGenerator>();
    private readonly ITemperatureReadingPublisher _publisher = Substitute.For<ITemperatureReadingPublisher>();
    private readonly ITemperatureResultConsumer _consumer = Substitute.For<ITemperatureResultConsumer>();
    private readonly IAlertService _alertService = Substitute.For<IAlertService>();
    private readonly ITemperatureResultRepository _repository = Substitute.For<ITemperatureResultRepository>();

    // IServiceScopeFactory requires a small chain of mocks so that
    // scope.ServiceProvider.GetRequiredService<ITemperatureResultRepository>() resolves correctly.
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    // --- System Under Test ---
    private readonly Worker _sut;

    public WorkerHandleResultTests()
    {
        // Wire up the scope factory chain
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);

        // GetRequiredService<T> is an extension method that internally calls GetService(typeof(T)),
        // so we mock the underlying IServiceProvider.GetService method.
        _serviceProvider
            .GetService(typeof(ITemperatureResultRepository))
            .Returns(_repository);

        _sut = new Worker(
            _generator,
            _publisher,
            _consumer,
            _alertService,
            _scopeFactory,
            NullLogger<Worker>.Instance);
    }

    [Fact]
    public async Task HandleResultAsync_AlwaysSavesResultToRepository()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 75.0,
            Timestamp = DateTime.UtcNow,
            Status = TemperatureStatus.Normal
        };

        // Act
        await _sut.HandleResultAsync(result);

        // Assert
        await _repository.Received(1).SaveAsync(result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleResultAsync_WhenStatusIsNormal_DoesNotRaiseAlert()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 75.0,
            Timestamp = DateTime.UtcNow,
            Status = TemperatureStatus.Normal
        };

        // Act
        await _sut.HandleResultAsync(result);

        // Assert
        await _alertService.DidNotReceive().RaiseAlertAsync(Arg.Any<TemperatureAnalysisResult>());
    }

    [Fact]
    public async Task HandleResultAsync_WhenStatusIsCritical_RaisesAlert()
    {
        // Arrange
        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 120.0,
            Timestamp = DateTime.UtcNow,
            Status = TemperatureStatus.Critical
        };

        // Act
        await _sut.HandleResultAsync(result);

        // Assert
        await _alertService.Received(1).RaiseAlertAsync(result);
    }

    [Fact]
    public async Task HandleResultAsync_WhenStatusIsCritical_SavesBeforeRaisingAlert()
    {
        // Arrange
        var callOrder = new List<string>();

        _repository
            .SaveAsync(Arg.Any<TemperatureAnalysisResult>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("save"));

        _alertService
            .RaiseAlertAsync(Arg.Any<TemperatureAnalysisResult>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("alert"));

        var result = new TemperatureAnalysisResult
        {
            SensorId = Guid.NewGuid(),
            TempCelsius = 95.0,
            Timestamp = DateTimeOffset.UtcNow,
            Status = TemperatureStatus.Critical
        };

        // Act
        await _sut.HandleResultAsync(result);

        // Assert
        Assert.Equal(["save", "alert"], callOrder);
    }
}
