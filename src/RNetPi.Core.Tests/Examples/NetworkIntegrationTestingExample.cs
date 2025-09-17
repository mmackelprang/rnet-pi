using Microsoft.Extensions.Logging;
using RNetPi.Core.Interfaces;
using RNetPi.Core.Services;
using Xunit;

namespace RNetPi.Core.Tests.Examples;

/// <summary>
/// Example demonstrating how to use both mock and real network port implementations for testing
/// </summary>
public class NetworkIntegrationTestingExample
{
    [Fact]
    public async Task Example_MockNetworkPort_ForUnitTesting()
    {
        // Arrange: Create a mock network port for isolated unit testing
        var mockFactory = new MockNetworkPortFactory();
        var networkPort = mockFactory.CreateNetworkPort() as TestableNetworkPort;
        Assert.NotNull(networkPort);

        var clientConnections = new List<string>();
        var dataReceived = new List<NetworkDataEventArgs>();

        networkPort.ClientConnected += (sender, clientId) => clientConnections.Add(clientId);
        networkPort.DataReceived += (sender, args) => dataReceived.Add(args);

        // Act: Simulate network activity without real network connections
        await networkPort.StartAsync(3000);
        
        // Simulate clients connecting
        networkPort.SimulateClientConnected("client1");
        networkPort.SimulateClientConnected("client2");
        
        // Simulate data received from clients
        var testData1 = new byte[] { 0x01, 0x02, 0x03 };
        var testData2 = new byte[] { 0x04, 0x05, 0x06 };
        
        networkPort.SimulateDataReceived("client1", testData1);
        networkPort.SimulateDataReceived("client2", testData2);
        
        // Send responses back to clients
        var responseData = new byte[] { 0xFF, 0xFE, 0xFD };
        await networkPort.SendToClientAsync("client1", responseData);
        await networkPort.SendToAllAsync(responseData);

        // Assert: Verify all network activity was tracked correctly
        Assert.Equal(2, clientConnections.Count);
        Assert.Contains("client1", clientConnections);
        Assert.Contains("client2", clientConnections);
        
        Assert.Equal(2, dataReceived.Count);
        Assert.Equal("client1", dataReceived[0].ClientId);
        Assert.Equal(testData1, dataReceived[0].Data);
        
        // Verify data sent to specific clients
        var client1Data = networkPort.GetClientSentData("client1");
        Assert.Equal(2, client1Data.Count); // Individual response + broadcast
        
        await networkPort.StopAsync();
        networkPort.Dispose();
    }

    [Fact] 
    public async Task Example_TcpNetworkPort_ForIntegrationTesting()
    {
        // Arrange: Create a real TCP network port for integration testing
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var tcpFactory = new NetworkPortFactory(loggerFactory, "Integration Test Server", "127.0.0.1");
        var networkPort = tcpFactory.CreateNetworkPort();

        var clientConnections = new List<string>();
        var clientDisconnections = new List<string>();
        var dataReceived = new List<NetworkDataEventArgs>();
        var errors = new List<Exception>();

        // Wire up events for integration testing
        networkPort.ClientConnected += (sender, clientId) =>
        {
            clientConnections.Add(clientId);
        };
        
        networkPort.ClientDisconnected += (sender, clientId) =>
        {
            clientDisconnections.Add(clientId);
        };
        
        networkPort.DataReceived += (sender, args) =>
        {
            dataReceived.Add(args);
        };
        
        networkPort.ErrorReceived += (sender, error) =>
        {
            errors.Add(error);
        };

        try
        {
            // Act: Start a real TCP server for integration testing
            await networkPort.StartAsync(0); // Use port 0 for any available port
            
            Assert.True(networkPort.IsConnected);
            
            // In a real integration test, you could:
            // 1. Connect real TCP clients to the server
            // 2. Send actual RNet protocol messages
            // 3. Verify the server processes them correctly
            // 4. Test error handling with malformed packets
            
            // For this example, we just verify the server started correctly
            Assert.Empty(errors); // No errors during startup
            
        }
        finally
        {
            // Cleanup: Always stop the server and dispose resources
            if (networkPort.IsConnected)
            {
                await networkPort.StopAsync();
            }
            networkPort.Dispose();
        }
    }

    [Fact]
    public void Example_ComparingMockAndRealImplementations()
    {
        // Arrange: Create both implementations
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        var mockFactory = new MockNetworkPortFactory();
        var tcpFactory = new NetworkPortFactory(loggerFactory, "Comparison Test", "127.0.0.1");
        
        var mockPort = mockFactory.CreateNetworkPort();
        var tcpPort = tcpFactory.CreateNetworkPort();

        // Act & Assert: Both implementations support the same INetworkPort interface
        
        // Both should implement the same interface
        Assert.IsAssignableFrom<INetworkPort>(mockPort);
        Assert.IsAssignableFrom<INetworkPort>(tcpPort);
        
        // Both should have consistent initial state
        Assert.False(mockPort.IsConnected);
        Assert.False(tcpPort.IsConnected);
        
        // Both should expose the same interface methods and events
        // We can verify by subscribing to events (which proves they exist)
        var mockEventSubscribed = false;
        var tcpEventSubscribed = false;
        
        mockPort.ClientConnected += (s, e) => mockEventSubscribed = true;
        tcpPort.ClientConnected += (s, e) => tcpEventSubscribed = true;
        
        // Events are properly wired up
        Assert.False(mockEventSubscribed); // No events fired yet
        Assert.False(tcpEventSubscribed); // No events fired yet

        // Key difference: Mock provides additional testing methods
        if (mockPort is TestableNetworkPort testablePort)
        {
            // Mock has simulation methods for testing - verify they exist by calling them
            testablePort.SimulateClientConnected("test-client");
            Assert.Single(testablePort.ConnectedClients);
            
            // Verify other testing methods exist
            testablePort.ClearSentData();
            var clientData = testablePort.GetClientSentData("test-client");
            Assert.Empty(clientData);
        }
        
        // Real implementation uses actual TCP networking
        if (tcpPort is TcpNetworkPortAdapter tcpAdapter)
        {
            // TCP adapter wraps real networking functionality
            Assert.NotNull(tcpAdapter);
        }

        // Cleanup
        mockPort.Dispose();
        tcpPort.Dispose();
    }

    [Fact]
    public async Task Example_NetworkPortFactoryUsage_InServices()
    {
        // Example of how services can use either factory for different testing scenarios
        
        // Production scenario: Use TCP factory for real networking
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var productionFactory = new NetworkPortFactory(loggerFactory, "Production Server");
        
        // Test scenario: Use mock factory for isolated testing
        var testFactory = new MockNetworkPortFactory();
        
        // Create services with different factories
        var productionNetworkPort = productionFactory.CreateNetworkPort();
        var testNetworkPort = testFactory.CreateNetworkPort();
        
        // Both support the same interface, enabling consistent service behavior
        Assert.IsAssignableFrom<INetworkPort>(productionNetworkPort);
        Assert.IsAssignableFrom<INetworkPort>(testNetworkPort);
        
        // Test port can be used for fast, isolated unit tests
        await testNetworkPort.StartAsync(3000);
        Assert.True(testNetworkPort.IsConnected);
        await testNetworkPort.StopAsync();
        
        // Production port would be used for real integration tests
        // (We skip actually starting it to avoid port conflicts in tests)
        
        // Cleanup
        productionNetworkPort.Dispose();
        testNetworkPort.Dispose();
    }
}