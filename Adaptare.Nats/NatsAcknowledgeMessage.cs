using NATS.Client.JetStream;

namespace Adaptare.Nats;

public readonly struct NatsAcknowledgeMessage<TMessage>(NatsJSMsg<TMessage> msg) : IAcknowledgeMessage<TMessage>
{
	public TMessage? Data => msg.Data;

	public string Subject => msg.Subject;

	public IEnumerable<MessageHeaderValue>? HeaderValues => msg.Headers
		?.SelectMany(kv => kv.Value
			.Select(v => new MessageHeaderValue(kv.Key, v)));

	public ValueTask AckAsync(CancellationToken cancellationToken = default)
		=> msg.AckAsync(cancellationToken: cancellationToken);

	public ValueTask AckProgressAsync(CancellationToken cancellationToken = default)
		=> msg.AckProgressAsync(cancellationToken: cancellationToken);

	public ValueTask AckTerminateAsync(CancellationToken cancellationToken = default)
		=> msg.AckTerminateAsync(cancellationToken: cancellationToken);

	public ValueTask NakAsync(TimeSpan delay = default, CancellationToken cancellationToken = default)
		=> msg.NakAsync(delay: delay, cancellationToken: cancellationToken);
}