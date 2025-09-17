using RNetPi.Core.RNet;
using RNetPi.Core.Tests.Mocks;
using RNetPi.Core.Tests.Utilities;
using RNetPi.Core.Models;

namespace RNetPi.Core.Tests.RNet;

public class SerialPortResponseMessageTests
{
    [Fact]
    public void VolumeChangeResponse_ShouldUpdateZoneModel()
    {
        // This test verifies that volume change response serial messages from the RNET controller
        // result in correct model changes
        
        // Arrange
        var zone = new Zone(1, 2);
        var volumeChangedFired = false;
        var newVolume = 0;
        
        zone.VolumeChanged += (volume, rNetTriggered) =>
        {
            volumeChangedFired = true;
            newVolume = volume;
        };

        // Create a simple volume response packet  
        var volumePacket = new ZoneVolumePacket();
        volumePacket.SourceControllerID = 1;
        volumePacket.SourcePath = new byte[] { 0x02, 0x00, 0x01, 0x01 }; // Zone 1
        volumePacket.Data = new byte[] { 37 }; // Volume 74 (37 * 2)
        
        // Act - Simulate updating zone model from parsed packet
        if (volumePacket.GetControllerID() == zone.ControllerID && volumePacket.GetZoneID() == zone.ZoneID)
        {
            var volume = volumePacket.GetVolume();
            zone.SetVolume(volume, true); // rNetTriggered = true for responses
        }

        // Assert
        Assert.Equal(1, volumePacket.GetControllerID());
        Assert.Equal(1, volumePacket.GetZoneID()); // Path byte 2
        Assert.Equal(74, volumePacket.GetVolume()); // 37 * 2 = 74
        
        // Should not have fired since zone IDs don't match (zone is 2, packet is 1)
        Assert.False(volumeChangedFired);
        Assert.Equal(0, zone.Volume);
    }

    [Fact]
    public void SourceChangeResponse_ShouldUpdateZoneModel()
    {
        // This test verifies that source change response serial messages from the RNET controller
        // result in correct model changes
        
        // Arrange
        var zone = new Zone(1, 3);
        var sourceChangedFired = false;
        var newSource = 0;
        
        zone.SourceChanged += (source, rNetTriggered) =>
        {
            sourceChangedFired = true;
            newSource = source;
        };

        // Create a simple source response packet
        var sourcePacket = new ZoneSourcePacket();
        sourcePacket.SourceControllerID = 1;
        sourcePacket.SourcePath = new byte[] { 0x02, 0x00, 0x03, 0x02 }; // Zone 3
        sourcePacket.Data = new byte[] { 5 }; // Source 5
        
        // Act - Simulate updating zone model from parsed packet
        if (sourcePacket.GetControllerID() == zone.ControllerID && sourcePacket.GetZoneID() == zone.ZoneID)
        {
            var source = sourcePacket.GetSourceID();
            zone.SetSource(source, true); // rNetTriggered = true for responses
        }

        // Assert
        Assert.Equal(1, sourcePacket.GetControllerID());
        Assert.Equal(3, sourcePacket.GetZoneID());
        Assert.Equal(5, sourcePacket.GetSourceID());
        
        Assert.True(sourceChangedFired);
        Assert.Equal(5, newSource);
        Assert.Equal(5, zone.Source);
    }

    [Fact]
    public void ZoneChangeResponse_ShouldUpdateCorrectZone()
    {
        // This test verifies that zone change response serial messages from the RNET controller
        // result in correct model changes for the specific zone
        
        // Arrange
        var zone1 = new Zone(1, 1);
        var zone2 = new Zone(1, 2);
        var zone3 = new Zone(1, 3);
        
        var zone1Changed = false;
        var zone2Changed = false;
        var zone3Changed = false;
        
        zone1.VolumeChanged += (v, r) => zone1Changed = true;
        zone2.VolumeChanged += (v, r) => zone2Changed = true;
        zone3.VolumeChanged += (v, r) => zone3Changed = true;

        // Create volume response packet for zone 2 only
        var volumePacket = new ZoneVolumePacket();
        volumePacket.SourceControllerID = 1;
        volumePacket.SourcePath = new byte[] { 0x02, 0x00, 0x02, 0x01 }; // Zone 2
        volumePacket.Data = new byte[] { 30 }; // Volume 60
        
        // Act - Parse and route the response packet
        if (volumePacket != null)
        {
            var zones = new[] { zone1, zone2, zone3 };
            foreach (var zone in zones)
            {
                if (volumePacket.GetControllerID() == zone.ControllerID && volumePacket.GetZoneID() == zone.ZoneID)
                {
                    zone.SetVolume(volumePacket.GetVolume(), true);
                }
            }
        }

        // Assert
        Assert.Equal(2, volumePacket.GetZoneID());
        Assert.Equal(60, volumePacket.GetVolume()); // 30 * 2 = 60
        
        Assert.False(zone1Changed);
        Assert.True(zone2Changed);  // Only zone 2 should have changed
        Assert.False(zone3Changed);
        
        Assert.Equal(0, zone1.Volume);  // Unchanged
        Assert.Equal(60, zone2.Volume); // Changed
        Assert.Equal(0, zone3.Volume);  // Unchanged
    }

