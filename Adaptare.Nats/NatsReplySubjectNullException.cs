namespace Adaptare.Nats;

public class NatsReplySubjectNullException : Exception
{
	public NatsReplySubjectNullException() : base("Reply subject can't be null or empty, maybe use fault method.")
	{
	}
}