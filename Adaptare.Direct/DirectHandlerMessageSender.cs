﻿using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adaptare.Direct;

internal class DirectHandlerMessageSender<TData, TMessageHandler>(
	Func<IServiceProvider, TMessageHandler> messageHandlerFactory,
	IServiceProvider serviceProvider,
	ILogger<DirectHandlerMessageSender<TData, TMessageHandler>> logger) : IMessageSender
	where TMessageHandler : class, IMessageHandler<TData>
{
	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Direct Publish", ActivityKind.Producer);

		_ = HandleMessageAsync(
			subject,
			data,
			header,
			cancellationToken).AsTask()
			.ContinueWith(
				async t =>
				{
					if (t.IsFaulted)
					{
						var ex = t.Exception!;

						logger.LogError(ex, "Handle {subject} occur error.", subject);

						foreach (var handler in serviceProvider.GetServices<ExceptionHandler>())
							await handler.HandleExceptionAsync(
								ex,
								cancellationToken).ConfigureAwait(false);
					}
				},
				cancellationToken,
				TaskContinuationOptions.None,
				TaskScheduler.Current);

		return ValueTask.CompletedTask;
	}

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public async ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity($"Direct Publish", ActivityKind.Producer);

		await HandleMessageAsync(
			subject,
			data,
			[.. header],
			cancellationToken).ConfigureAwait(false);
	}

	private async ValueTask HandleMessageAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity(subject, ActivityKind.Producer);

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(TMessageHandler).Name));

		var scope = serviceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			var handler = messageHandlerFactory(scope.ServiceProvider);

			if (data is TData messageData)
				await handler.HandleAsync(
					subject,
					messageData,
					headerValues,
					cancellationToken).ConfigureAwait(false);
			else
				throw new InvalidCastException($"type {typeof(TMessage).Name} Can't cast type {typeof(TData).Name}");
		}
	}
}