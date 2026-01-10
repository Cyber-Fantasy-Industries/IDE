using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GatewayIDE.App.Services.Network;


namespace GatewayIDE.App.Services.Auth;

/// <summary>
/// UI-neutrales Bootstrap für GitHub Device Flow + WireGuard-Key-Erzeugung.
/// </summary>
public sealed class AuthBootstrapService
{
    private readonly HttpClient _http;

    public AuthBootstrapService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public event Action<AuthBootstrapStage, string?>? Status;

    public Func<string, bool>? TryOpenBrowser { get; set; }

    public async Task<AuthBootstrapResult> RunAsync(
        string githubClientId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(githubClientId))
            throw new ArgumentException("GitHub ClientId fehlt.", nameof(githubClientId));

        Status?.Invoke(AuthBootstrapStage.Starting, null);

        Status?.Invoke(AuthBootstrapStage.RequestingDeviceCode, null);
        var flow = new GitHubDeviceFlow(_http, githubClientId);

        var dc = await flow.StartAsync(ct).ConfigureAwait(false);

        Status?.Invoke(AuthBootstrapStage.DeviceCodeReceived, dc.verification_uri);

        var opened = false;
        try
        {
            opened = (TryOpenBrowser?.Invoke(dc.verification_uri) == true) || DefaultOpenBrowser(dc.verification_uri);
        }
        catch
        {
            opened = false;
        }

        Status?.Invoke(AuthBootstrapStage.BrowserOpenAttempted, opened ? "opened" : "failed");

        Status?.Invoke(AuthBootstrapStage.PollingForToken, null);

        var intervalSeconds = Math.Max(3, dc.interval);
        var token = await flow.PollTokenAsync(dc.device_code, intervalSeconds, ct).ConfigureAwait(false);

        Status?.Invoke(AuthBootstrapStage.TokenReceived, null);

        Status?.Invoke(AuthBootstrapStage.FetchingUser, null);

        var user = await flow.GetUserAsync(token.access_token, ct).ConfigureAwait(false);

        Status?.Invoke(AuthBootstrapStage.UserReceived, user.login);

        Status?.Invoke(AuthBootstrapStage.EnsuringWireGuardKeys, null);

        var userKey = $"github-{user.id}";
        WireGuardKeys.EnsureForUser(userKey, out var pubKey);

        Status?.Invoke(AuthBootstrapStage.Completed, pubKey);

        return new AuthBootstrapResult(
            VerificationUri: dc.verification_uri,
            UserCode: dc.user_code,
            DeviceCode: dc.device_code,
            AccessToken: token.access_token,
            GitHubLogin: user.login,
            GitHubUserId: user.id.ToString(),
            WireGuardPublicKey: pubKey
        );
    }

    private static bool DefaultOpenBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public enum AuthBootstrapStage
{
    Starting,
    RequestingDeviceCode,
    DeviceCodeReceived,
    BrowserOpenAttempted,
    PollingForToken,
    TokenReceived,
    FetchingUser,
    UserReceived,
    EnsuringWireGuardKeys,
    Completed
}

public sealed record AuthBootstrapResult(
    string VerificationUri,
    string UserCode,
    string DeviceCode,
    string AccessToken,
    string GitHubLogin,
    string GitHubUserId,
    string WireGuardPublicKey
);
