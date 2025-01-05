using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Exchanges;

public class AlwaysMatchMessageExchange<TMessageSender> : IMessageExchange
	where TMessageSender : class, IMessageSender
{
	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
		=> serviceProvider.GetRequiredService<TMessageSender>();

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header) => true;
}