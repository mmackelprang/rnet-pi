using Microsoft.Extensions.Logging;
using Moq;
using RNetPi.Core.Interfaces;
using RNetPi.Core.Models;
using RNetPi.Core.Services;
using Xunit;

namespace RNetPi.Core.Tests.Examples;

/// <summary>
/// Example demonstrating how to use the factory pattern for enhanced integration testing
/// </summary>
public class IntegrationTestingExample
{
    [Fact]
    public async Task Example_EnhancedIntegrationTesting_WithMockFactories()
    {
        // Arrange: Set up mock dependencies
        var mockLogger = new Mock<ILogger<RNetService>>();
        var mockConfigService = new Mock<IConfigurationService>();
        mockConfigService.Setup(x => x.Configuration).Returns(new Configuration 
        { 
            Simulate = false,
            SerialDevice = "/dev/test" 
        });

        // Create mock factories for testing
        var mockSerialFactory = new MockSerialPortFactory();
        var mockNetworkFactory = new MockNetworkPortFactory();

        // Create service with mock factories
        var service = new RNetService(mockLogger.Object, mockConfigService.Object, mockSerialFactory);

        // Act & Assert: Test connection with simulation mode
        mockConfigService.Setup(x => x.Configuration).Returns(new Configuration { Simulate = true });
        var connected = await service.ConnectAsync();

        Assert.True(connected);
        Assert.True(service.IsConnected);

        // Demonstrate cleanup
        await service.DisconnectAsync();
        Assert.False(service.IsConnected);
    }

    [Fact]
    public void Example_SerialPortTesting_WithDataSimulation()
    {
        // Arrange: Create a testable serial port
        var factory = new MockSerialPortFactory();
        var serialPort = factory.CreateSerialPort("/dev/test") as TestableSerialPort;
        Assert.NotNull(serialPort);

        var receivedMessages = new List<byte[]>();
        serialPort.DataReceived += (sender, data) => receivedMessages.Add(data);

        // Act: Simulate opening the port and receiving data
        serialPort.Open();
        
        var testMessage1 = new byte[] { 0xF0, 0x00, 0x01, 0xF7 }; // Example RNet message
        var testMessage2 = new byte[] { 0xF0, 0x00, 0x02, 0xF7 }; // Another example message
        
        serialPort.SimulateDataReceived(testMessage1);
        serialPort.SimulateDataReceived(testMessage2);

        // Simulate sending data to the serial port
        var commandMessage = new byte[] { 0xF0, 0x7F, 0x00, 0x01, 0xF7 };
        serialPort.Write(commandMessage);

        // Assert: Verify both received and sent data
        Assert.Equal(2, receivedMessages.Count);
        Assert.Equal(testMessage1, receivedMessages[0]);
        Assert.Equal(testMessage2, receivedMessages[1]);

        Assert.Single(serialPort.SentData);
        Assert.Equal(commandMessage, serialPort.SentData[0]);
        Assert.Equal(commandMessage, serialPort.LastSentData);
    }

    [Fact]
    public async Task Example_NetworkPortTesting_WithClientSimulation()
    {
        // Arrange: Create a testable network port
        var factory = new MockNetworkPortFactory();
        var networkPort = factory.CreateNetworkPort() as TestableNetworkPort;
        Assert.NotNull(networkPort);

        var connectedClients = new List<string>();
        var disconnectedClients = new List<string>();
        var receivedData = new List<NetworkDataEventArgs>();

        networkPort.ClientConnected += (sender, clientId) => connectedClients.Add(clientId);
        networkPort.ClientDisconnected += (sender, clientId) => disconnectedClients.Add(clientId);
        networkPort.DataReceived += (sender, args) => receivedData.Add(args);

        // Act: Start the network port and simulate client connections
        await networkPort.StartAsync(3000);
        Assert.True(networkPort.IsConnected);

        // Simulate multiple clients connecting
        networkPort.SimulateClientConnected("client1");
        networkPort.SimulateClientConnected("client2");
        networkPort.SimulateClientConnected("client3");

        // Simulate data received from clients
        var clientMessage1 = new byte[] { 0x01, 0x02, 0x03 };
        var clientMessage2 = new byte[] { 0x04, 0x05, 0x06 };
        
        networkPort.SimulateDataReceived("client1", clientMessage1);
        networkPort.SimulateDataReceived("client2", clientMessage2);

        // Send data to specific clients and all clients
        var responseMessage = new byte[] { 0xFF, 0xFE, 0xFD };
        var broadcastMessage = new byte[] { 0xAA, 0xBB, 0xCC };
        
        await networkPort.SendToClientAsync("client1", responseMessage);
        await networkPort.SendToAllAsync(broadcastMessage);

        // Simulate a client disconnecting
        networkPort.SimulateClientDisconnected("client3");

        // Assert: Verify all network activity was tracked correctly
        Assert.Equal(3, connectedClients.Count);
        Assert.Single(disconnectedClients);
        Assert.Equal("client3", disconnectedClients[0]);

        Assert.Equal(2, receivedData.Count);
        Assert.Equal("client1", receivedData[0].ClientId);
        Assert.Equal(clientMessage1, receivedData[0].Data);

        // Verify data sent to specific client
        var client1Data = networkPort.GetClientSentData("client1");
        Assert.Equal(2, client1Data.Count); // Response + broadcast
        Assert.Equal(responseMessage, client1Data[0]);
        Assert.Equal(broadcastMessage, client1Data[1]);

        // Verify broadcast data
        Assert.Single(networkPort.SentData);
        Assert.Equal(broadcastMessage, networkPort.SentData[0]);

        // Cleanup
        await networkPort.StopAsync();
        Assert.False(networkPort.IsConnected);
    }
}