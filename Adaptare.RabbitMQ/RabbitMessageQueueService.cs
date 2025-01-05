using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;

internal class RabbitMessageQueueService : IMessageQueueService
{
	private readonly string m_ExchangeName;
	private readonly IChannel m_Channel;
	private readonly IRabbitMQSerializerRegistry m_SerializerRegistry;

	public RabbitMessageQueueService(
		string exchangeName,
		IChannel channel,
		IRabbitMQSerializerRegistry serializerRegistry)
	{
		m_ExchangeName = exchangeName;
		m_Channel = channel;
		m_SerializerRegistry = serializerRegistry;
	}

	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	public async ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = RabbitMQConnectionManager._RabbitMQActivitySource.StartActivity("RabbitMQ Publish");

		var appendHeaders = new List<MessageHeaderValue>(header);

		TraceContextPropagator.Inject(
			activity,
			appendHeaders,
			(headers, key, value) =>
			{
				if (!string.IsNullOrEmpty(value))
					headers.Add(new MessageHeaderValue(key, value));
			});

		var properties = BuildBasicProperties(appendHeaders);

		cancellationToken.ThrowIfCancellationRequested();

		var binaryData = m_SerializerRegistry.GetSerializer<TMessage>().Serialize(data);
		await m_Channel.BasicPublishAsync(
			m_ExchangeName,
			subject,
			false,
			properties,
			binaryData,
			cancellationToken).ConfigureAwait(false);
	}

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	private static BasicProperties BuildBasicProperties(IEnumerable<MessageHeaderValue> header)
	{
		var properties = new BasicProperties
		{
			Headers = header.ToDictionary(
				v => v.Name,
				v => (object?)v.Value)
		};

		return properties;
	}
}