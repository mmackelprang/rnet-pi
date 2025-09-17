using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RNetPi.Core.Interfaces;
using RNetPi.Core.Packets;

namespace RNetPi.Core.Services;

/// <summary>
/// Adapter that wraps TcpNetworkServer to implement INetworkPort interface
/// </summary>
public class TcpNetworkPortAdapter : INetworkPort
{
    private readonly string _serverName;
    private readonly string? _host;
    private readonly ILogger<TcpNetworkPortAdapter> _logger;
    private readonly ILogger<TcpNetworkServer> _tcpServerLogger;
    private readonly ConcurrentDictionary<string, TcpNetworkClient> _clientMap;
    
    private TcpNetworkServer? _tcpServer;
    private bool _disposed = false;

    public bool IsConnected { get; private set; }

    public event EventHandler<string>? ClientConnected;
    public event EventHandler<string>? ClientDisconnected;
    public event EventHandler<NetworkDataEventArgs>? DataReceived;
    public event EventHandler<Exception>? ErrorReceived;

    public TcpNetworkPortAdapter(string serverName, string? host, ILogger<TcpNetworkPortAdapter> logger, ILogger<TcpNetworkServer> tcpServerLogger)
    {
        _serverName = serverName ?? throw new ArgumentNullException(nameof(serverName));
        _host = host;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tcpServerLogger = tcpServerLogger ?? throw new ArgumentNullException(nameof(tcpServerLogger));
        _clientMap = new ConcurrentDictionary<string, TcpNetworkClient>();
    }

    public async Task StartAsync(int port)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TcpNetworkPortAdapter));

        try
        {
            _tcpServer = new TcpNetworkServer(_serverName, _host, port, _tcpServerLogger);
            
            // Wire up events
            _tcpServer.Started += OnServerStarted;
            _tcpServer.ClientConnected += OnClientConnected;
            _tcpServer.ClientDisconnected += OnClientDisconnected;
            _tcpServer.RawDataReceived += OnRawDataReceived;
            _tcpServer.Error += OnError;

            await _tcpServer.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TCP network port on port {Port}", port);
            ErrorReceived?.Invoke(this, ex);
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_disposed || _tcpServer == null) return;

        try
        {
            await _tcpServer.StopAsync();
            IsConnected = false;
            _clientMap.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping TCP network port");
            ErrorReceived?.Invoke(this, ex);
        }
    }

    public async Task SendToAllAsync(byte[] data)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TcpNetworkPortAdapter));
        if (_tcpServer == null) throw new InvalidOperationException("Server not started");

        try
        {
            await _tcpServer.BroadcastBufferAsync(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending data to all clients");
            ErrorReceived?.Invoke(this, ex);
            throw;
        }
    }

    public async Task SendToClientAsync(string clientId, byte[] data)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TcpNetworkPortAdapter));

        try
        {
            if (_clientMap.TryGetValue(clientId, out var client))
            {
                await client.SendBufferAsync(data);
            }
            else
            {
                _logger.LogWarning("Attempted to send data to unknown client: {ClientId}", clientId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending data to client {ClientId}", clientId);
            ErrorReceived?.Invoke(this, ex);
            throw;
        }
    }

    private void OnServerStarted(object? sender, EventArgs e)
    {
        IsConnected = true;
        _logger.LogInformation("TCP network port started on {Address}", _tcpServer?.GetAddress());
    }

    private void OnClientConnected(object? sender, TcpNetworkClient client)
    {
        var clientId = client.GetAddress();
        _clientMap[clientId] = client;
        ClientConnected?.Invoke(this, clientId);
        _logger.LogDebug("Client connected: {ClientId}", clientId);
    }

    private void OnClientDisconnected(object? sender, TcpNetworkClient client)
    {
        var clientId = client.GetAddress();
        _clientMap.TryRemove(clientId, out _);
        ClientDisconnected?.Invoke(this, clientId);
        _logger.LogDebug("Client disconnected: {ClientId}", clientId);
    }

    private void OnRawDataReceived(object? sender, (TcpNetworkClient Client, byte PacketType, byte[] Data) args)
    {
        try
        {
            var clientId = args.Client.GetAddress();
            
            // Create complete packet data including packet type and length
            var completeData = new byte[args.Data.Length + 2];
            completeData[0] = args.PacketType;
            completeData[1] = (byte)args.Data.Length;
            Array.Copy(args.Data, 0, completeData, 2, args.Data.Length);
            
            DataReceived?.Invoke(this, new NetworkDataEventArgs(clientId, completeData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received raw data");
            ErrorReceived?.Invoke(this, ex);
        }
    }

    private void OnError(object? sender, Exception error)
    {
        ErrorReceived?.Invoke(this, error);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        if (_tcpServer != null)
        {
            _tcpServer.Started -= OnServerStarted;
            _tcpServer.ClientConnected -= OnClientConnected;
            _tcpServer.ClientDisconnected -= OnClientDisconnected;
            _tcpServer.RawDataReceived -= OnRawDataReceived;
            _tcpServer.Error -= OnError;
            
            _tcpServer.Dispose();
        }
        
        _clientMap.Clear();
    }
}