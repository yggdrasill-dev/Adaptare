using System.Collections.Concurrent;
using Adaptare.RabbitMQ.Configuration;
using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.RabbitMQ.Exchanges;

internal class RabbitGlobMessageExchange
	: IMessageExchange
	, IDisposable
	, IAsyncDisposable
{
	private readonly string m_RegisterName;
	private readonly string m_ExchangeName;
	private readonly RabbitMQSenderOptions m_SenderOptions;
	private readonly IRabbitMQSerializerRegistry? m_SerializerRegistry;
	private readonly Glob m_Glob;
	private readonly ConcurrentDictionary<string, IMessageSender> m_MessageSenders = [];
	private bool m_DisposedValue;

	public RabbitGlobMessageExchange(
		string registerName,
		string pattern,
		string exchangeName,
		RabbitMQSenderOptions? senderOptions,
		IRabbitMQSerializerRegistry? serializerRegistry)
	{
		ArgumentException.ThrowIfNullOrEmpty(exchangeName, nameof(exchangeName));

		m_Glob = Glob.Parse(pattern);
		m_RegisterName = registerName;
		m_ExchangeName = exchangeName;
		m_SenderOptions = senderOptions ?? new RabbitMQSenderOptions();
		m_SerializerRegistry = serializerRegistry;
	}

	public async ValueTask<IMessageSender> GetMessageSenderAsync(
		string subject,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(m_DisposedValue, nameof(RabbitGlobMessageExchange));

		var connectionManager = serviceProvider.GetRequiredKeyedService<RabbitMQConnectionManager>(m_RegisterName);
		var connection = await connectionManager.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
		var serializerRegistry = m_SerializerRegistry
			?? serviceProvider.GetRequiredKeyedService<IRabbitMQSerializerRegistry>(m_RegisterName);
		var messageSenderFactory = serviceProvider.GetRequiredService<IMessageSenderFactory>();

		if (m_MessageSenders.TryGetValue(subject, out var messageSender))
			return messageSender;
		else
		{
			var options = m_SenderOptions.Clone();
			options.AppId ??= connectionManager.AppId;

			var sender = messageSenderFactory.CreateMessageSender(
					serviceProvider,
					m_ExchangeName,
					await connection.CreateChannelAsync(
						m_SenderOptions.CreateChannelOptions,
						cancellationToken).ConfigureAwait(false),
					serializerRegistry,
					options);

			if (!m_MessageSenders.TryAdd(subject, sender))
			{
				if (sender is IAsyncDisposable asyncDisposable)
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				else if (sender is IDisposable disposable)
					disposable.Dispose();

				return m_MessageSenders.GetValueOrDefault(subject)
					?? throw new InvalidOperationException($"Failed to get message sender for subject '{subject}'");
			}

			return sender;
		}
	}

	public ValueTask<bool> MatchAsync(
		string subject,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(m_Glob.IsMatch(subject));
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		var allSenders = m_MessageSenders.Values.ToArray();
		m_MessageSenders.Clear();

		foreach (var messageSender in allSenders)
		{
			if (messageSender is IAsyncDisposable asyncDisposable)
				await asyncDisposable.DisposeAsync().ConfigureAwait(false);
			else if (messageSender is IDisposable disposable)
				disposable.Dispose();
		}

		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!m_DisposedValue)
		{
			if (disposing)
			{
				var allSenders = m_MessageSenders.Values.ToArray();
				m_MessageSenders.Clear();

				foreach (var messageSender in allSenders)
					if (messageSender is IDisposable disposable)
						disposable.Dispose();
			}

			m_DisposedValue = true;
		}
	}
}