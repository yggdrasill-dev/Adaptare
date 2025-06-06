﻿using NATS.Client.Core;

namespace Adaptare.Nats;

internal record NatsSubscriptionSettings<TMessage>(
	string Subject,
	Func<NatsMsg<TMessage>, CancellationToken, ValueTask> EventHandler,
	INatsDeserialize<TMessage>? Deserializer) : INatsSubscribe
{
	public ValueTask<IDisposable> SubscribeAsync(INatsConnectionManager connectionManager, CancellationToken cancellationToken = default)
	{
		var ctd = new CancellationTokenDisposable(cancellationToken);

		async void Core(CancellationToken token)
		{
			await foreach (var msg in connectionManager.Connection.SubscribeAsync(
				Subject,
				serializer: Deserializer,
				cancellationToken: token).ConfigureAwait(false))
			{
				using var activity = msg.StartActivity(
					name: $"Handle {Subject}",
					tags: [
						new KeyValuePair<string, object?>("mq", "NATS")
					]);

				if (EventHandler is not null)
					await EventHandler(msg, token).ConfigureAwait(false);
			}
		}

		Core(ctd.Token);

		return ValueTask.FromResult((IDisposable)ctd);
	}
}