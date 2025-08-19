using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.Configuration;

internal class AcknowledgeRegistration<TMessage, TAcknowledgeHandler>(
	Glob subjectGlob,
	DirectAcknowledgeOptions acknowledgeOptions,
	Func<IServiceProvider, TAcknowledgeHandler> handlerFactory)
	: ISubscribeRegistration
	where TAcknowledgeHandler : class, IAcknowledgeMessageHandler<TMessage>
{
	private readonly Func<IServiceProvider, TAcknowledgeHandler> m_HandlerFactory = handlerFactory
		?? throw new ArgumentNullException(nameof(handlerFactory));

	public Glob SubjectGlob => subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectAcknowledgeHandlerMessageSender<TMessage, TAcknowledgeHandler>>(
			serviceProvider,
			m_HandlerFactory,
			acknowledgeOptions);
}