using GatewayIDE.App.Commands;
using GatewayIDE.App.Views;

namespace GatewayIDE.App;

public sealed class MainState
{
    public LayoutState Layout { get; }
    public MainCommands Commands { get; }

#if !ISOLATION_MODE
    public GatewayIDE.App.Views.Chat.ChatState Chat { get; }
    public GatewayIDE.App.Views.KiSystem.ThreadRouter Threads { get; }

    public GatewayIDE.App.Views.Dashboard.DashboardPanelState Dashboard { get; }
    public GatewayIDE.App.Views.Docker.DockerPanelState Docker { get; }
    public GatewayIDE.App.Views.Explorer.ExplorerPanelState Explorer { get; }
    public GatewayIDE.App.Views.Engines.EnginesPanelState Engines { get; }
    public GatewayIDE.App.Views.GitHub.GitHubPanelState GitHub { get; }
    public GatewayIDE.App.Views.KiSystem.KiSystemPanelState KiSystem { get; }
    public GatewayIDE.App.Views.Network.NetworkPanelState Network { get; }
    public GatewayIDE.App.Views.Settings.SettingsPanelState Settings { get; }
#endif

#if ISOLATION_MODE
    public MainState(LayoutState layout, MainCommands commands)
    {
        Layout = layout;
        Commands = commands;
    }
#else
    public MainState(
        LayoutState layout,
        GatewayIDE.App.Views.Chat.ChatState chat,
        GatewayIDE.App.Views.KiSystem.ThreadRouter threads,
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
        Commands = commands;

        Chat = chat;
        Threads = threads;

        Dashboard = dashboard;
        Docker = docker;
        Explorer = explorer;
        Engines = engines;
        GitHub = gitHub;
        KiSystem = kiSystem;
        Network = network;
        Settings = settings;
    }
#endif
}
