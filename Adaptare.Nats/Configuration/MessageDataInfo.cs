using Microsoft.Extensions.Logging;

namespace Adaptare.Nats.Configuration;

internal record MessageDataInfo<TNatsMsg>(
	TNatsMsg Msg,
	ILogger Logger,
	IServiceProvider ServiceProvider);