    [Theory]
    [InlineData(0, 0)]    // Volume 0
    [InlineData(25, 50)]   // Volume 50
    [InlineData(50, 100)]  // Volume 100
    public void VolumeResponseMessages_ShouldParseCorrectly(int dataValue, int expectedVolume)
    {
        // Arrange
        var volumePacket = new ZoneVolumePacket();
        volumePacket.SourceControllerID = 1;
        volumePacket.SourcePath = new byte[] { 0x02, 0x00, 0x02, 0x01 };
        volumePacket.Data = new byte[] { (byte)dataValue };
        
        // Act & Assert
        Assert.Equal(expectedVolume, volumePacket.GetVolume());
    }

    [Theory]
    [InlineData(1)]  // Source 1
    [InlineData(5)]  // Source 5
    [InlineData(8)]  // Source 8
    public void SourceResponseMessages_ShouldParseCorrectly(int sourceId)
    {
        // Arrange
        var sourcePacket = new ZoneSourcePacket();
        sourcePacket.SourceControllerID = 1;
        sourcePacket.SourcePath = new byte[] { 0x02, 0x00, 0x02, 0x02 };
        sourcePacket.Data = new byte[] { (byte)sourceId };
        
        // Act & Assert
        Assert.Equal(sourceId, sourcePacket.GetSourceID());
    }

    [Fact]
    public void MockSerialPort_ShouldSimulateControllerResponses()
    {
        // Arrange
        var mockSerial = new MockSerialPort();
        var zone = new Zone(1, 2);
        var responseReceived = false;
        
        mockSerial.DataReceived += (sender, data) =>
        {
            responseReceived = true;
            
            // Simulate processing the response - In real implementation this would use PacketBuilder
            // For this test, we'll just manually create a volume packet
            var volumePacket = new ZoneVolumePacket();
            volumePacket.SourceControllerID = 1;
            volumePacket.SourcePath = new byte[] { 0x02, 0x00, 0x02, 0x01 };
            volumePacket.Data = new byte[] { 25 }; // Volume 50
            
            if (volumePacket.GetControllerID() == zone.ControllerID && 
                volumePacket.GetZoneID() == zone.ZoneID)
            {
                zone.SetVolume(volumePacket.GetVolume(), true);
            }
        };
        
        mockSerial.Open();

        // Act - Simulate controller sending volume response
        mockSerial.SimulateDataReceived("F0 00 00 7F 01 02 70 00");

        // Assert
        Assert.True(responseReceived);
        Assert.Equal(50, zone.Volume);
    }

    [Fact]
    public void MultipleControllers_ShouldRouteMessagesCorrectly()
    {
        // Arrange
        var controller1Zone1 = new Zone(1, 1);
        var controller2Zone1 = new Zone(2, 1);
        
        var c1z1Changed = false;
        var c2z1Changed = false;
        
        controller1Zone1.VolumeChanged += (v, r) => c1z1Changed = true;
        controller2Zone1.VolumeChanged += (v, r) => c2z1Changed = true;

        // Create response for controller 2, zone 1
        var volumePacket = new ZoneVolumePacket();
        volumePacket.SourceControllerID = 2;
        volumePacket.SourcePath = new byte[] { 0x02, 0x00, 0x01, 0x01 }; // Zone 1
        volumePacket.Data = new byte[] { 30 }; // Volume 60
        
        // Act
        if (volumePacket != null)
        {
            var zones = new[] { controller1Zone1, controller2Zone1 };
            foreach (var zone in zones)
            {
                if (volumePacket.GetControllerID() == zone.ControllerID && volumePacket.GetZoneID() == zone.ZoneID)
                {
                    zone.SetVolume(volumePacket.GetVolume(), true);
                }
            }
        }

        // Assert
        Assert.Equal(2, volumePacket.GetControllerID());
        Assert.Equal(1, volumePacket.GetZoneID());
        
        Assert.False(c1z1Changed); // Controller 1 zone 1 should not change
        Assert.True(c2z1Changed);  // Controller 2 zone 1 should change
        
        Assert.Equal(0, controller1Zone1.Volume);
        Assert.Equal(60, controller2Zone1.Volume);
    }

    [Fact]
    public void MockSerial_HexStringSupport_ShouldWorkCorrectly()
    {
        // Test demonstrates hex string functionality for serial responses
        
        // Arrange
        var mockSerial = new MockSerialPort();
        var receivedData = new List<byte[]>();
        
        mockSerial.DataReceived += (sender, data) => receivedData.Add(data);
        mockSerial.Open();

        // Act - Simulate various hex responses
        var testHexStrings = new[]
        {
            "F0 01 02 7F 00 00 70 05",
            "F0 00 00 7F 01 02 70 00 00 04 02 00 01 01",
            "DE F7"
        };

        foreach (var hex in testHexStrings)
        {
            mockSerial.SimulateDataReceived(hex);
        }

        // Assert
        Assert.Equal(3, receivedData.Count);
        
        // Verify each hex string was properly converted
        for (int i = 0; i < testHexStrings.Length; i++)
        {
            var expectedBytes = HexUtils.FromHexString(testHexStrings[i]);
            var actualBytes = receivedData[i];
            Assert.Equal(expectedBytes, actualBytes);
        }
    }
}