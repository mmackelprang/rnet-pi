using RNetPi.Core.Interfaces;

namespace RNetPi.Core.Services;

/// <summary>
/// Factory for creating real serial port instances
/// </summary>
public class SerialPortFactory : ISerialPortFactory
{
    public ISerialPort CreateSerialPort(string portName, int baudRate = 19200)
    {
        return new SerialPortWrapper(portName, baudRate);
    }
}