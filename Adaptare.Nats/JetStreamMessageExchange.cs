using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Adaptare.Nats;

internal class JetStreamMessageExchange(string pattern, INatsSerializerRegistry? natsSerializerRegistry) : IMessageExchange
{
	private readonly Glob m_Glob = Glob.Parse(pattern);

	public async ValueTask<IMessageSender> GetMessageSenderAsync(
		string subject,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
	{
		var connectionMgr = serviceProvider.GetRequiredService<INatsConnectionManager>();

		return await Task.FromResult(connectionMgr.CreateJetStreamMessageSender(
			serviceProvider,
			natsSerializerRegistry));
	}

	public async ValueTask<bool> MatchAsync(
		string subject,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> await Task.FromResult(m_Glob.IsMatch(subject));
}
