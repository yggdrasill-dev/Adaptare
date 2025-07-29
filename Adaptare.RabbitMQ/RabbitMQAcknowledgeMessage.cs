using Adaptare.RabbitMQ.Configuration;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;

internal class RabbitMQAcknowledgeMessage<TMessage>(
	string subject,
	TMessage? data,
	IEnumerable<MessageHeaderValue>? headerValues,
	ulong deliveryTag,
	IChannel channel,
	AcknowledgeOptions options) : IAcknowledgeMessage<TMessage>
{
	public string Subject => subject;
	public TMessage? Data => data;
	public IEnumerable<MessageHeaderValue>? HeaderValues => headerValues;

	public ValueTask AckAsync(CancellationToken cancellationToken = default)
		=> channel.BasicAckAsync(deliveryTag, options.Multiple, cancellationToken);

	public ValueTask AckProgressAsync(CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask AckTerminateAsync(CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask NakAsync(TimeSpan delay = default, CancellationToken cancellationToken = default)
		=> channel.BasicNackAsync(
			deliveryTag,
			options.Multiple,
			options.NackRequeue,
			cancellationToken);
}