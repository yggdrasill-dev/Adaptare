using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.Exchanges;

internal class RabbitGlobMessageExchange
	: IMessageExchange
	, IDisposable
	, IAsyncDisposable
{
	private readonly string m_RegisterName;
	private readonly string m_ExchangeName;
	private readonly CreateChannelOptions? m_CreateChannelOptions;
	private readonly IRabbitMQSerializerRegistry? m_RabbitMQSerializerRegistry;
	private readonly Glob m_Glob;
	private readonly Dictionary<string, IMessageSender> m_MessageSenders = [];
	private readonly SemaphoreSlim m_DictionaryLock = new(1, 1);
	private bool m_DisposedValue;

	public RabbitGlobMessageExchange(
		string registerName,
		string pattern,
		string exchangeName,
		CreateChannelOptions? createChannelOptions,
		IRabbitMQSerializerRegistry? rabbitMQSerializerRegistry)
	{
		ArgumentException.ThrowIfNullOrEmpty(exchangeName, nameof(exchangeName));

		m_Glob = Glob.Parse(pattern);
		m_RegisterName = registerName;
		m_ExchangeName = exchangeName;
		m_CreateChannelOptions = createChannelOptions;
		m_RabbitMQSerializerRegistry = rabbitMQSerializerRegistry;
	}

	public async ValueTask<IMessageSender> GetMessageSenderAsync(
		string subject,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(m_DisposedValue, nameof(RabbitGlobMessageExchange));

		var connectionManager = serviceProvider.GetRequiredKeyedService<RabbitMQConnectionManager>(m_RegisterName);
		var connection = await connectionManager.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
		var serializerRegistry = m_RabbitMQSerializerRegistry
			?? serviceProvider.GetRequiredKeyedService<IRabbitMQSerializerRegistry>(m_RegisterName);
		var messageSenderFactory = serviceProvider.GetRequiredService<IMessageSenderFactory>();

		if (m_MessageSenders.TryGetValue(subject, out var messageSender))
			return messageSender;
		else
		{
			await m_DictionaryLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (m_MessageSenders.TryGetValue(subject, out messageSender))
					return messageSender;

				var sender = messageSenderFactory.CreateMessageSender(
					serviceProvider,
					m_ExchangeName,
					await connection.CreateChannelAsync(m_CreateChannelOptions, cancellationToken).ConfigureAwait(false),
					serializerRegistry);

				m_MessageSenders[subject] = sender;

				return sender;
			}
			finally
			{
				m_DictionaryLock.Release();
			}
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