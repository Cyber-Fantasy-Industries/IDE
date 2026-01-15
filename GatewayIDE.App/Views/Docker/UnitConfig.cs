namespace GatewayIDE.App.Views.Docker;

public sealed class UnitConfig
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";

    // Compose
    public string? ComposeFile { get; set; }
    public string? ComposeProfile { get; set; }
    public string? ProjectName { get; set; }

    // 🔹 NEU
    public string? EnvFile { get; set; }

    // Runtime
    public string? ServiceName { get; set; }
    public string? ContainerName { get; set; }

    // Dev (optional)
    public string? DevServiceName { get; set; }
    public string? DevContainerName { get; set; }
}
