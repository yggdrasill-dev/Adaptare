using System.Diagnostics;
using Adaptare.RabbitMQ.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Adaptare.RabbitMQ;

internal class RabbitMQConnectionManager(
	RabbitMQConnectionOptions rabbitMQConnectionOptions,
	IServiceProvider serviceProvider,
	ILogger<RabbitMQConnectionManager> logger)
	: IMessageReceiver<RabbitSubscriptionSettings>
	, IDisposable
	, IAsyncDisposable
	, IRabbitMQConnectionManager
{
	internal static readonly ActivitySource _RabbitMQActivitySource = new("Adaptare.MessageQueue.RabbitMQ");

	private readonly TaskCompletionSource m_InitializationCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private bool m_DisposedValue;
	private IChannel? m_DeclareChannel;

	public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
	{
		await m_InitializationCompletionSource.Task.ConfigureAwait(false);

		return await rabbitMQConnectionOptions.ConnectionPromise.ConfigureAwait(false);
	}

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		var connection = await rabbitMQConnectionOptions.ConnectionPromise.ConfigureAwait(false);
		rabbitMQConnectionOptions.ConfigureConnection(connection);

		m_DeclareChannel = await connection.CreateChannelAsync(
			cancellationToken: cancellationToken).ConfigureAwait(false);

		await rabbitMQConnectionOptions.SetupQueueAndExchange(m_DeclareChannel, cancellationToken).ConfigureAwait(false);

		m_InitializationCompletionSource.SetResult();
	}

	public async Task StopAsync(CancellationToken cancellationToken = default)
	{
		if (m_DeclareChannel is not null)
		{
			await m_DeclareChannel.CloseAsync(cancellationToken).ConfigureAwait(false);
			await m_DeclareChannel.DisposeAsync().ConfigureAwait(false);
			m_DeclareChannel = null;
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
		var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
		var channel = await connection.CreateChannelAsync(
			settings.CreateChannelOptions,
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
	}
}