using GatewayIDE.App.Commands;
using GatewayIDE.App.Views;

#if !ISOLATION_MODE && FEATURE_CHAT
using GatewayIDE.App.Views.Chat;
#endif

#if !ISOLATION_MODE && FEATURE_KISYSTEM
using GatewayIDE.App.Views.KiSystem;
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

    public MainState(
        LayoutState layout,
        MainCommands commands
#if !ISOLATION_MODE && FEATURE_CHAT
        , ChatState chat
#endif
#if !ISOLATION_MODE && FEATURE_KISYSTEM
        , ThreadRouter threads
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
    }
}
