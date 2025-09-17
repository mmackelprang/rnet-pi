using RNetPi.Core.RNet;
using RNetPi.Core.Tests.Utilities;

namespace RNetPi.Core.Tests.RNet;

public class InspectPacketFormats
{
    [Fact]
    public void InspectVolumePacketFormat()
    {
        // Create a volume packet and inspect its actual format
        var packet = new SetVolumePacket(0x01, 0x02, 50);
        var buffer = packet.GetBuffer();
        var hex = HexUtils.ToHexString(buffer);
        
        // This will help us see the actual format
        Assert.Equal("", hex + $" (Length: {buffer.Length})");
    }

    [Fact]
    public void InspectTreblePacketFormat()
    {
        // Create a treble packet and inspect its actual format
        var packet = new SetTreblePacket(0x01, 0x02, 3);
        var buffer = packet.GetBuffer();
        var hex = HexUtils.ToHexString(buffer);
        
        // This will help us see the actual format
        Assert.Equal("", hex + $" (Length: {buffer.Length})");
    }
}