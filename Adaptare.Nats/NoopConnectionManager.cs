﻿using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Adaptare.Nats;

internal class NoopConnectionManager : INatsConnectionManager
{
	public INatsConnection Connection => throw new NotSupportedException();

	public INatsJSContext CreateJsContext() => throw new NotSupportedException();

	public IMessageSender CreateMessageSender(
		IServiceProvider serviceProvider,
		INatsSerializerRegistry? natsSerializerRegistry)
		=> serviceProvider.GetRequiredService<NoopMessageQueueService>();

	public IMessageSender CreateJetStreamMessageSender(
		IServiceProvider serviceProvider,
		INatsSerializerRegistry? natsSerializerRegistry)
		=> serviceProvider.GetRequiredService<NoopMessageQueueService>();
}