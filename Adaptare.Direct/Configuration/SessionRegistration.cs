using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.Configuration;

internal class SessionRegistration<TMessage, TSession> : ISubscribeRegistration
	where TSession : IMessageSession<TMessage>
{
	private readonly Func<IServiceProvider, TSession> m_SessionFactory;

	public SessionRegistration(Glob subjectGlob, Func<IServiceProvider, TSession> sessionFactory)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
		m_SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
	}

	public Glob SubjectGlob { get; }

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectSessionMessageSender<TMessage, TSession>>(
			serviceProvider,
			m_SessionFactory);
}