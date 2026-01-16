using System;
using Microsoft.Extensions.DependencyInjection;

using GatewayIDE.App.Commands;
using GatewayIDE.App.Services.App;
using GatewayIDE.App.Services.Network; // IMMER: AppState braucht NetworkSession
using GatewayIDE.App.Views;

#if FEATURE_NETWORK
using System.Net.Http;
#endif

#if FEATURE_AUTH
using GatewayIDE.App.Services.Auth;
#endif

#if FEATURE_CHAT
using GatewayIDE.App.Services.Chat;
using GatewayIDE.App.Views.Chat;
#endif

#if FEATURE_KISYSTEM
using GatewayIDE.App.Views.KiSystem;
#endif

#if FEATURE_DOCKER
using GatewayIDE.App.Views.Docker;
#endif

namespace GatewayIDE.App;

public static class AppBootstrap
{
    public static IServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();

        // =========================
        // Core UI (immer)
        // =========================
        sc.AddSingleton<LayoutState>();

        // =========================
        // App / Settings / Registry (immer)
        // =========================
        sc.AddSingleton<SettingsService>();
        sc.AddSingleton<GatewayIDEConfig>();
        sc.AddSingleton<RegistryService>();
        sc.AddSingleton<AppState>();

        // =========================
        // Core: NetworkSession (immer)
        // - AppState nutzt ApplyAuth/Clear
        // - Panel/UI bleibt trotzdem über FEATURE_NETWORK aus
        // =========================
        sc.AddSingleton<NetworkSession>();

#if FEATURE_NETWORK
        // =========================
        // Optional: Network API
        // =========================
        sc.AddSingleton(sp =>
        {
            var cfg = sp.GetRequiredService<GatewayIDEConfig>();
            return new HttpClient { BaseAddress = new Uri(cfg.NetworkApiBaseUrl, UriKind.Absolute) };
        });

        sc.AddSingleton<NetworkApiService>();
#endif

#if FEATURE_AUTH
        sc.AddSingleton<AuthBootstrapService>();
#endif

#if FEATURE_CHAT
        sc.AddSingleton<ChatService>();
#endif

#if !ISOLATION_MODE
// =========================
// Feature UI state
// =========================
#if !ISOLATION_MODE && FEATURE_KISYSTEM
sc.AddSingleton<ThreadRouter>();
#endif

#if !ISOLATION_MODE && FEATURE_CHAT
sc.AddSingleton<ChatState>();
#endif

// =========================
// Panel states (NICHT an ISOLATION_MODE koppeln)
// =========================
#if FEATURE_DASHBOARD
sc.AddSingleton<GatewayIDE.App.Views.Dashboard.DashboardPanelState>();
#endif

#if FEATURE_DOCKER
sc.AddSingleton<DockerPanelState>();
#endif

#if FEATURE_EXPLORER
sc.AddSingleton<GatewayIDE.App.Views.Explorer.ExplorerPanelState>();
#endif

#if FEATURE_ENGINES
sc.AddSingleton<GatewayIDE.App.Views.Engines.EnginesPanelState>();
#endif

#if FEATURE_GITHUB
sc.AddSingleton<GatewayIDE.App.Views.GitHub.GitHubPanelState>();
#endif

#if FEATURE_KISYSTEM
sc.AddSingleton<GatewayIDE.App.Views.KiSystem.KiSystemPanelState>();
#endif

#if FEATURE_NETWORK
sc.AddSingleton<GatewayIDE.App.Views.Network.NetworkPanelState>();
#endif

#if FEATURE_SETTINGS
sc.AddSingleton<GatewayIDE.App.Views.Settings.SettingsPanelState>();
#endif

#endif // !ISOLATION_MODE

        // =========================
        // Commands + UI Root (immer)
        // =========================
        sc.AddSingleton<MainCommands>();
        sc.AddSingleton<MainState>();

        return sc.BuildServiceProvider();
    }
}
