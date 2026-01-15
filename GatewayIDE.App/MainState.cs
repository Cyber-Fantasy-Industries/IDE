using GatewayIDE.App.Commands;
using GatewayIDE.App.Views;
using GatewayIDE.App.Views.Chat;
using GatewayIDE.App.Views.Docker;
using GatewayIDE.App.Views.KiSystem;

namespace GatewayIDE.App;

public sealed class MainState
{
    public LayoutState Layout { get; }
    public ChatState Chat { get; }
    public ThreadRouter Threads { get; }
    public DockerUi Docker { get; }
    public MainCommands Commands { get; }

    public MainState(
        LayoutState layout,
        ChatState chat,
        ThreadRouter threads,
        DockerUi docker,
        MainCommands commands)
    {
        Layout   = layout;
        Chat     = chat;
        Threads  = threads;
        Docker   = docker;
        Commands = commands;
    }
}
