namespace Adaptare.Nats.UnitTests;

internal class StubMessageSession<TMessage> : IMessageSession<TMessage>
{
    public ValueTask HandleAsync(Question<TMessage> question, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}