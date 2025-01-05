using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;

public class RabbitMQOptions
{
	public Action<IChannel> OnSetupQueueAndExchange { get; set; } = _ => { };

	public Func<ConnectionFactory> BuildConnectionFactory { get; set; } = () => new ConnectionFactory();
}