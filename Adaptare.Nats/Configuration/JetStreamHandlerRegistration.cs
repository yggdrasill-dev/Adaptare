﻿using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Adaptare.Nats.Configuration;

internal class JetStreamHandlerRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : IAcknowledgeMessageHandler<TMessage>
{
	private readonly string m_Stream;
	private readonly ConsumerConfig m_ConsumerConfig;
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;
	private readonly Func<IServiceProvider, THandler> m_HandlerFactory;

	public JetStreamHandlerRegistration(
		string subject,
		string stream,
		ConsumerConfig consumerConfig,
		INatsSerializerRegistry? natsSerializerRegistry,
		Func<IServiceProvider, THandler> handlerFactory)
	{
		if (string.IsNullOrEmpty(stream))
			throw new ArgumentException($"'{nameof(stream)}' 不可為 Null 或空白。", nameof(stream));

		Subject = subject;
		m_Stream = stream;
		m_ConsumerConfig = consumerConfig ?? throw new ArgumentNullException(nameof(consumerConfig));
		m_NatsSerializerRegistry = natsSerializerRegistry;
		m_HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
	}

	public string Subject { get; }

	public async ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> await receiver.SubscribeAsync(
			new JetStreamSubscriptionSettings<TMessage>(
				Subject,
				m_Stream,
				m_ConsumerConfig,
				(msg, ct) => HandleMessageAsync(
					new MessageDataInfo<NatsJSMsg<TMessage>>(
						msg,
						logger,
						serviceProvider),
					ct),
				m_NatsSerializerRegistry?.GetDeserializer<TMessage>()),
			cancellationToken).ConfigureAwait(false);

	private async ValueTask HandleMessageAsync(MessageDataInfo<NatsJSMsg<TMessage>> dataInfo, CancellationToken cancellationToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Msg.Headers,
			(header, key) => (header?[key] ?? string.Empty)!,
			out var context)
			? NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				Subject,
				ActivityKind.Server,
				context,
				tags: [
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				])
			: NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				ActivityKind.Server,
				name: Subject,
				tags: [
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				]);

		try
		{
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using var d = scope.ConfigureAwait(false);
			var handler = m_HandlerFactory(scope.ServiceProvider);
			var natsSender = scope.ServiceProvider.GetRequiredService<INatsMessageQueueService>();

			await handler.HandleAsync(
				new NatsAcknowledgeMessage<TMessage>(dataInfo.Msg),
				cts.Token).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(
					ex,
					cts.Token).ConfigureAwait(false);
		}
	}
}