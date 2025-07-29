using System.Diagnostics;
using Adaptare.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.Configuration;

public class RabbitMessageQueueConfiguration
{
	internal static ActivitySource _RabbitActivitySource = new("Adaptare.MessageQueue.RabbitMQ");
	private readonly string m_RegisterName;

	public IServiceCollection Services { get; }

	public RabbitMessageQueueConfiguration(string registerName, MessageQueueConfiguration coreConfiguration)
	{
		ArgumentNullException.ThrowIfNull(coreConfiguration, nameof(coreConfiguration));

		Services = coreConfiguration.Services;
		m_RegisterName = registerName;
		Services.TryAddKeyedSingleton<IRabbitMQSerializerRegistry>(m_RegisterName, RabbitMQSerializerRegistry.Default);
		Services.AddSingleton<IMessageQueueBackgroundRegistration, RabbitMQBackgroundRegistration>(
			sp => ActivatorUtilities.CreateInstance<RabbitMQBackgroundRegistration>(
				sp,
				sp.GetKeyedServices<ISubscribeRegistration>(m_RegisterName),
				sp.GetRequiredKeyedService<RabbitMQConnectionManager>(m_RegisterName)));
	}

	public RabbitMessageQueueConfiguration ConfigureConnection(
		Func<IServiceProvider, RabbitMQConnectionOptions> createOptions)
	{
		Services.AddKeyedSingleton(
			m_RegisterName,
			(sp, _) => ActivatorUtilities.CreateInstance<RabbitMQConnectionManager>(
				sp,
				createOptions(sp)));

		return this;
	}

	public RabbitMessageQueueConfiguration UseSerializerRegistry<TSerializerRegister>()
		where TSerializerRegister : class, IRabbitMQSerializerRegistry
	{
		Services.AddKeyedSingleton<IRabbitMQSerializerRegistry, TSerializerRegister>(m_RegisterName);

		return this;
	}

	public RabbitMessageQueueConfiguration AddHandler<THandler>(
		string queueName,
		bool autoAck = true,
		CreateChannelOptions? createChannelOptions = null,
		IRabbitMQSerializerRegistry? serializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddHandler(
			handlerType,
			queueName,
			autoAck,
			createChannelOptions,
			serializerRegistry,
			handlerFactory);

		return this;
	}

	public RabbitMessageQueueConfiguration AddHandler(
		Type handlerType,
		string queueName,
		bool autoAck = true,
		CreateChannelOptions? createChannelOptions = null,
		IRabbitMQSerializerRegistry? serializerRegistry = null,
		Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(SubscribeRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			m_RegisterName,
			queueName,
			autoAck,
			createChannelOptions,
			serializerRegistry,
			factory)
			?? throw new InvalidOperationException(
				$"Unable to create registration for handler type {handlerType.FullName}");

		_ = Services.AddKeyedSingleton(m_RegisterName, registration);

		return this;
	}

	public RabbitMessageQueueConfiguration AddAcknowledgeHandler<THandler>(
		string queueName,
		AcknowledgeOptions? acknowledgeOptions = null,
		CreateChannelOptions? createChannelOptions = null,
		IRabbitMQSerializerRegistry? serializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddAcknowledgeHandler(
			handlerType,
			queueName,
			acknowledgeOptions,
			createChannelOptions,
			serializerRegistry,
			handlerFactory);

		return this;
	}

	public RabbitMessageQueueConfiguration AddAcknowledgeHandler(
		Type handlerType,
		string queueName,
		AcknowledgeOptions? acknowledgeOptions = null,
		CreateChannelOptions? createChannelOptions = null,
		IRabbitMQSerializerRegistry? serializerRegistry = null,
		Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IAcknowledgeMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(AcknowledgeSubscribeRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			m_RegisterName,
			queueName,
			acknowledgeOptions ?? new AcknowledgeOptions
			{
				Multiple = false,
				NackRequeue = false
			},
			createChannelOptions,
			serializerRegistry,
			factory)
			?? throw new InvalidOperationException(
				$"Unable to create registration for handler type {handlerType.FullName}");

		_ = Services.AddKeyedSingleton(m_RegisterName, registration);

		return this;
	}

	public RabbitMessageQueueConfiguration HandleRabbitMessageException(Func<Exception, CancellationToken, Task> handleException)
	{
		_ = Services.AddKeyedSingleton(
			m_RegisterName,
			(sp, _) => ActivatorUtilities.CreateInstance<ExceptionHandler>(
				sp,
				handleException));

		return this;
	}
}