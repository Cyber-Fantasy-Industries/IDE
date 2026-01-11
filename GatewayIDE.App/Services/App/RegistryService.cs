using System.Collections.ObjectModel;
using System.Linq;

namespace GatewayIDE.App.Services.App;

public sealed class RegistryService
{
    private readonly SettingsService _settings;
    private readonly GatewayIDEConfig _cfg;

    public ObservableCollection<NetworkProfile> ConnectedNetworks { get; }
    public ObservableCollection<NetworkProfile> HostedNetworks { get; }
    public ObservableCollection<ServiceProfile> Services { get; }

    public RegistryService(SettingsService settings, GatewayIDEConfig cfg)
    {
        _settings = settings;
        _cfg = cfg;

        ConnectedNetworks = new ObservableCollection<NetworkProfile>(_cfg.NetworkRegistry.Connected);
        HostedNetworks = new ObservableCollection<NetworkProfile>(_cfg.NetworkRegistry.Hosted);
        Services = new ObservableCollection<ServiceProfile>(_cfg.ServiceRegistry.Services);
    }

    public void Save()
    {
        _cfg.NetworkRegistry.Connected = ConnectedNetworks.ToList();
        _cfg.NetworkRegistry.Hosted = HostedNetworks.ToList();
        _cfg.ServiceRegistry.Services = Services.ToList();
        _settings.Save(_cfg);
    }

    public ServiceProfile? FindServiceById(string? id)
        => string.IsNullOrWhiteSpace(id) ? null : Services.FirstOrDefault(s => s.Id == id);

    public ServiceProfile GetOrCreateService(string key, string displayName)
    {
        var existing = Services.FirstOrDefault(s => s.Key == key);
        if (existing != null) return existing;

        var created = new ServiceProfile { Key = key, DisplayName = displayName };
        Services.Add(created);
        return created;
    }
}
