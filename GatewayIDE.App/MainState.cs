using GatewayIDE.App.Commands;
using GatewayIDE.App.Views;
using GatewayIDE.App.Views.Chat;
using GatewayIDE.App.Views.KiSystem;

namespace GatewayIDE.App;

public sealed class MainState
{
    public LayoutState Layout { get; }
    public ChatState Chat { get; }
    public ThreadRouter Threads { get; }
    public MainCommands Commands { get; }

    public GatewayIDE.App.Views.Dashboard.DashboardPanelState Dashboard { get; }
    public GatewayIDE.App.Views.Docker.DockerPanelState Docker { get; }
    public GatewayIDE.App.Views.Explorer.ExplorerPanelState Explorer { get; }
    public GatewayIDE.App.Views.Engines.EnginesPanelState Engines { get; }
    public GatewayIDE.App.Views.GitHub.GitHubPanelState GitHub { get; }
    public GatewayIDE.App.Views.KiSystem.KiSystemPanelState KiSystem { get; }
    public GatewayIDE.App.Views.Network.NetworkPanelState Network { get; }
    public GatewayIDE.App.Views.Settings.SettingsPanelState Settings { get; }

    public MainState(
        LayoutState layout,
        ChatState chat,
        ThreadRouter threads,
        MainCommands commands,
        GatewayIDE.App.Views.Dashboard.DashboardPanelState dashboard,
        GatewayIDE.App.Views.Docker.DockerPanelState docker,
        GatewayIDE.App.Views.Explorer.ExplorerPanelState explorer,
        GatewayIDE.App.Views.Engines.EnginesPanelState engines,
        GatewayIDE.App.Views.GitHub.GitHubPanelState gitHub,
        GatewayIDE.App.Views.KiSystem.KiSystemPanelState kiSystem,
        GatewayIDE.App.Views.Network.NetworkPanelState network,
        GatewayIDE.App.Views.Settings.SettingsPanelState settings)
    {
        Layout = layout;
        Chat = chat;
        Threads = threads;
        Commands = commands;

        Dashboard = dashboard;
        Docker = docker;
        Explorer = explorer;
        Engines = engines;
        GitHub = gitHub;
        KiSystem = kiSystem;
        Network = network;
        Settings = settings;
    }
}
