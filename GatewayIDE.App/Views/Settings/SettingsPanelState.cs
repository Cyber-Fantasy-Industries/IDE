using System.Collections.ObjectModel;
using System.Linq;

namespace GatewayIDE.App.Views.Settings;

public sealed class SettingsPanelState
{
    // Tabs
    public int SelectedTabIndex { get; set; } = 0;

    // Sub-sections
    public GeneralSettingsVm General { get; } = new();
    public EditorSettingsVm Editor { get; } = new();
    public ServicesSettingsVm Services { get; } = new();

    // Commands (werden von SettingsPanelCommands gesetzt)
    public SettingsPanelCommands Commands { get; }

    public SettingsPanelState()
    {
        Commands = new SettingsPanelCommands(this);

        // Dummy data (damit UI sofort lebt)
        Services.Items.Add(new ServiceSettingsVm
        {
            Name = "leona",
            Version = "0.1",
            StatusText = "not installed",
            Description = "AI backend service",
            Settings = new ObservableCollection<KvSetting>
            {
                new() { Key = "LEONA_PORT", Value = "8082" },
                new() { Key = "LEONA_ENV",  Value = "dev" }
            }
        });

        Services.Items.Add(new ServiceSettingsVm
        {
            Name = "network",
            Version = "0.1",
            StatusText = "installed",
            Description = "overlay network service",
            Settings = new ObservableCollection<KvSetting>
            {
                new() { Key = "WG_SUBNET", Value = "10.77.0.0/16" },
                new() { Key = "WG_DNS",    Value = "1.1.1.1" }
            }
        });

        Services.Selected = Services.Items.FirstOrDefault();
    }

    // Expose commands for bindings
    public object SaveCommand => Commands.SaveCommand;
    public object ResetCommand => Commands.ResetCommand;
    public object ReloadServiceSettingsCommand => Commands.ReloadServiceSettingsCommand;
    public object ApplyServiceSettingsCommand => Commands.ApplyServiceSettingsCommand;
    public object OpenServiceLogsCommand => Commands.OpenServiceLogsCommand;
}

public sealed class GeneralSettingsVm
{
    public string WorkspacePath { get; set; } = @"C:\Projects\GatewayIDE";
    public ObservableCollection<string> ThemeOptions { get; } = new() { "Dark", "Light" };
    public string SelectedTheme { get; set; } = "Dark";

    public ObservableCollection<string> LanguageOptions { get; } = new() { "de-DE", "en-US" };
    public string SelectedLanguage { get; set; } = "de-DE";

    public bool AutoStartServices { get; set; } = true;
    public bool TelemetryEnabled { get; set; } = false;

    public string InfoText { get; set; } = "Settings scaffold ready.";
}

public sealed class EditorSettingsVm
{
    public string FontFamily { get; set; } = "Cascadia Mono";
    public string FontSize { get; set; } = "13";
    public string TabSize { get; set; } = "4";

    public bool AutoSave { get; set; } = false;
    public bool FormatOnSave { get; set; } = true;
    public bool WordWrap { get; set; } = true;
}

public sealed class ServicesSettingsVm
{
    public string Search { get; set; } = "";
    public ObservableCollection<ServiceSettingsVm> Items { get; } = new();
    public ServiceSettingsVm? Selected { get; set; }
}

public sealed class ServiceSettingsVm
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string Description { get; set; } = "";
    public ObservableCollection<KvSetting> Settings { get; set; } = new();
}

public sealed class KvSetting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
