namespace GatewayIDE.App.ViewModels;

public sealed class UnitConfig
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";

    public string ComposeFile { get; init; } = "docker-compose.yml";
    public string ProjectName { get; init; } = "";

    // docker compose service name
    public string ServiceName { get; init; } = "";

    // docker container name (for exec)
    public string ContainerName { get; init; } = "";
}
