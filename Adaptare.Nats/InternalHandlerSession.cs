﻿using System.Diagnostics;
using Adaptare.Nats.Configuration;

namespace Adaptare.Nats;

internal class InternalHandlerSession<TMessage, THandler> : IMessageSession<TMessage>
	where THandler : IMessageHandler<TMessage>
{
	private readonly THandler m_Handler;

	public InternalHandlerSession(THandler handler)
	{
		m_Handler = handler;
	}

	public async ValueTask HandleAsync(Question<TMessage> question, CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			"InternalHandlerSession",
			ActivityKind.Consumer);

		_ = (activity?.AddTag("mq", "NATS")
			.AddTag("handler", typeof(THandler).Name));

		await m_Handler.HandleAsync(
				question.Subject,
				question.Data,
				question.HeaderValues,
				cancellationToken).ConfigureAwait(false);

		await question.CompleteAsync(
			Array.Empty<byte>(),
			cancellationToken).ConfigureAwait(false);
	}
}