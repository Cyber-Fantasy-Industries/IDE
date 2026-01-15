#if ISOLATION_MODE
using System.Threading;
using System.Threading.Tasks;

namespace GatewayIDE.App.Services.Chat;

/// <summary>
/// Isolation-Stub: ersetzt die echte ChatService.cs, damit Build/Publish durchläuft.
/// </summary>
public sealed class ChatService
{
    public Task SendAsync(string message, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<string> PingAsync(CancellationToken ct = default)
        => Task.FromResult("chat:disabled");
}
#endif
