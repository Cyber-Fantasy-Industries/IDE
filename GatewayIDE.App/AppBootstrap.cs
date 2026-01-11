// File: GatewayIDE.App/AppBootstrap.cs
using System;

using Microsoft.Extensions.DependencyInjection;

using GatewayIDE.App.Services.App;
using GatewayIDE.App.Services.Auth;
using GatewayIDE.App.Services.Network;

namespace GatewayIDE.App;

public static class AppBootstrap
{
    public static IServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();

        // -------------------------
        // Settings / Config
        // -------------------------
        sc.AddSingleton<SettingsService>();

        // Config wird aus settings.json geladen (inkl. ENV override GATEWAY_NETWORK_API)
        sc.AddSingleton(sp => sp.GetRequiredService<SettingsService>().Load());

        // Registry basiert auf Config
        sc.AddSingleton<RegistryService>();

        // -------------------------
        // Network / Auth Core
        // -------------------------
        sc.AddSingleton<NetworkSession>();
        sc.AddSingleton<AppState>();

        // AuthBootstrap braucht HttpClient (für GitHub device flow)
        sc.AddHttpClient<AuthBootstrapService>();

        // NetworkApiService braucht HttpClient + BaseAddress aus Config
        sc.AddHttpClient<NetworkApiService>((sp, http) =>
        {
            var cfg = sp.GetRequiredService<GatewayIDEConfig>();
            http.BaseAddress = new Uri(cfg.NetworkApiBaseUrl);
        });

        // -------------------------
        // App state (Composition Root)
        // -------------------------
        sc.AddSingleton<MainState>();

        // Commands
        sc.AddSingleton<GatewayIDE.App.Commands.MainCommands>();

        // Views / VMs
        sc.AddSingleton<GatewayIDE.App.Views.Network.NetworkPanelViewModel>();

        return sc.BuildServiceProvider();
    }
}
