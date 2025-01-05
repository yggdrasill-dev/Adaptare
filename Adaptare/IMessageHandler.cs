namespace Adaptare;

public interface IMessageHandler<in TMessage>
{
	ValueTask HandleAsync(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default);
}