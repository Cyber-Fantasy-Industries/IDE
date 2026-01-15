#if ISOLATION_MODE
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayIDE.App.Services.Docker;

/// <summary>
/// Isolation-Stub: ersetzt die echte DockerService.cs, damit Build/Publish durchläuft.
/// </summary>
public sealed class DockerService
{
    public Task<bool> IsDockerAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<string>> ListUnitsAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<string>)new List<string>());

    public Task StartAsync(string unitName, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task StopAsync(string unitName, CancellationToken ct = default)
        => Task.CompletedTask;
}
#endif
