using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.Configuration;

internal class SessionRegistration<TMessage, TSession>(
	Glob subjectGlob,
	Func<IServiceProvider, TSession> sessionFactory)
	: ISubscribeRegistration
	where TSession : IMessageSession<TMessage>
{
	private readonly Func<IServiceProvider, TSession> m_SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));

	public Glob SubjectGlob { get; } = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectSessionMessageSender<TMessage, TSession>>(
			serviceProvider,
			m_SessionFactory);
}