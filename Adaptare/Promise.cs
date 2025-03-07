﻿namespace Adaptare;

class Promise<TPromiseReply>(TaskCompletionSource<Answer<TPromiseReply>> completionSource) : IPromise
{
	private readonly TaskCompletionSource<Answer<TPromiseReply>> m_CompletionSource = completionSource ?? throw new ArgumentNullException(nameof(completionSource));

	public void Cancel()
		=> _ = m_CompletionSource.TrySetCanceled();

	public void ThrowException(Exception ex)
		=> _ = m_CompletionSource.TrySetException(ex);

	public void SetResult<TReply>(Answer<TReply> answer)
		=> _ = m_CompletionSource.TrySetResult((Answer<TPromiseReply>)(object)answer);
}