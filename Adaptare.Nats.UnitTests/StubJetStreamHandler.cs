﻿namespace Adaptare.Nats.UnitTests;

internal class StubJetStreamHandler<TMessage> : IAcknowledgeMessageHandler<TMessage>
{
    public ValueTask HandleAsync(IAcknowledgeMessage<TMessage> msg, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}