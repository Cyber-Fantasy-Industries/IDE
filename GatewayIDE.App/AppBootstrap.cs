// File: GatewayIDE.App/AppBootstrap.cs
using System;

using Microsoft.Extensions.DependencyInjection;

using GatewayIDE.App.Services.App;
using GatewayIDE.App.Services.Auth;
using GatewayIDE.App.Services.Network;

// NOTE: DockerService ist statisch -> nicht in DI registrieren
// using GatewayIDE.App.Services.Processes;

namespace GatewayIDE.App;

public static class AppBootstrap
{
    public static IServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();

        // -------------------------
        // Core app services
        // -------------------------
        sc.AddSingleton<AppState>();
        sc.AddSingleton<GatewayIDEConfig>();
        sc.AddSingleton<SettingsService>();
        sc.AddSingleton<RegistryService>();

        // Auth / Network
        sc.AddSingleton<AuthBootstrapService>();
        sc.AddSingleton<NetworkSession>();
        sc.AddSingleton<NetworkApiService>();

        // -------------------------
        // App state (Composition Root)
        // -------------------------
        sc.AddSingleton<MainState>();

        // Commands
        sc.AddSingleton<GatewayIDE.App.Commands.MainCommands>();

        return sc.BuildServiceProvider();
    }
}
