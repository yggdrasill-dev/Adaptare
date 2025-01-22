namespace Adaptare;

public class MessageSenderNotFoundException(string subject)
	: Exception($"The subject({subject}) can't find sender.")
{
	public string Subject { get; } = subject;
}