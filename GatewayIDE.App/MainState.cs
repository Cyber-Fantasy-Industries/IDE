// MainWindowViewModel — FINAL (Core Only)
// Zweck: reine Komposition (Composition Root).
// KEINE UI-Logik, KEINE Commands, KEINE Services.
//

namespace GatewayIDE.App.ViewModels;

public sealed class MainWindowViewModel
{
    public MainLayoutState Layout { get; }
    public ChatState Chat { get; }
    public ThreadRouter Threads { get; }
    public DockerUi Docker { get; }

    // XAML bindet ausschließlich über Commands.*
    public MainCommands Commands { get; }

    public MainWindowViewModel()
    {
        // Basis
        Threads = new ThreadRouter();
        Layout  = new MainLayoutState();

        // Features
        Docker = new DockerUi(Layout);
        Chat   = new ChatState(Threads);

        // Commands (nur Delegation)
        Commands = new MainCommands(this);
    }
}
