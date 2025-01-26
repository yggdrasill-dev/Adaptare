using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Adaptare.RabbitMQ;

internal class RabbitMQConnectionManager(
	ConnectionFactory connectionFactory,
	Action<IChannel> setupQueueAndExchange,
	IServiceProvider serviceProvider,
	ILogger<RabbitMQConnectionManager> logger)
	: IDisposable
	, IAsyncDisposable
	, IMessageReceiver<RabbitSubscriptionSettings>
{
	internal static readonly ActivitySource _RabbitMQActivitySource = new("Adaptare.MessageQueue.RabbitMQ");

	private readonly TaskCompletionSource<IConnection> m_ConnectionCompletionSource = new();
	private IConnection? m_Connection;
	private bool m_DisposedValue;
	private IChannel? m_DeclareChannel;

	public Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
		=> m_ConnectionCompletionSource.Task.WaitAsync(cancellationToken);

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		m_Connection = await connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
		m_Connection.CallbackExceptionAsync += Connection_CallbackExceptionAsync;
		m_Connection.ConnectionBlockedAsync += Connection_ConnectionBlockedAsync;
		m_Connection.ConnectionShutdownAsync += Connection_ConnectionShutdownAsync;
		m_Connection.ConnectionUnblockedAsync += Connection_ConnectionUnblockedAsync;

		m_DeclareChannel = await m_Connection.CreateChannelAsync(
			cancellationToken: cancellationToken).ConfigureAwait(false);

		m_DeclareChannel.CallbackExceptionAsync += MessageQueueChannel_CallbackExceptionAsync;
		m_DeclareChannel.ChannelShutdownAsync += MessageQueueChannel_ModelShutdownAsync;

		setupQueueAndExchange(m_DeclareChannel);
	}

	public async Task StopAsync(CancellationToken cancellationToken = default)
	{
		if (m_DeclareChannel is not null)
		{
			await m_DeclareChannel.CloseAsync(cancellationToken).ConfigureAwait(false);
			await m_DeclareChannel.DisposeAsync().ConfigureAwait(false);
			m_DeclareChannel = null;
		}

		if (m_Connection is not null)
		{
			await m_Connection.CloseAsync(cancellationToken).ConfigureAwait(false);
			await m_Connection.DisposeAsync().ConfigureAwait(false);
			m_Connection = null;
		}
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);

		GC.SuppressFinalize(this);
	}

	public async ValueTask<IDisposable> SubscribeAsync(RabbitSubscriptionSettings settings, CancellationToken cancellationToken = default)
	{
		connectionFactory.ConsumerDispatchConcurrency = settings.ConsumerDispatchConcurrency;

		var connection = await connectionFactory.CreateConnectionAsync(
			cancellationToken).ConfigureAwait(false);
		var channel = await connection.CreateChannelAsync(
			cancellationToken: cancellationToken).ConfigureAwait(false);
		var consumer = new AsyncEventingBasicConsumer(channel);
		consumer.ReceivedAsync += (sender, args) => settings.EventHandler(channel, args);

		var consumerTag = await channel.BasicConsumeAsync(
			queue: settings.Subject,
			autoAck: settings.AutoAck,
			consumer: consumer,
			cancellationToken).ConfigureAwait(false);

		return new RabbitSubscription(
			settings.Subject,
			connection,
			channel,
			consumerTag,
			serviceProvider.GetRequiredService<ILogger<RabbitSubscription>>());
	}

	public async ValueTask DisposeAsync()
	{
		await CoreDisposeAsync().ConfigureAwait(false);

		Dispose(disposing: false);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!m_DisposedValue)
		{
			if (disposing)
			{
				logger.LogWarning($"{nameof(RabbitMQConnectionManager)} disposing.");

				if (m_DeclareChannel is IDisposable disposableChannel)
				{
					disposableChannel.Dispose();
					m_DeclareChannel = null;
				}

				if (m_Connection is IDisposable disposableConnection)
				{
					disposableConnection.Dispose();
					m_Connection = null;
				}
			}

			m_DisposedValue = true;
		}
	}

	protected async ValueTask CoreDisposeAsync()
	{
		if (m_DeclareChannel is not null)
		{
			await m_DeclareChannel.CloseAsync().ConfigureAwait(false);
			await m_DeclareChannel.DisposeAsync().ConfigureAwait(false);
		}

		m_DeclareChannel = null;

		if (m_Connection is not null)
		{
			await m_Connection.CloseAsync().ConfigureAwait(false);
			await m_Connection.DisposeAsync().ConfigureAwait(false);
		}

		m_Connection = null;
	}

	private Task MessageQueueChannel_CallbackExceptionAsync(object sender, CallbackExceptionEventArgs args)
	{
		logger.LogError(args.Exception, "MessageQueueChannel CallbackExceptionEvent");
		return Task.CompletedTask;
	}

	private Task Connection_CallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
	{
		logger.LogError(e.Exception, "CallbackExceptionEvent");
		return Task.CompletedTask;
	}

	private Task Connection_ConnectionBlockedAsync(object? sender, ConnectionBlockedEventArgs e)
	{
		logger.LogInformation("ConnectionBlockedEvent, Reason: {reason}", e.Reason);
		return Task.CompletedTask;
	}

	private Task Connection_ConnectionShutdownAsync(object? sender, ShutdownEventArgs e)
	{
		logger.LogInformation("ConnectionShutdownEvent");
		return Task.CompletedTask;
	}

	private Task Connection_ConnectionUnblockedAsync(object? sender, AsyncEventArgs e)
	{
		logger.LogInformation("ConnectionUnblockedEvent");
		return Task.CompletedTask;
	}

	private Task MessageQueueChannel_ModelShutdownAsync(object? sender, ShutdownEventArgs e)
	{
		logger.LogInformation("Declare ChannelShutdownEvent");
		return Task.CompletedTask;
	}
}