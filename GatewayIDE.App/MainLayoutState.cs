using Avalonia.Controls;

namespace GatewayIDE.App.ViewModels;

public sealed class MainLayoutState : ViewModelBase
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

    // Default: Docker
    private string _activeTab = TAB_DOCK;
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


}
