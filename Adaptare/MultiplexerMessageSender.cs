using System.Diagnostics;

namespace Adaptare;

internal class MultiplexerMessageSender(
	IServiceProvider serviceProvider,
	IEnumerable<IMessageExchange> exchanges)
	: IMessageSender
{
	private static readonly ActivitySource _SenderActivitySource = new($"Adaptare.MessageQueue.{nameof(MultiplexerMessageSender)}");

	private readonly IMessageExchange[] m_Exchanges = (exchanges ?? throw new ArgumentNullException(nameof(exchanges))).ToArray();
	private readonly IServiceProvider m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

	public async ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(AskAsync)}");
		_ = (activity?.AddTag("subject", subject));

		var sender = await GetMessageSenderAsync(subject, header, cancellationToken).ConfigureAwait(false);

		cancellationToken.ThrowIfCancellationRequested();

		return await sender.AskAsync<TMessage, TReply>(
			subject,
			data,
			header,
			cancellationToken).ConfigureAwait(false);
	}

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

	private async Task<IMessageSender> GetMessageSenderAsync(
		string subject,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var activity = _SenderActivitySource.StartActivity($"{nameof(MultiplexerMessageSender)}.{nameof(GetMessageSenderAsync)}");
		_ = (activity?.AddTag("subject", subject));

		foreach (var exchange in m_Exchanges)
			if (await exchange.MatchAsync(subject, header, cancellationToken).ConfigureAwait(false))
				return await exchange.GetMessageSenderAsync(
					subject,
					m_ServiceProvider,
					cancellationToken).ConfigureAwait(false);

		throw new MessageSenderNotFoundException(subject);
	}
}