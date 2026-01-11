using System;
using Microsoft.Extensions.DependencyInjection;
using GatewayIDE.App.Services.App;
using GatewayIDE.App.Services.Auth;
using GatewayIDE.App.Services.Network;

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

        Services = services.BuildServiceProvider();
    }
}
