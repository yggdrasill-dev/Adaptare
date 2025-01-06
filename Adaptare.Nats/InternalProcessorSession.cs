using Adaptare.Nats.Configuration;

namespace Adaptare.Nats;

internal class InternalProcessorSession<TMessage, TReply, TProcessor> : IMessageSession<TMessage>
	where TProcessor : IMessageProcessor<TMessage, TReply>
{
	private readonly TProcessor m_Processor;

	public InternalProcessorSession(TProcessor processor)
	{
		m_Processor = processor;
	}

	public async ValueTask HandleAsync(Question<TMessage> question, CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			"InternalHandlerSession",
			System.Diagnostics.ActivityKind.Consumer);

		_ = (activity?.AddTag("mq", "NATS")
			.AddTag("handler", typeof(TProcessor).Name));

		var result = await m_Processor.HandleAsync(
			question.Subject,
			question.Data,
			question.HeaderValues,
			cancellationToken).ConfigureAwait(false);

		await question.CompleteAsync(
			result,
			cancellationToken).ConfigureAwait(false);
	}
}