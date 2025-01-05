using RabbitMQ.Client;
using Adaptare.RabbitMQ;

namespace Adaptare.RabbitMQ;

internal class NoopMessageQueueServiceFactory : IMessageQueueServiceFactory
{
	public IMessageQueueService CreateMessageQueueService(IServiceProvider serviceProvider, string exchangeName, IChannel channel)
		=> new NoopMessageQueueService();
}