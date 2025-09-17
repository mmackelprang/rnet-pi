using Microsoft.Extensions.Logging;
using Moq;
using RNetPi.Core.Interfaces;
using RNetPi.Core.Models;
using RNetPi.Core.Services;
using Xunit;

namespace RNetPi.Core.Tests.Services;

public class SerialPortFactoryTests
{
    [Fact]
    public void SerialPortFactory_CreateSerialPort_ReturnsSerialPortWrapper()
    {
        // Arrange
        var factory = new SerialPortFactory();
        const string portName = "/dev/ttyUSB0";
        const int baudRate = 19200;

        // Act
        var serialPort = factory.CreateSerialPort(portName, baudRate);

        // Assert
        Assert.NotNull(serialPort);
        Assert.IsType<SerialPortWrapper>(serialPort);
        Assert.Equal(baudRate, serialPort.BaudRate);
    }

    [Fact]
    public void MockSerialPortFactory_CreateSerialPort_ReturnsTestableSerialPort()
    {
        // Arrange
        var factory = new MockSerialPortFactory();
        const string portName = "/dev/ttyUSB0";
        const int baudRate = 19200;

        // Act
        var serialPort = factory.CreateSerialPort(portName, baudRate);

        // Assert
        Assert.NotNull(serialPort);
        Assert.IsType<TestableSerialPort>(serialPort);
        Assert.Equal(baudRate, serialPort.BaudRate);
        
        // Verify it's testable
        if (serialPort is TestableSerialPort testablePort)
        {
            Assert.Empty(testablePort.SentData);
        }
    }

    [Fact]
    public void TestableSerialPort_CanSimulateDataReceived()
    {
        // Arrange
        var factory = new MockSerialPortFactory();
        var serialPort = factory.CreateSerialPort("/dev/ttyUSB0") as TestableSerialPort;
        Assert.NotNull(serialPort);

        var receivedData = new List<byte[]>();
        serialPort.DataReceived += (sender, data) => receivedData.Add(data);
        serialPort.Open();

        // Act
        var testData = new byte[] { 0x01, 0x02, 0x03 };
        serialPort.SimulateDataReceived(testData);

        // Assert
        Assert.Single(receivedData);
        Assert.Equal(testData, receivedData[0]);
    }

    [Fact]
    public void TestableSerialPort_TracksWrittenData()
    {
        // Arrange
        var factory = new MockSerialPortFactory();
        var serialPort = factory.CreateSerialPort("/dev/ttyUSB0") as TestableSerialPort;
        Assert.NotNull(serialPort);

        serialPort.Open();

        // Act
        var testData = new byte[] { 0x01, 0x02, 0x03 };
        serialPort.Write(testData);

        // Assert
        Assert.Single(serialPort.SentData);
        Assert.Equal(testData, serialPort.SentData[0]);
        Assert.Equal(testData, serialPort.LastSentData);
    }
}