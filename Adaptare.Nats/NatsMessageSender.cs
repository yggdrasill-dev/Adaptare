using System.Diagnostics;
using Adaptare.Nats.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats;

internal class NatsMessageSender(
	INatsSerializerRegistry? natsSerializerRegistry,
	INatsConnection connection,
	ILogger<NatsMessageSender> logger)
	: IMessageSender
{
	private readonly ILogger<NatsMessageSender> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly INatsConnection m_Connection = connection ?? throw new ArgumentNullException(nameof(connection));

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			$"Nats Publish",
			ActivityKind.Producer);

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value) && !headers.Any(header => header.Name == key))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var msg = new NatsMsg<TMessage>
		{
			Subject = subject,
			Data = data,
			Headers = MakeMsgHeader(appendHeaders)
		};

		cancellationToken.ThrowIfCancellationRequested();
		return natsSerializerRegistry is null
			? m_Connection.PublishAsync(msg, cancellationToken: cancellationToken)
			: m_Connection.PublishAsync(
				msg,
				serializer: natsSerializerRegistry.GetSerializer<TMessage>(),
				cancellationToken: cancellationToken);
	}

	public async ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			$"Nats Request",
			ActivityKind.Producer);

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value) && !headers.Any(header => header.Name == key))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var headers = MakeMsgHeader(appendHeaders);

		cancellationToken.ThrowIfCancellationRequested();

		var reply = natsSerializerRegistry is null
			? await m_Connection.RequestAsync<TMessage, TReply>(
				subject,
				data,
				headers,
				cancellationToken: cancellationToken).ConfigureAwait(false)
			: await m_Connection.RequestAsync(
				subject,
				data,
				headers,
				requestSerializer: natsSerializerRegistry.GetSerializer<TMessage>(),
				replySerializer: natsSerializerRegistry.GetDeserializer<TReply>(),
				cancellationToken: cancellationToken).ConfigureAwait(false);

		return reply.Headers?.TryGetValue(MessageHeaderValueConsts.FailHeaderKey, out var values) == true
			? throw new MessageProcessFailException(values.ToString())
			: reply.Data!;
	}

	public async ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			$"Nats Send",
			ActivityKind.Producer);

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value) && !headers.Any(header => header.Name == key))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var headers = MakeMsgHeader(appendHeaders);

		cancellationToken.ThrowIfCancellationRequested();

		var reply = natsSerializerRegistry is null
			? await m_Connection.RequestAsync<TMessage, ReadOnlyMemory<byte>>(
				subject,
				data,
				headers,
				cancellationToken: cancellationToken).ConfigureAwait(false)
			: await m_Connection.RequestAsync(
				subject,
				data,
				headers,
				requestSerializer: natsSerializerRegistry.GetSerializer<TMessage>(),
				replySerializer: natsSerializerRegistry.GetDeserializer<ReadOnlyMemory<byte>>(),
				cancellationToken: cancellationToken).ConfigureAwait(false);

		if (reply.Headers?.TryGetValue(MessageHeaderValueConsts.FailHeaderKey, out var values) == true)
			throw new MessageProcessFailException(values.ToString());
	}

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