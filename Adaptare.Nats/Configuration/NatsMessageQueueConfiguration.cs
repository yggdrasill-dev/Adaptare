using System.Diagnostics;
using Adaptare.Configuration;
using Adaptare.Nats.Configuration.Registrations;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;

namespace Adaptare.Nats.Configuration;

public class NatsMessageQueueConfiguration(
	string registerName,
	MessageQueueConfiguration coreConfiguration)
{
	internal static ActivitySource _NatsActivitySource = new("Adaptare.MessageQueue.Nats");

	public IServiceCollection Services { get; } = coreConfiguration.Services;

	public NatsMessageQueueConfiguration ConfigureResolveConnection(Func<IServiceProvider, NatsConnection> configure)
	{
		_ = Services
			.AddKeyedSingleton<INatsConnectionManager>(
				registerName,
				(sp, _) => new NatsConnectionManager(configure(sp)))
			.AddKeyedSingleton(
				registerName,
				(sp, _) => sp.GetRequiredKeyedService<INatsConnectionManager>(registerName).CreateMessageSender(sp, null))
			.AddKeyedSingleton<INatsMessageQueueService>(
				registerName,
				(sp, _) => new NatsMessageQueueService(sp.GetRequiredKeyedService<INatsConnectionManager>(registerName)))
			.AddSingleton<IMessageQueueBackgroundRegistration>(sp => ActivatorUtilities.CreateInstance<NatsBackgroundRegistration>(
				sp,
				sp.GetRequiredKeyedService<INatsMessageQueueService>(registerName),
				sp.GetKeyedServices<ISubscribeRegistration>(registerName),
				sp.GetKeyedServices<StreamConfig>(registerName)));

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler<THandler>(
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddHandler(handlerType, subject, natsSerializerRegistry, handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler(
		Type handlerType,
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
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
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(SubscribeRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			registerName,
			subject,
			natsSerializerRegistry,
			factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		Services.AddKeyedSingleton(registerName, registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler<THandler>(
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddHandler(handlerType, subject, group, natsSerializerRegistry, handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddHandler(
		Type handlerType,
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
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
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(QueueSubscribeRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			registerName,
			subject,
			group,
			natsSerializerRegistry,
			factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		Services.AddKeyedSingleton(registerName, registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddJetStreamHandler<THandler>(
		string subject,
		string stream,
		ConsumerConfig consumerConfig,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddJetStreamHandler(
			handlerType,
			subject,
			stream,
			consumerConfig,
			natsSerializerRegistry,
			handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddJetStreamHandler(
		Type handlerType,
		string subject,
		string stream,
		ConsumerConfig consumerConfig,
		INatsSerializerRegistry? natsSerializerRegistry = null,
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
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(JetStreamHandlerRegistration<,>).MakeGenericType(typeArguments);

		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			subject,
			stream,
			consumerConfig,
			natsSerializerRegistry,
			factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		Services.AddKeyedSingleton(registerName, registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor<TProcessor>(
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, TProcessor>? processorFactory = null)
	{
		var processorType = typeof(TProcessor);

		AddProcessor(
			processorType,
			subject,
			natsSerializerRegistry,
			processorFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor(
		Type processorType,
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? processorFactory = null)
	{
		var typeArguments = processorType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var factory = processorFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(processorType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(ProcessRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			registerName,
			subject,
			natsSerializerRegistry,
			factory)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		Services.AddKeyedSingleton(registerName, registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor<TProcessor>(
		string subject,
		string queue,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, TProcessor>? processorFactory = null)
	{
		var processorType = typeof(TProcessor);

		AddProcessor(
			processorType,
			subject,
			queue,
			natsSerializerRegistry,
			processorFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddProcessor(
		Type processorType,
		string subject,
		string queue,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Delegate? processorFactory = null)
	{
		var typeArguments = processorType
			.GetInterfaces()
			.Where(t => t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var factory = processorFactory
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(processorType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(QueueProcessRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			registerName,
			subject,
			queue,
			natsSerializerRegistry,
			factory)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		Services.AddKeyedSingleton(registerName, registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler<THandler>(
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddReplyHandler(handlerType, subject, natsSerializerRegistry, handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler(
		Type handlerType,
		string subject,
		INatsSerializerRegistry? natsSerializerRegistry = null,
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
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(ReplyRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			registerName,
			subject,
			natsSerializerRegistry,
			factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		Services.AddKeyedSingleton(registerName, registration);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler<THandler>(
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddReplyHandler(
			handlerType,
			subject,
			group,
			natsSerializerRegistry,
			handlerFactory);

		return this;
	}

	public NatsMessageQueueConfiguration AddReplyHandler(
		Type handlerType,
		string subject,
		string group,
		INatsSerializerRegistry? natsSerializerRegistry = null,
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
			?? typeof(DefaultMessageHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(QueueReplyRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			registerName,
			subject,
			group,
			natsSerializerRegistry,
			factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		Services.AddKeyedSingleton(registerName, registration);

		return this;
	}

	public NatsMessageQueueConfiguration ConfigJetStream(StreamConfig config)
	{
		Services.AddKeyedSingleton(registerName, config);

		return this;
	}
}