using Adaptare.Configuration;
using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.Configuration;

public class DirectMessageQueueConfiguration(MessageQueueConfiguration messageQueueConfiguration)
{
	private static readonly List<ISubscribeRegistration> _SubscribeRegistrations = [];

	public IServiceCollection Services { get; } = messageQueueConfiguration.Services;

	internal static IEnumerable<ISubscribeRegistration> SubscribeRegistrations => _SubscribeRegistrations;

	public DirectMessageQueueConfiguration AddHandler<THandler>(string subject, Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddHandler(handlerType, subject, handlerFactory);

		return this;
	}

	public DirectMessageQueueConfiguration AddHandler(Type handlerType, string subject, Delegate? handlerFactory = null)
	{
		var typeArguments = handlerType.GetInterfaces()
			.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(handlerType)
			.ToArray();

		var factory = handlerFactory
			?? typeof(DefaultHandlerFactory<>)
				.MakeGenericType(handlerType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(HandlerRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject), factory)
			?? throw new InvalidOperationException($"Unable to create a registration for handler type {handlerType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration AddProcessor<TProcessor>(string subject, Func<IServiceProvider, TProcessor>? processorFactory = null)
	{
		var processorType = typeof(TProcessor);

		AddProcessor(processorType, subject, processorFactory);

		return this;
	}

	public DirectMessageQueueConfiguration AddProcessor(Type processorType, string subject, Delegate? processorFactory = null)
	{
		var typeArguments = processorType.GetInterfaces()
			.Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageProcessor<,>))
			.Take(1)
			.SelectMany(t => t.GetGenericArguments())
			.Append(processorType)
			.ToArray();

		var factory = processorFactory
			?? typeof(DefaultHandlerFactory<>)
				.MakeGenericType(processorType)
				.GetField("Default")!
				.GetValue(null);

		var registrationType = typeof(ProcessorRegistration<,,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(registrationType, Glob.Parse(subject), factory)
			?? throw new InvalidOperationException($"Unable to create a registration for processor type {processorType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}

	public DirectMessageQueueConfiguration AddAcknowledgeHandler<THandler>(
		string subject,
		DirectAcknowledgeOptions? acknowledgeOptions = null,
		Func<IServiceProvider, THandler>? handlerFactory = null)
	{
		var handlerType = typeof(THandler);

		AddAcknowledgeHandler(
			handlerType,
			subject,
			acknowledgeOptions,
			handlerFactory);

		return this;
	}

	public DirectMessageQueueConfiguration AddAcknowledgeHandler(
		Type handlerType,
		string subject,
		DirectAcknowledgeOptions? acknowledgeOptions = null,
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

		var registrationType = typeof(AcknowledgeRegistration<,>).MakeGenericType(typeArguments);
		var registration = (ISubscribeRegistration?)Activator.CreateInstance(
			registrationType,
			Glob.Parse(subject),
			acknowledgeOptions ?? new DirectAcknowledgeOptions(),
			factory)
			?? throw new InvalidOperationException(
				$"Unable to create registration for handler type {handlerType.FullName}");

		_SubscribeRegistrations.Add(registration);

		return this;
	}
}