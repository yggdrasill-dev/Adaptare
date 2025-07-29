using Adaptare.Configuration;
using Adaptare.RabbitMQ;
using Adaptare.RabbitMQ.Configuration;
using Adaptare.RabbitMQ.Exchanges;
using RabbitMQ.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddRabbitGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob,
		string exchangeName,
		RabbitMQSenderOptions? senderOptions = null,
		IRabbitMQSerializerRegistry? serializerRegistry = null)
		=> AddRabbitGlobPatternExchange(
			configuration,
			string.Empty,
			glob,
			exchangeName,
			senderOptions,
			serializerRegistry);

	public static MessageQueueConfiguration AddRabbitGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string registerName,
		string glob,
		string exchangeName,
		RabbitMQSenderOptions? senderOptions = null,
		IRabbitMQSerializerRegistry? serializerRegistry = null)
		=> configuration.AddExchange(new RabbitGlobMessageExchange(
			registerName,
			glob,
			exchangeName,
			senderOptions,
			serializerRegistry));
}