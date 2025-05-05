using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.Configuration;

public class RabbitMQConnectionOptions
{
	public required TaskResource<IConnection> ConnectionPromise { get; set; }
	public Func<IChannel, CancellationToken, Task> SetupQueueAndExchange { get; set; } = (_, _) => Task.CompletedTask;
	public Action<IConnection> ConfigureConnection { get; set; } = _ => { };
}