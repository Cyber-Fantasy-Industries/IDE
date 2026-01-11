// File: GatewayIDE.App/Services/Network/NetworkApiService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayIDE.App.Services.Network;

public sealed class NetworkApiService
{
    private readonly HttpClient _http;
    private readonly NetworkSession _session;

    public NetworkApiService(HttpClient http, NetworkSession session)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(_session.UserId))
            throw new InvalidOperationException("UserId fehlt (nicht authentifiziert).");

        var req = new HttpRequestMessage(method, relativeUrl);
        req.Headers.TryAddWithoutValidation("x-user-id", _session.UserId);

        var role = _session.RoleHeaderValue;
        if (!string.IsNullOrWhiteSpace(role))
            req.Headers.TryAddWithoutValidation("x-user-role", role);

        return req;
    }

    // ---------- /api/network/* ----------

    public async Task<NetworkStatusDto?> GetStatusAsync(CancellationToken ct = default)
    {
        using var req = NewRequest(HttpMethod.Get, "api/network/status");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<NetworkStatusDto>(cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<PeerSelfDto?> GetSelfPeerAsync(CancellationToken ct = default)
    {
        using var req = NewRequest(HttpMethod.Get, "api/network/peers/self");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<PeerSelfDto>(cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<EnrollResponseDto?> EnrollAsync(string inviteCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            throw new ArgumentException("inviteCode fehlt.");

        if (string.IsNullOrWhiteSpace(_session.WireGuardPublicKey))
            throw new InvalidOperationException("WireGuardPublicKey fehlt in NetworkSession.");

        var payload = new EnrollRequestDto
        {
            InviteCode = inviteCode.Trim(),
            ClientPublicKey = _session.WireGuardPublicKey,
            Device = new EnrollDeviceDto
            {
                DeviceId = Environment.MachineName,
                DeviceName = Environment.MachineName
            }
        };

        using var req = NewRequest(HttpMethod.Post, "api/network/enroll");
        req.Content = JsonContent.Create(payload);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<EnrollResponseDto>(cancellationToken: ct).ConfigureAwait(false);
    }

    // ---------- /api/admin/network/* ----------

    public async Task<AdminInviteCreateResponseDto?> CreateInviteAsync(
        string label,
        int? expiresInMinutes = 60,
        CancellationToken ct = default)
    {
        var payload = new AdminInviteCreateRequestDto
        {
            Label = label,
            ExpiresInMinutes = expiresInMinutes
        };

        using var req = NewRequest(HttpMethod.Post, "api/admin/network/invites");
        req.Content = JsonContent.Create(payload);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<AdminInviteCreateResponseDto>(cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<List<AdminPeerDto>?> ListPeersAsync(CancellationToken ct = default)
    {
        using var req = NewRequest(HttpMethod.Get, "api/admin/network/peers");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<List<AdminPeerDto>>(cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task RevokePeerAsync(string peerId, CancellationToken ct = default)
    {
        using var req = NewRequest(HttpMethod.Post, $"api/admin/network/peers/{peerId}/revoke");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }

    public async Task PatchPeerAsync(string peerId, AdminPeerPatchRequestDto patch, CancellationToken ct = default)
    {
        using var req = NewRequest(HttpMethod.Patch, $"api/admin/network/peers/{peerId}");
        req.Content = JsonContent.Create(patch);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }
}
