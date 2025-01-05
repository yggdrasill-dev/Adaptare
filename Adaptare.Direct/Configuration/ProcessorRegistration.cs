using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.Configuration;

internal class ProcessorRegistration<TMessage, TReply, TProcessor> : ISubscribeRegistration
	where TProcessor : class, IMessageProcessor<TMessage, TReply>
{
	private readonly Func<IServiceProvider, TProcessor> m_ProcessorFactory;

	public ProcessorRegistration(Glob subjectGlob, Func<IServiceProvider, TProcessor> processorFactory)
	{
		SubjectGlob = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));
		m_ProcessorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
	}

	public Glob SubjectGlob { get; }

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectProcessorMessageSender<TMessage, TReply, TProcessor>>(
			serviceProvider,
			m_ProcessorFactory);
}