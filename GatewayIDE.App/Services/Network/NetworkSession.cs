// GatewayIDE.App/Services/Network/NetworkSession.cs
namespace GatewayIDE.App.Services.Network;

public enum UserRole { User, Admin, Owner }

public sealed class NetworkSession
{
    public string UserId { get; private set; } = "";
    public string? WireGuardPublicKey { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;

    // <<< DAS ist das, was du überall verwenden solltest >>>
    public bool IsReady =>
        !string.IsNullOrWhiteSpace(UserId) &&
        !string.IsNullOrWhiteSpace(WireGuardPublicKey);

    public void ApplyAuth(string githubUserId, string wireGuardPublicKey, UserRole role = UserRole.User)
    {
        UserId = $"github-{githubUserId}";
        WireGuardPublicKey = wireGuardPublicKey;
        Role = role;
    }

    public void Clear()
    {
        UserId = "";
        WireGuardPublicKey = null;
        Role = UserRole.User;
    }

    public string? RoleHeaderValue => Role switch
    {
        UserRole.Owner => "owner",
        UserRole.Admin => "admin",
        _ => null
    };
}
