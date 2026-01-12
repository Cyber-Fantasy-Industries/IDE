using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GatewayIDE.App.Views.Docker;

// Dev/Prod Umschalter pro Unit
public enum UnitMode
{
    Prod,
    Dev
}

public sealed class UnitRuntime : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private UnitStatus _status = UnitStatus.Unknown;
    public UnitStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private UnitMode _mode = UnitMode.Prod;
    public UnitMode Mode
    {
        get => _mode;
        set => SetField(ref _mode, value);
    }

    private string? _lastError;
    public string? LastError
    {
        get => _lastError;
        set => SetField(ref _lastError, value);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }
}
