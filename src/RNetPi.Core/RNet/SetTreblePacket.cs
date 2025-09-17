using System;
using RNetPi.Core.Constants;

namespace RNetPi.Core.RNet;

/// <summary>
/// Packet for setting zone treble level
/// </summary>
public class SetTreblePacket : SetParameterPacket
{
    public SetTreblePacket(byte controllerID, byte zoneID, int treble)
        : base(controllerID, zoneID, (byte)ZoneParameters.Treble, TrebleToValue(treble))
    {
        if (treble < -10 || treble > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(treble), "Treble must be between -10 and 10");
        }
    }

    public new byte GetControllerID()
    {
        return TargetControllerID;
    }

    public new byte GetZoneID()
    {
        return TargetZoneID;
    }

    public int GetTreble()
    {
        return ValueToTreble(GetParameterValue());
    }

    private static byte TrebleToValue(int treble)
    {
        // Convert treble range -10 to +10 to byte range 0 to 20
        return (byte)(treble + 10);
    }

    private static int ValueToTreble(byte value)
    {
        // Convert byte range 0 to 20 to treble range -10 to +10
        return value - 10;
    }
}