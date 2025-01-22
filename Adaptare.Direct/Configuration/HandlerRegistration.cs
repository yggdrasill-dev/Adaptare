using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.Configuration;

internal class HandlerRegistration<TMessage, THandler>(
	Glob subjectGlob,
	Func<IServiceProvider, THandler> handlerFactory)
	: ISubscribeRegistration
	where THandler : class, IMessageHandler<TMessage>
{
	private readonly Func<IServiceProvider, THandler> m_HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));

	public Glob SubjectGlob { get; } = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectHandlerMessageSender<TMessage, THandler>>(
			serviceProvider,
			m_HandlerFactory);
}