using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GatewayIDE.App.Services.Docker;

namespace GatewayIDE.App.Services.Network;

public sealed class NetworkHostService
{
    public string BuildEnvText(
        string networkName,
        string serverEndpoint,
        string wgSubnet,
        string serverOverlayIp,
        string? dns,
        string serverPublicKey)
    {
        // Nur die Variablen, die euer Network/bootstrap.py tatsächlich liest.
        var sb = new StringBuilder();

        sb.AppendLine($"NETWORK_ENV_NAME=net.host.env");
        sb.AppendLine($"NETWORK_NAME={Clean(networkName)}");

        sb.AppendLine($"WG_SUBNET={Clean(wgSubnet)}");
        sb.AppendLine($"WG_SERVER_OVERLAY_IP={Clean(serverOverlayIp)}");
        sb.AppendLine($"WG_SERVER_ENDPOINT={Clean(serverEndpoint)}");
        sb.AppendLine($"WG_SERVER_PUBLIC_KEY={Clean(serverPublicKey)}");

        if (!string.IsNullOrWhiteSpace(dns))
            sb.AppendLine($"WG_DNS={Clean(dns)}");

        sb.AppendLine("WG_KEEPALIVE=25");

        return sb.ToString();
    }

    public async Task<string> WriteEnvFileAsync(string envText, string envPath, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(envPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(envPath, envText, Encoding.UTF8, ct).ConfigureAwait(false);
        return envPath;
    }

    public Task<int> HostAsync(
        string envFilePath,
        string composeServiceName,
        Action<string>? stdout = null,
        Action<string>? stderr = null,
        CancellationToken ct = default)
    {
        // startet docker compose up -d <service> mit env-file
        return DockerService.ComposeUpWithEnvFileAsync(envFilePath, composeServiceName, stdout, stderr, ct);
    }

    private static string Clean(string s) => (s ?? "").Replace("\r", "").Replace("\n", "").Trim();
}
