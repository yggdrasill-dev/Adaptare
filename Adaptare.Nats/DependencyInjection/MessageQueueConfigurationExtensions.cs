using Adaptare.Configuration;
using Adaptare.Nats;
using NATS.Client.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddNatsGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		INatsSerializerRegistry? natsSerializerRegistry = null)
		=> AddNatsGlobPatternExchange(
			configuration,
			string.Empty,
			glob,
			natsSerializerRegistry);

	public static MessageQueueConfiguration AddNatsGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string registerName,
		string glob,
		INatsSerializerRegistry? natsSerializerRegistry = null)
		=> configuration.AddExchange(new NatsGlobMessageExchange(
			registerName,
			glob,
			natsSerializerRegistry));

	public static MessageQueueConfiguration AddNatsJetStreamGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		INatsSerializerRegistry? natsSerializerRegistry = null)
		=> configuration
			.AddExchange(new JetStreamMessageExchange(
				glob,
				natsSerializerRegistry));
}