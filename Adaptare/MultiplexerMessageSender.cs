using System.Diagnostics;

namespace Adaptare;

internal class MultiplexerMessageSender(
	IServiceProvider serviceProvider,
	IEnumerable<IMessageExchange> exchanges)
	: IMessageSender
	, IDisposable
	, IAsyncDisposable
{
	private static readonly ActivitySource _SenderActivitySource = new($"Adaptare.MessageQueue.{nameof(MultiplexerMessageSender)}");

	private bool m_DisposedValue;

	public async ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(PublishAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = await GetMessageSenderAsync(subject, header, cancellationToken).ConfigureAwait(false);

		cancellationToken.ThrowIfCancellationRequested();

		await sender.PublishAsync(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(RequestAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = await GetMessageSenderAsync(subject, header, cancellationToken).ConfigureAwait(false);

		cancellationToken.ThrowIfCancellationRequested();

		return await sender.RequestAsync<TMessage, TReply>(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(SendAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = await GetMessageSenderAsync(subject, header, cancellationToken).ConfigureAwait(false);

		cancellationToken.ThrowIfCancellationRequested();

		await sender.SendAsync(subject, data, header, cancellationToken).ConfigureAwait(false);
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		foreach (var exchange in exchanges)
		{
			if (exchange is IAsyncDisposable asyncDisposable)
				await asyncDisposable.DisposeAsync().ConfigureAwait(false);
			else if (exchange is IDisposable disposable)
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
				foreach (var exchange in exchanges)
				{
					if (exchange is IDisposable disposable)
						disposable.Dispose();
				}
			}

			m_DisposedValue = true;
		}
	}

	private async Task<IMessageSender> GetMessageSenderAsync(
		string subject,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(m_DisposedValue, nameof(MultiplexerMessageSender));

		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(GetMessageSenderAsync)}");
		_ = (activity?.AddTag("subject", subject));

		foreach (var exchange in exchanges)
			if (await exchange.MatchAsync(subject, header, cancellationToken).ConfigureAwait(false))
				return await exchange.GetMessageSenderAsync(
					subject,
					serviceProvider,
					cancellationToken).ConfigureAwait(false);

		throw new MessageSenderNotFoundException(subject);
	}
}