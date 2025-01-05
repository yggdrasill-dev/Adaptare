namespace Adaptare;

public interface IMessageExchange
{
	IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider);

	bool Match(string subject, IEnumerable<MessageHeaderValue> header);
}