using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Adaptare.Nats;

internal class NatsConnectionManager(
	NatsConnection natsConnection)
	: INatsConnectionManager
{
	public INatsConnection Connection => natsConnection;

	public INatsJSContext CreateJsContext()
		=> new NatsJSContext(natsConnection);

	public IMessageSender CreateMessageSender(
		IServiceProvider serviceProvider,
		INatsSerializerRegistry? natsSerializerRegistry)
		=> new NatsMessageSender(
			natsSerializerRegistry,
			Connection,
			serviceProvider.GetRequiredService<ILogger<NatsMessageSender>>());

	public IMessageSender CreateJetStreamMessageSender(
		IServiceProvider serviceProvider,
		INatsSerializerRegistry? natsSerializerRegistry)
		=> new JetStreamMessageSender(
			natsSerializerRegistry,
			CreateJsContext(),
			serviceProvider.GetRequiredService<ILogger<JetStreamMessageSender>>());
}