using Adaptare.Configuration;
using Adaptare.RabbitMQ;
using Adaptare.RabbitMQ.Exchanges;
using RabbitMQ.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddRabbitGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		string exchangeName,
		CreateChannelOptions? createChannelOptions = null,
		IRabbitMQSerializerRegistry? rabbitMQSerializerRegistry = null)
		=> AddRabbitGlobPatternExchange(
			configuration,
			string.Empty,
			glob,
			exchangeName,
			createChannelOptions,
			rabbitMQSerializerRegistry);

	public static MessageQueueConfiguration AddRabbitGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string registerName,
		string glob,
		string exchangeName,
		CreateChannelOptions? createChannelOptions = null,
		IRabbitMQSerializerRegistry? rabbitMQSerializerRegistry = null)
		=> configuration.AddExchange(new RabbitGlobMessageExchange(
			registerName,
			glob,
			exchangeName,
			createChannelOptions,
			rabbitMQSerializerRegistry));
}