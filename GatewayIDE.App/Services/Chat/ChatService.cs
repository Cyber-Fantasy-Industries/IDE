using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace GatewayIDE.App.ViewModels;

public sealed class ChatService
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("http://localhost:8080/")
    };

    private sealed class ChatResponse
    {
        public List<ResponseItem>? Responses { get; set; }
    }

    private sealed class ResponseItem
    {
        public string? agent { get; set; }
        public string? content { get; set; }
    }

    private readonly ThreadRouter _threads;

    public ChatService(ThreadRouter threads)
    {
        _threads = threads;
    }

    public async Task SendPromptAsync(string prompt)
    {
        try
        {
            var req = new { prompt };
            using var resp = await _http.PostAsJsonAsync("chat", req);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var obj = JsonSerializer.Deserialize<ChatResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            var items = obj?.Responses ?? new List<ResponseItem>();
            if (items.Count == 0)
            {
                _threads.Append(ThreadId.T3, "⚠️ Backend lieferte keine Antworten.");
                return;
            }

            foreach (var it in items)
                _threads.AppendAgentReply(it.agent, it.content);
        }
        catch (Exception ex)
        {
            _threads.Append(ThreadId.T3, $"❌ Chat-Fehler: {ex.Message}");
        }
    }
}
