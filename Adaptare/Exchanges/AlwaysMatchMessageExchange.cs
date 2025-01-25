using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Exchanges;

public class AlwaysMatchMessageExchange<TMessageSender> : IMessageExchange
	where TMessageSender : class, IMessageSender
{
	public ValueTask<IMessageSender> GetMessageSenderAsync(
		string subject,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
		=> ValueTask.FromResult<IMessageSender>(serviceProvider.GetRequiredService<TMessageSender>());

	public ValueTask<bool> MatchAsync(
		string subject,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(true);
}
