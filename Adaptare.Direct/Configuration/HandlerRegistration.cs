using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.Configuration;

internal class HandlerRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : class, IMessageHandler<TMessage>
{
	private readonly Func<IServiceProvider, THandler> m_HandlerFactory;

	public HandlerRegistration(Glob subjectGlob, Func<IServiceProvider, THandler> handlerFactory)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
		m_HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
	}

	public Glob SubjectGlob { get; }

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectHandlerMessageSender<TMessage, THandler>>(
			serviceProvider,
			m_HandlerFactory);
}