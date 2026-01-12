// File: GatewayIDE.App/Services/Network/NetworkDtos.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GatewayIDE.App.Services.Network;

public sealed class NetworkStatusDto
{
    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    [JsonPropertyName("server_endpoint")]
    public string? ServerEndpoint { get; set; }

    [JsonPropertyName("overlay_ip")]
    public string? OverlayIp { get; set; }

    [JsonPropertyName("peer_seen_at")]
    public string? PeerSeenAt { get; set; }

    // ✅ Damit NetworkPanel.cs dto.Status benutzen kann
    [JsonIgnore]
    public string Status => Connected ? "Connected" : "Disconnected";
}


public sealed class PeerSelfDto
{
    public string? PeerId { get; set; }
    public string? PublicKey { get; set; }
    public string? Address { get; set; }
    public string? Dns { get; set; }
    public string? Endpoint { get; set; }
}

public sealed class EnrollDeviceDto
{
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }
}

public sealed class EnrollRequestDto
{
    [JsonPropertyName("invite_code")]
    public string? InviteCode { get; set; }

    [JsonPropertyName("client_public_key")]
    public string? ClientPublicKey { get; set; }

    [JsonPropertyName("device")]
    public EnrollDeviceDto? Device { get; set; }
}

public sealed class EnrollResponseDto
{
    public string? PeerId { get; set; }
    public string? WgConfig { get; set; }  // falls ihr WG config zurückliefert
    public string? Message { get; set; }
}

public sealed class AdminInviteCreateRequestDto
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("expires_in_minutes")]
    public int? ExpiresInMinutes { get; set; }
}

public sealed class AdminInviteCreateResponseDto
{
    public string? InviteCode { get; set; }
    public string? Message { get; set; }
}

public sealed class AdminPeerDto
{
    public string? PeerId { get; set; }
    public string? UserId { get; set; }
    public string? DeviceId { get; set; }
    public string? Name { get; set; }
    public bool? Revoked { get; set; }

    public string? PublicKey { get; set; }
    public string? Address { get; set; }
}

public sealed class AdminPeerPatchRequestDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("revoked")]
    public bool? Revoked { get; set; }
}
