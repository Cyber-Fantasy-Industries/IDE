using Avalonia.Controls;


namespace GatewayIDE.App.Views;

public sealed class LayoutState : ObservableBase
{
    // =========================
    // Tabs
    // =========================
    public const string TAB_DASH     = "Dashboard";
    public const string TAB_DOCK     = "Docker";
    public const string TAB_KI       = "KI System";
    public const string TAB_EXPLORER = "Explorer";
    public const string TAB_ENGINES  = "Engines";
    public const string TAB_GITHUB   = "GitHub";
    public const string TAB_NETWORK  = "Network";
    public const string TAB_SETTINGS = "Settings";

    private string _activeTab = TAB_DASH;
    public string ActiveTab
    {
        get => _activeTab;
        set
        {
            if (_activeTab == value) return;
            _activeTab = value;
            Raise();

            Raise(nameof(IsDashboard));
            Raise(nameof(IsDocker));
            Raise(nameof(IsKiSystem));
            Raise(nameof(IsExplorer));
            Raise(nameof(IsEngines));
            Raise(nameof(IsGitHub));
            Raise(nameof(IsNetwork));
            Raise(nameof(IsSettings));
            Raise(nameof(IsNone));
        }
    }

    // =========================
    // Tab Flags (für XAML)
    // =========================
    public bool IsDashboard => ActiveTab == TAB_DASH;
    public bool IsDocker    => ActiveTab == TAB_DOCK;
    public bool IsKiSystem  => ActiveTab == TAB_KI;
    public bool IsExplorer  => ActiveTab == TAB_EXPLORER;
    public bool IsEngines   => ActiveTab == TAB_ENGINES;
    public bool IsGitHub    => ActiveTab == TAB_GITHUB;
    public bool IsNetwork   => ActiveTab == TAB_NETWORK;
    public bool IsSettings  => ActiveTab == TAB_SETTINGS;
    public bool IsNone =>
        !(IsDashboard || IsDocker || IsKiSystem || IsExplorer || IsEngines || IsGitHub || IsNetwork || IsSettings);

    // =========================
    // Docker-Layout
    // =========================
    private GridLength _topSmallRowHeight = GridLength.Star;
    public GridLength TopSmallRowHeight
    {
        get => _topSmallRowHeight;
        set { _topSmallRowHeight = value; Raise(); }
    }

    private GridLength _topLeftColWidth = GridLength.Star;
    public GridLength TopLeftColWidth
    {
        get => _topLeftColWidth;
        set { _topLeftColWidth = value; Raise(); }
    }

    private GridLength _topRightColWidth = GridLength.Star;
    public GridLength TopRightColWidth
    {
        get => _topRightColWidth;
        set { _topRightColWidth = value; Raise(); }
    }
public void Select(string? raw)
{
    var tab = (raw ?? "").Trim();
    if (string.IsNullOrWhiteSpace(tab)) tab = TAB_DASH;

    ActiveTab = tab.ToLowerInvariant() switch
    {
        "dashboard" => TAB_DASH,
        "docker"    => TAB_DOCK,
        "ki system" => TAB_KI,
        "explorer"  => TAB_EXPLORER,
        "engines"   => TAB_ENGINES,
        "github"    => TAB_GITHUB,
        "network"   => TAB_NETWORK,
        "settings"  => TAB_SETTINGS,
        _           => tab
    };
}


}
