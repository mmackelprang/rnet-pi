using RNetPi.Core.Interfaces;
using RNetPi.Core.Services;
using Xunit;

namespace RNetPi.Core.Tests.Services;

public class NetworkPortFactoryTests
{
    [Fact]
    public void MockNetworkPortFactory_CreateNetworkPort_ReturnsTestableNetworkPort()
    {
        // Arrange
        var factory = new MockNetworkPortFactory();

        // Act
        var networkPort = factory.CreateNetworkPort();

        // Assert
        Assert.NotNull(networkPort);
        Assert.IsType<TestableNetworkPort>(networkPort);
        Assert.False(networkPort.IsConnected);
    }

    [Fact]
    public async Task TestableNetworkPort_CanSimulateClientConnections()
    {
        // Arrange
        var factory = new MockNetworkPortFactory();
        var networkPort = factory.CreateNetworkPort() as TestableNetworkPort;
        Assert.NotNull(networkPort);

        var connectedClients = new List<string>();
        networkPort.ClientConnected += (sender, clientId) => connectedClients.Add(clientId);

        // Act
        networkPort.SimulateClientConnected("client1");
        networkPort.SimulateClientConnected("client2");

        // Assert
        Assert.Equal(2, connectedClients.Count);
        Assert.Contains("client1", connectedClients);
        Assert.Contains("client2", connectedClients);
        Assert.Equal(2, networkPort.ConnectedClients.Count);
    }

    [Fact]
    public async Task TestableNetworkPort_TracksDataSentToClients()
    {
        // Arrange
        var factory = new MockNetworkPortFactory();
        var networkPort = factory.CreateNetworkPort() as TestableNetworkPort;
        Assert.NotNull(networkPort);

        networkPort.SimulateClientConnected("client1");
        
        // Act
        var testData = new byte[] { 0x01, 0x02, 0x03 };
        await networkPort.SendToClientAsync("client1", testData);

        // Assert
        var clientData = networkPort.GetClientSentData("client1");
        Assert.Single(clientData);
        Assert.Equal(testData, clientData[0]);
    }

    [Fact]
    public async Task TestableNetworkPort_TracksDataSentToAll()
    {
        // Arrange
        var factory = new MockNetworkPortFactory();
        var networkPort = factory.CreateNetworkPort() as TestableNetworkPort;
        Assert.NotNull(networkPort);

        networkPort.SimulateClientConnected("client1");
        networkPort.SimulateClientConnected("client2");
        
        // Act
        var testData = new byte[] { 0x01, 0x02, 0x03 };
        await networkPort.SendToAllAsync(testData);

        // Assert
        Assert.Single(networkPort.SentData);
        Assert.Equal(testData, networkPort.SentData[0]);
        
        // Both clients should have received the data
        Assert.Single(networkPort.GetClientSentData("client1"));
        Assert.Single(networkPort.GetClientSentData("client2"));
    }

    [Fact]
    public void TestableNetworkPort_CanSimulateDataReceived()
    {
        // Arrange
        var factory = new MockNetworkPortFactory();
        var networkPort = factory.CreateNetworkPort() as TestableNetworkPort;
        Assert.NotNull(networkPort);

        var receivedData = new List<NetworkDataEventArgs>();
        networkPort.DataReceived += (sender, args) => receivedData.Add(args);
        
        networkPort.SimulateClientConnected("client1");

        // Act
        var testData = new byte[] { 0x01, 0x02, 0x03 };
        networkPort.SimulateDataReceived("client1", testData);

        // Assert
        Assert.Single(receivedData);
        Assert.Equal("client1", receivedData[0].ClientId);
        Assert.Equal(testData, receivedData[0].Data);
    }
}