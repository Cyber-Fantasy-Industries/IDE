using System;
using System.Collections.Generic;

namespace GatewayIDE.App.Services.App;

public sealed class ServiceRegistryConfig
{
    public List<ServiceProfile> Services { get; set; } = new();
}

public sealed class ServiceProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    // e.g. "network", "gateway", "leona"
    public string Key { get; set; } = "network";
    public string DisplayName { get; set; } = "Network Service";

    // Optional docker binding
    public string? DockerComposeServiceName { get; set; } // e.g. "network"
    public string? EnvFilePath { get; set; }              // e.g. net.dev.env

    // Editable ENV vars
    public Dictionary<string, string> Env { get; set; } = new();

    public string Mode { get; set; } = "Auto"; // Auto/Advanced
}
