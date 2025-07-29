using System.Buffers;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.Configuration;

internal class AcknowledgeSubscribeRegistration<TMessage, THandler> : ISubscribeRegistration
	where THandler : IAcknowledgeMessageHandler<TMessage>
{
	private static readonly Random _Random = new();
	private readonly string m_RegisterName;
	private readonly AcknowledgeOptions m_AcknowledgeOptions;
	private readonly CreateChannelOptions? m_CreateChannelOptions;
	private readonly IRabbitMQSerializerRegistry? m_SerializerRegistry;
	private readonly Func<IServiceProvider, THandler> m_HandlerFactory;

	public string Subject { get; }

	public AcknowledgeSubscribeRegistration(
		string registerName,
		string subject,
		AcknowledgeOptions acknowledgeOptions,
		CreateChannelOptions? createChannelOptions,
		IRabbitMQSerializerRegistry? serializerRegistry,
		Func<IServiceProvider, THandler> handlerFactory)
	{
		ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));
		m_RegisterName = registerName;
		Subject = subject;
		m_AcknowledgeOptions = acknowledgeOptions;
		m_CreateChannelOptions = createChannelOptions;
		m_SerializerRegistry = serializerRegistry;
		m_HandlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
	}

	public ValueTask<IDisposable> SubscribeAsync(
		IMessageReceiver<RabbitSubscriptionSettings> messageReceiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken)
		=> messageReceiver.SubscribeAsync(
			new RabbitSubscriptionSettings
			{
				Subject = Subject,
				CreateChannelOptions = m_CreateChannelOptions,
				EventHandler = (model, args) => HandleMessageAsync(new MessageDataInfo
				{
					Args = args,
					ServiceProvider = serviceProvider,
					Logger = logger,
					CancellationToken = cancellationToken,
					Channel = model
				})
			},
			cancellationToken);

	private async Task HandleMessageAsync(MessageDataInfo dataInfo)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(dataInfo.CancellationToken);
		using var activity = TraceContextPropagator.TryExtract(
			dataInfo.Args.BasicProperties.Headers,
			(headers, key) => headers?.TryGetValue(key, out var data) == true
				&& data is byte[] encodedData
				? Encoding.UTF8.GetString(encodedData)
				: string.Empty,
			out var context)
			? RabbitMQConnectionManager._RabbitMQActivitySource.StartActivity(
				Subject,
				ActivityKind.Consumer,
				context,
				tags: [
					new KeyValuePair<string, object?>("mq", "RabbitMQ"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				])
			: RabbitMQConnectionManager._RabbitMQActivitySource.StartActivity(
				ActivityKind.Consumer,
				name: Subject,
				tags: [
					new KeyValuePair<string, object?>("mq", "RabbitMQ"),
					new KeyValuePair<string, object?>("handler", typeof(THandler).Name)
				]);

		try
		{
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(continueOnCapturedContext: false))
			{
				var handler = m_HandlerFactory(scope.ServiceProvider);
				var serializeRegistration = m_SerializerRegistry
					?? scope.ServiceProvider.GetRequiredKeyedService<IRabbitMQSerializerRegistry>(m_RegisterName);
				var deserializer = serializeRegistration.GetDeserializer<TMessage>();

				var msgData = deserializer.Deserialize(new ReadOnlySequence<byte>(dataInfo.Args.Body));
				var msg = new RabbitMQAcknowledgeMessage<TMessage>(
					dataInfo.Args.RoutingKey,
					msgData,
					dataInfo.Args.BasicProperties.Headers
						?.Select(kv => new MessageHeaderValue(kv.Key, kv.Value?.ToString())),
					dataInfo.Args.DeliveryTag,
					dataInfo.Channel,
					m_AcknowledgeOptions);

				await handler.HandleAsync(
					msg,
					cts.Token).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			_ = (activity?.AddTag("error", true));
			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);

			foreach (var handler in dataInfo.ServiceProvider.GetServices<ExceptionHandler>())
				await handler.HandleExceptionAsync(
					ex,
					cts.Token).ConfigureAwait(false);
		}
	}
}