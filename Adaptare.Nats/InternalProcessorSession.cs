using Adaptare.Nats.Configuration;

namespace Adaptare.Nats;

internal class InternalProcessorSession<TMessage, TReply, TProcessor>(TProcessor processor) : IMessageSession<TMessage>
	where TProcessor : IMessageProcessor<TMessage, TReply>
{
	public async ValueTask HandleAsync(Question<TMessage> question, CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			"InternalHandlerSession",
			System.Diagnostics.ActivityKind.Consumer);

		_ = (activity?.AddTag("mq", "NATS")
			.AddTag("handler", typeof(TProcessor).Name));

		var result = await processor.HandleAsync(
			question.Subject,
			question.Data,
			question.HeaderValues,
			cancellationToken).ConfigureAwait(false);

		await question.CompleteAsync(
			result,
			cancellationToken).ConfigureAwait(false);
	}
}