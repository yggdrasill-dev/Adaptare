namespace Adaptare;

public interface IMessageExchange
{
	ValueTask<IMessageSender> GetMessageSenderAsync(
		string subject,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default);

	ValueTask<bool> MatchAsync(
		string subject,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default);
}
