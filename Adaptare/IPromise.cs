﻿namespace Adaptare;

internal interface IPromise
{
	void Cancel();

	void SetResult<TReply>(Answer<TReply> answer);

	void ThrowException(Exception ex);
}