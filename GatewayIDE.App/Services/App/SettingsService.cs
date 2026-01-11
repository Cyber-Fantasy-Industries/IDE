using System;
using System.IO;
using System.Text.Json;

namespace GatewayIDE.App.Services.App;

public sealed class SettingsService
{
    private readonly string _path;

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GatewayIDE"
        );
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }

    public GatewayIDEConfig Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                var cfg = new GatewayIDEConfig();
                Save(cfg);
                return cfg;
            }

            var json = File.ReadAllText(_path);
            var cfg2 = JsonSerializer.Deserialize<GatewayIDEConfig>(json) ?? new GatewayIDEConfig();

            // ENV override optional (damit du schnell umschalten kannst)
            var envBase = (Environment.GetEnvironmentVariable("GATEWAY_NETWORK_API") ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(envBase))
                cfg2.NetworkApiBaseUrl = envBase;

            return cfg2;
        }
        catch
        {
            // fail-safe default
            return new GatewayIDEConfig();
        }
    }

    public void Save(GatewayIDEConfig cfg)
    {
        var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
