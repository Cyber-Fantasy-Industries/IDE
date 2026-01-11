// File: GatewayIDE.App/Views/Chat/SidePanel.cs
using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace GatewayIDE.App.Views.Chat;

/// <summary>
/// Chat side panel. Kept minimal to avoid compile issues during refactors.
/// </summary>
public partial class SidePanel : UserControl
{
    public SidePanel()
    {
        InitializeComponent();

        // No explicit VisualTreeAttachmentEventArgs signature needed; avoids missing-using errors.
        this.AttachedToVisualTree += (_, __) =>
        {
            // Optional: focus input if present (name depends on your XAML).
            var tb = this.FindControl<TextBox>("ChatInput");
            tb?.Focus();
        };
    }

    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);

    // Optional: Enter-to-send behavior (if you wire KeyDown in XAML)
    public void ChatInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Shift) == 0)
        {
            e.Handled = true;
            // Leave send action to VM/Commands binding.
        }
    }
}
