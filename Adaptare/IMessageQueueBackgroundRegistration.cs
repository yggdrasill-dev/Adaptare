namespace Adaptare;

public interface IMessageQueueBackgroundRegistration
{
	Task ExecuteAsync(CancellationToken cancellationToken = default);
}