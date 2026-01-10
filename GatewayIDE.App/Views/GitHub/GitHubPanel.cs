using System;
using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GatewayIDE.App.Services.Auth;

namespace GatewayIDE.App.Views.GitHub;

public partial class GitHubPanel : UserControl
{
    public GitHubPanel()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            const string clientId = "PASTE_YOUR_GITHUB_CLIENT_ID";

            using var http = new HttpClient();
            var auth = new AuthBootstrapService(http);

            var result = await auth.RunAsync(clientId, System.Threading.CancellationToken.None);

            Console.WriteLine($"[GITHUB] login={result.GitHubLogin} id={result.GitHubUserId}");
            Console.WriteLine($"[GITHUB] wg_pub={result.WireGuardPublicKey}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[GITHUB_LOGIN_ERROR] " + ex);
        }
    }
}
