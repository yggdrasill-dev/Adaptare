using Adaptare.Configuration;
using Adaptare.RabbitMQ.Exchanges;
using RabbitMQ.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddRabbitGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		string exchangeName,
		CreateChannelOptions? createChannelOptions = null)
		=> configuration.AddExchange(new RabbitGlobMessageExchange(glob, exchangeName, createChannelOptions));
}