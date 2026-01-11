namespace GatewayIDE.App.Services.App;

public sealed class GatewayIDEConfig
{
    // Beispiel: später "https://net-gateway.myteam.lan:8080/"
    public string NetworkApiBaseUrl { get; set; } = "http://localhost:8081/";
    public NetworkRegistryConfig NetworkRegistry { get; set; } = new();
    public ServiceRegistryConfig ServiceRegistry { get; set; } = new();
    // optional: extra Einstellungen
    public bool TrustProxyHeaders { get; set; } = false;
}
