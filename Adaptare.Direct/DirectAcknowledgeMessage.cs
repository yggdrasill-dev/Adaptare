using Adaptare.Direct.Configuration;

namespace Adaptare.Direct;

public readonly struct DirectAcknowledgeMessage<TMessage>(
	string subject,
	TMessage message,
	IEnumerable<MessageHeaderValue>? headerValues,
	DirectAcknowledgeOptions acknowledgeOptions)
	: IAcknowledgeMessage<TMessage>
{
	public string Subject => subject;
	public TMessage? Data => message;
	public IEnumerable<MessageHeaderValue>? HeaderValues => headerValues;

	public ValueTask AckAsync(CancellationToken cancellationToken = default)
	{
		var messageId = HeaderValues?.FirstOrDefault(h => h.Name == DirectMessageDefaults.MessageIdKeyName)
			?.Value;

		var response = new AcknowledgeResponse(AcknowledgeType.Ack, null);

		if (!string.IsNullOrWhiteSpace(messageId))
			response = response with
			{
				MessageId = Guid.Parse(messageId)
			};

		acknowledgeOptions.AcknowledgeCallback?.Invoke(response);

		return ValueTask.CompletedTask;
	}

	public ValueTask AckProgressAsync(CancellationToken cancellationToken = default)
	{
		var messageId = HeaderValues?.FirstOrDefault(h => h.Name == DirectMessageDefaults.MessageIdKeyName)
			?.Value;

		var response = new AcknowledgeResponse(AcknowledgeType.Progress, null);

		if (!string.IsNullOrWhiteSpace(messageId))
			response = response with
			{
				MessageId = Guid.Parse(messageId)
			};

		acknowledgeOptions.AcknowledgeCallback?.Invoke(response);

		return ValueTask.CompletedTask;
	}

	public ValueTask AckTerminateAsync(CancellationToken cancellationToken = default)
	{
		var messageId = HeaderValues?.FirstOrDefault(h => h.Name == DirectMessageDefaults.MessageIdKeyName)
			?.Value;

		var response = new AcknowledgeResponse(AcknowledgeType.Terminate, null);

		if (!string.IsNullOrWhiteSpace(messageId))
			response = response with
			{
				MessageId = Guid.Parse(messageId)
			};

		acknowledgeOptions.AcknowledgeCallback?.Invoke(response);

		return ValueTask.CompletedTask;
	}

	public ValueTask NakAsync(TimeSpan delay = default, CancellationToken cancellationToken = default)
	{
		var messageId = HeaderValues?.FirstOrDefault(h => h.Name == DirectMessageDefaults.MessageIdKeyName)
			?.Value;

		var response = new AcknowledgeResponse(AcknowledgeType.NAck, null);

		if (!string.IsNullOrWhiteSpace(messageId))
			response = response with
			{
				MessageId = Guid.Parse(messageId)
			};

		acknowledgeOptions.AcknowledgeCallback?.Invoke(response);

		return ValueTask.CompletedTask;
	}
}