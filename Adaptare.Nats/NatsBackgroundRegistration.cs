using Adaptare.Nats.Configuration.Registrations;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream.Models;

namespace Adaptare.Nats;

internal class NatsBackgroundRegistration(
	INatsMessageQueueService natsMessageQueueService,
	IEnumerable<ISubscribeRegistration> subscribes,
	IEnumerable<StreamConfig> streamRegistrations,
	IServiceProvider serviceProvider,
	ILogger<NatsBackgroundRegistration> logger)
	: IMessageQueueBackgroundRegistration
{
	public async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var subscriptions = new List<IDisposable>();

		foreach (var config in streamRegistrations)
			await natsMessageQueueService.RegisterStreamAsync(
				config,
				cancellationToken).ConfigureAwait(false);

		foreach (var registration in subscribes)
		{
			var subsctiption = await registration.SubscribeAsync(
				natsMessageQueueService,
				serviceProvider,
				logger,
				cancellationToken).ConfigureAwait(false);

			if (subsctiption is not null)
				subscriptions.Add(subsctiption);
		}

		logger.LogDebug("Nats subscribe completed.");

		await cancellationToken.ConfigureAwait(false);

		foreach (var sub in subscriptions)
			sub.Dispose();
	}
}