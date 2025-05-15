using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct;

internal class DirectProcessorMessageSender<TData, TResult, TMessageProcessor>(
	Func<IServiceProvider, TMessageProcessor> processorFactory,
	IServiceProvider serviceProvider)
	: IMessageSender
	where TMessageProcessor : class, IMessageProcessor<TData, TResult>
{
	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public async ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = DirectDiagnostics.ActivitySource.StartActivity(
			subject,
			ActivityKind.Producer);

		_ = (activity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(TMessageProcessor).Name));

		var scope = serviceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			var handler = processorFactory(scope.ServiceProvider);

			if (data is TData messageData)
			{
				var result = await handler.HandleAsync(
					subject,
					messageData,
					header,
					cancellationToken).ConfigureAwait(false);

				return (TReply)(object)result!;
			}
			else
			{
				throw new InvalidCastException($"type {typeof(TMessage).Name} Can't cast type {typeof(TData).Name}");
			}
		}
	}

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();
}