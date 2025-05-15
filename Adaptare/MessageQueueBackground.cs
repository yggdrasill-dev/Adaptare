using Microsoft.Extensions.Hosting;

namespace Adaptare;

internal class MessageQueueBackground(
	IEnumerable<IMessageQueueBackgroundRegistration> backgroundRegistrations)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var registrationTasks = backgroundRegistrations
			.Select(registration => registration.ExecuteAsync(stoppingToken))
			.ToArray();

		await Task.WhenAll(registrationTasks).ConfigureAwait(false);
	}
}