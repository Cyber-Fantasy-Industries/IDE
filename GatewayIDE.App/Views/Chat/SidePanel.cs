using Avalonia.Controls;
using Avalonia.Input;

namespace GatewayIDE.App.Views.Chat;

public partial class SidePanel : UserControl
{
    public SidePanel()
    {
        InitializeComponent();
    }

    private void ChatInput_KeyDown(object? sender, KeyEventArgs e)
    {
        // MVVM-Bridge: wir delegieren nur weiter.
        // DataContext ist dein MainWindowViewModel.
        if (DataContext is not GatewayIDE.App.ViewModels.MainWindowViewModel vm)
            return;

        // Enter => senden (Shift+Enter erlaubt Zeilenumbruch, wenn du willst)
        if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Shift) == 0)
        {
            if (vm.Commands.SendChatCommand?.CanExecute(null) == true)
                vm.Commands.SendChatCommand.Execute(null);

            e.Handled = true;
        }
    }
}
