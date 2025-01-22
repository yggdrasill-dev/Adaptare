using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Adaptare.Nats;

internal class NatsGlobMessageExchange(string pattern, string? sessionReplySubject, INatsSerializerRegistry? natsSerializerRegistry) : IMessageExchange
{
	private readonly Glob m_Glob = Glob.Parse(pattern);

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
	{
		var connectionMgr = serviceProvider.GetRequiredService<INatsConnectionManager>();

		return connectionMgr.CreateMessageSender(
			serviceProvider,
			natsSerializerRegistry,
			sessionReplySubject);
	}

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header)
		=> m_Glob.IsMatch(subject);
}