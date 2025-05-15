using Adaptare.RabbitMQ.Configuration;
using Microsoft.Extensions.Logging;

namespace Adaptare.RabbitMQ;

internal class RabbitMQBackgroundRegistration(
	IEnumerable<ISubscribeRegistration> subscribes,
	RabbitMQConnectionManager rabbitMQConnectionManager,
	IServiceProvider serviceProvider,
	ILogger<RabbitMQBackgroundRegistration> logger)
	: IMessageQueueBackgroundRegistration
{
	public async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		await rabbitMQConnectionManager.StartAsync(
			cancellationToken).ConfigureAwait(false);

		var subscriptions = new List<IDisposable>();

		foreach (var registration in subscribes)
			subscriptions.Add(
				await registration.SubscribeAsync(
					rabbitMQConnectionManager,
					serviceProvider,
					logger,
					cancellationToken).ConfigureAwait(false));

		_ = cancellationToken.Register(() =>
		{
			rabbitMQConnectionManager.StopAsync().Wait();

			foreach (var sub in subscriptions)
				sub.Dispose();
		});
	}
}