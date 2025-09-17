using System;
using System.Globalization;
using System.Text;

namespace RNetPi.Core.Tests.Utilities;

/// <summary>
/// Utility class for converting between hexadecimal strings and byte arrays in tests
/// </summary>
public static class HexUtils
{
    /// <summary>
    /// Converts a hexadecimal string to a byte array
    /// </summary>
    /// <param name="hex">Hexadecimal string (e.g., "F0 01 02 F7" or "F00102F7")</param>
    /// <returns>Byte array representation of the hex string</returns>
    public static byte[] FromHexString(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return Array.Empty<byte>();
        
        // Remove spaces and make uppercase
        var cleanHex = hex.Replace(" ", "").Replace("-", "").ToUpperInvariant();
        
        // Ensure even number of characters
        if (cleanHex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even number of characters", nameof(hex));
        
        var bytes = new byte[cleanHex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(cleanHex.Substring(i * 2, 2), NumberStyles.HexNumber);
        }
        
        return bytes;
    }
    
    /// <summary>
    /// Converts a byte array to a hexadecimal string
    /// </summary>
    /// <param name="bytes">Byte array to convert</param>
    /// <param name="spaceSeparated">Whether to include spaces between bytes</param>
    /// <returns>Hexadecimal string representation</returns>
    public static string ToHexString(byte[] bytes, bool spaceSeparated = true)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;
        
        var sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0 && spaceSeparated)
                sb.Append(' ');
            
            sb.Append(bytes[i].ToString("X2"));
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Compares two byte arrays and returns a human-readable difference report
    /// </summary>
    /// <param name="expected">Expected byte array</param>
    /// <param name="actual">Actual byte array</param>
    /// <returns>String describing differences, or empty string if arrays match</returns>
    public static string CompareByteArrays(byte[] expected, byte[] actual)
    {
        if (expected.Length != actual.Length)
        {
            return $"Length mismatch: expected {expected.Length} bytes, got {actual.Length} bytes\n" +
                   $"Expected: {ToHexString(expected)}\n" +
                   $"Actual:   {ToHexString(actual)}";
        }
        
        var differences = new StringBuilder();
        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                differences.AppendLine($"Byte {i}: expected 0x{expected[i]:X2}, got 0x{actual[i]:X2}");
            }
        }
        
        if (differences.Length > 0)
        {
            return $"Byte differences found:\n{differences}" +
                   $"Expected: {ToHexString(expected)}\n" +
                   $"Actual:   {ToHexString(actual)}";
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Creates an RNet packet from a hex string, automatically calculating checksum
    /// </summary>
    /// <param name="hexWithoutChecksum">Hex string without the checksum byte (before F7)</param>
    /// <returns>Complete packet with calculated checksum</returns>
    public static byte[] CreateRNetPacketFromHex(string hexWithoutChecksum)
    {
        var packetWithoutChecksum = FromHexString(hexWithoutChecksum);
        
        // Calculate checksum (XOR of all bytes after start byte and before end byte)
        byte checksum = 0;
        for (int i = 1; i < packetWithoutChecksum.Length; i++) // Skip start byte (F0)
        {
            checksum ^= packetWithoutChecksum[i];
        }
        
        // Create complete packet
        var completePacket = new byte[packetWithoutChecksum.Length + 2]; // +1 for checksum, +1 for end byte
        Array.Copy(packetWithoutChecksum, completePacket, packetWithoutChecksum.Length);
        completePacket[completePacket.Length - 2] = checksum;
        completePacket[completePacket.Length - 1] = 0xF7; // End byte
        
        return completePacket;
    }
}