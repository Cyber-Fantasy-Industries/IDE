using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

using GatewayIDE.App.Commands;
using GatewayIDE.App.Services.App;
using GatewayIDE.App.Services.Auth;
using GatewayIDE.App.Services.Chat;
using GatewayIDE.App.Services.Network;

using GatewayIDE.App.Views;
using GatewayIDE.App.Views.Chat;
using GatewayIDE.App.Views.Docker;
using GatewayIDE.App.Views.KiSystem;
namespace GatewayIDE.App;

public static class AppBootstrap
{
    public static IServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();

        // =========================
        // Core UI state
        // =========================
        sc.AddSingleton<LayoutState>();
        sc.AddSingleton<ThreadRouter>();
        sc.AddSingleton<ChatState>();

        // =========================
        // App / Settings / Registry
        // =========================
        sc.AddSingleton<SettingsService>();
        sc.AddSingleton<GatewayIDEConfig>();   // optional: später aus SettingsService.Load() befüllen
        sc.AddSingleton<RegistryService>();
        sc.AddSingleton<AppState>();

        // =========================
        // Network
        // =========================
        sc.AddSingleton<NetworkSession>();

        // HttpClient für NetworkApiService (BaseAddress aus Config)
        sc.AddSingleton(sp =>
        {
            var cfg = sp.GetRequiredService<GatewayIDEConfig>();
            return new HttpClient { BaseAddress = new Uri(cfg.NetworkApiBaseUrl, UriKind.Absolute) };
        });

        sc.AddSingleton<NetworkApiService>();


        // Feature services (falls Panels sie später brauchen)
        sc.AddSingleton<AuthBootstrapService>();
        sc.AddSingleton<ChatService>();

        // =========================
        // Panel states
        // =========================
        sc.AddSingleton<GatewayIDE.App.Views.Dashboard.DashboardPanelState>();

        // WICHTIG: bei dir heißt der Docker-Facade-Typ "DockerUi"
        sc.AddSingleton<DockerPanelState>();

        sc.AddSingleton<GatewayIDE.App.Views.Explorer.ExplorerPanelState>();
        sc.AddSingleton<GatewayIDE.App.Views.Engines.EnginesPanelState>();
        sc.AddSingleton<GatewayIDE.App.Views.GitHub.GitHubPanelState>();
        sc.AddSingleton<GatewayIDE.App.Views.KiSystem.KiSystemPanelState>();
        sc.AddSingleton<GatewayIDE.App.Views.Network.NetworkPanelState>();
        sc.AddSingleton<GatewayIDE.App.Views.Settings.SettingsPanelState>();

        // Commands
        sc.AddSingleton<MainCommands>();

        // UI Root
        sc.AddSingleton<MainState>();

        return sc.BuildServiceProvider();
    }
}
