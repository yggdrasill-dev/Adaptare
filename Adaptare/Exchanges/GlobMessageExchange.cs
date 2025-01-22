using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Exchanges;

internal class GlobMessageExchange<TMessageSender>(string pattern) : IMessageExchange
	where TMessageSender : class, IMessageSender
{
	private readonly Glob m_Glob = Glob.Parse(pattern);

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
		=> serviceProvider.GetRequiredService<TMessageSender>();

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header)
		=> m_Glob.IsMatch(subject);
}