using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.Configuration;

public class RabbitMQConnectionOptions
{
	public string? AppId { get; set; }
	public required TaskResource<IConnection> ConnectionPromise { get; set; }
	public Func<IChannel, CancellationToken, Task> SetupQueueAndExchange { get; set; } = (_, _) => Task.CompletedTask;
}