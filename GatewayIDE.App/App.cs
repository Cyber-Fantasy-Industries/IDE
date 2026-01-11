using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using GatewayIDE.App.ViewModels;

namespace GatewayIDE.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    public override void Initialize()
    {
        // 🔑 DI / AppState / NetworkSession bootstrap
        AppBootstrap.Init();

        // 👉 ServiceProvider einmal übernehmen
        Services = AppBootstrap.Services;

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var msg = "[UNHANDLED] " + (e.ExceptionObject?.ToString() ?? "unknown");
            System.IO.File.AppendAllText("IDE-crash.log", msg + Environment.NewLine);
        }
        catch { /* ignore */ }
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            var msg = "[UNOBSERVED] " + e.Exception;
            System.IO.File.AppendAllText("IDE-crash.log", msg + Environment.NewLine);
        }
        catch { /* ignore */ }
    }
}
