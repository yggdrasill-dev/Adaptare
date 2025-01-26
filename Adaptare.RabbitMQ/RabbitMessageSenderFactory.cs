using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;

internal class RabbitMessageSenderFactory : IMessageSenderFactory
{
	public IMessageSender CreateMessageSender(IServiceProvider serviceProvider, string exchangeName, IChannel channel)
		=> ActivatorUtilities.CreateInstance<RabbitMessageSender>(serviceProvider, exchangeName, channel);
}
