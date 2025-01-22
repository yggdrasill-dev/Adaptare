using System.Diagnostics;
using Adaptare.Nats.Configuration;

namespace Adaptare.Nats;

internal class InternalHandlerSession<TMessage, THandler>(THandler handler) : IMessageSession<TMessage>
	where THandler : IMessageHandler<TMessage>
{
	public async ValueTask HandleAsync(Question<TMessage> question, CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			"InternalHandlerSession",
			ActivityKind.Consumer);

		_ = (activity?.AddTag("mq", "NATS")
			.AddTag("handler", typeof(THandler).Name));

		await handler.HandleAsync(
				question.Subject,
				question.Data,
				question.HeaderValues,
				cancellationToken).ConfigureAwait(false);

		await question.CompleteAsync(
			Array.Empty<byte>(),
			cancellationToken).ConfigureAwait(false);
	}
}