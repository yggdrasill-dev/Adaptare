﻿using Microsoft.Extensions.Logging;

namespace Adaptare.Nats.Configuration;

internal interface ISubscribeRegistration
{
	ValueTask<IDisposable?> SubscribeAsync(
		IMessageReceiver<INatsSubscribe> receiver,
		IServiceProvider serviceProvider,
		ILogger logger,
		CancellationToken cancellationToken);
}