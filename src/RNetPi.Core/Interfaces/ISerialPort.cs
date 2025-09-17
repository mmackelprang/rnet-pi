using System;
using System.IO.Ports;

namespace RNetPi.Core.Interfaces;

/// <summary>
/// Interface for serial port abstraction to enable mocking
/// </summary>
public interface ISerialPort : IDisposable
{
    /// <summary>
    /// Gets or sets the baud rate
    /// </summary>
    int BaudRate { get; set; }
    
    /// <summary>
    /// Gets or sets the data bits
    /// </summary>
    int DataBits { get; set; }
    
    /// <summary>
    /// Gets or sets the parity
    /// </summary>
    Parity Parity { get; set; }
    
    /// <summary>
    /// Gets or sets the stop bits
    /// </summary>
    StopBits StopBits { get; set; }
    
    /// <summary>
    /// Gets a value indicating whether the port is open
    /// </summary>
    bool IsOpen { get; }
    
    /// <summary>
    /// Opens the serial port
    /// </summary>
    void Open();
    
    /// <summary>
    /// Closes the serial port
    /// </summary>
    void Close();
    
    /// <summary>
    /// Writes data to the serial port
    /// </summary>
    /// <param name="buffer">The buffer containing data to write</param>
    /// <param name="offset">The offset in the buffer to start writing from</param>
    /// <param name="count">The number of bytes to write</param>
    void Write(byte[] buffer, int offset, int count);
    
    /// <summary>
    /// Writes data to the serial port
    /// </summary>
    /// <param name="buffer">The buffer containing data to write</param>
    void Write(byte[] buffer);
    
    /// <summary>
    /// Event raised when data is received
    /// </summary>
    event EventHandler<byte[]>? DataReceived;
    
    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    event EventHandler<Exception>? ErrorReceived;
}