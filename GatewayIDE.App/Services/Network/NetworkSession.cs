// File: GatewayIDE.App/Services/Network/NetworkSession.cs
using System;

namespace GatewayIDE.App.Services.Network;

public enum UserRole
{
    User,
    Admin,
    Owner
}

public sealed class NetworkSession
{
    // Muss gesetzt sein, sonst 401/400 im Backend (x-user-id ist zwingend).
    public string UserId { get; private set; } = "";

    public UserRole Role { get; private set; } = UserRole.User;

    public string? WireGuardPublicKey { get; private set; }

    public string? DeviceId { get; private set; }
    public string? DeviceName { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);

    public void ApplyAuth(
        string githubUserId,
        string wireGuardPublicKey,
        UserRole role = UserRole.User,
        string? deviceId = null,
        string? deviceName = null)
    {
        if (string.IsNullOrWhiteSpace(githubUserId))
            throw new ArgumentException("githubUserId fehlt.");

        UserId = $"github-{githubUserId}";
        WireGuardPublicKey = wireGuardPublicKey;
        Role = role;

        DeviceId = deviceId ?? Environment.MachineName;
        DeviceName = deviceName ?? Environment.MachineName;
    }

    public string? RoleHeaderValue => Role switch
    {
        UserRole.Owner => "owner",
        UserRole.Admin => "admin",
        _ => null
    };
}
