using System;
using Microsoft.Extensions.DependencyInjection;

using GatewayIDE.App.Commands;
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

        // -------------------------
        // UI State (Single Owner)
        // -------------------------
        sc.AddSingleton<LayoutState>();
        sc.AddSingleton<ThreadRouter>();
        sc.AddSingleton<ChatState>();
        sc.AddSingleton<DockerUi>();

        // -------------------------
        // Commands (App-level)
        // -------------------------
        sc.AddSingleton<MainCommands>();

        // -------------------------
        // UI Root
        // -------------------------
        sc.AddSingleton<MainState>();

        return sc.BuildServiceProvider();
    }
}
