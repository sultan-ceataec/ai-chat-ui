namespace Web.Features.Chat;

public sealed class ChatException : Exception
{
    public ChatException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
