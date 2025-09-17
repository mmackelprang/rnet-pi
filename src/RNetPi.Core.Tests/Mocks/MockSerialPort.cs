using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using RNetPi.Core.Interfaces;

namespace RNetPi.Core.Tests.Mocks;

/// <summary>
/// Mock implementation of ISerialPort for testing
/// </summary>
public class MockSerialPort : ISerialPort
{
    private bool _isOpen = false;
    private readonly List<byte[]> _sentData = new();
    private readonly Queue<byte[]> _receiveQueue = new();
    
    public int BaudRate { get; set; } = 19200;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
    public bool IsOpen => _isOpen;
    
    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler<Exception>? ErrorReceived;
    
    /// <summary>
    /// Gets all data that has been sent to this mock serial port
    /// </summary>
    public IReadOnlyList<byte[]> SentData => _sentData.AsReadOnly();
    
    /// <summary>
    /// Gets the last data sent to this mock serial port
    /// </summary>
    public byte[]? LastSentData => _sentData.Count > 0 ? _sentData[^1] : null;
    
    /// <summary>
    /// Clears the sent data history
    /// </summary>
    public void ClearSentData()
    {
        _sentData.Clear();
    }
    
    /// <summary>
    /// Simulates receiving data from the serial port
    /// </summary>
    /// <param name="data">Data to simulate receiving</param>
    public void SimulateDataReceived(byte[] data)
    {
        if (!_isOpen) return;
        
        DataReceived?.Invoke(this, data);
    }
    
    /// <summary>
    /// Simulates receiving data from a hex string
    /// </summary>
    /// <param name="hexData">Hex string representation of data</param>
    public void SimulateDataReceived(string hexData)
    {
        var data = Utilities.HexUtils.FromHexString(hexData);
        SimulateDataReceived(data);
    }
    
    /// <summary>
    /// Simulates an error on the serial port
    /// </summary>
    /// <param name="error">Error to simulate</param>
    public void SimulateError(Exception error)
    {
        ErrorReceived?.Invoke(this, error);
    }
    
    /// <summary>
    /// Queues data to be received when Read methods are called
    /// </summary>
    /// <param name="data">Data to queue for reading</param>
    public void QueueDataForReading(byte[] data)
    {
        _receiveQueue.Enqueue(data);
    }
    
    /// <summary>
    /// Queues data from hex string to be received when Read methods are called
    /// </summary>
    /// <param name="hexData">Hex string representation of data</param>
    public void QueueDataForReading(string hexData)
    {
        var data = Utilities.HexUtils.FromHexString(hexData);
        QueueDataForReading(data);
    }
    
    public void Open()
    {
        _isOpen = true;
    }
    
    public void Close()
    {
        _isOpen = false;
    }
    
    public void Write(byte[] buffer, int offset, int count)
    {
        if (!_isOpen)
            throw new InvalidOperationException("Serial port is not open");
        
        var data = new byte[count];
        Array.Copy(buffer, offset, data, 0, count);
        _sentData.Add(data);
    }
    
    public void Write(byte[] buffer)
    {
        Write(buffer, 0, buffer.Length);
    }
    
    public void Dispose()
    {
        Close();
        _sentData.Clear();
        _receiveQueue.Clear();
    }
}