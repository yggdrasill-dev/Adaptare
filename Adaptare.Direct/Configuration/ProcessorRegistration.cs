using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.Configuration;

internal class ProcessorRegistration<TMessage, TReply, TProcessor>(
	Glob subjectGlob,
	Func<IServiceProvider, TProcessor> processorFactory)
	: ISubscribeRegistration
	where TProcessor : class, IMessageProcessor<TMessage, TReply>
{
	private readonly Func<IServiceProvider, TProcessor> m_ProcessorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));

	public Glob SubjectGlob { get; } = subjectGlob ?? throw new ArgumentNullException(nameof(subjectGlob));

	public IMessageSender ResolveMessageSender(IServiceProvider serviceProvider)
		=> ActivatorUtilities.CreateInstance<DirectProcessorMessageSender<TMessage, TReply, TProcessor>>(
			serviceProvider,
			m_ProcessorFactory);
}