using Adaptare;
using Adaptare.Configuration;
using Adaptare.Nats;
using Adaptare.Nats.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddFakeNatsMessageQueue(this IServiceCollection services)
	{
		foreach (var desc in services.Where(
			desc => desc.ServiceType == typeof(IMessageQueueBackgroundRegistration)
				&& desc.ImplementationType == typeof(NatsBackgroundRegistration)).ToArray())
			_ = services.Remove(desc);

		return services
			.AddSingleton<NoopMessageQueueService>()
			.AddSingleton<INatsConnectionManager, NoopConnectionManager>();
	}

	public static MessageQueueConfiguration AddNatsMessageQueue(
		this MessageQueueConfiguration configuration,
		Action<NatsMessageQueueConfiguration> configure)
		=> AddNatsMessageQueue(configuration, string.Empty, configure);

	public static MessageQueueConfiguration AddNatsMessageQueue(
		this MessageQueueConfiguration configuration,
		string registerName,
		Action<NatsMessageQueueConfiguration> configure)
	{
		var natsConfiguration = new NatsMessageQueueConfiguration(registerName, configuration);

		configure(natsConfiguration);

		return configuration;
	}
}