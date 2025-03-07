﻿using Adaptare;
using Adaptare.Configuration;
using Adaptare.RabbitMQ;
using Adaptare.RabbitMQ.Configuration;
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
		{
			_ = services.Remove(desc);
		}

		return services
			.AddSingleton<IMessageSenderFactory, NoopMessageSenderFactory>();
	}

	public static MessageQueueConfiguration AddRabbitMessageQueue(
		this MessageQueueConfiguration configuration,
		Action<RabbitMessageQueueConfiguration> configure)
	{
		var rabbitConfiguration = new RabbitMessageQueueConfiguration(configuration);

		configure(rabbitConfiguration);

		InitialRabbitMessageQueueConfiguration(rabbitConfiguration);

		return configuration;
	}

	private static void InitialRabbitMessageQueueConfiguration(RabbitMessageQueueConfiguration configuration)
		=> configuration.Services
			.AddSingleton<RabbitMQConnectionManager>()
			.AddSingleton<IMessageSenderFactory, RabbitMessageSenderFactory>()
			.AddSingleton<IRabbitMQSerializerRegistry, RabbitMQSerializerRegistry>()
			.AddHostedService<MessageQueueBackground>();
}