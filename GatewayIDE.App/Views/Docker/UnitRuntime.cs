namespace GatewayIDE.App.ViewModels;

public sealed class UnitRuntime : ViewModelBase
{
    private UnitStatus _status = UnitStatus.Unknown;
    public UnitStatus Status
    {
        get => _status;
        set { _status = value; Raise(); }
    }

    private string? _lastError;
    public string? LastError
    {
        get => _lastError;
        set { _lastError = value; Raise(); }
    }
}
