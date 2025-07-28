using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;

internal class RabbitMessageSenderFactory : IMessageSenderFactory
{
	public IMessageSender CreateMessageSender(
		IServiceProvider serviceProvider,
		string exchangeName,
		IChannel channel,
		IRabbitMQSerializerRegistry serializerRegistry,
		string? appId = null)
		=> ActivatorUtilities.CreateInstance<RabbitMessageSender>(
			serviceProvider,
			exchangeName,
			appId!,
			channel,
			serializerRegistry);
}