using System.Diagnostics;
using Adaptare.Nats.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats;

internal class NatsMessageSender(
	INatsSerializerRegistry? natsSerializerRegistry,
	string? sessionReplySubject,
	INatsConnection connection,
	IReplyPromiseStore replyPromiseStore,
	ILogger<NatsMessageSender> logger) : IMessageSender
{
	private readonly IReplyPromiseStore m_ReplyPromiseStore = replyPromiseStore ?? throw new ArgumentNullException(nameof(replyPromiseStore));
	private readonly ILogger<NatsMessageSender> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly INatsConnection m_Connection = connection ?? throw new ArgumentNullException(nameof(connection));

	public async ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			$"Nats Ask",
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

		if (appendHeaders.Any(value => value.Name == MessageHeaderValueConsts.SessionAskKey))
			return await InternalAskAsync<TMessage, TReply>(
				subject,
				data,
				appendHeaders.Where(value => value.Name != MessageHeaderValueConsts.SessionAskKey),
				cancellationToken).ConfigureAwait(false);

		m_Logger.LogDebug("Ask");

		var headers = MakeMsgHeader(appendHeaders);

		if (!string.IsNullOrEmpty(sessionReplySubject))
			headers.Add(MessageHeaderValueConsts.SessionReplySubjectKey, sessionReplySubject);

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

		if (reply.Headers?.TryGetValue(MessageHeaderValueConsts.FailHeaderKey, out var values) == true)
			throw new MessageProcessFailException(values.ToString());

		var replySubject = reply.Headers?.TryGetValue(MessageHeaderValueConsts.SessionReplySubjectKey, out var replySubjects) == true
			? replySubjects.FirstOrDefault()
			: null;
		var askId = reply.Headers?.TryGetValue(MessageHeaderValueConsts.SessionAskKey, out var askKeys) == true
			? askKeys.FirstOrDefault()
			: null;
		var askGuid = askId != null && Guid.TryParse(askId, out var id)
			? (Guid?)id
			: null;

		return new NatsAnswer<TReply>(
			reply.Data!,
			this,
			replySubject,
			askGuid);
	}

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

		var answer = await AskAsync<TMessage, TReply>(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);

		if (answer.CanResponse)
			await answer
				.FailAsync("Send can't complete.", cancellationToken)
				.ConfigureAwait(false);

		return answer.Result;
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

		var answer = await AskAsync<TMessage, ReadOnlyMemory<byte>>(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);

		if (answer.CanResponse)
			await answer
				.FailAsync("Send can't complete.", cancellationToken)
				.ConfigureAwait(false);
	}

	internal async ValueTask<Answer<TReply>> InternalAskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
			$"Nats Internal Ask",
			ActivityKind.Producer);

		m_Logger.LogDebug("Internal Ask: {subject}", subject);
		var (id, promise) = m_ReplyPromiseStore.CreatePromise<TReply>(cancellationToken);

		var appendHeaders = new List<MessageHeaderValue>();

		if (!string.IsNullOrEmpty(sessionReplySubject))
			appendHeaders.Add(new MessageHeaderValue(MessageHeaderValueConsts.SessionReplySubjectKey, sessionReplySubject));

		appendHeaders.Add(new MessageHeaderValue(MessageHeaderValueConsts.SessionAskKey, id.ToString()));

		await PublishAsync(
			subject,
			data,
			header.Concat(appendHeaders),
			cancellationToken).ConfigureAwait(false);

		return await promise.ConfigureAwait(false);
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