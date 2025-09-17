using Microsoft.Extensions.Logging;
using RNetPi.Core.Interfaces;

namespace RNetPi.Core.Services;

/// <summary>
/// Factory for creating real network port instances using TcpNetworkServer
/// </summary>
public class NetworkPortFactory : INetworkPortFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _serverName;
    private readonly string? _host;

    public NetworkPortFactory(ILoggerFactory loggerFactory, string serverName = "RNetPi Server", string? host = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serverName = serverName;
        _host = host;
    }

    public INetworkPort CreateNetworkPort()
    {
        var logger = _loggerFactory.CreateLogger<TcpNetworkPortAdapter>();
        var tcpServerLogger = _loggerFactory.CreateLogger<TcpNetworkServer>();
        return new TcpNetworkPortAdapter(_serverName, _host, logger, tcpServerLogger);
    }
}