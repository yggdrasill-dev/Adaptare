namespace Adaptare;

public class MessageProcessFailException(string? responseData)
	: Exception("MessageQueue remote process fail.")
{
	public MessageProcessFailException()
		: this(null)
	{
	}

	public string? ResponseData { get; } = responseData;
}