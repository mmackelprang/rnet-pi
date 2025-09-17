using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RNetPi.Core.Interfaces;

namespace RNetPi.Core.Tests.Mocks;

/// <summary>
/// Mock implementation of INetworkPort for testing
/// </summary>
public class MockNetworkPort : INetworkPort
{
    private bool _isConnected = false;
    private readonly List<byte[]> _sentData = new();
    private readonly ConcurrentDictionary<string, MockClient> _clients = new();
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
        _clients[clientId] = new MockClient(clientId);
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
    /// Simulates receiving data from a client using hex string
    /// </summary>
    /// <param name="clientId">Client ID sending data</param>
    /// <param name="hexData">Hex string representation of data</param>
    public void SimulateDataReceived(string clientId, string hexData)
    {
        var data = Utilities.HexUtils.FromHexString(hexData);
        SimulateDataReceived(clientId, data);
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
    
    private class MockClient
    {
        private readonly List<byte[]> _sentData = new();
        
        public string ClientId { get; }
        public IReadOnlyList<byte[]> SentData => _sentData.AsReadOnly();
        
        public MockClient(string clientId)
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