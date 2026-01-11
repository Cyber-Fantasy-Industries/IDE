using System;
using System.Collections.Generic;

namespace GatewayIDE.App.Services.App;

public sealed class NetworkRegistryConfig
{
    public List<NetworkProfile> Connected { get; set; } = new();
    public List<NetworkProfile> Hosted { get; set; } = new();
}

public sealed class NetworkProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "MyTeamNet";

    // what the user typed in "Invite Code / Address"
    public string JoinHint { get; set; } = "";

    // Host/config (optional for connected)
    public string? ServerEndpoint { get; set; }  // your.static.ip:51820
    public string? WgSubnet { get; set; }        // 10.77.0.0/16
    public string? ServerIp { get; set; }        // 10.77.0.10
    public string? Dns { get; set; }             // 1.1.1.1

    // Runtime/UI
    public string Status { get; set; } = "Unknown"; // Online/Offline/...
    public DateTimeOffset? LastSeenUtc { get; set; }
    public string? OverlayIpSelf { get; set; }      // 10.77.0.23
    public int? PeersConnected { get; set; }

    // 🔗 Link to ServiceProfile (holds ENV)
    public string? ServiceRefId { get; set; }
}
