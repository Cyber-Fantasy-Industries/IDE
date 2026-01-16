using GatewayIDE.App.Commands;
using GatewayIDE.App.Views;

#if !ISOLATION_MODE && FEATURE_CHAT
using GatewayIDE.App.Views.Chat;
#endif

#if !ISOLATION_MODE && FEATURE_KISYSTEM
using GatewayIDE.App.Views.KiSystem;
#endif

#if !ISOLATION_MODE && FEATURE_DASHBOARD
using GatewayIDE.App.Views.Dashboard;
#endif

#if !ISOLATION_MODE && FEATURE_EXPLORER
using GatewayIDE.App.Views.Explorer;
#endif

#if !ISOLATION_MODE && FEATURE_ENGINES
using GatewayIDE.App.Views.Engines;
#endif

#if !ISOLATION_MODE && FEATURE_GITHUB
using GatewayIDE.App.Views.GitHub;
#endif

#if !ISOLATION_MODE && FEATURE_DOCKER
using GatewayIDE.App.Views.Docker;
#endif

#if !ISOLATION_MODE && FEATURE_NETWORK
using GatewayIDE.App.Views.Network;
#endif

#if !ISOLATION_MODE && FEATURE_SETTINGS
using GatewayIDE.App.Views.Settings;
#endif

namespace GatewayIDE.App;

public sealed class MainState
{
    public LayoutState Layout { get; }
    public MainCommands Commands { get; }

#if !ISOLATION_MODE && FEATURE_CHAT
    public ChatState Chat { get; }
#endif

#if !ISOLATION_MODE && FEATURE_KISYSTEM
    public ThreadRouter Threads { get; }
#endif

    // Panel DataContexts (für Layout.axaml: DataContext="{Binding X}")
#if !ISOLATION_MODE && FEATURE_DASHBOARD
    public DashboardPanelState Dashboard { get; }
#endif

#if !ISOLATION_MODE && FEATURE_EXPLORER
    public ExplorerPanelState Explorer { get; }
#endif

#if !ISOLATION_MODE && FEATURE_ENGINES
    public EnginesPanelState Engines { get; }
#endif

#if !ISOLATION_MODE && FEATURE_GITHUB
    public GitHubPanelState GitHub { get; }
#endif

#if !ISOLATION_MODE && FEATURE_KISYSTEM
    public KiSystemPanelState KiSystem { get; }
#endif

#if !ISOLATION_MODE && FEATURE_DOCKER
    public DockerPanelState Docker { get; }
#endif

#if !ISOLATION_MODE && FEATURE_NETWORK
    public NetworkPanelState Network { get; }
#endif

#if !ISOLATION_MODE && FEATURE_SETTINGS
    public SettingsPanelState Settings { get; }
#endif

    public MainState(
        LayoutState layout,
        MainCommands commands
#if !ISOLATION_MODE && FEATURE_CHAT
        , ChatState chat
#endif
#if !ISOLATION_MODE && FEATURE_KISYSTEM
        , ThreadRouter threads
#endif

#if !ISOLATION_MODE && FEATURE_DASHBOARD
        , DashboardPanelState dashboard
#endif
#if !ISOLATION_MODE && FEATURE_EXPLORER
        , ExplorerPanelState explorer
#endif
#if !ISOLATION_MODE && FEATURE_ENGINES
        , EnginesPanelState engines
#endif
#if !ISOLATION_MODE && FEATURE_GITHUB
        , GitHubPanelState gitHub
#endif
#if !ISOLATION_MODE && FEATURE_KISYSTEM
        , KiSystemPanelState kiSystem
#endif
#if !ISOLATION_MODE && FEATURE_DOCKER
        , DockerPanelState docker
#endif
#if !ISOLATION_MODE && FEATURE_NETWORK
        , NetworkPanelState network
#endif
#if !ISOLATION_MODE && FEATURE_SETTINGS
        , SettingsPanelState settings
#endif
        )
    {
        Layout = layout;
        Commands = commands;

#if !ISOLATION_MODE && FEATURE_CHAT
        Chat = chat;
#endif

#if !ISOLATION_MODE && FEATURE_KISYSTEM
        Threads = threads;
#endif

#if !ISOLATION_MODE && FEATURE_DASHBOARD
        Dashboard = dashboard;
#endif
#if !ISOLATION_MODE && FEATURE_EXPLORER
        Explorer = explorer;
#endif
#if !ISOLATION_MODE && FEATURE_ENGINES
        Engines = engines;
#endif
#if !ISOLATION_MODE && FEATURE_GITHUB
        GitHub = gitHub;
#endif
#if !ISOLATION_MODE && FEATURE_KISYSTEM
        KiSystem = kiSystem;
#endif
#if !ISOLATION_MODE && FEATURE_DOCKER
        Docker = docker;
#endif
#if !ISOLATION_MODE && FEATURE_NETWORK
        Network = network;
#endif
#if !ISOLATION_MODE && FEATURE_SETTINGS
        Settings = settings;
#endif
    }
}
