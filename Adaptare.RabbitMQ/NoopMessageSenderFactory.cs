using Adaptare.RabbitMQ.Configuration;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;

internal class NoopMessageSenderFactory : IMessageSenderFactory
{
	public IMessageSender CreateMessageSender(
		IServiceProvider serviceProvider,
		string exchangeName,
		IChannel channel,
		IRabbitMQSerializerRegistry rabbitMQSerializerRegistry,
		RabbitMQSenderOptions senderOptions)
		=> new NoopMessageSender();
}