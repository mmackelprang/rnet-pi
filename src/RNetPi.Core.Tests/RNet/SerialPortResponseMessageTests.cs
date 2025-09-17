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
        var initialVolume = zone.Volume;
        var volumeChangedFired = false;
        var newVolume = 0;
        
        zone.VolumeChanged += (volume, rNetTriggered) =>
        {
            volumeChangedFired = true;
            newVolume = volume;
        };

        // Simulate volume response packet from controller (Controller 1, Zone 2, Volume 75)
        var responseHex = "F0 00 00 7F 01 02 70 00 00 04 02 00 01 01 00 00 01 00 01 00 25";
        var responseBuffer = HexUtils.CreateRNetPacketFromHex(responseHex);
        
        // Act - Parse the response packet
        var packet = RNetPacket.FromData(responseBuffer);
        var volumePacket = PacketBuilder.Build(responseBuffer) as ZoneVolumePacket;
        
        // Simulate updating zone model from parsed packet
        if (volumePacket != null && volumePacket.GetControllerID() == zone.ControllerID && volumePacket.GetZoneID() == zone.ZoneID)
        {
            var volume = volumePacket.GetVolume();
            zone.SetVolume(volume, true); // rNetTriggered = true for responses
        }

        // Assert
        Assert.NotNull(volumePacket);
        Assert.Equal(1, volumePacket.GetControllerID());
        Assert.Equal(2, volumePacket.GetZoneID());
        Assert.Equal(75, volumePacket.GetVolume()); // 37 * 2 + 1 = 75 (protocol uses half values)
        
        Assert.True(volumeChangedFired);
        Assert.Equal(75, newVolume);
        Assert.Equal(75, zone.Volume);
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

        // Simulate source response packet from controller (Controller 1, Zone 3, Source 5)
        var responseHex = "F0 00 00 7F 01 03 70 00 00 04 02 00 01 02 00 00 01 00 01 00 05";
        var responseBuffer = HexUtils.CreateRNetPacketFromHex(responseHex);
        
        // Act - Parse the response packet
        var packet = RNetPacket.FromData(responseBuffer);
        var sourcePacket = PacketBuilder.Build(responseBuffer) as ZoneSourcePacket;
        
        // Simulate updating zone model from parsed packet
        if (sourcePacket != null && sourcePacket.GetControllerID() == zone.ControllerID && sourcePacket.GetZoneID() == zone.ZoneID)
        {
            var source = sourcePacket.GetSourceID();
            zone.SetSource(source, true); // rNetTriggered = true for responses
        }

        // Assert
        Assert.NotNull(sourcePacket);
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

        // Simulate volume response packet for zone 2 only
        var responseHex = "F0 00 00 7F 01 02 70 00 00 04 02 00 01 01 00 00 01 00 01 00 1E";
        var responseBuffer = HexUtils.CreateRNetPacketFromHex(responseHex);
        
        // Act - Parse and route the response packet
        var volumePacket = PacketBuilder.Build(responseBuffer) as ZoneVolumePacket;
        
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
        Assert.NotNull(volumePacket);
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
    [InlineData("F0 00 00 7F 01 02 70 00 00 04 02 00 01 01 00 00 01 00 01 00 00", 0)]    // Volume 0
    [InlineData("F0 00 00 7F 01 02 70 00 00 04 02 00 01 01 00 00 01 00 01 00 19", 50)]   // Volume 50
    [InlineData("F0 00 00 7F 01 02 70 00 00 04 02 00 01 01 00 00 01 00 01 00 32", 100)]  // Volume 100
    public void VolumeResponseMessages_ShouldParseCorrectly(string responseHex, int expectedVolume)
    {
        // Arrange
        var responseBuffer = HexUtils.CreateRNetPacketFromHex(responseHex);
        
        // Act
        var volumePacket = PacketBuilder.Build(responseBuffer) as ZoneVolumePacket;
        
        // Assert
        Assert.NotNull(volumePacket);
        Assert.Equal(expectedVolume, volumePacket.GetVolume());
    }

    [Theory]
    [InlineData("F0 00 00 7F 01 02 70 00 00 04 02 00 01 02 00 00 01 00 01 00 01", 1)]  // Source 1
    [InlineData("F0 00 00 7F 01 02 70 00 00 04 02 00 01 02 00 00 01 00 01 00 05", 5)]  // Source 5
    [InlineData("F0 00 00 7F 01 02 70 00 00 04 02 00 01 02 00 00 01 00 01 00 08", 8)]  // Source 8
    public void SourceResponseMessages_ShouldParseCorrectly(string responseHex, int expectedSource)
    {
        // Arrange
        var responseBuffer = HexUtils.CreateRNetPacketFromHex(responseHex);
        
        // Act
        var sourcePacket = PacketBuilder.Build(responseBuffer) as ZoneSourcePacket;
        
        // Assert
        Assert.NotNull(sourcePacket);
        Assert.Equal(expectedSource, sourcePacket.GetSourceID());
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
            
            // Simulate processing the response
            var packet = PacketBuilder.Build(data);
            if (packet is ZoneVolumePacket volumePacket && 
                volumePacket.GetControllerID() == zone.ControllerID && 
                volumePacket.GetZoneID() == zone.ZoneID)
            {
                zone.SetVolume(volumePacket.GetVolume(), true);
            }
        };
        
        mockSerial.Open();

        // Act - Simulate controller sending volume response
        var responseHex = "F0 00 00 7F 01 02 70 00 00 04 02 00 01 01 00 00 01 00 01 00 19";
        mockSerial.SimulateDataReceived(responseHex);

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

        // Simulate response for controller 2, zone 1
        var responseHex = "F0 00 00 7F 02 01 70 00 00 04 02 00 02 01 00 00 01 00 01 00 1E";
        var responseBuffer = HexUtils.CreateRNetPacketFromHex(responseHex);
        
        // Act
        var volumePacket = PacketBuilder.Build(responseBuffer) as ZoneVolumePacket;
        
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
        Assert.NotNull(volumePacket);
        Assert.Equal(2, volumePacket.GetControllerID());
        Assert.Equal(1, volumePacket.GetZoneID());
        
        Assert.False(c1z1Changed); // Controller 1 zone 1 should not change
        Assert.True(c2z1Changed);  // Controller 2 zone 1 should change
        
        Assert.Equal(0, controller1Zone1.Volume);
        Assert.Equal(60, controller2Zone1.Volume);
    }
}