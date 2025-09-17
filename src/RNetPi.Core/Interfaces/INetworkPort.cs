using System;
using System.Threading.Tasks;

namespace RNetPi.Core.Interfaces;

/// <summary>
/// Interface for network port abstraction to enable mocking
/// </summary>
public interface INetworkPort : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the network port is connected
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Starts listening for network connections
    /// </summary>
    /// <param name="port">The port to listen on</param>
    /// <returns>A task representing the async operation</returns>
    Task StartAsync(int port);
    
    /// <summary>
    /// Stops listening for network connections
    /// </summary>
    /// <returns>A task representing the async operation</returns>
    Task StopAsync();
    
    /// <summary>
    /// Sends data to all connected clients
    /// </summary>
    /// <param name="data">The data to send</param>
    /// <returns>A task representing the async operation</returns>
    Task SendToAllAsync(byte[] data);
    
    /// <summary>
    /// Sends data to a specific client
    /// </summary>
    /// <param name="clientId">The client ID to send to</param>
    /// <param name="data">The data to send</param>
    /// <returns>A task representing the async operation</returns>
    Task SendToClientAsync(string clientId, byte[] data);
    
    /// <summary>
    /// Event raised when a client connects
    /// </summary>
    event EventHandler<string>? ClientConnected;
    
    /// <summary>
    /// Event raised when a client disconnects
    /// </summary>
    event EventHandler<string>? ClientDisconnected;
    
    /// <summary>
    /// Event raised when data is received from a client
    /// </summary>
    event EventHandler<NetworkDataEventArgs>? DataReceived;
    
    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    event EventHandler<Exception>? ErrorReceived;
}

/// <summary>
/// Event args for network data received events
/// </summary>
public class NetworkDataEventArgs : EventArgs
{
    public string ClientId { get; }
    public byte[] Data { get; }
    
    public NetworkDataEventArgs(string clientId, byte[] data)
    {
        ClientId = clientId;
        Data = data;
    }
}