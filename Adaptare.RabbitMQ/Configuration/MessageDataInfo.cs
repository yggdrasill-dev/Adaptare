using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Adaptare.RabbitMQ.Configuration;

internal class MessageDataInfo
{
	public required BasicDeliverEventArgs Args { get; init; }

	public required CancellationToken CancellationToken { get; init; }

	public required IChannel Channel { get; init; }

	public required ILogger Logger { get; init; }

	public required IServiceProvider ServiceProvider { get; init; }
}