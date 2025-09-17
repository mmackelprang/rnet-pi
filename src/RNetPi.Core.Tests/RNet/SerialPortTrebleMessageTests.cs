using RNetPi.Core.RNet;
using RNetPi.Core.Tests.Mocks;
using RNetPi.Core.Tests.Utilities;
using RNetPi.Core.Constants;

namespace RNetPi.Core.Tests.RNet;

public class SerialPortTrebleMessageTests
{
    [Fact]
    public void SetTreblePacket_ShouldCreateValidSerialMessage()
    {
        // Arrange
        var packet = new SetTreblePacket(0x01, 0x02, 5);

        // Act
        var buffer = packet.GetBuffer();

        // Assert - Verify basic packet structure
        Assert.True(buffer.Length > 10, "Packet should have minimum length");
        Assert.Equal(0xF0, buffer[0]); // Start byte
        Assert.Equal(0x01, buffer[1]); // Target Controller ID
        Assert.Equal(0xF7, buffer[^1]); // End byte
        
        // Verify treble-specific fields
        Assert.Equal(0x00, buffer[7]); // Message Type (Data)
        Assert.Equal((byte)ZoneParameters.Treble, packet.GetParameterID());
        Assert.Equal(15, packet.GetParameterValue()); // Treble 5 -> value 15 (5+10)
        Assert.Equal(5, packet.GetTreble()); // Round-trip conversion
    }

    [Fact]
    public void SetTreblePacket_ShouldThrowException_WhenTrebleOutOfRange()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SetTreblePacket(0x01, 0x02, -11));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SetTreblePacket(0x01, 0x02, 11));
    }

    [Fact]
    public void MockSerialPort_ShouldCaptureTrebleChangeMessage()
    {
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();
        
        var treblePacket = new SetTreblePacket(0x01, 0x02, 3);

        // Act
        mockSerial.Write(treblePacket.GetBuffer());

        // Assert
        Assert.Single(mockSerial.SentData);
        var sentData = mockSerial.LastSentData!;
        
        // Verify key protocol fields
        Assert.Equal(0xF0, sentData[0]);  // Start byte
        Assert.Equal(0x01, sentData[1]);  // Target Controller ID
        Assert.Equal(0xF7, sentData[^1]); // End byte
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(-5, 5)]
    [InlineData(0, 10)]
    [InlineData(5, 15)]
    [InlineData(10, 20)]
    public void TrebleConversion_ShouldMatchProtocolSpecification(int trebleValue, byte expectedParameterValue)
    {
        // Arrange & Act
        var packet = new SetTreblePacket(0x01, 0x02, trebleValue);

        // Assert
        Assert.Equal(expectedParameterValue, packet.GetParameterValue());
        Assert.Equal(trebleValue, packet.GetTreble());
    }

    [Fact]
    public void TrebleChangeCall_ShouldResultInCorrectSerialMessage()
    {
        // This test verifies that treble change calls to the controller 
        // result in the correct treble change serial messages
        
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();
        mockSerial.ClearSentData();

        // Act - Simulate setting treble to +5 on controller 1, zone 2
        var treblePacket = new SetTreblePacket(0x01, 0x02, 5);
        mockSerial.Write(treblePacket.GetBuffer());

        // Assert
        Assert.Single(mockSerial.SentData);
        
        // Verify the packet structure
        var sentData = mockSerial.LastSentData!;
        Assert.Equal(0xF0, sentData[0]);  // Start byte
        Assert.Equal(0x01, sentData[1]);  // Target Controller ID
        Assert.Equal(0xF7, sentData[^1]); // End byte
        
        // Verify treble packet properties
        Assert.Equal(0x00, treblePacket.MessageType); // Data packet
        Assert.Equal(15, treblePacket.GetParameterValue()); // Treble +5 -> 15
        Assert.Equal((byte)ZoneParameters.Treble, treblePacket.GetParameterID());
    }

    [Fact]
    public void TreblePacket_ShouldUseCorrectParameterID()
    {
        // Arrange & Act
        var packet = new SetTreblePacket(0x01, 0x02, 0);

        // Assert
        Assert.Equal((byte)ZoneParameters.Treble, packet.GetParameterID());
    }

    [Fact]
    public void MultipleTrebleChanges_ShouldBeTrackedCorrectly()
    {
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();

        // Act - Send multiple treble commands
        var trebleValues = new[] { -10, -5, 0, 5, 10 };
        foreach (var treble in trebleValues)
        {
            var packet = new SetTreblePacket(0x01, 0x02, treble);
            mockSerial.Write(packet.GetBuffer());
        }

        // Assert
        Assert.Equal(5, mockSerial.SentData.Count);
        
        // Verify each packet was captured
        for (int i = 0; i < trebleValues.Length; i++)
        {
            var sentData = mockSerial.SentData[i];
            Assert.Equal(0xF0, sentData[0]); // Start byte
            Assert.Equal(0xF7, sentData[^1]); // End byte
        }
    }

    [Fact]
    public void TreblePacket_ShouldInheritFromSetParameterPacket()
    {
        // Arrange & Act
        var packet = new SetTreblePacket(0x01, 0x02, 5);

        // Assert
        Assert.IsAssignableFrom<SetParameterPacket>(packet);
        Assert.Equal(0x00, packet.MessageType); // Data packet
        Assert.True(packet.RequiresHandshake());
    }

    [Fact]
    public void TreblePacketHexConversion_ShouldWorkWithMockSerial()
    {
        // Test that demonstrates hex string functionality with mock
        
        // Arrange
        var mockSerial = new MockSerialPort();
        mockSerial.Open();
        
        var packet = new SetTreblePacket(0x01, 0x02, -3);
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