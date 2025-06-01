using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Adaptare.Nats.Configuration.Registrations;

internal class JetStreamHandlerRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : IAcknowledgeMessageHandler<TMessage>
{
	private readonly string m_Stream;
	private readonly ConsumerConfig m_ConsumerConfig;
	private readonly INatsSerializerRegistry? m_SerializerRegistry;
	private readonly Func<IServiceProvider, THandler> m_HandlerFactory;

	public string Subject { get; }

	public JetStreamHandlerRegistration(
		string subject,
		string stream,
		ConsumerConfig consumerConfig,
		INatsSerializerRegistry? serializerRegistry,
		Func<IServiceProvider, THandler> handlerFactory)
	{
		ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));
		ArgumentException.ThrowIfNullOrEmpty(stream, nameof(stream));
		ArgumentNullException.ThrowIfNull(consumerConfig, nameof(consumerConfig));

		Subject = subject;
		m_Stream = stream;
		m_ConsumerConfig = consumerConfig;
		m_SerializerRegistry = serializerRegistry;
		m_HandlerFactory = handlerFactory;
	}

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
				m_SerializerRegistry?.GetDeserializer<TMessage>()),
			cancellationToken).ConfigureAwait(false);

	private async ValueTask HandleMessageAsync(MessageDataInfo<NatsJSMsg<TMessage>> dataInfo, CancellationToken cancellationToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				ActivityKind.Server,
				name: Subject,
				tags: [
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				]);

		try
		{
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(false))
			{
				var handler = m_HandlerFactory(scope.ServiceProvider);

				await handler.HandleAsync(
					new NatsAcknowledgeMessage<TMessage>(dataInfo.Msg),
					cts.Token).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);
		}
	}
}