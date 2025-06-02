using System.Diagnostics;
using Adaptare.Nats.Configuration;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Adaptare.Nats;

record JetStreamSubscriptionSettings<TMessage>(
	string Subject,
	string Stream,
	ConsumerConfig ConsumerConfig,
	Func<NatsJSMsg<TMessage>, CancellationToken, ValueTask> EventHandler,
	INatsDeserialize<TMessage>? Deserializer)
	: INatsSubscribe
{
	public async ValueTask<IDisposable> SubscribeAsync(INatsConnectionManager connectionManager, CancellationToken cancellationToken = default)
	{
		var js = connectionManager.CreateJsContext();

		var consumer = await js.CreateOrUpdateConsumerAsync(
			Stream,
			ConsumerConfig,
			cancellationToken).ConfigureAwait(false);

		var ctd = new CancellationTokenDisposable(cancellationToken);

		async void Core(INatsJSConsumer jsConsumer, CancellationToken token)
		{
			await foreach (var msg in jsConsumer.ConsumeAsync(
				serializer: Deserializer,
				cancellationToken: token).ConfigureAwait(false))
			{
				using var activity = TraceContextPropagator.TryExtract(
					msg.Headers,
					(header, key) => (header?[key] ?? string.Empty)!,
					out var context)
					? NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
						Subject,
						ActivityKind.Consumer,
						context,
						tags: [
							new KeyValuePair<string, object?>("mq", "NATS"),
							new KeyValuePair<string, object?>("subscription", "JetStream"),
						])
					: NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
						ActivityKind.Consumer,
						name: Subject,
						tags: [
							new KeyValuePair<string, object?>("mq", "NATS"),
							new KeyValuePair<string, object?>("subscription", "JetStream"),
						]);

				if (EventHandler is not null)
					await EventHandler(msg, token).ConfigureAwait(false);
			}
		}

		Core(consumer, ctd.Token);

		return ctd;
		;
	}
}