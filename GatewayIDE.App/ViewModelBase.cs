using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
namespace GatewayIDE.App;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Raise([CallerMemberName] string? name = null)
    {
        var handler = PropertyChanged;
        if (handler == null) return;

        // Always marshal to UI thread to avoid cross-thread crashes/glitches.
        if (Dispatcher.UIThread.CheckAccess())
        {
            handler(this, new PropertyChangedEventArgs(name));
            return;
        }

        Dispatcher.UIThread.Post(() =>
            handler(this, new PropertyChangedEventArgs(name)));
    }
}
