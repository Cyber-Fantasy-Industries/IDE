namespace GatewayIDE.App.Views.Docker;

public sealed class UnitConfig
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";

    public string ComposeFile { get; init; } = "docker-compose.yml";
    public string ProjectName { get; init; } = "";

    // PROD
    public string ServiceName { get; init; } = "";
    public string ContainerName { get; init; } = "";

    // DEV (optional)
    public string? DevServiceName { get; init; }
    public string? DevContainerName { get; init; }

    // Compose profile (optional) – z.B. "dev"
    public string? ComposeProfile { get; init; }

    public bool HasDevMode => !string.IsNullOrWhiteSpace(DevServiceName);
}
