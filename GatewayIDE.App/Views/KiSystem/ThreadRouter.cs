using System.Text;

namespace GatewayIDE.App.ViewModels;

public enum ThreadId { T1 = 1, T2 = 2, T3 = 3, T4 = 4, T5 = 5, T6 = 6 }

public sealed class ThreadBuffers : ViewModelBase
{
    private readonly StringBuilder _t2 = new(), _t4 = new(), _t5 = new(), _t6 = new();

    public string T2Buffer => _t2.ToString();
    public string T4Buffer => _t4.ToString();
    public string T5Buffer => _t5.ToString();
    public string T6Buffer => _t6.ToString();

    public int T2Caret => _t2.Length;
    public int T4Caret => _t4.Length;
    public int T5Caret => _t5.Length;
    public int T6Caret => _t6.Length;

    internal void AppendTo(ThreadId id, string text)
    {
        switch (id)
        {
            case ThreadId.T2:
                _t2.AppendLine(text);
                Raise(nameof(T2Buffer));
                Raise(nameof(T2Caret));
                break;

            case ThreadId.T4:
                _t4.AppendLine(text);
                Raise(nameof(T4Buffer));
                Raise(nameof(T4Caret));
                break;

            case ThreadId.T5:
                _t5.AppendLine(text);
                Raise(nameof(T5Buffer));
                Raise(nameof(T5Caret));
                break;

            case ThreadId.T6:
                _t6.AppendLine(text);
                Raise(nameof(T6Buffer));
                Raise(nameof(T6Caret));
                break;
        }
    }
}

public sealed class ThreadRouter
{
    public ThreadBuffers Buffers { get; } = new();

    /// <summary>
    /// Für T1/T3 (sichtbar im Chat) feuern wir ein Event.
    /// Für T2/T4/T5/T6 schreiben wir in Buffers.
    /// </summary>
    public event Action<ThreadId, string>? Message;

    public void Append(ThreadId id, string text)
    {
        if (id == ThreadId.T1 || id == ThreadId.T3)
        {
            Message?.Invoke(id, text);
            return;
        }

        Buffers.AppendTo(id, text);
    }

    /// <summary>
    /// Zentraler Agent→Thread Router (Logik aus dem Original, nur ausgelagert).
    /// </summary>
    public void AppendAgentReply(string? agent, string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var a = (agent ?? "").Trim().ToUpperInvariant();

        switch (a)
        {
            case "SOM":
                Append(ThreadId.T1, $"[SOM]\n{content}");
                break;

            case "SOM:INNER":
                Append(ThreadId.T2, content!);
                break;

            case "TASKMANAGER":
                Append(ThreadId.T4, $"[TaskManager]\n{content}");
                break;

            case "LIBRARIAN":
                Append(ThreadId.T5, $"[Librarian]\n{content}");
                break;

            case "TRAINER":
                Append(ThreadId.T6, $"[Trainer]\n{content}");
                break;

            case "RETURN":
                Append(ThreadId.T3, content!);
                break;

            default:
                Append(ThreadId.T1, $"[{(agent ?? "HMA")}]\n{content}");
                break;
        }
    }
}

