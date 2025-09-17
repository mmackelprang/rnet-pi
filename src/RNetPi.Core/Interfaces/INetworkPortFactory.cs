using System;

namespace RNetPi.Core.Interfaces;

/// <summary>
/// Factory interface for creating network port instances
/// </summary>
public interface INetworkPortFactory
{
    /// <summary>
    /// Creates a network port instance
    /// </summary>
    /// <returns>An INetworkPort instance</returns>
    INetworkPort CreateNetworkPort();
}