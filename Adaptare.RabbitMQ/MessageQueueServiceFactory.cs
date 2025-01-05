using RabbitMQ.Client;
using Adaptare.RabbitMQ;

namespace Adaptare.RabbitMQ;

internal interface IMessageQueueServiceFactory
{
	IMessageQueueService CreateMessageQueueService(IServiceProvider serviceProvider, string exchangeName, IChannel channel);
}
