using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using GatewayIDE.App.Services.Chat;
using GatewayIDE.App.Views.KiSystem;

namespace GatewayIDE.App.Views.Chat;

public abstract class ObservableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ChatState : ObservableBase, IDisposable
{
    public sealed class ChatLine
    {
        public string Text { get; }
        public IBrush Brush { get; }
        public ChatLine(string text, IBrush brush) { Text = text; Brush = brush; }
    }

    public ObservableCollection<ChatLine> ChatLines { get; } = new();

    private double _leftPaneWidth = 260;
    public double LeftPaneWidth
    {
        get => _leftPaneWidth;
        set { _leftPaneWidth = value; Raise(); }
    }

    private bool _isSidePanelShowingChat = true;
    public bool IsSidePanelShowingChat
    {
        get => _isSidePanelShowingChat;
        set
        {
            if (_isSidePanelShowingChat == value) return;
            _isSidePanelShowingChat = value;
            Raise();
            Raise(nameof(IsSidePanelShowingDashboard));
        }
    }

    public bool IsSidePanelShowingDashboard => !IsSidePanelShowingChat;

    private readonly StringBuilder _chat = new();
    public string ChatBuffer => _chat.ToString();

    private string _chatInput = string.Empty;
    public string ChatInput
    {
        get => _chatInput;
        set { _chatInput = value; Raise(); }
    }

    private int _chatSelectedIndex = -1;
    public int ChatSelectedIndex
    {
        get => _chatSelectedIndex;
        set { _chatSelectedIndex = value; Raise(); }
    }

    private readonly ThreadRouter _threads;
    private readonly ChatService _service;

    public ChatState(ThreadRouter threads)
    {
        _threads = threads;
        _service = new ChatService(threads);

        _threads.Message += OnThreadMessage;
    }

    public void Dispose()
        => _threads.Message -= OnThreadMessage;

    public void ToggleChatSidebar()
    {
        if (LeftPaneWidth <= 0) { LeftPaneWidth = 260; IsSidePanelShowingChat = true; return; }
        if (!IsSidePanelShowingChat) { IsSidePanelShowingChat = true; return; }
        LeftPaneWidth = 0;
    }

    public void ToggleDashboardSidebar(Action<string> appendTerm)
    {
        appendTerm("[SIDE] Dashboard (via Rail)");

        if (LeftPaneWidth <= 0) { LeftPaneWidth = 260; IsSidePanelShowingChat = false; return; }
        if (IsSidePanelShowingChat) { IsSidePanelShowingChat = false; return; }
        LeftPaneWidth = 0;
    }

    public async Task SendAsync()
    {
        var text = (ChatInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        _threads.Append(ThreadId.T1, $"[YOU] {text}");

        _chat.AppendLine($"[YOU] {text}");
        Raise(nameof(ChatBuffer));

        ChatInput = string.Empty;

        await _service.SendPromptAsync(text);
    }

    private void OnThreadMessage(ThreadId id, string text)
    {
        void apply()
        {
            var brush = id switch
            {
                ThreadId.T1 => Brushes.DodgerBlue,
                ThreadId.T3 => Brushes.OrangeRed,
                _ => Brushes.Gray
            };

            ChatLines.Add(new ChatLine(text, brush));
            ChatSelectedIndex = ChatLines.Count - 1;
        }

        if (Dispatcher.UIThread.CheckAccess()) apply();
        else Dispatcher.UIThread.Post(apply);
    }
}
