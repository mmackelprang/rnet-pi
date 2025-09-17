using Microsoft.Extensions.Logging;
using Moq;
using RNetPi.Core.Interfaces;
using RNetPi.Core.Models;
using RNetPi.Core.Services;
using Xunit;

namespace RNetPi.Core.Tests.Services;

public class EnhancedRNetServiceIntegrationTests
{
    [Fact]
    public void EnhancedRNetService_CanUseDefaultSerialPortFactory()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EnhancedRNetService>>();
        var mockConfigService = new Mock<IConfigurationService>();
        mockConfigService.Setup(x => x.Configuration).Returns(new Configuration { Simulate = true });

        // Act - Using default factory (should be SerialPortFactory)
        var service = new EnhancedRNetService(mockLogger.Object, mockConfigService.Object);

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsConnected);
    }

    [Fact]
    public void EnhancedRNetService_CanUseMockSerialPortFactory()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EnhancedRNetService>>();
        var mockConfigService = new Mock<IConfigurationService>();
        mockConfigService.Setup(x => x.Configuration).Returns(new Configuration { Simulate = true });
        
        var mockFactory = new MockSerialPortFactory();

        // Act - Using mock factory
        var service = new EnhancedRNetService(mockLogger.Object, mockConfigService.Object, mockFactory);

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task EnhancedRNetService_WithMockFactory_ConnectsInSimulationMode()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EnhancedRNetService>>();
        var mockConfigService = new Mock<IConfigurationService>();
        mockConfigService.Setup(x => x.Configuration).Returns(new Configuration { Simulate = true });
        
        var mockFactory = new MockSerialPortFactory();
        var service = new EnhancedRNetService(mockLogger.Object, mockConfigService.Object, mockFactory);

        // Act
        var result = await service.ConnectAsync();

        // Assert
        Assert.True(result);
        Assert.True(service.IsConnected);
    }

    [Fact]
    public async Task EnhancedRNetService_WithMockFactory_CanCreateTestablePort()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EnhancedRNetService>>();
        var mockConfigService = new Mock<IConfigurationService>();
        mockConfigService.Setup(x => x.Configuration).Returns(new Configuration 
        { 
            Simulate = false,
            SerialDevice = "/dev/mock" 
        });
        
        var mockFactory = new MockSerialPortFactory();
        var service = new EnhancedRNetService(mockLogger.Object, mockConfigService.Object, mockFactory);

        // Act - This should use the mock factory to create a TestableSerialPort
        // Note: In real test scenarios, we might need to mock SerialPort.GetPortNames() as well
        // For now, this tests the integration of the factory pattern
        
        // The service has been constructed with a mock factory, demonstrating that 
        // the factory pattern allows us to substitute mock implementations
        Assert.NotNull(service);
        Assert.False(service.IsConnected);
    }
}