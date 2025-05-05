using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Adaptare.RabbitMQ;

record RabbitSubscriptionSettings
{
	public required string Subject { get; init; }

	public bool AutoAck { get; init; } = true;

	public CreateChannelOptions? CreateChannelOptions { get; init; }

	public required Func<IChannel, BasicDeliverEventArgs, Task> EventHandler { get; init; }
}