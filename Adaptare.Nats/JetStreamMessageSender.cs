﻿using System.Diagnostics;
using Adaptare.Nats.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Adaptare.Nats;

internal class JetStreamMessageSender(
	INatsSerializerRegistry? natsSerializerRegistry,
	INatsJSContext natsJSContext,
	ILogger<JetStreamMessageSender> logger)
	: IMessageSender
{
	private readonly INatsJSContext m_NatsJSContext = natsJSContext ?? throw new ArgumentNullException(nameof(natsJSContext));
	private readonly ILogger<JetStreamMessageSender> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			$"Nats JetStream Publish",
			ActivityKind.Producer);

		var appendHeaders = new List<MessageHeaderValue>(header);

		var ack = natsSerializerRegistry is null
			? await m_NatsJSContext.PublishAsync(
				subject,
				data,
				headers: MakeMsgHeader(appendHeaders),
				cancellationToken: cancellationToken).ConfigureAwait(false)
			: await m_NatsJSContext.PublishAsync(
				subject,
				data,
				headers: MakeMsgHeader(appendHeaders),
				serializer: natsSerializerRegistry.GetSerializer<TMessage>(),
				cancellationToken: cancellationToken).ConfigureAwait(false);

		ack.EnsureSuccess();
	}

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	private NatsHeaders MakeMsgHeader(IEnumerable<MessageHeaderValue> header)
	{
		ArgumentNullException.ThrowIfNull(header, nameof(header));

		var msgHeader = new NatsHeaders();
		foreach (var headerValue in header)
		{
			msgHeader.Add(headerValue.Name, headerValue.Value);
			m_Logger.LogDebug("Header: {headerValue.Name} = {headerValue.Value}", headerValue.Name, headerValue.Value);
		}

		return msgHeader;
	}
}