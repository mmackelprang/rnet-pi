# Factory Pattern Implementation for RNetPi

## Overview
This implementation provides a factory pattern that allows selecting between "real" and "mock" versions of serial port and network port implementations. This enables enhanced integration testing across all message types while maintaining clean separation between production and test code.

## Components Added

### Interfaces
- `ISerialPortFactory` - Factory interface for creating serial port instances
- `INetworkPortFactory` - Factory interface for creating network port instances

### Production Implementations
- `SerialPortFactory` - Creates real serial port wrappers
- `SerialPortWrapper` - Wraps System.IO.Ports.SerialPort to implement ISerialPort
- `NetworkPortFactory` - Creates real network port instances (placeholder for future implementation)

### Test/Mock Implementations
- `MockSerialPortFactory` - Creates testable serial port instances
- `TestableSerialPort` - Mock serial port with simulation capabilities
- `MockNetworkPortFactory` - Creates testable network port instances  
- `TestableNetworkPort` - Mock network port with simulation capabilities

## Usage Examples

### Production Code (Real Implementations)
```csharp
// Default usage - automatically uses real implementations
var service = new EnhancedRNetService(logger, configService);

// Explicit usage with real factory
var realFactory = new SerialPortFactory();
var service = new EnhancedRNetService(logger, configService, realFactory);
```

### Test Code (Mock Implementations)
```csharp
// Using mock factory for testing
var mockFactory = new MockSerialPortFactory();
var service = new EnhancedRNetService(logger, configService, mockFactory);

// Test serial communication
await service.ConnectAsync();

// Get the testable port for assertions
var testablePort = mockFactory.CreateSerialPort("/dev/test") as TestableSerialPort;
testablePort.SimulateDataReceived(new byte[] { 0x01, 0x02, 0x03 });

// Verify data was sent
Assert.Single(testablePort.SentData);
```

### Network Port Testing
```csharp
var mockNetworkFactory = new MockNetworkPortFactory();
var networkPort = mockNetworkFactory.CreateNetworkPort() as TestableNetworkPort;

// Simulate client connections
networkPort.SimulateClientConnected("client1");
await networkPort.SendToClientAsync("client1", data);

// Verify data was sent to client
var clientData = networkPort.GetClientSentData("client1");
Assert.Single(clientData);
```

## Key Benefits

1. **Enhanced Testing**: Mock implementations allow full simulation of serial and network communication without hardware dependencies
2. **Separation of Concerns**: Production code uses real implementations, test code uses mocks
3. **Backward Compatibility**: Existing code works unchanged with default factory usage
4. **Easy Integration**: Simple dependency injection pattern for test scenarios
5. **Observable Behavior**: Mock implementations track all sent data and allow simulation of received data

## Integration with Existing Code

Both `EnhancedRNetService` and `RNetService` (in Infrastructure) have been updated to support factory injection:

- Default constructors maintain backward compatibility
- Optional factory parameters allow test code to inject mock implementations
- All existing functionality preserved while adding testability

This pattern makes the newly created mocks in the RNetPi.Core.Tests Mocks folder fully accessible for enhanced integration testing across all message types.