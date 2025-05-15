using System.Diagnostics;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;

internal class RabbitMessageSender(
	string exchangeName,
	IChannel channel,
	IRabbitMQSerializerRegistry serializerRegistry)
	: IMessageSender
	, IDisposable
	, IAsyncDisposable
{
	private bool m_DisposedValue;

	public async ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = RabbitMQConnectionManager._RabbitMQActivitySource.StartActivity(
			"RabbitMQ Publish",
			ActivityKind.Producer);

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

		var binaryData = serializerRegistry.GetSerializer<TMessage>().Serialize(data);
		await channel.BasicPublishAsync(
			exchangeName,
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

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		await CoreDisposeAsync().ConfigureAwait(false);

		Dispose(disposing: false);
		GC.SuppressFinalize(this);
	}

	protected virtual async ValueTask CoreDisposeAsync()
	{
		if (channel is not null)
		{
			await channel.DisposeAsync().ConfigureAwait(false);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!m_DisposedValue)
		{
			if (disposing)
			{
				if (channel is IDisposable disposableChannel)
				{
					disposableChannel.Dispose();
				}
			}

			m_DisposedValue = true;
		}
	}

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