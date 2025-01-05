using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Adaptare.RabbitMQ;

namespace Adaptare.RabbitMQ;

internal class RabbitMessageQueueServiceFactory : IMessageQueueServiceFactory
{
	public IMessageQueueService CreateMessageQueueService(IServiceProvider serviceProvider, string exchangeName, IChannel channel)
		=> ActivatorUtilities.CreateInstance<RabbitMessageQueueService>(serviceProvider, exchangeName, channel);
}
