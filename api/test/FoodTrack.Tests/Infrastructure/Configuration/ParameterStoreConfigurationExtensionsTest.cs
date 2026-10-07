using FluentAssertions;
using FoodTrack.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace FoodTrack.Tests.Infrastructure.Configuration;

public sealed class ParameterStoreConfigurationExtensionsTest
{
    [Fact]
    public void ReadPath_ReturnsTheConfiguredPath()
    {
        // Arrange
        var configuration = BuildConfiguration("/food-track/api/");

        // Act
        var path = ParameterStoreConfigurationExtensions.ReadPath(configuration);

        // Assert
        path.Should().Be("/food-track/api/");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReadPath_ReturnsNull_WhenNoPathIsConfigured(string? configuredPath)
    {
        // Arrange
        var configuration = BuildConfiguration(configuredPath);

        // Act
        var path = ParameterStoreConfigurationExtensions.ReadPath(configuration);

        // Assert
        path.Should().BeNull();
    }

    [Fact]
    public void AddParameterStoreWhenConfigured_AddsNoSource_WhenNoPathIsConfigured()
    {
        // Arrange
        var configuration = new ConfigurationManager();
        var sourceCountBefore = configuration.Sources.Count;

        // Act
        configuration.AddParameterStoreWhenConfigured();

        // Assert
        configuration.Sources.Should().HaveCount(sourceCountBefore);
    }

    private static IConfiguration BuildConfiguration(string? configuredPath)
    {
        var settings = new Dictionary<string, string?> { [ParameterStoreConfigurationExtensions.PathKey] = configuredPath };

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }
}
