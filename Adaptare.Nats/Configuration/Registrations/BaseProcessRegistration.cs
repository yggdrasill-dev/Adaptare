using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration.Registrations;
internal abstract class BaseProcessRegistration<TMessage, TReply, TProcessor>(
	string registerName,
	Func<IServiceProvider, TProcessor> processorFactory)
	: BaseRegistration<TMessage, TProcessor>
	where TProcessor : IMessageProcessor<TMessage, TReply>
{
	protected override async ValueTask ExecuteMessageHandlerAsync(
		IServiceProvider serviceProvider,
		MessageDataInfo<NatsMsg<TMessage>> dataInfo,
		CancellationToken cancellationToken)
	{
		var processor = processorFactory(serviceProvider);
		var messageSender = serviceProvider.GetRequiredKeyedService<IMessageSender>(registerName);

		var reply = await processor.HandleAsync(
			dataInfo.Msg.Subject,
			dataInfo.Msg.Data!,
			dataInfo.Msg.Headers
				?.SelectMany(kv => kv.Value
					.Select(v => new MessageHeaderValue(kv.Key, v))),
			cancellationToken).ConfigureAwait(false);

		await messageSender.PublishAsync(
			dataInfo.Msg.ReplyTo!,
			reply,
			dataInfo.Msg.Headers
				?.SelectMany(kv => kv.Value.Select(v => new MessageHeaderValue(kv.Key, v)))
				?? [],
			cancellationToken).ConfigureAwait(false);
	}
}
