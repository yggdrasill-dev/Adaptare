using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Exchanges;

internal class GlobMessageExchange<TMessageSender>(string pattern) : IMessageExchange
	where TMessageSender : class, IMessageSender
{
	private readonly Glob m_Glob = Glob.Parse(pattern);

	public ValueTask<IMessageSender> GetMessageSenderAsync(
		string subject,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
		=> ValueTask.FromResult<IMessageSender>(serviceProvider.GetRequiredService<TMessageSender>());

	public ValueTask<bool> MatchAsync(
		string subject,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(m_Glob.IsMatch(subject));
}
