using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GatewayIDE.App.Views.Engines;

public partial class EnginesPanel : UserControl
{
    public EnginesPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);
}
