using System.Text;

namespace Adaptare.RabbitMQ.UnitTests;

internal class StubAcknowledgeMessageHandler : IAcknowledgeMessageHandler<ReadOnlyMemory<byte>>
{
    private static readonly TaskCompletionSource<string> _CompletionSource = new();

    internal static Task<string> GetResultAsync()
        => _CompletionSource.Task;

    public ValueTask HandleAsync(IAcknowledgeMessage<ReadOnlyMemory<byte>> msg, CancellationToken cancellationToken = default)
    {
        _CompletionSource.TrySetResult(Encoding.UTF8.GetString(msg.Data.Span));

        return ValueTask.CompletedTask;
    }
}