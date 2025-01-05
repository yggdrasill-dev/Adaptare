using Microsoft.Extensions.Logging;
using Adaptare.RabbitMQ;

namespace Adaptare.RabbitMQ.Configuration;

internal interface ISubscribeRegistration
{
	ValueTask<IDisposable> SubscribeAsync(
		IMessageReceiver<RabbitSubscriptionSettings> messageReceiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken);
}