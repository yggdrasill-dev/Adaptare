using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Adaptare.RabbitMQ;

internal sealed class RabbitSubscription : IDisposable, IAsyncDisposable
{
	private readonly IChannel m_Channel;
	private readonly string m_ConsumerTag;
	private readonly ILogger<RabbitSubscription> m_Logger;
	private readonly string m_Subject;
	private bool m_DisposedValue;

	public RabbitSubscription(
		string subject,
		IChannel channel,
		string consumerTag,
		ILogger<RabbitSubscription> logger)
	{
		m_Subject = subject;
		m_Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		m_ConsumerTag = consumerTag;
		m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

		m_Channel.CallbackExceptionAsync += Connection_CallbackExceptionAsync;
		m_Channel.ChannelShutdownAsync += Connection_ChannelShutdownAsync;
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'DisposeAsyncCore' 方法
		await CoreDisposeAsync().ConfigureAwait(false);

		// 將此行放入終結器的 'Dispose(bool disposing)' 方法中
		Dispose(disposing: false);

		// 將下列行程式碼放入 'Dispose(bool disposing)' 方法的結尾
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!m_DisposedValue)
		{
			if (disposing)
			{
				if (m_Channel is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}

			m_DisposedValue = true;
		}
	}

	private Task Connection_ChannelShutdownAsync(object sender, ShutdownEventArgs args)
	{
		m_Logger.LogWarning(
			"Receiver({Subject}) ChannelShutdownEvent: {ReplyCode} {ReplyText}",
			m_Subject,
			args.ReplyCode,
			args.ReplyText);

		return Task.CompletedTask;
	}

	private Task Connection_CallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
	{
		m_Logger.LogError(e.Exception, "Receiver({Subject}) CallbackExceptionEvent", m_Subject);

		return Task.CompletedTask;
	}

	private async ValueTask CoreDisposeAsync()
	{
		if (m_Channel is not null)
		{
			await m_Channel.BasicCancelAsync(m_ConsumerTag).ConfigureAwait(false);
			await m_Channel.DisposeAsync().ConfigureAwait(false);
		}
	}
}