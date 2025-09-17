using System.Collections.Concurrent;
using RNetPi.Core.Interfaces;

namespace RNetPi.Core.Services;

/// <summary>
/// Factory for creating mock network port instances for testing
/// </summary>
public class MockNetworkPortFactory : INetworkPortFactory
{
    public INetworkPort CreateNetworkPort()
    {
        return new TestableNetworkPort();
    }
}

/// <summary>
/// Testable network port implementation that can be used in place of real network ports
/// </summary>
public class TestableNetworkPort : INetworkPort
{
    private bool _isConnected = false;
    private readonly List<byte[]> _sentData = new();
    private readonly ConcurrentDictionary<string, TestableClient> _clients = new();
    private readonly List<string> _connectedClients = new();
    
    public bool IsConnected => _isConnected;
    
    public event EventHandler<string>? ClientConnected;
    public event EventHandler<string>? ClientDisconnected;
    public event EventHandler<NetworkDataEventArgs>? DataReceived;
    public event EventHandler<Exception>? ErrorReceived;
    
    /// <summary>
    /// Gets all data that has been sent to all clients
    /// </summary>
    public IReadOnlyList<byte[]> SentData => _sentData.AsReadOnly();
    
    /// <summary>
    /// Gets all currently connected client IDs
    /// </summary>
    public IReadOnlyList<string> ConnectedClients => _connectedClients.AsReadOnly();
    
    /// <summary>
    /// Gets data sent to a specific client
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>List of data sent to the client</returns>
    public IReadOnlyList<byte[]> GetClientSentData(string clientId)
    {
        return _clients.TryGetValue(clientId, out var client) ? client.SentData : new List<byte[]>();
    }
    
    /// <summary>
    /// Clears all sent data history
    /// </summary>
    public void ClearSentData()
    {
        _sentData.Clear();
        foreach (var client in _clients.Values)
        {
            client.ClearSentData();
        }
    }
    
    /// <summary>
    /// Simulates a client connecting
    /// </summary>
    /// <param name="clientId">Client ID to connect</param>
    public void SimulateClientConnected(string clientId)
    {
        _clients[clientId] = new TestableClient(clientId);
        _connectedClients.Add(clientId);
        ClientConnected?.Invoke(this, clientId);
    }
    
    /// <summary>
    /// Simulates a client disconnecting
    /// </summary>
    /// <param name="clientId">Client ID to disconnect</param>
    public void SimulateClientDisconnected(string clientId)
    {
        _clients.TryRemove(clientId, out _);
        _connectedClients.Remove(clientId);
        ClientDisconnected?.Invoke(this, clientId);
    }
    
    /// <summary>
    /// Simulates receiving data from a client
    /// </summary>
    /// <param name="clientId">Client ID sending data</param>
    /// <param name="data">Data received</param>
    public void SimulateDataReceived(string clientId, byte[] data)
    {
        if (_connectedClients.Contains(clientId))
        {
            DataReceived?.Invoke(this, new NetworkDataEventArgs(clientId, data));
        }
    }
    
    /// <summary>
    /// Simulates an error
    /// </summary>
    /// <param name="error">Error to simulate</param>
    public void SimulateError(Exception error)
    {
        ErrorReceived?.Invoke(this, error);
    }
    
    public Task StartAsync(int port)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }
    
    public Task StopAsync()
    {
        _isConnected = false;
        _connectedClients.Clear();
        _clients.Clear();
        return Task.CompletedTask;
    }
    
    public Task SendToAllAsync(byte[] data)
    {
        _sentData.Add(data);
        foreach (var client in _clients.Values)
        {
            client.AddSentData(data);
        }
        return Task.CompletedTask;
    }
    
    public Task SendToClientAsync(string clientId, byte[] data)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            client.AddSentData(data);
        }
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _isConnected = false;
        _sentData.Clear();
        _connectedClients.Clear();
        _clients.Clear();
    }
    
    private class TestableClient
    {
        private readonly List<byte[]> _sentData = new();
        
        public string ClientId { get; }
        public IReadOnlyList<byte[]> SentData => _sentData.AsReadOnly();
        
        public TestableClient(string clientId)
        {
            ClientId = clientId;
        }
        
        public void AddSentData(byte[] data)
        {
            _sentData.Add(data);
        }
        
        public void ClearSentData()
        {
            _sentData.Clear();
        }
    }
}