// File: GatewayIDE.App/Views/Network/NetworkPanel.cs

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using GatewayIDE.App.Services.App;
using GatewayIDE.App.Services.Network;
using GatewayIDE.App.ViewModels; // DelegateCommand liegt im Namespace GatewayIDE.App.ViewModels

namespace GatewayIDE.App.Views.Network;

public partial class NetworkPanel : UserControl
{
    public NetworkPanel()
    {
        AvaloniaXamlLoader.Load(this);

        // Verdrahtung später (DI / DataContext)
        // DataContext = ...;
    }
}

public sealed class NetworkPanelViewModel : INotifyPropertyChanged
{
    private readonly NetworkApiService _api;
    private readonly NetworkSession _session;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NetworkPanelViewModel(NetworkApiService api, NetworkSession session, AppState appState)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        // ✅ wenn Auth reinkommt, Commands neu evaluieren
        appState.Authenticated += _ => RefreshCanExecutes();
        appState.LoggedOut += () => RefreshCanExecutes();

        RefreshStatusCommand = new DelegateCommand(async _ => await RefreshStatusAsync(), _ => _session.IsReady);
        RefreshSelfPeerCommand = new DelegateCommand(async _ => await RefreshSelfPeerAsync(), _ => _session.IsReady);
        EnrollCommand = new DelegateCommand(async _ => await EnrollAsync(), _ => _session.IsReady && !string.IsNullOrWhiteSpace(InviteCode));

        RefreshPeersCommand = new DelegateCommand(async _ => await RefreshPeersAsync(), _ => _session.IsReady && IsAdmin);
        CreateInviteCommand = new DelegateCommand(async _ => await CreateInviteAsync(), _ => _session.IsReady && IsAdmin);
    }

    // ---------- UI Properties ----------

    private string _log = "";
    public string Log
    {
        get => _log;
        set => Set(ref _log, value);
    }

    private string _statusText = "—";
    public string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    private string _inviteCode = "";
    public string InviteCode
    {
        get => _inviteCode;
        set
        {
            if (Set(ref _inviteCode, value))
            {
                EnrollCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsAdmin => _session.Role is UserRole.Admin or UserRole.Owner;

    private PeerSelfDto? _selfPeer;
    public PeerSelfDto? SelfPeer
    {
        get => _selfPeer;
        set => Set(ref _selfPeer, value);
    }

    public ObservableCollection<AdminPeerDto> Peers { get; } = new();

    private string _newInviteLabel = "default";
    public string NewInviteLabel
    {
        get => _newInviteLabel;
        set => Set(ref _newInviteLabel, value);
    }

    private string _createdInviteCode = "";
    public string CreatedInviteCode
    {
        get => _createdInviteCode;
        set => Set(ref _createdInviteCode, value);
    }

    // ---------- Commands ----------

    public DelegateCommand RefreshStatusCommand { get; }
    public DelegateCommand RefreshSelfPeerCommand { get; }
    public DelegateCommand EnrollCommand { get; }

    public DelegateCommand RefreshPeersCommand { get; }
    public DelegateCommand CreateInviteCommand { get; }

    // ---------- Command Methods ----------

    private async Task RefreshStatusAsync()
    {
        try
        {
            var st = await _api.GetStatusAsync().ConfigureAwait(false);
            StatusText = st?.Status ?? "unknown";
            AppendLog($"Status: {StatusText}");
        }
        catch (Exception ex)
        {
            AppendLog($"❌ Status-Fehler: {ex.Message}");
        }
    }

    private async Task RefreshSelfPeerAsync()
    {
        try
        {
            var peer = await _api.GetSelfPeerAsync().ConfigureAwait(false);
            SelfPeer = peer;
            AppendLog(peer is null ? "SelfPeer: (null)" : $"SelfPeer: {peer.PeerId} {peer.Address}");
        }
        catch (Exception ex)
        {
            AppendLog($"❌ SelfPeer-Fehler: {ex.Message}");
        }
    }

    private async Task EnrollAsync()
    {
        try
        {
            var res = await _api.EnrollAsync(InviteCode).ConfigureAwait(false);
            AppendLog($"✅ Enroll ok: {res?.PeerId ?? "(no peerId)"}");
            await RefreshSelfPeerAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppendLog($"❌ Enroll-Fehler: {ex.Message}");
        }
    }

    private async Task RefreshPeersAsync()
    {
        try
        {
            Peers.Clear();
            var peers = await _api.ListPeersAsync().ConfigureAwait(false) ?? new();
            foreach (var p in peers) Peers.Add(p);

            AppendLog($"Peers geladen: {Peers.Count}");
        }
        catch (Exception ex)
        {
            AppendLog($"❌ Peers-Fehler: {ex.Message}");
        }
    }

    private async Task CreateInviteAsync()
    {
        try
        {
            var res = await _api.CreateInviteAsync(NewInviteLabel, expiresInMinutes: 60).ConfigureAwait(false);
            CreatedInviteCode = res?.InviteCode ?? "";
            AppendLog(string.IsNullOrWhiteSpace(CreatedInviteCode)
                ? "Invite erstellt, aber kein Code zurückbekommen."
                : $"✅ InviteCode: {CreatedInviteCode}");
        }
        catch (Exception ex)
        {
            AppendLog($"❌ Invite-Fehler: {ex.Message}");
        }
    }

    // Optional: Admin Aktionen direkt aus UI (z.B. Button pro Peer)
    public async Task RevokePeerAsync(AdminPeerDto peer)
    {
        if (peer?.PeerId is null) return;
        try
        {
            await _api.RevokePeerAsync(peer.PeerId).ConfigureAwait(false);
            AppendLog($"✅ Revoked: {peer.PeerId}");
            await RefreshPeersAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppendLog($"❌ Revoke-Fehler: {ex.Message}");
        }
    }

    // ---------- Helpers ----------

    private void AppendLog(string line)
    {
        Log = string.IsNullOrWhiteSpace(Log) ? line : $"{Log}\n{line}";
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }

    private void RefreshCanExecutes()
    {
        RefreshStatusCommand.RaiseCanExecuteChanged();
        RefreshSelfPeerCommand.RaiseCanExecuteChanged();
        EnrollCommand.RaiseCanExecuteChanged();
        RefreshPeersCommand.RaiseCanExecuteChanged();
        CreateInviteCommand.RaiseCanExecuteChanged();

        // optional, falls du UI-Elemente an IsAdmin bindest
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAdmin)));
    }
}
