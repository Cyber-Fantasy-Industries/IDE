using System.Collections.ObjectModel;

namespace GatewayIDE.App.ViewModels;

public sealed class DockerUnitsCatalog : ViewModelBase
{
    public ObservableCollection<ServiceUnitVm> Units { get; } = new();

    private ServiceUnitVm? _selectedUnit;
    public ServiceUnitVm? SelectedUnit
    {
        get => _selectedUnit;
        set { _selectedUnit = value; Raise(); }
    }

    public void SetUnits(IEnumerable<ServiceUnitVm> units)
    {
        Units.Clear();
        foreach (var u in units)
            Units.Add(u);

        SelectedUnit = Units.FirstOrDefault();
    }
}
