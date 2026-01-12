namespace GatewayIDE.App.ViewModels;

public sealed class DockerUi
{
    public DockerUnitsCatalog Units { get; }
    public DockerState State { get; }
    public DockerController Controller { get; }

    public DockerUi(MainLayoutState layout)
    {
        Units = new DockerUnitsCatalog();
        State = new DockerState();
        Controller = new DockerController(layout, State, Units);

        // 👉 EIN Ort für Unit-Definitionen
        Units.SetUnits(new[]
        {
            new ServiceUnitVm(new UnitConfig
            {
                Id = "network",
                DisplayName = "NETWORK",
                ComposeFile = "docker-compose.yml",
                ProjectName = "gateway-network",

                ServiceName = "network",
                ContainerName = "network-container",

                DevServiceName = "network-dev",
                DevContainerName = "network-dev-container"
            })
        });
    }
}
