using System;

namespace RNetPi.Core.Interfaces;

/// <summary>
/// Factory interface for creating serial port instances
/// </summary>
public interface ISerialPortFactory
{
    /// <summary>
    /// Creates a serial port instance
    /// </summary>
    /// <param name="portName">Name of the serial port</param>
    /// <param name="baudRate">Baud rate for the connection</param>
    /// <returns>An ISerialPort instance</returns>
    ISerialPort CreateSerialPort(string portName, int baudRate = 19200);
}