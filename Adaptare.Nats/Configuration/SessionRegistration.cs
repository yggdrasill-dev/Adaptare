﻿using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration;

internal class SessionRegistration<TMessage, TMessageSession> : ISubscribeRegistration
	where TMessageSession : IMessageSession<TMessage>
{
	private readonly bool m_IsSession;
	private readonly INatsSerializerRegistry? m_NatsSerializerRegistry;
	private readonly Func<IServiceProvider, TMessageSession> m_MessagaeSessionFactory;

	public SessionRegistration(
		string subject,
		bool isSession,
		INatsSerializerRegistry? natsSerializerRegistry,
		Func<IServiceProvider, TMessageSession> messagaeSessionFactory)
	{
		if (string.IsNullOrEmpty(subject))
			throw new ArgumentException($"'{nameof(subject)}' is not Null or Empty.", nameof(subject));
		Subject = subject;
		m_IsSession = isSession;
		m_NatsSerializerRegistry = natsSerializerRegistry;
		m_MessagaeSessionFactory = messagaeSessionFactory;
	}

	public string Subject { get; }

	public async ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> await receiver.SubscribeAsync(
			new NatsSubscriptionSettings<TMessage>(
				Subject,
				(msg, ct) => HandleMessageAsync(
					new MessageDataInfo<NatsMsg<TMessage>>(
						msg,
						logger,
						serviceProvider),
					ct),
				m_NatsSerializerRegistry?.GetDeserializer<TMessage>()),
			cancellationToken).ConfigureAwait(false);

	private static async Task ProcessMessageAsync(
		Question<TMessage> question,
		TMessageSession handler,
		CancellationToken cancellationToken)
	{
		try
		{
			await handler
				.HandleAsync(question, cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception)
		{
			if (question.CanResponse)
				await question.FailAsync(
					"Process message occur error",
					cancellationToken).ConfigureAwait(false);

			throw;
		}
	}

	private Question<TMessage> CreateQuestion(MessageDataInfo<NatsMsg<TMessage>> dataInfo, IMessageSender messageSender)
		=> m_IsSession
			? new NatsQuestion<TMessage>(
				dataInfo.Msg.Subject,
				dataInfo.Msg.Data!,
				dataInfo.Msg.Headers
					?.SelectMany(kv => kv.Value
						.Select(v => new MessageHeaderValue(kv.Key, v))),
				messageSender,
				dataInfo.Msg.ReplyTo)
			: new NatsAction<TMessage>(
				dataInfo.Msg.Subject,
				dataInfo.Msg.Data!,
				dataInfo.Msg.Headers
					?.SelectMany(kv => kv.Value
						.Select(v => new MessageHeaderValue(kv.Key, v))));

	private async ValueTask HandleMessageAsync(MessageDataInfo<NatsMsg<TMessage>> dataInfo, CancellationToken cancellationToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Msg.Headers,
			(header, key) => (header?[key] ?? string.Empty)!,
			out var context)
			? NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				Subject,
				ActivityKind.Consumer,
				context,
				tags: [
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(TMessageSession).Name)
				])
			: NatsMessageQueueConfiguration._NatsActivitySource.StartActivity(
				ActivityKind.Consumer,
				name: Subject,
				tags: [
					new KeyValuePair<string, object?>("mq", "NATS"),
					new KeyValuePair<string, object?>("handler", typeof(TMessageSession).Name)
				]);

		try
		{
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(false))
			{
				var handler = m_MessagaeSessionFactory(scope.ServiceProvider);
				var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
				var question = CreateQuestion(dataInfo, messageSender);

				await ProcessMessageAsync(
					question,
					handler,
					cts.Token).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(
					ex,
					cts.Token).ConfigureAwait(false);
		}
	}
}