using System.ClientModel.Primitives;

namespace Web.Features.Chat;

public sealed class ApimSubscriptionKeyHandler : PipelinePolicy
{
    private const string HeaderName = "api-key";

    private readonly string _subscriptionKey;

    public ApimSubscriptionKeyHandler(string subscriptionKey)
    {
        _subscriptionKey = subscriptionKey;
    }

    public override void Process(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        message.Request.Headers.Set(HeaderName, _subscriptionKey);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override ValueTask ProcessAsync(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        message.Request.Headers.Set(HeaderName, _subscriptionKey);
        return ProcessNextAsync(message, pipeline, currentIndex);
    }
}
