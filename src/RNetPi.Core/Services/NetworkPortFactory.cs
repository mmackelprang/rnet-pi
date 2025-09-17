using RNetPi.Core.Interfaces;

namespace RNetPi.Core.Services;

/// <summary>
/// Factory for creating real network port instances
/// </summary>
public class NetworkPortFactory : INetworkPortFactory
{
    public INetworkPort CreateNetworkPort()
    {
        // For now, return a basic implementation
        // This would be replaced with actual network port implementation when available
        throw new NotImplementedException("Real network port implementation not yet available");
    }
}