using System;
using GatewayIDE.App.Services.Network;

#if FEATURE_AUTH
using GatewayIDE.App.Services.Auth;
#endif

namespace GatewayIDE.App.Services.App;

public sealed class AppState
{
    private readonly NetworkSession _net;

    public AppState(NetworkSession net)
    {
        _net = net ?? throw new ArgumentNullException(nameof(net));
    }

#if FEATURE_AUTH
    public AuthBootstrapResult? Auth { get; private set; }
    public bool IsAuthenticated => Auth is not null;

    public event Action<AuthBootstrapResult>? Authenticated;
#else
    // Auth ist aus: wir halten nur den Build stabil.
    public bool IsAuthenticated => false;
#endif

    public event Action? LoggedOut;

#if FEATURE_AUTH
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
#else
    // Auth ist aus: diese Methode soll nicht genutzt werden.
    public void SetAuthenticated(object _ignored, UserRole role = UserRole.User)
        => throw new InvalidOperationException("FEATURE_AUTH ist deaktiviert.");
#endif

    public void Logout()
    {
#if FEATURE_AUTH
        Auth = null;
#endif
        _net.Clear();
        LoggedOut?.Invoke();
    }
}
