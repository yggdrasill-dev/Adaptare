using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration;

internal class SubscribeRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : IMessageHandler<TMessage>
{
	private readonly SessionRegistration<TMessage, InternalHandlerSession<TMessage, THandler>> m_SessionRegistration;

	public SubscribeRegistration(string subject, INatsSerializerRegistry? natsSerializerRegistry, Func<IServiceProvider, THandler> handlerFactory)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		Subject = subject;

		m_SessionRegistration = new SessionRegistration<TMessage, InternalHandlerSession<TMessage, THandler>>(
			subject,
			false,
			natsSerializerRegistry,
			sp => ActivatorUtilities.CreateInstance<InternalHandlerSession<TMessage, THandler>>(
				sp,
				handlerFactory(sp)));
	}

	public string Subject { get; }

	public ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> m_SessionRegistration.SubscribeAsync(
			receiver,
			serviceProvider,
			logger,
			cancellationToken);
}