using RNetPi.Core.RNet;
using RNetPi.Core.Tests.Mocks;
using RNetPi.Core.Tests.Utilities;

namespace RNetPi.Core.Tests.RNet;

public class SerialPortVolumeMessageTests
{
    [Fact]
    public void SetVolumePacket_ShouldCreateValidSerialMessage()
    {
        // Arrange
        var packet = new SetVolumePacket(0x01, 0x02, 50);

        // Act
        var buffer = packet.GetBuffer();

        // Assert - Verify basic packet structure
        Assert.True(buffer.Length > 10, "Packet should have minimum length");
        Assert.Equal(0xF0, buffer[0]); // Start byte
        Assert.Equal(0x01, buffer[1]); // Target Controller ID
        Assert.Equal(0xF7, buffer[^1]); // End byte
        
        // Verify volume-specific fields
        Assert.Equal(0x05, buffer[7]); // Message Type (Event)
        Assert.Equal(25, packet.EventTimestamp); // Volume 50 -> timestamp 25
        Assert.Equal(0x02, packet.EventData); // Zone ID
        Assert.Equal(0xDE, packet.EventID); // Set Volume event ID
    }

    [Fact]
    public void MockSerialPort_ShouldCaptureVolumeChangeMessage()
    {
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();
        
        var volumePacket = new SetVolumePacket(0x01, 0x02, 50);

        // Act
        mockSerial.Write(volumePacket.GetBuffer());

        // Assert
        Assert.Single(mockSerial.SentData);
        var sentData = mockSerial.LastSentData!;
        
        // Verify key protocol fields
        Assert.Equal(0xF0, sentData[0]);  // Start byte
        Assert.Equal(0x01, sentData[1]);  // Target Controller ID
        Assert.Equal(0xF7, sentData[^1]); // End byte
    }

    [Theory]
    [InlineData(25, 50)]   // Protocol uses half volume values
    [InlineData(0, 0)]
    [InlineData(50, 100)]
    [InlineData(49, 98)]   // Odd values get rounded down
    public void VolumeConversion_ShouldMatchProtocolSpecification(int protocolValue, int actualVolume)
    {
        // Arrange & Act
        var packet = new SetVolumePacket(0x01, 0x02, actualVolume);

        // Assert
        Assert.Equal(protocolValue, packet.EventTimestamp);
        Assert.Equal(actualVolume, packet.GetVolume());
    }

    [Fact]
    public void VolumeChangeCall_ShouldResultInCorrectSerialMessage()
    {
        // This test verifies that volume change calls to the controller 
        // result in the correct volume change serial messages
        
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();
        mockSerial.ClearSentData();

        // Act - Simulate setting volume to 75 on controller 1, zone 3
        var volumePacket = new SetVolumePacket(0x01, 0x03, 75);
        mockSerial.Write(volumePacket.GetBuffer());

        // Assert
        Assert.Single(mockSerial.SentData);
        
        // Verify the packet structure matches protocol specification
        var sentData = mockSerial.LastSentData!;
        Assert.Equal(0xF0, sentData[0]);  // Start byte
        Assert.Equal(0x01, sentData[1]);  // Target Controller ID
        Assert.Equal(0xF7, sentData[^1]); // End byte
        
        // Verify packet contains volume information
        Assert.Equal(37, volumePacket.EventTimestamp); // 75/2 = 37.5, truncated to 37
        Assert.Equal(0x03, volumePacket.EventData); // Zone ID
        Assert.Equal(74, volumePacket.GetVolume()); // Verify round-trip conversion (37*2=74)
    }

    [Fact]
    public void MultipleVolumeChanges_ShouldBeTrackedCorrectly()
    {
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();

        // Act - Send multiple volume commands
        var volumes = new[] { 25, 50, 75, 100 };
        foreach (var volume in volumes)
        {
            var packet = new SetVolumePacket(0x01, 0x02, volume);
            mockSerial.Write(packet.GetBuffer());
        }

        // Assert
        Assert.Equal(4, mockSerial.SentData.Count);
        
        // Verify each packet was captured
        for (int i = 0; i < volumes.Length; i++)
        {
            var sentData = mockSerial.SentData[i];
            Assert.Equal(0xF0, sentData[0]); // Start byte
            Assert.Equal(0xF7, sentData[^1]); // End byte
        }
    }

    [Fact]
    public void VolumePacketHexConversion_ShouldWorkWithMockSerial()
    {
        // Test that demonstrates hex string functionality with mock
        
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();
        
        var packet = new SetVolumePacket(0x01, 0x02, 50);
        var buffer = packet.GetBuffer();
        var hexString = HexUtils.ToHexString(buffer);

        // Act - Send via mock and verify hex conversion works
        mockSerial.Write(buffer);

        // Assert
        Assert.Single(mockSerial.SentData);
        var sentHex = HexUtils.ToHexString(mockSerial.LastSentData!);
        Assert.Equal(hexString, sentHex);
        
        // Verify we can convert back from hex
        var reconstructed = HexUtils.FromHexString(sentHex);
        Assert.Equal(buffer, reconstructed);
    }
}