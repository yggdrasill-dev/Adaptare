using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;

internal interface IMessageSenderFactory
{
	IMessageSender CreateMessageSender(IServiceProvider serviceProvider, string exchangeName, IChannel channel);
}