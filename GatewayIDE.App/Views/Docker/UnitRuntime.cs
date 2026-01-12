namespace GatewayIDE.App.ViewModels;

// Dev/Prod Umschalter pro Unit
public enum UnitMode
{
    Prod,
    Dev
}

public sealed class UnitRuntime : ViewModelBase
{
    private UnitStatus _status = UnitStatus.Unknown;
    public UnitStatus Status
    {
        get => _status;
        set { _status = value; Raise(); }
    }

    private UnitMode _mode = UnitMode.Prod;
    public UnitMode Mode
    {
        get => _mode;
        set { _mode = value; Raise(); }
    }

    private string? _lastError;
    public string? LastError
    {
        get => _lastError;
        set { _lastError = value; Raise(); }
    }
}
