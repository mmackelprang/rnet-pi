using System;
using System.IO.Ports;
using RNetPi.Core.Interfaces;

namespace RNetPi.Core.Services;

/// <summary>
/// Wrapper around System.IO.Ports.SerialPort that implements ISerialPort
/// </summary>
public class SerialPortWrapper : ISerialPort
{
    private readonly SerialPort _serialPort;
    private bool _disposed = false;

    public int BaudRate 
    { 
        get => _serialPort.BaudRate; 
        set => _serialPort.BaudRate = value; 
    }
    
    public int DataBits 
    { 
        get => _serialPort.DataBits; 
        set => _serialPort.DataBits = value; 
    }
    
    public Parity Parity 
    { 
        get => _serialPort.Parity; 
        set => _serialPort.Parity = value; 
    }
    
    public StopBits StopBits 
    { 
        get => _serialPort.StopBits; 
        set => _serialPort.StopBits = value; 
    }
    
    public bool IsOpen => _serialPort.IsOpen;

    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler<Exception>? ErrorReceived;

    public SerialPortWrapper(string portName, int baudRate = 19200)
    {
        _serialPort = new SerialPort(portName, baudRate)
        {
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Handshake = Handshake.None
        };
        
        _serialPort.DataReceived += OnSerialPortDataReceived;
        _serialPort.ErrorReceived += OnSerialPortErrorReceived;
    }

    public void Open()
    {
        _serialPort.Open();
    }

    public void Close()
    {
        _serialPort.Close();
    }

    public void Write(byte[] buffer, int offset, int count)
    {
        _serialPort.Write(buffer, offset, count);
    }

    public void Write(byte[] buffer)
    {
        _serialPort.Write(buffer, 0, buffer.Length);
    }

    private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort.BytesToRead > 0)
        {
            var buffer = new byte[_serialPort.BytesToRead];
            var bytesRead = _serialPort.Read(buffer, 0, buffer.Length);
            
            if (bytesRead > 0)
            {
                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);
                DataReceived?.Invoke(this, data);
            }
        }
    }

    private void OnSerialPortErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        var exception = new InvalidOperationException($"Serial port error: {e.EventType}");
        ErrorReceived?.Invoke(this, exception);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= OnSerialPortDataReceived;
                _serialPort.ErrorReceived -= OnSerialPortErrorReceived;
                _serialPort.Dispose();
            }
            _disposed = true;
        }
    }
}