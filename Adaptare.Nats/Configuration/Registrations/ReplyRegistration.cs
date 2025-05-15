using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration.Registrations;

internal class ReplyRegistration<TMessage, THandler>
	: BaseReplyRegistration<TMessage, THandler>
	, ISubscribeRegistration
	where THandler : IMessageHandler<TMessage>
{
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;

	public override string Subject { get; }

	public ReplyRegistration(
		string registerName,
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry,
		Func<IServiceProvider, THandler> handlerFactory)
		: base(registerName, handlerFactory)
	{
		ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));

		Subject = subject;
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