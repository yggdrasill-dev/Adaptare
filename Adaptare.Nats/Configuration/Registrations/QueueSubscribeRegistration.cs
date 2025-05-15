using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration.Registrations;

internal class QueueSubscribeRegistration<TMessage, THandler>
	: BaseSubscribeRegistration<TMessage, THandler>
	, ISubscribeRegistration
	where THandler : IMessageHandler<TMessage>
{
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;

	public string Queue { get; }

	public override string Subject { get; }

	public QueueSubscribeRegistration(
		string registerName,
		string subject,
		string queue,
		INatsSerializerRegistry? natsSerializerRegistry,
		Func<IServiceProvider, THandler> handlerFactory)
		: base(registerName, handlerFactory)
	{
		ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));
		ArgumentException.ThrowIfNullOrEmpty(queue, nameof(queue));

		Subject = subject;
		Queue = queue;
		m_NatsSerializerRegistry = natsSerializerRegistry;
	}

	public async ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> await receiver.SubscribeAsync(
			new NatsSubscriptionSettings<TMessage>(
				Subject,
				(msg, ct) => HandleMessageAsync(
					new MessageDataInfo<NatsMsg<TMessage>>(
						msg,
						logger,
						serviceProvider),
					ct),
				m_NatsSerializerRegistry?.GetDeserializer<TMessage>()),
			cancellationToken).ConfigureAwait(false);
}