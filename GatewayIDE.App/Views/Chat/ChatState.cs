using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using System;
namespace GatewayIDE.App.ViewModels;

public sealed class ChatState : ViewModelBase, IDisposable
{
    // UI-Model für die Liste
    public sealed class ChatLine
    {
        public string Text { get; }
        public IBrush Brush { get; }
        public ChatLine(string text, IBrush brush) { Text = text; Brush = brush; }
    }

    public void Dispose()
    {
        _threads.Message -= OnThreadMessage;
    }    public ObservableCollection<ChatLine> ChatLines { get; } = new();

    // Sidebar (links)
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

    // Input + „Buffer“ (optional fürs UI, falls du’s noch brauchst)
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

        // ThreadRouter → Chat UI (nur T1/T3 kommen als Event rein)
        _threads.Message += OnThreadMessage;
    }

    // ------------- Sidebar UX -------------
    public void ToggleChatSidebar()
    {
        // 1) Sidepanel ist zu → öffnen + Chat anzeigen
        if (LeftPaneWidth <= 0)
        {
            LeftPaneWidth = 260;
            IsSidePanelShowingChat = true;
            return;
        }

        // 2) Sidepanel ist offen, zeigt aber Dashboard → auf Chat umschalten
        if (!IsSidePanelShowingChat)
        {
            IsSidePanelShowingChat = true;
            return;
        }

        // 3) Sidepanel ist offen und zeigt Chat → schließen
        LeftPaneWidth = 0;
    }

    public void ToggleDashboardSidebar(Action<string> appendTerm)
    {
        appendTerm("[SIDE] Dashboard (via Rail)");

        // 1) Sidepanel ist zu → öffnen + Dashboard anzeigen
        if (LeftPaneWidth <= 0)
        {
            LeftPaneWidth = 260;
            IsSidePanelShowingChat = false;
            return;
        }

        // 2) Sidepanel ist offen, zeigt Chat → auf Dashboard umschalten
        if (IsSidePanelShowingChat)
        {
            IsSidePanelShowingChat = false;
            return;
        }

        // 3) Sidepanel ist offen und zeigt Dashboard → schließen
        LeftPaneWidth = 0;
    }

    // ------------- Send -------------
    public async Task SendAsync()
    {
        var text = (ChatInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        // YOU → T1
        _threads.Append(ThreadId.T1, $"[YOU] {text}");

        _chat.AppendLine($"[YOU] {text}");
        Raise(nameof(ChatBuffer));

        ChatInput = string.Empty;

        await _service.SendPromptAsync(text);
    }

    // ------------- Router → UI -------------
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
            Raise(nameof(ChatSelectedIndex));
        }

        if (Dispatcher.UIThread.CheckAccess()) apply();
        else Dispatcher.UIThread.Post(apply);
    }
}
