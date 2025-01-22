using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Adaptare.Nats;

internal class JetStreamMessageExchange(string pattern, INatsSerializerRegistry? natsSerializerRegistry) : IMessageExchange
{
	private readonly Glob m_Glob = Glob.Parse(pattern);

	public IMessageSender GetMessageSender(string subject, IServiceProvider serviceProvider)
	{
		var connectionMgr = serviceProvider.GetRequiredService<INatsConnectionManager>();

		return connectionMgr.CreateJetStreamMessageSender(
			serviceProvider,
			natsSerializerRegistry);
	}

	public bool Match(string subject, IEnumerable<MessageHeaderValue> header)
		=> m_Glob.IsMatch(subject);
}