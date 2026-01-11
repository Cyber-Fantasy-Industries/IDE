using System;
using Microsoft.Extensions.DependencyInjection;
using GatewayIDE.App.Services.App;
using GatewayIDE.App.Services.Auth;
using GatewayIDE.App.Services.Network;
using GatewayIDE.App.ViewModels;
using GatewayIDE.App.Views.Network;
namespace GatewayIDE.App;

public static class AppBootstrap
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static void Init()
    {
        var services = new ServiceCollection();

        // Globaler State
        services.AddSingleton<NetworkSession>();
        services.AddSingleton<AppState>();

        // HTTP Services
        services.AddHttpClient<AuthBootstrapService>();
        services.AddHttpClient<NetworkApiService>(c =>
        {
            c.BaseAddress = new Uri("http://localhost:8080/");
        });
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<NetworkPanelViewModel>();
        Services = services.BuildServiceProvider();
    }
}
