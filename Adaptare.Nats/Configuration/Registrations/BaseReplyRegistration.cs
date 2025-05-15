using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration.Registrations;

internal abstract class BaseReplyRegistration<TMessage, THandler>(
	string registerName,
	Func<IServiceProvider, THandler> handlerFactory)
	: BaseRegistration<TMessage, THandler>
	where THandler : IMessageHandler<TMessage>
{
	protected override async ValueTask ExecuteMessageHandlerAsync(
		IServiceProvider serviceProvider,
		MessageDataInfo<NatsMsg<TMessage>> dataInfo,
		CancellationToken cancellationToken)
	{
		var handler = handlerFactory(serviceProvider);
		var messageSender = serviceProvider.GetRequiredKeyedService<IMessageSender>(registerName);

		await handler.HandleAsync(
			dataInfo.Msg.Subject,
			dataInfo.Msg.Data!,
			dataInfo.Msg.Headers
				?.SelectMany(kv => kv.Value
					.Select(v => new MessageHeaderValue(kv.Key, v))),
			cancellationToken).ConfigureAwait(false);

		await messageSender.PublishAsync(
			dataInfo.Msg.ReplyTo!,
			Array.Empty<byte>(),
			dataInfo.Msg.Headers
				?.SelectMany(kv => kv.Value.Select(v => new MessageHeaderValue(kv.Key, v)))
				?? [],
			cancellationToken).ConfigureAwait(false);
	}
}