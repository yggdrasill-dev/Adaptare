using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration.Registrations;

internal class QueueProcessRegistration<TMessage, TReply, TProcessor>
	: BaseProcessRegistration<TMessage, TReply, TProcessor>
	, ISubscribeRegistration
	where TProcessor : IMessageProcessor<TMessage, TReply>
{
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;

	public string Queue { get; }

	public override string Subject { get; }

	public QueueProcessRegistration(
		string registerName,
		string subject,
		string queue,
		INatsSerializerRegistry? natsSerializerRegistry,
		Func<IServiceProvider, TProcessor> processorFactory)
		: base(registerName, processorFactory)
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
			new NatsQueueScriptionSettings<TMessage>(
				Subject,
				Queue,
				(msg, ct) => HandleMessageAsync(
					new MessageDataInfo<NatsMsg<TMessage>>(
						msg,
						logger,
						serviceProvider),
					ct),
				m_NatsSerializerRegistry?.GetDeserializer<TMessage>()),
			cancellationToken).ConfigureAwait(false);
}