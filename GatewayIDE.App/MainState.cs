// File: GatewayIDE.App/MainState.cs
// Zweck: reine Komposition (Composition Root).

using GatewayIDE.App.Commands;
using GatewayIDE.App.ViewModels;      // MainLayoutState
using GatewayIDE.App.Views.Chat;
using GatewayIDE.App.Views.Docker;
using GatewayIDE.App.Views.KiSystem;

namespace GatewayIDE.App;

public sealed class MainState
{
    public MainLayoutState Layout { get; }
    public ChatState Chat { get; }
    public ThreadRouter Threads { get; }
    public DockerUi Docker { get; }

    // XAML bindet über Commands.*
    public Commands.MainCommands Commands { get; }

    public MainState()
    {
        Threads = new ThreadRouter();
        Layout  = new MainLayoutState();

        Docker = new DockerUi(Layout);
        Chat   = new ChatState(Threads);

        Commands = new Commands.MainCommands(this);
    }
}
