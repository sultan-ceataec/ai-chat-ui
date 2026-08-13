namespace Web.Features.Chat;

public sealed class ChatSession
{
    private readonly IChatService _chatService;
    private readonly List<ChatMessage> _messages = [];
    private CancellationTokenSource? _cts;

    public ChatSession(IChatService chatService)
    {
        _chatService = chatService;
    }

    public IReadOnlyList<ChatMessage> Messages => _messages;

    public bool IsStreaming { get; private set; }

    public string? Error { get; private set; }

    public event Action? Changed;

    public async Task SendAsync(string userText)
    {
        if (IsStreaming || string.IsNullOrWhiteSpace(userText))
        {
            return;
        }

        Error = null;
        _messages.Add(new ChatMessage(ChatRole.User, userText.Trim()));
        _messages.Add(new ChatMessage(ChatRole.Assistant, string.Empty));
        IsStreaming = true;
        Notify();

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
        }
        catch (OperationCanceledException)
        {
            // User cancelled; keep partial assistant text if any.
        }
        catch (ChatException ex)
        {
            Error = ex.Message;
            if (string.IsNullOrEmpty(_messages[assistantIndex].Content))
            {
                _messages.RemoveAt(assistantIndex);
            }
        }
        catch (Exception ex)
        {
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
        _cts?.Cancel();
    }

    public void Clear()
    {
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
