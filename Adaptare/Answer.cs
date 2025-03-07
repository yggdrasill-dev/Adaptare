﻿namespace Adaptare;

public abstract record Answer<TAnswer>
{
	public abstract bool CanResponse { get; }
	public TAnswer Result { get; protected set; } = default!;
	public IEnumerable<MessageHeaderValue>? HeaderValues { get; protected set; }

	public abstract ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default);

	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		TMessage data,
		CancellationToken cancellationToken = default)
		=> AskAsync<TMessage, TReply>(data, [], cancellationToken);

	public abstract ValueTask CompleteAsync<TReply>(TReply data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default);

	public ValueTask CompleteAsync<TReply>(TReply data, CancellationToken cancellationToken = default)
		=> CompleteAsync(data, [], cancellationToken);

	public abstract ValueTask CompleteAsync(IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default);

	public ValueTask CompleteAsync(CancellationToken cancellationToken = default)
		=> CompleteAsync(cancellationToken);

	public abstract ValueTask FailAsync(string data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default);

	public ValueTask FailAsync(string data, CancellationToken cancellationToken = default)
		=> FailAsync(data, [], cancellationToken);
}