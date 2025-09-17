using RNetPi.Core.RNet;
using RNetPi.Core.Tests.Mocks;
using RNetPi.Core.Tests.Utilities;
using RNetPi.Core.Models;

namespace RNetPi.Core.Tests.Integration;

/// <summary>
/// Integration tests demonstrating how network and serial mocks work together
/// for testing RNet communication scenarios
/// </summary>
public class RNetIntegrationTests
{
    [Fact]
    public async Task CompleteVolumeControlScenario_ShouldWorkWithMocks()
    {
        // This test demonstrates a complete volume control scenario using both
        // serial and network mocks to verify the protocol handling
        
        // Arrange - Setup mocks and zone
        var mockSerial = new MockSerialPort();
        var mockNetwork = new MockNetworkPort();
        var zone = new Zone(1, 2);
        var clientId = "test-client-1";
        
        mockSerial.Open();
        await mockNetwork.StartAsync(8080);
        mockNetwork.SimulateClientConnected(clientId);
        
        // Act - Simulate volume change command and response cycle
        
        // 1. Send volume command via serial
        var volumeCommand = new SetVolumePacket(0x01, 0x02, 75);
        mockSerial.Write(volumeCommand.GetBuffer());
        
        // 2. Simulate controller acknowledging volume change via serial response
        var volumeResponse = new ZoneVolumePacket();
        volumeResponse.SourceControllerID = 1;
        volumeResponse.SourcePath = new byte[] { 0x02, 0x00, 0x02, 0x01 }; // Zone 2
        volumeResponse.Data = new byte[] { 37 }; // Volume 74 (37*2)
        zone.SetVolume(volumeResponse.GetVolume(), true);
        
        // 3. Broadcast update to network clients
        var updateMessage = $"Zone {zone.ZoneID} volume changed to {zone.Volume}";
        var updateBytes = System.Text.Encoding.UTF8.GetBytes(updateMessage);
        await mockNetwork.SendToAllAsync(updateBytes);
        
        // Assert - Verify all components worked correctly
        
        // Verify serial command was sent
        Assert.Single(mockSerial.SentData);
        var sentSerial = mockSerial.LastSentData!;
        Assert.Equal(0xF0, sentSerial[0]); // Start byte
        Assert.Equal(0x01, sentSerial[1]); // Controller ID
        Assert.Equal(0xF7, sentSerial[^1]); // End byte
        
        // Verify zone model was updated from response
        Assert.Equal(74, zone.Volume); // 37 * 2 = 74
        
        // Verify network message was broadcast
        Assert.Single(mockNetwork.SentData);
        var sentNetwork = mockNetwork.SentData[0];
        var networkMessage = System.Text.Encoding.UTF8.GetString(sentNetwork);
        Assert.Equal("Zone 2 volume changed to 74", networkMessage);
        
        // Verify client received the message
        var clientMessages = mockNetwork.GetClientSentData(clientId);
        Assert.Single(clientMessages);
        Assert.Equal(updateBytes, clientMessages[0]);
    }

    [Fact]
    public void HexStringWorkflow_ShouldSupportTestingWithProtocolHex()
    {
        // This test demonstrates using hex strings for testing protocol messages
        
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();
        
        // Act - Send commands using hex strings
        var volumeHex = "F0 01 00 7F 00 00 70 05"; // Partial volume command
        var trebleHex = "F0 01 02 7F 00 00 70 00"; // Partial treble command
        
        mockSerial.SimulateDataReceived(volumeHex);
        mockSerial.SimulateDataReceived(trebleHex);
        
        // Also send real packets for comparison
        var volumePacket = new SetVolumePacket(0x01, 0x02, 50);
        var treblePacket = new SetTreblePacket(0x01, 0x02, 5);
        
        mockSerial.Write(volumePacket.GetBuffer());
        mockSerial.Write(treblePacket.GetBuffer());
        
        // Assert - Verify hex conversion utilities work correctly
        Assert.Equal(2, mockSerial.SentData.Count);
        
        // Convert sent data back to hex for verification
        var sentVolumeHex = HexUtils.ToHexString(mockSerial.SentData[0]);
        var sentTrebleHex = HexUtils.ToHexString(mockSerial.SentData[1]);
        
        // Verify we can round-trip hex conversions
        var volumeBytes = HexUtils.FromHexString(sentVolumeHex);
        var trebleBytes = HexUtils.FromHexString(sentTrebleHex);
        
        Assert.Equal(mockSerial.SentData[0], volumeBytes);
        Assert.Equal(mockSerial.SentData[1], trebleBytes);
        
        // Verify hex strings contain expected protocol markers
        Assert.StartsWith("F0", sentVolumeHex); // Start byte
        Assert.EndsWith("F7", sentVolumeHex); // End byte
        Assert.StartsWith("F0", sentTrebleHex); // Start byte  
        Assert.EndsWith("F7", sentTrebleHex); // End byte
    }

