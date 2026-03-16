using TemperatureMonitor.Simulation;

namespace TemperatureMonitor.Tests.UnitTests;

public sealed class RandomTemperatureGeneratorTests
{
    private readonly RandomTemperatureGenerator _sut = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ReturnsReadingsWithUniqueSensorIds()
    {
        // Arrange
        const int numberOfReadings = 100;

        // Act
        var readings = Enumerable.Range(0, numberOfReadings)
            .Select(_ => _sut.Generate())
            .ToList();

        // Assert
        var uniqueSensorIds = readings.Select(r => r.SensorId).Distinct().Count();
        Assert.All(readings, r => Assert.NotEqual(Guid.Empty, r.SensorId));
        Assert.Equal(numberOfReadings, uniqueSensorIds);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ReturnsTempCelsiusWithinExpectedRange()
    {
        // Arrange
        const double min = -10.0;
        const double max = 100.0;

        // Act
        var reading = _sut.Generate();

        // Assert
        Assert.InRange(reading.TempCelsius, min, max);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ReturnsTempCelsiusWithinExpectedRangeAcrossManySamples()
    {
        // Arrange
        const int numberOfReadings = 1_000;
        const double min = -10.0;
        const double max = 100.0;

        // Act
        var readings = Enumerable.Range(0, numberOfReadings)
            .Select(_ => _sut.Generate())
            .ToList();

        // Assert
        Assert.All(readings, r => Assert.InRange(r.TempCelsius, min, max));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ReturnsTempCelsiusRoundedToTwoDecimalPlaces()
    {
        // Arrange
        const int numberOfReadings = 100;

        // Act
        var readings = Enumerable.Range(0, numberOfReadings)
            .Select(_ => _sut.Generate())
            .ToList();

        // Assert
        Assert.All(readings, r =>
            Assert.Equal(r.TempCelsius, Math.Round(r.TempCelsius, 2)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ReturnsReadingsWithCurrentTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var reading = _sut.Generate();

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(reading.Timestamp, before, after);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Generate_ReturnsTimestampWithUtcOffset()
    {
        // Act
        var reading = _sut.Generate();

        // Assert
        Assert.Equal(TimeSpan.Zero, reading.Timestamp.Offset);
    }
}
