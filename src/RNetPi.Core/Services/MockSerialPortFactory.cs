using RNetPi.Core.Interfaces;

namespace RNetPi.Core.Services;

/// <summary>
/// Factory for creating mock serial port instances for testing
/// </summary>
public class MockSerialPortFactory : ISerialPortFactory
{
    public ISerialPort CreateSerialPort(string portName, int baudRate = 19200)
    {
        // Create a lightweight mock that can be cast to the test mock if needed
        return new TestableSerialPort(portName, baudRate);
    }
}

/// <summary>
/// Testable serial port implementation that can be used in place of real serial ports
/// </summary>
public class TestableSerialPort : ISerialPort
{
    private bool _isOpen = false;
    private readonly List<byte[]> _sentData = new();
    private readonly Queue<byte[]> _receiveQueue = new();
    
    public string PortName { get; }
    public int BaudRate { get; set; } = 19200;
    public int DataBits { get; set; } = 8;
    public System.IO.Ports.Parity Parity { get; set; } = System.IO.Ports.Parity.None;
    public System.IO.Ports.StopBits StopBits { get; set; } = System.IO.Ports.StopBits.One;
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
    
    public TestableSerialPort(string portName, int baudRate = 19200)
    {
        PortName = portName;
        BaudRate = baudRate;
    }
    
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