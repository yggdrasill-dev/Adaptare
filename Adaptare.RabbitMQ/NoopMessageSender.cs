namespace Adaptare.RabbitMQ;

internal class NoopMessageSender : IMessageSender
{
	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> ValueTask.CompletedTask;

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> ValueTask.FromResult(default(TReply)!);

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken)
		=> ValueTask.CompletedTask;
}