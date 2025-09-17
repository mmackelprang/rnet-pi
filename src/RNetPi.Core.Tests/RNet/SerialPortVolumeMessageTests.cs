using RNetPi.Core.RNet;
using RNetPi.Core.Tests.Mocks;
using RNetPi.Core.Tests.Utilities;

namespace RNetPi.Core.Tests.RNet;

public class SerialPortVolumeMessageTests
{
    [Theory]
    [InlineData(0x01, 0x02, 50, "F0 01 00 7F 00 00 70 05 02 02 00 00 DE 00 19 00 02 00 01")]
    [InlineData(0x02, 0x03, 0, "F0 02 00 7F 00 00 70 05 02 02 00 00 DE 00 00 00 03 00 01")]
    [InlineData(0x03, 0x01, 100, "F0 03 00 7F 00 00 70 05 02 02 00 00 DE 00 32 00 01 00 01")]
    [InlineData(0x05, 0x07, 75, "F0 05 00 7F 00 00 70 05 02 02 00 00 DE 00 25 00 07 00 01")]
    public void SetVolumePacket_ShouldCreateCorrectSerialMessage(byte controllerID, byte zoneID, int volume, string expectedHexWithoutChecksum)
    {
        // Arrange
        var packet = new SetVolumePacket(controllerID, zoneID, volume);
        var expectedBuffer = HexUtils.CreateRNetPacketFromHex(expectedHexWithoutChecksum);

        // Act
        var actualBuffer = packet.GetBuffer();

        // Assert
        var comparison = HexUtils.CompareByteArrays(expectedBuffer, actualBuffer);
        Assert.True(string.IsNullOrEmpty(comparison), $"Volume packet mismatch:\n{comparison}");
    }

    [Fact]
    public void MockSerialPort_ShouldCaptureVolumeChangeMessage()
    {
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();
        
        var volumePacket = new SetVolumePacket(0x01, 0x02, 50);
        var expectedHex = "F0 01 00 7F 00 00 70 05 02 02 00 00 DE 00 19 00 02 00 01";
        var expectedBuffer = HexUtils.CreateRNetPacketFromHex(expectedHex);

        // Act
        mockSerial.Write(volumePacket.GetBuffer());

        // Assert
        Assert.Single(mockSerial.SentData);
        var sentData = mockSerial.LastSentData!;
        var comparison = HexUtils.CompareByteArrays(expectedBuffer, sentData);
        Assert.True(string.IsNullOrEmpty(comparison), $"Sent data mismatch:\n{comparison}");
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
        Assert.Equal(0x00, sentData[2]);  // Target Zone ID (0 for controller commands)
        Assert.Equal(0x7F, sentData[3]);  // Target Keypad ID
        Assert.Equal(0x00, sentData[4]);  // Source Controller ID
        Assert.Equal(0x00, sentData[5]);  // Source Zone ID
        Assert.Equal(0x70, sentData[6]);  // Source Keypad ID
        Assert.Equal(0x05, sentData[7]);  // Message Type (Event)
        
        // Event packet structure
        Assert.Equal(0x02, sentData[8]);  // Target path length
        Assert.Equal(0x02, sentData[9]);  // Target path[0] - Root Menu
        Assert.Equal(0x00, sentData[10]); // Target path[1] - Run Mode
        Assert.Equal(0x00, sentData[11]); // Source path length
        
        // Event data
        Assert.Equal(0xDE, sentData[12]); // Event ID (Set Volume)
        Assert.Equal(0x00, sentData[13]); // Event ID high byte
        Assert.Equal(37, sentData[14]);   // Event timestamp (75/2 = 37.5, truncated to 37)
        Assert.Equal(0x00, sentData[15]); // Event timestamp high byte
        Assert.Equal(0x03, sentData[16]); // Event data (Zone ID)
        Assert.Equal(0x00, sentData[17]); // Event data high byte
        Assert.Equal(0x01, sentData[18]); // Event priority
        
        Assert.Equal(0xF7, sentData[^1]); // End byte
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
        
        // Verify each message has correct volume timestamp
        for (int i = 0; i < volumes.Length; i++)
        {
            var sentData = mockSerial.SentData[i];
            var expectedTimestamp = volumes[i] / 2;
            Assert.Equal(expectedTimestamp, sentData[14]); // Event timestamp position
        }
    }
}