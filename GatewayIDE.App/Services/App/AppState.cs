using System;
using GatewayIDE.App.Services.Auth;
using GatewayIDE.App.Services.Network;

namespace GatewayIDE.App.Services.App;

public sealed class AppState
{
    private readonly NetworkSession _net;

    public AppState(NetworkSession net)
    {
        _net = net ?? throw new ArgumentNullException(nameof(net));
    }

    public AuthBootstrapResult? Auth { get; private set; }

    public bool IsAuthenticated => Auth is not null;

    public event Action<AuthBootstrapResult>? Authenticated;
    public event Action? LoggedOut;

    public void SetAuthenticated(AuthBootstrapResult result, UserRole role = UserRole.User)
    {
        Auth = result ?? throw new ArgumentNullException(nameof(result));

        // Atomarer Bootstrap: Auth -> NetworkSession
        _net.ApplyAuth(
            githubUserId: result.GitHubUserId,
            wireGuardPublicKey: result.WireGuardPublicKey,
            role: role
        );

        Authenticated?.Invoke(result);
    }

    public void Logout()
    {
        Auth = null;
        _net.Clear();
        LoggedOut?.Invoke();
    }
}
