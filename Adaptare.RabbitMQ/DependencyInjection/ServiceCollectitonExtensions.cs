using Adaptare.Configuration;
using Adaptare.RabbitMQ;
using Adaptare.RabbitMQ.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectitonExtensions
{
	public static IServiceCollection AddFakeRabbitMessageQueue(
		this IServiceCollection services)
	{
		foreach (var desc in services.Where(
			desc => desc.ServiceType == typeof(IHostedService)
				&& desc.ImplementationType == typeof(MessageQueueBackground)).ToArray())
			_ = services.Remove(desc);

		return services
			.AddSingleton<IMessageSenderFactory, NoopMessageSenderFactory>();
	}

	public static MessageQueueConfiguration AddRabbitMessageQueue(
		this MessageQueueConfiguration configuration,
		Action<RabbitMessageQueueConfiguration> configure)
		=> AddRabbitMessageQueue(configuration, string.Empty, configure);

	public static MessageQueueConfiguration AddRabbitMessageQueue(
		this MessageQueueConfiguration configuration,
		string registerName,
		Action<RabbitMessageQueueConfiguration> configure)
	{
		var rabbitConfiguration = new RabbitMessageQueueConfiguration(registerName, configuration);
		rabbitConfiguration.UseSerializerRegistry<RabbitMQSerializerRegistry>();
		configuration.Services
			.TryAddSingleton<IMessageSenderFactory, RabbitMessageSenderFactory>();
		configure(rabbitConfiguration);
		return configuration;
	}
}