    [Fact]
    public void MultiControllerScenario_ShouldRouteCorrectly()
    {
        // This test demonstrates handling multiple controllers and zones
        
        // Arrange
        var mockSerial = new MockSerialPort();
        var controller1Zone1 = new Zone(1, 1);
        var controller1Zone2 = new Zone(1, 2);
        var controller2Zone1 = new Zone(2, 1);
        
        var zones = new[] { controller1Zone1, controller1Zone2, controller2Zone1 };
        var changedZones = new List<Zone>();
        
        foreach (var zone in zones)
        {
            zone.VolumeChanged += (v, r) => changedZones.Add(zone);
        }
        
        mockSerial.Open();
        
        // Act - Send commands to different controllers and zones
        var commands = new RNetPacket[]
        {
            new SetVolumePacket(0x01, 0x01, 25), // Controller 1, Zone 1
            new SetVolumePacket(0x01, 0x02, 50), // Controller 1, Zone 2  
            new SetVolumePacket(0x02, 0x01, 75), // Controller 2, Zone 1
            new SetTreblePacket(0x01, 0x01, 5),  // Controller 1, Zone 1 treble
        };
        
        foreach (var command in commands)
        {
            mockSerial.Write(command.GetBuffer());
        }
        
        // Simulate responses from controllers
        var responses = new[]
        {
            (1, 1, 25), // Controller 1, Zone 1, Volume 25
            (1, 2, 50), // Controller 1, Zone 2, Volume 50
            (2, 1, 75), // Controller 2, Zone 1, Volume 75
        };
        
        foreach (var (controllerId, zoneId, volume) in responses)
        {
            var response = new ZoneVolumePacket();
            response.SourceControllerID = (byte)controllerId;
            response.SourcePath = new byte[] { 0x02, 0x00, (byte)zoneId, 0x01 };
            response.Data = new byte[] { (byte)(volume / 2) };
            
            // Route to correct zone
            var targetZone = zones.FirstOrDefault(z => z.ControllerID == controllerId && z.ZoneID == zoneId);
            targetZone?.SetVolume(response.GetVolume(), true);
        }
        
        // Assert
        Assert.Equal(4, mockSerial.SentData.Count); // 4 commands sent
        Assert.Equal(3, changedZones.Count); // 3 volume changes
        
        // Verify each zone has correct volume (accounting for protocol conversion)
        Assert.Equal(24, controller1Zone1.Volume); // 25/2=12.5->12, 12*2=24
        Assert.Equal(50, controller1Zone2.Volume); // 50/2=25, 25*2=50
        Assert.Equal(74, controller2Zone1.Volume); // 75/2=37.5->37, 37*2=74
        
        // Verify command routing by checking controller IDs in sent packets
        var sentPackets = mockSerial.SentData.ToArray();
        Assert.Equal(0x01, sentPackets[0][1]); // Controller 1
        Assert.Equal(0x01, sentPackets[1][1]); // Controller 1
        Assert.Equal(0x02, sentPackets[2][1]); // Controller 2
        Assert.Equal(0x01, sentPackets[3][1]); // Controller 1 (treble)
    }
}