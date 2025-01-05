namespace Adaptare;

public class MessageProcessFailException : Exception
{
	public MessageProcessFailException()
		: this(null)
	{
	}

	public MessageProcessFailException(string? responseData)
		: base("MessageQueue remote process fail.")
	{
		ResponseData = responseData;
	}

	public string? ResponseData { get; }
}