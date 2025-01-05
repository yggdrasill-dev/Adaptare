using Adaptare.RabbitMQ.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adaptare.RabbitMQ;

internal class MessageQueueBackground : BackgroundService
{
	private readonly IMessageReceiver<RabbitSubscriptionSettings> m_MessageReceiver;
	private readonly IServiceProvider m_ServiceProvider;
	private readonly IEnumerable<ISubscribeRegistration> m_Subscribes;
	private readonly RabbitMQConnectionManager m_RabbitMQConnectionManager;
	private readonly ILogger<MessageQueueBackground> m_Logger;

	public MessageQueueBackground(
		IMessageReceiver<RabbitSubscriptionSettings> messageReceiver,
		IServiceProvider serviceProvider,
		IEnumerable<ISubscribeRegistration> subscribes,
		RabbitMQConnectionManager rabbitMQConnectionManager,
		ILogger<MessageQueueBackground> logger)
	{
		m_MessageReceiver = messageReceiver;
		m_ServiceProvider = serviceProvider;
		m_Subscribes = subscribes;
		m_RabbitMQConnectionManager = rabbitMQConnectionManager;
		m_Logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await m_RabbitMQConnectionManager.StartAsync(
			stoppingToken).ConfigureAwait(false);

		var subscriptions = new List<IDisposable>();

		foreach (var registration in m_Subscribes)
			subscriptions.Add(
				await registration.SubscribeAsync(
					m_MessageReceiver,
					m_ServiceProvider,
					m_Logger,
					stoppingToken).ConfigureAwait(false));

		_ = stoppingToken.Register(() =>
		{
			m_RabbitMQConnectionManager.StopAsync().Wait();

			foreach (var sub in subscriptions)
				sub.Dispose();
		});
	}
}