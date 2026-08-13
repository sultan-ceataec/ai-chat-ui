using Microsoft.Extensions.Logging;

namespace Web.Features.Chat;

public sealed class ChatSession
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatSession> _logger;
    private readonly List<ChatMessage> _messages = [];
    private CancellationTokenSource? _cts;

    public ChatSession(IChatService chatService, ILogger<ChatSession> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public IReadOnlyList<ChatMessage> Messages => _messages;

    public bool IsStreaming { get; private set; }

    public string? Error { get; private set; }

    public event Action? Changed;

    public async Task SendAsync(string userText)
    {
        if (IsStreaming || string.IsNullOrWhiteSpace(userText))
        {
            _logger.LogDebug("Send ignored. IsStreaming={IsStreaming} HasText={HasText}", IsStreaming, !string.IsNullOrWhiteSpace(userText));
            return;
        }

        Error = null;
        _messages.Add(new ChatMessage(ChatRole.User, userText.Trim()));
        _messages.Add(new ChatMessage(ChatRole.Assistant, string.Empty));
        IsStreaming = true;
        Notify();

        _logger.LogDebug("Send started. ConversationMessages={MessageCount}.", _messages.Count);

        _cts = new CancellationTokenSource();
        var assistantIndex = _messages.Count - 1;

        try
        {
            await foreach (var token in _chatService.StreamAsync(_messages.Take(assistantIndex).ToList(), _cts.Token))
            {
                _messages[assistantIndex] = _messages[assistantIndex] with
                {
                    Content = _messages[assistantIndex].Content + token
                };
                Notify();
            }

            _logger.LogDebug(
                "Send completed. AssistantLength={Length}.",
                _messages[assistantIndex].Content.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat request cancelled.");
            _logger.LogDebug(
                "Partial assistant reply kept. Length={Length}.",
                _messages[assistantIndex].Content.Length);
        }
        catch (ChatException ex)
        {
            _logger.LogError(ex, "Chat request failed.");
            Error = ex.Message;
            if (string.IsNullOrEmpty(_messages[assistantIndex].Content))
            {
                _messages.RemoveAt(assistantIndex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat request failed.");
            Error = ex.Message;
            if (string.IsNullOrEmpty(_messages[assistantIndex].Content))
            {
                _messages.RemoveAt(assistantIndex);
            }
        }
        finally
        {
            IsStreaming = false;
            _cts.Dispose();
            _cts = null;
            Notify();
        }
    }

    public void Cancel()
    {
        _logger.LogDebug("Cancel requested. IsStreaming={IsStreaming}.", IsStreaming);
        _cts?.Cancel();
    }

    public void Clear()
    {
        _logger.LogDebug("Clear requested. MessageCount={MessageCount} IsStreaming={IsStreaming}.", _messages.Count, IsStreaming);

        if (IsStreaming)
        {
            Cancel();
        }

        _messages.Clear();
        Error = null;
        Notify();
    }

    private void Notify() => Changed?.Invoke();
}
