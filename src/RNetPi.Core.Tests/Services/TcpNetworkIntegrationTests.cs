using Microsoft.Extensions.Logging;
using RNetPi.Core.Interfaces;
using RNetPi.Core.Services;
using Xunit;

namespace RNetPi.Core.Tests.Services;

public class TcpNetworkIntegrationTests
{
    [Fact]
    public void TcpNetworkPortAdapter_CanBeUsedForTesting()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var factory = new NetworkPortFactory(loggerFactory, "Test Server", "127.0.0.1");
        var networkPort = factory.CreateNetworkPort();

        // Assert
        Assert.NotNull(networkPort);
        Assert.IsType<TcpNetworkPortAdapter>(networkPort);
        Assert.False(networkPort.IsConnected);
        
        // Dispose
        networkPort.Dispose();
    }

    [Fact]
    public async Task TcpNetworkPortAdapter_CanStartAndStop()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var factory = new NetworkPortFactory(loggerFactory, "Test Server", "127.0.0.1");
        var networkPort = factory.CreateNetworkPort();

        var started = false;
        networkPort.ClientConnected += (sender, clientId) => { };
        
        try
        {
            // Act - Start on a random available port
            await networkPort.StartAsync(0); // Port 0 means any available port
            started = networkPort.IsConnected;

            // Assert
            Assert.True(started);
        }
        finally
        {
            // Cleanup
            if (started)
            {
                await networkPort.StopAsync();
            }
            networkPort.Dispose();
        }
    }

    [Fact]
    public async Task CompareNetworkPortImplementations_MockVsTcp()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        var mockFactory = new MockNetworkPortFactory();
        var tcpFactory = new NetworkPortFactory(loggerFactory, "Test Server", "127.0.0.1");
        
        var mockPort = mockFactory.CreateNetworkPort();
        var tcpPort = tcpFactory.CreateNetworkPort();

        // Act & Assert - Both should implement INetworkPort
        Assert.IsAssignableFrom<INetworkPort>(mockPort);
        Assert.IsAssignableFrom<INetworkPort>(tcpPort);
        
        // Both should start as not connected
        Assert.False(mockPort.IsConnected);
        Assert.False(tcpPort.IsConnected);
        
        // Test mock port functionality
        await mockPort.StartAsync(3000);
        Assert.True(mockPort.IsConnected);
        await mockPort.StopAsync();
        Assert.False(mockPort.IsConnected);
        
        // Cleanup
        mockPort.Dispose();
        tcpPort.Dispose();
    }

    [Fact]
    public void TcpNetworkPortAdapter_HandlesEventsCorrectly()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var factory = new NetworkPortFactory(loggerFactory, "Test Server", "127.0.0.1");
        var networkPort = factory.CreateNetworkPort();

        var clientConnections = new List<string>();
        var clientDisconnections = new List<string>();
        var dataReceived = new List<NetworkDataEventArgs>();
        var errorsReceived = new List<Exception>();

        // Wire up events
        networkPort.ClientConnected += (sender, clientId) => clientConnections.Add(clientId);
        networkPort.ClientDisconnected += (sender, clientId) => clientDisconnections.Add(clientId);
        networkPort.DataReceived += (sender, args) => dataReceived.Add(args);
        networkPort.ErrorReceived += (sender, error) => errorsReceived.Add(error);

        // Assert - Events are wired up (no immediate events should fire)
        Assert.Empty(clientConnections);
        Assert.Empty(clientDisconnections);
        Assert.Empty(dataReceived);
        Assert.Empty(errorsReceived);
        
        // Cleanup
        networkPort.Dispose();
    }
}