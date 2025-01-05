namespace Adaptare;

public class MessageSenderNotFoundException : Exception
{
	public MessageSenderNotFoundException(string subject)
		: base($"The subject({subject}) can't find sender.")
	{
		Subject = subject;
	}

	public string Subject { get; }
}