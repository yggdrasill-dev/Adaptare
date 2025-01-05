using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Adaptare.Nats;

internal interface INatsConnectionManager
{
	INatsConnection Connection { get; }

	INatsJSContext CreateJsContext();

	IMessageSender CreateMessageSender(
		IServiceProvider serviceProvider,
		INatsSerializerRegistry? natsSerializerRegistry,
		string? sessionReplySubject);

	IMessageSender CreateJetStreamMessageSender(
		IServiceProvider serviceProvider,
		INatsSerializerRegistry? natsSerializerRegistry);
}