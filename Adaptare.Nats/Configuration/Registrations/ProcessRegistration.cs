using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration.Registrations;

internal class ProcessRegistration<TMessage, TReply, TProcessor>
	: BaseProcessRegistration<TMessage, TReply, TProcessor>
	, ISubscribeRegistration
	where TProcessor : IMessageProcessor<TMessage, TReply>
{
	private readonly INatsSerializerRegistry? m_SerializerRegistry;

	public override string Subject { get; }

	public ProcessRegistration(
		string registerName,
		string subject,
		INatsSerializerRegistry? serializerRegistry,
		Func<IServiceProvider, TProcessor> processorFactory)
		: base(registerName, processorFactory)
	{
		ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));

		Subject = subject;
		m_SerializerRegistry = serializerRegistry;
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
				m_SerializerRegistry?.GetDeserializer<TMessage>()),
			cancellationToken).ConfigureAwait(false);
}