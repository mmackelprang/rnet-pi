using RNetPi.Core.RNet;
using RNetPi.Core.Tests.Mocks;
using RNetPi.Core.Tests.Utilities;
using RNetPi.Core.Constants;

namespace RNetPi.Core.Tests.RNet;

public class SerialPortTrebleMessageTests
{
    [Theory]
    [InlineData(0x01, 0x02, -10, "F0 01 00 7F 00 00 70 00 05 02 00 01 00 01 00 00 01 00 01 00 00")]
    [InlineData(0x01, 0x02, 0, "F0 01 00 7F 00 00 70 00 05 02 00 01 00 01 00 00 01 00 01 00 0A")]
    [InlineData(0x01, 0x02, 10, "F0 01 00 7F 00 00 70 00 05 02 00 01 00 01 00 00 01 00 01 00 14")]
    [InlineData(0x02, 0x03, 5, "F0 02 00 7F 00 00 70 00 05 02 00 02 00 01 00 00 01 00 01 00 0F")]
    public void SetTreblePacket_ShouldCreateCorrectSerialMessage(byte controllerID, byte zoneID, int treble, string expectedHexWithoutChecksum)
    {
        // Arrange
        var packet = new SetTreblePacket(controllerID, zoneID, treble);
        var expectedBuffer = HexUtils.CreateRNetPacketFromHex(expectedHexWithoutChecksum);

        // Act
        var actualBuffer = packet.GetBuffer();

        // Assert
        var comparison = HexUtils.CompareByteArrays(expectedBuffer, actualBuffer);
        Assert.True(string.IsNullOrEmpty(comparison), $"Treble packet mismatch:\n{comparison}");
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
        var expectedHex = "F0 01 00 7F 00 00 70 00 05 02 00 01 00 01 00 00 01 00 01 00 0D";
        var expectedBuffer = HexUtils.CreateRNetPacketFromHex(expectedHex);

        // Act
        mockSerial.Write(treblePacket.GetBuffer());

        // Assert
        Assert.Single(mockSerial.SentData);
        var sentData = mockSerial.LastSentData!;
        var comparison = HexUtils.CompareByteArrays(expectedBuffer, sentData);
        Assert.True(string.IsNullOrEmpty(comparison), $"Sent data mismatch:\n{comparison}");
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
        
        // Verify the packet structure matches protocol specification
        var sentData = mockSerial.LastSentData!;
        Assert.Equal(0xF0, sentData[0]);  // Start byte
        Assert.Equal(0x01, sentData[1]);  // Target Controller ID
        Assert.Equal(0x00, sentData[2]);  // Target Zone ID (0 for controller commands)
        Assert.Equal(0x7F, sentData[3]);  // Target Keypad ID
        Assert.Equal(0x00, sentData[4]);  // Source Controller ID
        Assert.Equal(0x00, sentData[5]);  // Source Zone ID
        Assert.Equal(0x70, sentData[6]);  // Source Keypad ID
        Assert.Equal(0x00, sentData[7]);  // Message Type (Data)
        
        // Data packet structure
        Assert.Equal(0x05, sentData[8]);  // Target path length
        Assert.Equal(0x02, sentData[9]);  // Target path[0] - Root Menu
        Assert.Equal(0x00, sentData[10]); // Target path[1] - Run Mode
        Assert.Equal(0x01, sentData[11]); // Target path[2] - Controller ID
        Assert.Equal(0x00, sentData[12]); // Target path[3] - User Parameters
        Assert.Equal(0x01, sentData[13]); // Target path[4] - Treble Parameter ID
        
        Assert.Equal(0x00, sentData[14]); // Source path length
        
        // Packet number and count
        Assert.Equal(0x00, sentData[15]); // Packet number low
        Assert.Equal(0x00, sentData[16]); // Packet number high
        Assert.Equal(0x01, sentData[17]); // Packet count low
        Assert.Equal(0x00, sentData[18]); // Packet count high
        
        // Data length and data
        Assert.Equal(0x01, sentData[19]); // Data length low
        Assert.Equal(0x00, sentData[20]); // Data length high
        Assert.Equal(15, sentData[21]);   // Data: treble value (5 + 10 = 15)
        
        Assert.Equal(0xF7, sentData[^1]); // End byte
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
        
        // Verify each message has correct treble value
        for (int i = 0; i < trebleValues.Length; i++)
        {
            var sentData = mockSerial.SentData[i];
            var expectedValue = trebleValues[i] + 10; // Convert to protocol value
            Assert.Equal(expectedValue, sentData[21]); // Data value position
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
}